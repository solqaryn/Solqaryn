using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Registro persistente, no público, de la identidad de tiendas.
/// El host+clave NO concede permisos ERP; requiere grant global vivo MySQL.
/// Por defecto el registro se prepara inactivo para permitir revisiones.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/gestion/empresas/{empresaId:int}/tiendas")]
public sealed class PlataformaTiendasController : ControllerBase
{
    private static readonly Regex HostPattern = new(
        @"^(?=.{4,253}$)[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9]+(?:[a-z0-9-]*[a-z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex StoreKeyPattern = new(
        "^[a-z][a-z0-9-]{0,46}[a-z0-9]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPlataformaScopeService _platform;
    public PlataformaTiendasController(AppDbContext db, ICurrentUserService current,
        IPlataformaScopeService platform)
    { _db = db; _current = current; _platform = platform; }

    private async Task<UsuarioPlataformaScopeActual?> Authority(CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (User.Identity?.IsAuthenticated != true ||
            !_current.EstaAutenticado || _current.UsuarioId is not > 0) return null;
        return await _platform.ObtenerActualAsync(ct);
    }

    [HttpGet]
    public async Task<IActionResult> List(int empresaId, CancellationToken ct)
    {
        var actor = await Authority(ct);
        if (actor is null) return Forbid();
        var exists = await _db.Set<Empresa>().IgnoreQueryFilters()
            .AnyAsync(x => x.Id == empresaId, ct);
        if (!exists) return NotFound(ApiResponse<object>.Fail("Empresa inexistente."));
        var bindings = await _db.PublicStoreBindings.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId).OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Host, x.StoreKey, x.Activa })
            .Take(100).ToArrayAsync(ct);
        return Ok(ApiResponse<object>.Ok(bindings));
    }

    [HttpPut("{storeKey}")]
    public async Task<IActionResult> Upsert(int empresaId, string storeKey,
        [FromBody] StoreBindingRequest dto, CancellationToken ct)
    {
        var actor = await Authority(ct);
        if (actor is null) return Forbid();
        var host = dto?.Host?.Trim().ToLowerInvariant() ?? "";
        // La activación pública debe ser explícita y se permite sólo para
        // una empresa activa. Nunca se infiere de URL, JWT o nombre comercial.
        if (!HostPattern.IsMatch(host) || host.Split('.').Any(p => p.Length > 63) ||
            !StoreKeyPattern.IsMatch(storeKey) || storeKey.Length > 48 ||
            dto is null) return BadRequest(ApiResponse<object>.Fail("Host o clave inválidos."));
        var empresa = await _db.Set<Empresa>().IgnoreQueryFilters()
            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == empresaId, ct);
        if (empresa is null) return NotFound(ApiResponse<object>.Fail("Empresa inexistente."));
        if (dto.Activa && !empresa.Activa)
            return Conflict(ApiResponse<object>.Fail("No se publica una empresa suspendida."));

        // Registrar y auditar en una única transacción, con unicidad real MySQL.
        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(ct) : null;
        var existing = await _db.PublicStoreBindings
            .SingleOrDefaultAsync(x => x.Host == host && x.StoreKey == storeKey, ct);
        if (existing is not null && existing.EmpresaId != empresaId)
            return Conflict(ApiResponse<object>.Fail("La URL pertenece a otra empresa."));
        var binding = existing ?? new PublicStoreBinding {
            EmpresaId = empresaId, Host = host, StoreKey = storeKey,
            FechaCreacionUtc = DateTime.UtcNow
        };
        binding.Activa = dto.Activa;
        binding.FechaActualizacionUtc = DateTime.UtcNow;
        if (existing is null) _db.PublicStoreBindings.Add(binding);
        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria {
            UsuarioId = actor.UsuarioId, NombreUsuario = actor.NombreUsuario,
            EmpresaId = empresaId, AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Editar,
            Entidad = nameof(PublicStoreBinding), ReferenciaId = empresaId,
            Descripcion = dto.Activa ? "Vinculación pública de tienda activada." :
                "Vinculación pública de tienda registrada o desactivada.",
            CorrelationId = HttpContext.TraceIdentifier, Resultado = "Exito"
        });
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException) {
            if (tx is not null) await tx.RollbackAsync(ct);
            return Conflict(ApiResponse<object>.Fail("Conflicto de vinculación pública."));
        }
        if (tx is not null) await tx.CommitAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { binding.Id, binding.EmpresaId,
            binding.Host, binding.StoreKey, binding.Activa }));
    }

    public sealed class StoreBindingRequest
    {
        public string Host { get; set; } = string.Empty;
        public bool Activa { get; set; }
    }
}
