using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Alta exclusivamente de PLATAFORMA, nunca por un administrador de empresa.
/// No habilita la vía comercial A/B ni ninguna tienda automáticamente.
/// </summary>
[ApiController]
[Authorize]
[Route("plataforma/empresas")]
public sealed class PlataformaEmpresasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IPlataformaScopeService _plataforma;

    public PlataformaEmpresasController(
        AppDbContext db,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IPlataformaScopeService plataforma)
    {
        _db = db;
        _currentUser = currentUser;
        _configuration = configuration;
        _plataforma = plataforma;
    }

    /// <summary>
    /// Catálogo global mínimo; NO lee datos comerciales ni confiere contexto tenant.
    /// Los datos de empresas inactivas son visibles sólo con rol global vigente.
    /// </summary>
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 25,
        CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (User.Identity?.IsAuthenticated != true ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            return Forbid();
        var actor = await _plataforma.ObtenerActualAsync(cancellationToken);
        if (actor is null) return Forbid();
        if (pagina < 1 || tamano is < 1 or > 50)
            return BadRequest(ApiResponse<object>.Fail("Paginación fuera de límites."));

        var empresas = _db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking();
        var total = await empresas.CountAsync(cancellationToken);
        var rows = await empresas.OrderBy(e => e.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(e => new { e.Id, e.Nombre, e.Activa })
            .ToArrayAsync(cancellationToken);

        var ids = rows.Select(e => e.Id).ToArray();
        var membresias = await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => ids.Contains(m.EmpresaId) && m.Activa)
            .GroupBy(m => m.EmpresaId)
            .Select(g => new { EmpresaId = g.Key, Total = g.Count() })
            .ToArrayAsync(cancellationToken);
        var totals = membresias.ToDictionary(x => x.EmpresaId, x => x.Total);

        // Registrar únicamente ids/counts; no volcar PII o contenido de otros tenants.
        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
        {
            UsuarioId = actor.UsuarioId,
            NombreUsuario = actor.NombreUsuario,
            EmpresaId = null,
            AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.ConsultarHistorial,
            Entidad = nameof(Empresa),
            Descripcion = "Consulta global paginada del directorio de empresas.",
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = "Exito"
        });
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new
        {
            items = rows.Select(e => new
            {
                id = e.Id,
                nombre = e.Nombre,
                activa = e.Activa,
                usuariosActivos = totals.GetValueOrDefault(e.Id)
            }),
            pagina,
            tamano,
            total,
            puedeCrear = _configuration.GetValue<bool>("MultiEmpresa:CommercialIsolationCertified") &&
                         _configuration.GetValue<bool>("MultiEmpresa:PlatformOnboardingEnabled")
        }, "Directorio global de empresas consultado."));
    }

    [HttpPost("alta-controlada")]
    public async Task<IActionResult> AltaControlada(
        [FromBody] AltaEmpresaPlataformaRequest dto,
        CancellationToken cancellationToken)
    {
        // Cualquiera de las dos banderas ausente/falsa bloquea el alta.
        // La certificación A/B y la activación del onboarding son decisiones
        // independientes: nunca se deducen de la rama ni del JWT.
        if (!_configuration.GetValue<bool>("MultiEmpresa:CommercialIsolationCertified") ||
            !_configuration.GetValue<bool>("MultiEmpresa:PlatformOnboardingEnabled"))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail("Alta multiempresa no habilitada: requiere certificación y activación operativa."));

        if (User.Identity?.IsAuthenticated != true ||
            !_currentUser.EstaAutenticado ||
            _currentUser.UsuarioId is not > 0)
            return Forbid();

        var actorId = _currentUser.UsuarioId.Value;
        // Una sola autoridad global certificada en MySQL; esta misma fuente
        // se comparte con GET /plataforma/sesion.
        var actor = await _plataforma.ObtenerActualAsync(cancellationToken);

        // No aceptar rol global del JWT ni membresía UsuarioEmpresa como sustituto.
        if (actor is null)
            return Forbid();

        var nombre = dto?.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0 || nombre.Length > 200 ||
            dto!.AdministradorUsuarioId <= 0)
            return BadRequest(ApiResponse<object>.Fail("Nombre o usuario administrador inválido."));

        var administradorActivo = await _db.Usuarios.IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(u => u.Id == dto.AdministradorUsuarioId &&
                           u.Activo && !u.Bloqueado && !u.Eliminado,
                cancellationToken);
        if (!administradorActivo)
            return BadRequest(ApiResponse<object>.Fail("El administrador solicitado no está habilitado."));

        // Sólo un rol ADMIN de plantilla empresarial. Un rol de Plataforma
        // nunca se asigna como rol de una membresía UsuarioEmpresa.
        var rolEmpresaId = await _db.Roles.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(rol => rol.Ambito == AmbitoAutorizacion.Empresa &&
                          rol.EmpresaId == null &&
                          rol.EsSistema && rol.EsAdministrador &&
                          rol.Activo && !rol.Eliminado)
            .OrderBy(rol => rol.Id)
            .Select(rol => rol.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (rolEmpresaId <= 0)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.Fail("No existe una plantilla de administrador empresarial activa."));

        if (await _db.Set<Empresa>().IgnoreQueryFilters()
                .AsNoTracking().AnyAsync(e => e.Nombre == nombre, cancellationToken))
            return Conflict(ApiResponse<object>.Fail("Ya existe una empresa con el mismo nombre."));

        // En producción se exige transacción de MySQL: empresa, membresía
        // y bitácora crítica comparten el mismo commit o se revierten todos.
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var empresa = new Empresa(nombre)
        {
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        _db.Set<Empresa>().Add(empresa);
        await _db.SaveChangesAsync(cancellationToken);

        var membresia = new UsuarioEmpresa(
            dto.AdministradorUsuarioId, empresa.Id, rolEmpresaId)
        {
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        _db.Set<UsuarioEmpresa>().Add(membresia);

        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
        {
            UsuarioId = actorId,
            NombreUsuario = actor.NombreUsuario,
            EmpresaId = empresa.Id,
            AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.Crear,
            Entidad = nameof(Empresa),
            ReferenciaId = empresa.Id,
            Descripcion = "Alta controlada de empresa y membresía inicial de administrador.",
            ValoresNuevos = System.Text.Json.JsonSerializer.Serialize(new
            {
                EmpresaId = empresa.Id,
                AdministradorUsuarioId = dto.AdministradorUsuarioId,
                RolEmpresaId = rolEmpresaId
            }),
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = "Exito"
        });
        await _db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return Created($"/plataforma/empresas/{empresa.Id}", new
        {
            empresaId = empresa.Id,
            administradorUsuarioId = dto.AdministradorUsuarioId,
            // El alta no implica que las operaciones comerciales estén
            // certificadas ni habilitadas; el middleware permanece vigente.
            operacionComercialHabilitada = false
        });
    }
}

public sealed class AltaEmpresaPlataformaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int AdministradorUsuarioId { get; set; }
}
