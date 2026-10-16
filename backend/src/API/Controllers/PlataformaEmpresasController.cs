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
        if (pagina is < 1 or > 100000 || tamano is < 1 or > 50)
            return BadRequest(ApiResponse<object>.Fail("Paginación fuera de límites."));

        var empresas = _db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking();
        var total = await empresas.CountAsync(cancellationToken);
        var rows = await empresas.OrderBy(e => e.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .Select(e => new { e.Id, e.Nombre, e.Activa })
            .ToArrayAsync(cancellationToken);

        var ids = rows.Select(e => e.Id).ToArray();
        // El recuento nunca incluye membresías suspendidas ni cuentas
        // bloqueadas/inactivas; no consulta ni publica datos comerciales.
        var membresias = await (
            from m in _db.Set<UsuarioEmpresa>().IgnoreQueryFilters().AsNoTracking()
            join usuario in _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                on m.UsuarioId equals usuario.Id
            where ids.Contains(m.EmpresaId) && m.Activa &&
                  usuario.Activo && !usuario.Bloqueado && !usuario.Eliminado
            group m by m.EmpresaId into g
            select new { EmpresaId = g.Key, Total = g.Count() }
        ).ToArrayAsync(cancellationToken);
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

    /// <summary>
    /// Aprovisionamiento de identidad sin habilitar actividad comercial.
    /// Permite que la plataforma prepare A/B sin convertir un borrador
    /// en empresa activa ni alterar el tenant publicado existente.
    /// </summary>
    [HttpPost("borrador")]
    public async Task<IActionResult> CrearBorrador(
        [FromBody] BorradorEmpresaPlataformaRequest dto, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        if (User.Identity?.IsAuthenticated != true ||
            !_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            return Forbid();
        var actor = await _plataforma.ObtenerActualAsync(ct);
        if (actor is null) return Forbid();
        var nombre = dto?.Nombre?.Trim() ?? "";
        if (nombre.Length is < 2 or > 200)
            return BadRequest(ApiResponse<object>.Fail("Nombre empresarial inválido."));
        if (await _db.Set<Empresa>().IgnoreQueryFilters()
            .AnyAsync(e => e.Nombre == nombre, ct))
            return Conflict(ApiResponse<object>.Fail("La empresa ya existe."));

        await using var tx = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(ct) : null;
        // Antes de la segunda empresa, fijar en la misma transacción el
        // propietario de /tienda legacy. No inferirlo de la empresa activa
        // después del alta, ni permitir cambiarlo al suspender/activar.
        var legacyHost = _configuration["PublicStore:LegacyUpstreamHost"];
        if (string.IsNullOrWhiteSpace(legacyHost) ||
            legacyHost != legacyHost.Trim().ToLowerInvariant() ||
            !legacyHost.Contains('.') || legacyHost.Contains(':') ||
            legacyHost.Contains('/') || legacyHost.Contains('\\'))
            return StatusCode(503,
                ApiResponse<object>.Fail("Host upstream legacy sin configurar."));
        var currentOwners = await _db.Set<Empresa>().IgnoreQueryFilters()
            .AsNoTracking().Select(x => new { x.Id, x.Activa })
            .Take(2).ToArrayAsync(ct);
        if (currentOwners.Length == 1 && currentOwners[0].Activa)
        {
            var mapped = await _db.PublicStoreBindings.IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Host == legacyHost && x.StoreKey == "", ct);
            if (mapped is not null && mapped.EmpresaId != currentOwners[0].Id)
                return Conflict(ApiResponse<object>.Fail("Propietario legacy incompatible."));
            if (mapped is null)
                _db.PublicStoreBindings.Add(new PublicStoreBinding
                {
                    EmpresaId = currentOwners[0].Id, Host = legacyHost,
                    StoreKey = "", Activa = true
                });
        }
        else if (currentOwners.Length >= 2 &&
            !await _db.PublicStoreBindings.IgnoreQueryFilters().AnyAsync(
                x => x.Host == legacyHost && x.StoreKey == "" && x.Activa, ct))
        {
            return StatusCode(503,
                ApiResponse<object>.Fail("Falta vinculación segura de la tienda existente."));
        }

        var empresa = new Empresa(nombre)
        {
            CreadoPorUsuarioId = actor.UsuarioId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        empresa.Desactivar();
        _db.Set<Empresa>().Add(empresa);
        await _db.SaveChangesAsync(ct);
        _db.Set<RegistroAuditoria>().Add(new RegistroAuditoria
        {
            UsuarioId = actor.UsuarioId,
            NombreUsuario = actor.NombreUsuario,
            EmpresaId = empresa.Id,
            AmbitoAutoridad = AmbitoAutorizacion.Plataforma,
            OrigenAutorizacion = OrigenAutorizacion.RolPlataforma,
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.Crear,
            Entidad = nameof(Empresa),
            ReferenciaId = empresa.Id,
            Descripcion = "Empresa creada como borrador inactivo, sin operación comercial.",
            CorrelationId = HttpContext.TraceIdentifier,
            Resultado = "Exito"
        });
        await _db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);
        return Created("/plataforma/empresas/" + empresa.Id,
            new { empresaId = empresa.Id, activa = false, operacionComercialHabilitada = false });
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

        // Se admite la identidad PENDIENTE creada por el mismo operador global
        // solo si nunca recibió membresías ni grants globales. Permanece inactiva
        // hasta el commit atómico de empresa+membresía+auditoría.
        var administrador = await _db.Usuarios.IgnoreQueryFilters()
            .SingleOrDefaultAsync(u => u.Id == dto.AdministradorUsuarioId,
                cancellationToken);
        if (administrador is null || administrador.Bloqueado || administrador.Eliminado)
            return BadRequest(ApiResponse<object>.Fail("El administrador solicitado no está habilitado."));
        var pendiente = !administrador.Activo &&
            administrador.CreadoPorUsuarioId == actorId &&
            !await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(m => m.UsuarioId == administrador.Id, cancellationToken) &&
            !await _db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(g => g.UsuarioId == administrador.Id, cancellationToken);
        if (!administrador.Activo && !pendiente)
            return BadRequest(ApiResponse<object>.Fail("La cuenta del administrador está suspendida."));

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

        if (pendiente)
        {
            administrador.Activo = true;
            administrador.ActualizadoPorUsuarioId = actorId;
            administrador.FechaActualizacion = DateTime.UtcNow;
        }

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

public sealed class BorradorEmpresaPlataformaRequest
{
    public string Nombre { get; set; } = string.Empty;
}
