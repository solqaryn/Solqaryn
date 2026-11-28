using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.API.Controllers;

public sealed partial class TiendaCuentaController
{
    private const string PedidoKeyHeader = "X-Storefront-Order-Key";

    [HttpPost("pedidos/checkout")]
    [EnableRateLimiting("AuthLogin")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> CrearPedidoCheckout(
        [FromBody] CrearPedidoCheckoutDto dto,
        [FromServices] ICheckoutTiendaService checkout,
        CancellationToken ct)
    {
        if (_db.PublicStoreCheckoutEmpresaId is not int empresa)
            return StatusCode(403, ApiResponse<object>.Fail("No se pudo verificar la empresa de la tienda."));
        var key = ObtenerClavePedido();
        if (key is null)
            return BadRequest(ApiResponse<object>.Fail("Referencia de solicitud inválida."));
        var cuenta = await RequerirCuentaAsync();
        if (Request.Headers.ContainsKey(SessionHeader) && cuenta is null)
            return NoAutorizado();
        var nombre = Limpiar(dto?.Nombre);
        var telefono = Limpiar(dto?.Telefono);
        var correo = NormalizarCorreo(dto?.Correo);
        var notas = Limpiar(dto?.Notas);
        if (dto?.Checkout?.Items is not { Count: > 0 and <= 50 } items ||
            items.Any(x => x is null) || dto.LineasEsperadas is not { Count: > 0 and <= 50 } esperadas ||
            esperadas.Any(x => x is null) ||
            nombre is null || nombre.Length is < 2 or > 120 ||
            telefono is null || !Regex.IsMatch(telefono, @"^\+?[0-9 ()-]{8,24}$") ||
            (!string.IsNullOrWhiteSpace(dto.Correo) && correo is null) ||
            (notas?.Length ?? 0) > 600 ||
            dto.SubtotalEsperado is null or < 0 || dto.EnvioEsperado is null or < 0 ||
            !Regex.IsMatch(dto.MonedaEsperada ?? "", "^[A-Z]{3}$"))
            return BadRequest(ApiResponse<object>.Fail("Revisa los datos y el resumen del pedido."));

        // The key is a 256-bit capability for a guest receipt; only its scoped
        // digest is ever persisted. Account orders additionally bind to ClienteId.
        var clienteId = cuenta?.ClienteId;
        var scopedKey = ClavePedido(key, clienteId);
        var fingerprint = HashPedido(JsonSerializer.Serialize(dto));
        async Task<PedidoVenta?> BuscarAsync() => await _db.Set<PedidoVenta>().AsNoTracking()
            .Where(x => EF.Property<int?>(x, "EmpresaId") == empresa &&
                x.IdempotencyKey == scopedKey && x.CostoEnvio != null &&
                (clienteId.HasValue ? x.ClienteId == clienteId.Value && x.CreadoPorNombreUsuario == "storefront-cuenta" :
                    x.CreadoPorNombreUsuario == "storefront-invitado"))
            .Include(x => x.Detalles).AsSplitQuery().SingleOrDefaultAsync(ct);

        IActionResult Replay(PedidoVenta pedido) =>
            pedido.IdempotencyFingerprint == fingerprint
                ? Ok(ApiResponse<TiendaPedidoCuentaDto>.Ok(MapPedido(pedido), "Pedido ya registrado."))
                : Conflict(ApiResponse<object>.Fail("La referencia ya se utilizó con otros datos."));

        var previo = await BuscarAsync();
        if (previo is not null) return Replay(previo);

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var cotizado = await checkout.ValidarAsync(dto.Checkout);
            var datos = cotizado.Datos;
            if (datos is null)
                return cotizado.EsConflicto ? Conflict(ApiResponse<object>.Fail(cotizado.Mensaje)) :
                    BadRequest(ApiResponse<object>.Fail(cotizado.Mensaje));
            var moneda = await _db.EmpresaConfiguraciones.AsNoTracking().Where(x => x.Activa)
                .Select(x => x.Moneda).SingleOrDefaultAsync(ct);
            if (moneda is null || moneda != dto.MonedaEsperada || datos.CostoEnvio is not decimal envio ||
                datos.Fiscal is null || string.IsNullOrEmpty(datos.HuellaFiscal) ||
                datos.HuellaFiscal != dto.HuellaFiscalEsperada || datos.Total != dto.TotalEsperado ||
                datos.ExpiraUtc <= DateTime.UtcNow || string.IsNullOrWhiteSpace(datos.TarifaEnvio) ||
                datos.Subtotal != dto.SubtotalEsperado || envio != dto.EnvioEsperado ||
                !datos.Lineas.Select(x => (x.ProductoId, x.ProductoVarianteId, x.Unidades, x.PrecioUnitario))
                    .OrderBy(x => x.ProductoId).ThenBy(x => x.ProductoVarianteId)
                    .SequenceEqual(esperadas.Select(x => (x.ProductoId, x.ProductoVarianteId, x.Unidades, x.PrecioUnitario))
                        .OrderBy(x => x.ProductoId).ThenBy(x => x.ProductoVarianteId)))
                return Conflict(ApiResponse<object>.Fail("Cambió el resumen del pedido. Actualiza precios, impuestos y envío antes de continuar."));

            // Public quote has no verified fiscal customer identity. An authenticated
            // exempt customer must not be silently charged the public tax rate.
            if (clienteId.HasValue)
            {
                var impuestosAplicados = datos.Fiscal.Impuestos.Select(i => i.ImpuestoId).ToArray();
                if (impuestosAplicados.Length > 0 && await _db.Impuestos.AsNoTracking().AnyAsync(i =>
                    impuestosAplicados.Contains(i.Id) && i.ClientesExentos.Any(c => c.ClienteId == clienteId.Value), ct))
                    return Conflict(ApiResponse<object>.Fail("Tu cuenta requiere una cotización fiscal personalizada. Contacta al comercio."));
            }

            Cliente cliente;
            if (clienteId.HasValue)
            {
                var existente = await _db.Clientes.SingleOrDefaultAsync(x =>
                    x.Id == clienteId.Value && x.Activo && EF.Property<int?>(x, "EmpresaId") == empresa, ct);
                if (existente is null) return NoAutorizado();
                cliente = existente;
                _db.AutorizarClienteCheckout(cliente, nuevo: false);
            }
            else
            {
                // Never search by an unverified email or attach a guest to an
                // existing customer/account. The guest is specific to this draft.
                cliente = new Cliente
                {
                    Nombre = nombre, Telefono = telefono, Correo = correo,
                    Activo = true, TipoClienteId = await _tipoClienteResolver.ResolverIdPredeterminadoAsync(),
                    CreadoPorNombreUsuario = "storefront-invitado"
                };
                _db.AutorizarClienteCheckout(cliente, nuevo: true);
                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync(ct);
            }

            var entrega = JsonSerializer.Serialize(new { Nombre = nombre, Telefono = telefono,
                Correo = correo, Cobertura = dto.Checkout.Envio });
            var pedido = PedidoVenta.CrearDesdeCheckout(cliente,
                datos.Lineas.Select(x => new LineaPedidoCheckout(x.ProductoId, x.ProductoVarianteId,
                    x.Nombre, x.Modelo, x.Sku, x.Unidades, x.PrecioUnitario)),
                envio, moneda, datos.TarifaEnvio, entrega, notas, clienteId is null, datos.Fiscal);
            pedido.EstablecerIdempotencia(scopedKey, fingerprint);
            _db.AutorizarPedidoCheckout(pedido);
            _db.Set<PedidoVenta>().Add(pedido);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return StatusCode(201, ApiResponse<TiendaPedidoCuentaDto>.Ok(MapPedido(pedido), "Pedido registrado, pendiente de confirmación comercial."));
        }
        catch (DbUpdateException)
        {
            // The tenant-first unique key resolves competing requests. The
            // failed transaction has been disposed/rolled back (including guest).
            _db.ChangeTracker.Clear();
            var existente = await BuscarAsync();
            if (existente is null) throw;
            return Replay(existente);
        }
    }

