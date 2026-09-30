using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("tienda/cuenta")]
public sealed class TiendaCuentaController : ControllerBase
{
    private const string SessionHeader = "X-Storefront-Session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly AppDbContext _db;
    private readonly ITipoClientePredeterminadoResolver _tipoClienteResolver;

    public TiendaCuentaController(AppDbContext db, ITipoClientePredeterminadoResolver tipoClienteResolver)
    {
        _db = db;
        _tipoClienteResolver = tipoClienteResolver;
    }

    [HttpPost("registrar")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Registrar([FromBody] TiendaCuentaRegistroDto dto)
    {
        var nombre = Limpiar(dto?.Nombre);
        var correo = NormalizarCorreo(dto?.Correo);
        var clave = dto?.Clave ?? string.Empty;

        if (nombre is null || nombre.Length > 120)
            return BadRequest(ApiResponse<object>.Fail("Ingresa un nombre válido de hasta 120 caracteres."));
        if (correo is null)
            return BadRequest(ApiResponse<object>.Fail("Ingresa un correo electrónico válido."));
        if (clave.Length < 10 || clave.Length > 128)
            return BadRequest(ApiResponse<object>.Fail("La contraseña debe tener entre 10 y 128 caracteres."));

        if (await _db.TiendaCuentasCliente.AsNoTracking().AnyAsync(x => x.CorreoNormalizado == correo))
            return Conflict(ApiResponse<object>.Fail("Ya existe una cuenta con ese correo."));

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var tipoClienteId = await _tipoClienteResolver.ResolverIdPredeterminadoAsync();
            var cliente = new Cliente
            {
                Nombre = nombre,
                Correo = correo,
                Activo = true,
                TipoClienteId = tipoClienteId,
                CreadoPorNombreUsuario = "storefront-cuenta"
            };
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            var cuenta = new TiendaCuentaCliente
            {
                ClienteId = cliente.Id,
                Nombre = nombre,
                Correo = correo,
                CorreoNormalizado = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(clave, workFactor: 12),
                Activa = true,
                UltimoAccesoUtc = DateTime.UtcNow
            };
            _db.TiendaCuentasCliente.Add(cuenta);
            await _db.SaveChangesAsync();

            var sesion = await CrearSesionAsync(cuenta);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(ApiResponse<TiendaCuentaSesionDto>.Ok(sesion, "Cuenta creada correctamente."));
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Conflict(ApiResponse<object>.Fail("No se pudo crear la cuenta porque el correo ya está registrado."));
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] TiendaCuentaLoginDto dto)
    {
        var correo = NormalizarCorreo(dto?.Correo);
        var clave = dto?.Clave ?? string.Empty;
        if (correo is null || string.IsNullOrEmpty(clave))
            return Unauthorized(ApiResponse<object>.Fail("Correo o contraseña incorrectos."));

        var cuenta = await _db.TiendaCuentasCliente.FirstOrDefaultAsync(x => x.CorreoNormalizado == correo);
        if (cuenta is null || !cuenta.Activa || !BCrypt.Net.BCrypt.Verify(clave, cuenta.PasswordHash))
            return Unauthorized(ApiResponse<object>.Fail("Correo o contraseña incorrectos."));

        var ahora = DateTime.UtcNow;
        cuenta.UltimoAccesoUtc = ahora;
        cuenta.FechaActualizacion = ahora;

        var expiradas = await _db.TiendaSesionesCliente
            .Where(x => x.CuentaClienteId == cuenta.Id && (x.ExpiraUtc <= ahora || x.RevocadaUtc != null))
            .ToListAsync();
        if (expiradas.Count > 0) _db.TiendaSesionesCliente.RemoveRange(expiradas);

        var sesion = await CrearSesionAsync(cuenta);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<TiendaCuentaSesionDto>.Ok(sesion, "Sesión iniciada."));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sesion = await ObtenerSesionActualAsync(tracking: true);
        if (sesion is null)
            return Ok(ApiResponse<object>.Ok(new { }, "Sesión cerrada."));

