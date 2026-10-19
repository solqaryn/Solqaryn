using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Identidad de tienda vinculada a una empresa autorizada por el middleware
/// HMAC/Host+StoreKey. Jamás consulta EmpresaConfiguracion singleton.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("tienda/configuracion")]
public sealed class TiendaConfiguracionPublicaController : ControllerBase
{
    private readonly AppDbContext _db;

    public TiendaConfiguracionPublicaController(AppDbContext db) => _db = db;

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Consultar(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        Response.Headers.Remove("ETag");
        var empresaId = VerifiedPublicStoreTenant.Resolve(HttpContext);
        if (empresaId is not > 0)
            return StatusCode(503, ApiResponse<object>.Fail("Tienda no autorizada."));

        var empresa = await _db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == empresaId.Value && e.Activa)
            .Select(e => new { e.Nombre, e.LogoUrl })
            .SingleOrDefaultAsync(cancellationToken);
        if (empresa is null)
            return StatusCode(503, ApiResponse<object>.Fail("Tienda no disponible."));

        var config = await _db.Set<ConfigEmpresa>().IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.EmpresaId == empresaId.Value)
            .Select(e => new { e.Moneda })
            .SingleOrDefaultAsync(cancellationToken);
        var identidad = new TiendaIdentidadPublicaDto
        {
            NombreComercial = empresa.Nombre,
            LogoUrl = empresa.LogoUrl,
            Moneda = config?.Moneda ?? "HNL",
            EncabezadoActivo = true,
            EncabezadoTexto = empresa.Nombre,
            PiePaginaActivo = false,
            MostrarCopyright = false,
            UsarAnioAutomaticoCopyright = true
        };
        return Ok(ApiResponse<TiendaIdentidadPublicaDto>.Ok(identidad));
    }
}