    [HttpGet("pedidos/checkout/{id:int}")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> PedidoCheckout(int id, CancellationToken ct)
    {
        if (_db.PublicStoreEmpresaId is not int empresa)
            return StatusCode(403, ApiResponse<object>.Fail("No se pudo verificar la empresa de la tienda."));
        var cuenta = await RequerirCuentaAsync();
        if (Request.Headers.ContainsKey(SessionHeader) && cuenta is null) return NoAutorizado();
        var key = ObtenerClavePedido();
        if (cuenta is null && key is null) return NoAutorizado();
        var scopedKey = key is null ? null : ClavePedido(key, null);
        var pedido = await _db.Set<PedidoVenta>().AsNoTracking().Where(x => x.Id == id &&
            EF.Property<int?>(x, "EmpresaId") == empresa && x.CostoEnvio != null &&
            (cuenta != null ? x.ClienteId == cuenta.ClienteId :
                x.CreadoPorNombreUsuario == "storefront-invitado" && x.IdempotencyKey == scopedKey))
            .Include(x => x.Detalles).AsSplitQuery().SingleOrDefaultAsync(ct);
        return pedido is null ? NotFound(ApiResponse<object>.Fail("Pedido no encontrado.")) :
            Ok(ApiResponse<TiendaPedidoCuentaDto>.Ok(MapPedido(pedido)));
    }

    private string? ObtenerClavePedido()
    {
        var values = Request.Headers[PedidoKeyHeader];
        var value = values.Count == 1 ? values[0] : null;
        return value is not null && Regex.IsMatch(value, "^[0-9a-f]{64}$") ? value : null;
    }

    private static string ClavePedido(string key, int? clienteId) =>
        "checkout-" + HashPedido((clienteId.HasValue ? "cuenta:" + clienteId.Value : "invitado") + ":" + key);

    private static string HashPedido(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