        sesion.RevocadaUtc = DateTime.UtcNow;
        sesion.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Sesión cerrada."));
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();
        return Ok(ApiResponse<TiendaCuentaPerfilDto>.Ok(MapPerfil(cuenta)));
    }

    [HttpGet("direcciones")]
    public async Task<IActionResult> Direcciones()
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var items = await _db.TiendaDireccionesCliente.AsNoTracking()
            .Where(x => x.CuentaClienteId == cuenta.Id)
            .OrderByDescending(x => x.Predeterminada)
            .ThenBy(x => x.Id)
            .Select(x => new TiendaDireccionDto
            {
                Id = x.Id,
                Alias = x.Alias,
                Recibe = x.Recibe,
                Telefono = x.Telefono,
                Direccion = x.Direccion,
                Predeterminada = x.Predeterminada
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TiendaDireccionDto>>.Ok(items));
    }

    [HttpPost("direcciones")]
    public async Task<IActionResult> GuardarDireccion([FromBody] TiendaDireccionGuardarDto dto)
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var alias = Limpiar(dto?.Alias);
        var recibe = Limpiar(dto?.Recibe);
        var telefono = Limpiar(dto?.Telefono);
        var direccion = Limpiar(dto?.Direccion);
        if (alias is null || alias.Length > 60 || recibe is null || recibe.Length > 120
            || telefono is null || telefono.Length > 30 || direccion is null || direccion.Length > 300)
            return BadRequest(ApiResponse<object>.Fail("Revisa los datos de la dirección."));

        var cantidad = await _db.TiendaDireccionesCliente.CountAsync(x => x.CuentaClienteId == cuenta.Id);
        if (cantidad >= 12)
            return Conflict(ApiResponse<object>.Fail("Alcanzaste el máximo de 12 direcciones guardadas."));

        var predeterminada = dto?.Predeterminada == true || cantidad == 0;
        if (predeterminada)
        {
            var anteriores = await _db.TiendaDireccionesCliente.Where(x => x.CuentaClienteId == cuenta.Id && x.Predeterminada).ToListAsync();
            foreach (var anterior in anteriores)
            {
                anterior.Predeterminada = false;
                anterior.FechaActualizacion = DateTime.UtcNow;
            }
        }

        var item = new TiendaDireccionCliente
        {
            CuentaClienteId = cuenta.Id,
            Alias = alias,
            Recibe = recibe,
            Telefono = telefono,
            Direccion = direccion,
            Predeterminada = predeterminada
        };
        _db.TiendaDireccionesCliente.Add(item);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<TiendaDireccionDto>.Ok(new TiendaDireccionDto
        {
            Id = item.Id,
            Alias = item.Alias,
            Recibe = item.Recibe,
            Telefono = item.Telefono,
            Direccion = item.Direccion,
            Predeterminada = item.Predeterminada
        }, "Dirección guardada."));
    }

    [HttpDelete("direcciones/{id:int}")]
    public async Task<IActionResult> EliminarDireccion(int id)
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var item = await _db.TiendaDireccionesCliente.FirstOrDefaultAsync(x => x.Id == id && x.CuentaClienteId == cuenta.Id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Dirección no encontrada."));

        var eraPredeterminada = item.Predeterminada;
        _db.TiendaDireccionesCliente.Remove(item);
        await _db.SaveChangesAsync();

        if (eraPredeterminada)
        {
            var siguiente = await _db.TiendaDireccionesCliente
                .Where(x => x.CuentaClienteId == cuenta.Id)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();
            if (siguiente is not null)
            {
                siguiente.Predeterminada = true;
                siguiente.FechaActualizacion = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Dirección eliminada."));
    }

    [HttpGet("favoritos")]
    public async Task<IActionResult> Favoritos()
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var ids = await _db.TiendaFavoritosCliente.AsNoTracking()
            .Where(x => x.CuentaClienteId == cuenta.Id)
            .OrderByDescending(x => x.Id)
            .Select(x => x.ProductoId)
            .ToListAsync();
        return Ok(ApiResponse<List<int>>.Ok(ids));
    }

    [HttpPut("favoritos/{productoId:int}")]
    public async Task<IActionResult> AgregarFavorito(int productoId)
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();
        if (productoId <= 0 || !await _db.Productos.AsNoTracking().AnyAsync(x => x.Id == productoId && x.Activo && !x.Eliminado))
            return NotFound(ApiResponse<object>.Fail("Producto no disponible."));

        var existe = await _db.TiendaFavoritosCliente.AnyAsync(x => x.CuentaClienteId == cuenta.Id && x.ProductoId == productoId);
        if (!existe)
        {
            _db.TiendaFavoritosCliente.Add(new TiendaFavoritoCliente { CuentaClienteId = cuenta.Id, ProductoId = productoId });
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { productoId }, "Producto guardado en favoritos."));
    }

    [HttpDelete("favoritos/{productoId:int}")]
    public async Task<IActionResult> QuitarFavorito(int productoId)
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var item = await _db.TiendaFavoritosCliente.FirstOrDefaultAsync(x => x.CuentaClienteId == cuenta.Id && x.ProductoId == productoId);
        if (item is not null)
        {
            _db.TiendaFavoritosCliente.Remove(item);
            await _db.SaveChangesAsync();
        }
        return Ok(ApiResponse<object>.Ok(new { productoId }, "Producto retirado de favoritos."));
    }

    [HttpGet("pedidos")]
    public async Task<IActionResult> Pedidos()
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var pedidos = await _db.Set<PedidoVenta>().AsNoTracking()
            .Where(x => x.ClienteId == cuenta.ClienteId)
            .Include(x => x.Detalles)
            .OrderByDescending(x => x.Id)
            .Take(100)
            .AsSplitQuery()
            .ToListAsync();

        return Ok(ApiResponse<List<TiendaPedidoCuentaDto>>.Ok(pedidos.Select(MapPedido).ToList()));
    }

    [HttpGet("pedidos/{id:int}")]
    public async Task<IActionResult> Pedido(int id)
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var pedido = await _db.Set<PedidoVenta>().AsNoTracking()
            .Where(x => x.Id == id && x.ClienteId == cuenta.ClienteId)
            .Include(x => x.Detalles)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (pedido is null) return NotFound(ApiResponse<object>.Fail("Pedido no encontrado."));
        return Ok(ApiResponse<TiendaPedidoCuentaDto>.Ok(MapPedido(pedido)));
    }

    [HttpGet("notificaciones")]
    public async Task<IActionResult> Notificaciones()
    {
        var cuenta = await RequerirCuentaAsync();
        if (cuenta is null) return NoAutorizado();

        var pedidos = await _db.Set<PedidoVenta>().AsNoTracking()
            .Where(x => x.ClienteId == cuenta.ClienteId)
            .OrderByDescending(x => x.FechaActualizacion)
            .Take(20)
            .Select(x => new { x.Id, x.Estado, x.FechaActualizacion })
            .ToListAsync();

        var items = pedidos.Select(x => new TiendaNotificacionPedidoDto
        {
            PedidoId = x.Id,
            Estado = x.Estado.ToString(),
            FechaUtc = x.FechaActualizacion,
            Mensaje = x.Estado.ToString() switch
            {
                "Confirmado" => $"Pedido #{x.Id} confirmado.",
                "Anulado" => $"Pedido #{x.Id} fue anulado.",
                _ => $"Pedido #{x.Id} está en preparación."
            }
        }).ToList();

        return Ok(ApiResponse<List<TiendaNotificacionPedidoDto>>.Ok(items));
    }

    private async Task<TiendaCuentaSesionDto> CrearSesionAsync(TiendaCuentaCliente cuenta)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var ahora = DateTime.UtcNow;
        var expira = ahora.Add(SessionLifetime);
        _db.TiendaSesionesCliente.Add(new TiendaSesionCliente
        {
            CuentaClienteId = cuenta.Id,
            TokenHash = HashToken(token),
            ExpiraUtc = expira,
            UltimoUsoUtc = ahora
        });

        return new TiendaCuentaSesionDto
        {
            Token = token,
            ExpiraUtc = expira,
            Perfil = MapPerfil(cuenta)
        };
    }

    private async Task<TiendaCuentaCliente?> RequerirCuentaAsync()
    {
        var sesion = await ObtenerSesionActualAsync(tracking: false);
        return sesion?.CuentaCliente;
    }

    private async Task<TiendaSesionCliente?> ObtenerSesionActualAsync(bool tracking)
    {
        var raw = Request.Headers[SessionHeader].FirstOrDefault()?.Trim() ?? string.Empty;
        if (raw.Length < 32 || raw.Length > 128 || raw.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_')))
            return null;

        var hash = HashToken(raw);
        var ahora = DateTime.UtcNow;
        var query = tracking ? _db.TiendaSesionesCliente.AsTracking() : _db.TiendaSesionesCliente.AsNoTracking();
        return await query
            .Include(x => x.CuentaCliente)
            .FirstOrDefaultAsync(x => x.TokenHash == hash
                && x.RevocadaUtc == null
                && x.ExpiraUtc > ahora
                && x.CuentaCliente.Activa);
    }

    private IActionResult NoAutorizado() =>
        Unauthorized(ApiResponse<object>.Fail("Inicia sesión en tu cuenta de cliente para continuar."));

    private static TiendaCuentaPerfilDto MapPerfil(TiendaCuentaCliente cuenta) => new()
    {
        Id = cuenta.Id,
        Nombre = cuenta.Nombre,
        Correo = cuenta.Correo
    };

    private static TiendaPedidoCuentaDto MapPedido(PedidoVenta pedido) => new()
    {
        Id = pedido.Id,
        Estado = pedido.Estado.ToString(),
        Total = pedido.Total,
        FechaUtc = pedido.FechaCreacion,
        Lineas = pedido.Detalles.Select(x => new TiendaPedidoCuentaLineaDto
        {
            ProductoId = x.ProductoId,
            ProductoVarianteId = x.ProductoVarianteId,
            Nombre = x.ProductoNombreSnapshot ?? "Producto",
            Modelo = x.ProductoModeloSnapshot,
            Cantidad = x.Cantidad,
            PrecioUnitario = x.PrecioUnitario,
            Total = x.Total
        }).ToList()
    };

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? NormalizarCorreo(string? value)
    {
        var limpio = Limpiar(value)?.ToLowerInvariant();
        if (limpio is null || limpio.Length > 150) return null;
        try
        {
            var mail = new MailAddress(limpio);
            return string.Equals(mail.Address, limpio, StringComparison.OrdinalIgnoreCase) ? limpio : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? Limpiar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
