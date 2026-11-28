using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.ValueObjects;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.Application.Services;

/// <summary>Validación compartida de catálogo, variante, stock, precios y envío.</summary>
public sealed class CheckoutTiendaService(
    IProductoService _productoService,
    IPromocionPublicaService _promocionPublicaService,
    IInventarioPublicoService _inventarioPublicoService,
    ICostoEnvioService _costoEnvioService,
    ICalculoService _calculoService) : ICheckoutTiendaService
{
    private const int MaxLineasCheckout = 50;
    private const int MaxUnidadesPorLinea = 999;
    private const int MaxLongitudIdentidadVariante = 200;
    private static readonly TimeSpan VigenciaValidacionCheckout = TimeSpan.FromMinutes(10);

    public async Task<CheckoutTiendaResultado> ValidarAsync(ValidarCheckoutTiendaDto? dto)
    {
        if (dto?.Items is null || dto.Items.Count == 0)
            return CheckoutTiendaResultado.Invalido("El carrito está vacío.");

        if (dto.Items.Count > MaxLineasCheckout)
            return CheckoutTiendaResultado.Invalido("El carrito supera el máximo de líneas permitido.");

        if (dto.Items.Any(item =>
                item.ProductoId <= 0
                || item.ProductoVarianteId is <= 0
                || item.Unidades <= 0
                || item.Unidades > MaxUnidadesPorLinea
                || (item.ModeloNombre?.Length ?? 0) > MaxLongitudIdentidadVariante
                || (item.MarcaNombre?.Length ?? 0) > MaxLongitudIdentidadVariante))
        {
            return CheckoutTiendaResultado.Invalido("El carrito contiene una referencia o cantidad no válida.");
        }

        var envio = dto.Envio;
        if ((envio?.Departamento?.Length ?? 0) > 120 || (envio?.Ciudad?.Length ?? 0) > 120 ||
            (envio?.Zona?.Length ?? 0) > 150 || (envio?.Modalidad?.Length ?? 0) > 80)
            return CheckoutTiendaResultado.Invalido("La cobertura de envío supera la longitud permitida.");

        var agrupadas = dto.Items
            .GroupBy(item => new
            {
                item.ProductoId,
                item.ProductoVarianteId,
                ModeloId = item.ProductoVarianteId.HasValue ? null : item.ModeloId,
                ModeloNombre = item.ProductoVarianteId.HasValue || string.IsNullOrEmpty(item.ModeloNombre) ? null : item.ModeloNombre,
                MarcaNombre = item.ProductoVarianteId.HasValue || string.IsNullOrEmpty(item.MarcaNombre) ? null : item.MarcaNombre
            })
            .Select(grupo => new CheckoutTiendaItemRequestDto
            {
                ProductoId = grupo.Key.ProductoId,
                ProductoVarianteId = grupo.Key.ProductoVarianteId,
                ModeloId = grupo.Key.ModeloId,
                ModeloNombre = grupo.Key.ModeloNombre,
                MarcaNombre = grupo.Key.MarcaNombre,
                Unidades = grupo.Sum(item => item.Unidades)
            })
            .ToList();

        if (agrupadas.Any(item => item.Unidades > MaxUnidadesPorLinea))
            return CheckoutTiendaResultado.Invalido("La cantidad acumulada de un producto supera el máximo permitido.");

        var lineas = new List<CheckoutTiendaLineaDto>(agrupadas.Count);
        var ahoraUtc = DateTime.UtcNow;

        foreach (var solicitud in agrupadas)
        {
            var producto = await _productoService.GetByIdAsync(solicitud.ProductoId);
            if (producto is null || !producto.Activo)
                return CheckoutTiendaResultado.Conflict("Uno de los productos ya no está disponible. Actualiza el carrito antes de continuar.");

            var variantesActivas = producto.Variantes.Where(variante => variante.Activo).ToList();
            int? productoVarianteId = null;
            int? modeloId = solicitud.ModeloId;
            int stock;
            decimal precio;
            string? modelo;
            string? sku;

            if (variantesActivas.Count > 0)
            {
                ProductoVarianteDto? varianteSeleccionada = null;

                if (solicitud.ProductoVarianteId.HasValue)
                {
                    varianteSeleccionada = variantesActivas.FirstOrDefault(variante =>
                        variante.Id == solicitud.ProductoVarianteId.Value
                        && variante.ProductoId == producto.Id);

                    if (varianteSeleccionada is null)
                        return CheckoutTiendaResultado.Conflict("La variante exacta seleccionada ya no está disponible. Actualiza el carrito antes de continuar.");
                }
                else
                {
                    var variantesModelo = variantesActivas
                        .Where(variante =>
                            variante.ModeloId == solicitud.ModeloId
                            && string.Equals(variante.ModeloNombre ?? string.Empty, solicitud.ModeloNombre ?? string.Empty, StringComparison.Ordinal)
                            && string.Equals(variante.MarcaNombre ?? string.Empty, solicitud.MarcaNombre ?? string.Empty, StringComparison.Ordinal))
                        .ToList();

                    if (variantesModelo.Count == 0)
                        return CheckoutTiendaResultado.Conflict("Una variante seleccionada ya no está disponible. Actualiza el carrito antes de continuar.");

                    if (variantesModelo.Count > 1)
                        return CheckoutTiendaResultado.Conflict("La selección corresponde a más de una variante física. Actualiza el carrito y selecciona una variante exacta.");

                    varianteSeleccionada = variantesModelo[0];
                }

                productoVarianteId = varianteSeleccionada.Id;
                modeloId = varianteSeleccionada.ModeloId;
                var inventarioVariante = await _inventarioPublicoService.ObtenerPorVariantesAsync(
                    new[] { varianteSeleccionada.Id });
                stock = inventarioVariante.TryGetValue(varianteSeleccionada.Id, out var resumenInventario)
                    && resumenInventario.TieneFuenteAutoritativa
                    ? Math.Max(0, resumenInventario.CantidadDisponible)
                    : 0;
                precio = varianteSeleccionada.Precio;
                modelo = varianteSeleccionada.ModeloNombre;
                sku = varianteSeleccionada.Sku;
            }
            else
            {
                if (solicitud.ProductoVarianteId is not null
                    || solicitud.ModeloId is not null
                    || solicitud.ModeloNombre is not null
                    || solicitud.MarcaNombre is not null)
                {
                    return CheckoutTiendaResultado.Conflict("La variante seleccionada ya no existe. Actualiza el carrito antes de continuar.");
                }

                stock = Math.Max(0, producto.Cantidad);
                precio = producto.PrecioMinimo > 0 ? producto.PrecioMinimo : producto.Precio;
                modelo = producto.ModeloNombre ?? producto.Modelo;
                sku = null;
            }

            precio = Math.Max(0, precio);
            if (precio <= 0)
                return CheckoutTiendaResultado.Conflict("Uno de los productos no tiene un precio público válido. Intenta nuevamente más tarde.");

            if (stock <= 0 || stock < solicitud.Unidades)
                return CheckoutTiendaResultado.Conflict("Cambió la existencia disponible de uno de los productos. Actualiza el carrito antes de continuar.");

            var oferta = await _promocionPublicaService.ResolverAsync(
                producto.Id,
                producto.CategoriaId,
                precio,
                ahoraUtc);
            var precioVigente = oferta?.PrecioOferta ?? precio;

            lineas.Add(new CheckoutTiendaLineaDto
            {
                ProductoId = producto.Id,
                ProductoVarianteId = productoVarianteId,
                ModeloId = modeloId,
                Nombre = producto.Nombre,
                Modelo = modelo,
                Sku = sku,
                Unidades = solicitud.Unidades,
                StockDisponible = stock,
                PrecioUnitario = precioVigente,
                PrecioBase = precio,
                CategoriaId = producto.CategoriaId,
                Total = FiscalCheckoutSnapshot.Moneda(precioVigente * solicitud.Unidades)
            });
        }

        // Different client aliases (variant ID vs model/brand) can resolve to
        // the same physical SKU. Enforce limits on the server-resolved identity
        // before calculating the quote; never count the same stock twice.
        var normalizadas = new List<CheckoutTiendaLineaDto>(lineas.Count);
        foreach (var grupo in lineas.GroupBy(l => new { l.ProductoId, l.ProductoVarianteId }))
        {
            var primera = grupo.First();
            var unidades = grupo.Sum(l => l.Unidades);
            var stock = grupo.Min(l => l.StockDisponible);
            if (unidades > MaxUnidadesPorLinea)
                return CheckoutTiendaResultado.Invalido("La cantidad acumulada de un producto supera el máximo permitido.");
            if (unidades > stock)
                return CheckoutTiendaResultado.Conflict("Cambió la existencia disponible de uno de los productos. Actualiza el carrito antes de continuar.");
            if (grupo.Any(l => l.PrecioUnitario != primera.PrecioUnitario || l.PrecioBase != primera.PrecioBase))
                return CheckoutTiendaResultado.Conflict("Cambió el precio público. Actualiza el carrito antes de continuar.");

            primera.Unidades = unidades;
            primera.StockDisponible = stock;
            primera.Total = FiscalCheckoutSnapshot.Moneda(primera.PrecioUnitario * unidades);
            normalizadas.Add(primera);
        }
        lineas = normalizadas;
        var subtotal = lineas.Sum(linea => linea.Total);
        List<DetalleCalculoInput> Bases(bool netas) => lineas.Select(l => new DetalleCalculoInput
        {
            ProductoId = l.ProductoId, CategoriaId = l.CategoriaId, Cantidad = l.Unidades,
            PrecioUnitario = netas ? l.PrecioUnitario : l.PrecioBase
        }).ToList();
        FiscalCheckoutSnapshot fiscal;
        string huellaFiscal;
        try
        {
            var impuestos = await _calculoService.CalcularImpuestosCheckoutPublicoAsync(Bases(false), Bases(true), ahoraUtc);
            fiscal = new FiscalCheckoutSnapshot(1,
                lineas.OrderBy(l => l.ProductoId).ThenBy(l => l.ProductoVarianteId)
                    .Select(l => new FiscalCheckoutLinea(l.ProductoId, l.ProductoVarianteId, l.Unidades,
                        l.PrecioBase, l.PrecioUnitario)).ToArray(),
                impuestos.Select(i => new FiscalCheckoutImpuesto(i.ImpuestoId, i.Nombre, i.Codigo,
                    i.Tasa, i.BaseImponible, i.Monto, i.IncluidoEnPrecio)).ToArray());
            huellaFiscal = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fiscal.Serializar()))).ToLowerInvariant();
        }
        catch (Exception e) when (e is BusinessRuleException or InvalidOperationException or OverflowException)
        {
            return CheckoutTiendaResultado.Conflict("No fue posible validar el desglose fiscal. Contacta al comercio o actualiza el carrito.");
        }
        // The same resolver used by administration is scoped by verified middleware.
        var tarifa = await _costoEnvioService.ResolverAsync(new ResolverCostoEnvioDto
        {
            Departamento = envio?.Departamento, Ciudad = envio?.Ciudad,
            Zona = envio?.Zona, Modalidad = envio?.Modalidad, FechaUtc = ahoraUtc
        });
        var costoEnvio = tarifa is { Monto: >= 0m } ? tarifa.Monto : (decimal?)null;
        var expira = ahoraUtc.Add(VigenciaValidacionCheckout);
        if (costoEnvio.HasValue && tarifa?.VigenteHasta is DateTime limite)
        {
            limite = limite.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(limite, DateTimeKind.Utc) : limite.ToUniversalTime();
            if (limite < expira) expira = limite;
        }
        var validado = new CheckoutTiendaValidadoDto
        {
            ValidacionId = Guid.NewGuid().ToString("N"),
            ExpiraUtc = expira,
            Subtotal = subtotal,
            Fiscal = fiscal,
            HuellaFiscal = huellaFiscal,
            CostoEnvio = costoEnvio,
            CostoEnvioId = costoEnvio.HasValue ? tarifa!.Id : null,
            TarifaEnvio = costoEnvio.HasValue ? tarifa!.Nombre : null,
            Total = subtotal + (costoEnvio ?? 0m) + fiscal.ImpuestoAdicional,
            Lineas = lineas
        };

        return CheckoutTiendaResultado.Ok(validado, costoEnvio.HasValue
            ? "Carrito y envío cotizados contra la configuración vigente."
            : "Carrito validado; no existe una tarifa de envío aplicable.");
    }
}
