using System.Text.RegularExpressions;
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
    private static readonly Regex NombreUsuarioValido = new(
        "^[a-zA-Z0-9._-]{3,50}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
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
        var nuevo = dto?.AdministradorNuevo;
        // EXACTAMENTE una modalidad: el ID existente o una nueva credencial.
        // Nunca se interpreta la existencia de dos fuentes como autorización.
        if (dto is null || nombre.Length == 0 || nombre.Length > 200 ||
            (nuevo is null ? dto.AdministradorUsuarioId <= 0 :
                dto.AdministradorUsuarioId != 0))
            return BadRequest(ApiResponse<object>.Fail("Nombre o modalidad de administrador inválidos."));

        string? usuarioNuevo = null;
        string? nombreCompletoNuevo = null;
        if (nuevo is not null)
        {
            usuarioNuevo = nuevo.NombreUsuario?.Trim() ?? string.Empty;
            nombreCompletoNuevo = nuevo.NombreCompleto?.Trim() ?? string.Empty;
            var password = nuevo.Password ?? string.Empty;
            // Mantener el contrato de creación de cuentas de /plataforma/gestion/usuarios.
            if (!NombreUsuarioValido.IsMatch(usuarioNuevo) ||
                nombreCompletoNuevo.Length is < 3 or > 150 ||
                password.Length is < 10 or > 128 ||
                !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) ||
                !password.Any(c => !char.IsLetterOrDigit(c)))
                return BadRequest(ApiResponse<object>.Fail("Usuario, nombre o contraseña inválidos."));
            if (await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .AnyAsync(u => u.NombreUsuario == usuarioNuevo, cancellationToken))
                return Conflict(ApiResponse<object>.Fail("El nombre de usuario ya existe."));
        }

        Usuario? administrador = null;
        var pendiente = false;
        if (nuevo is null)
        {
            // Compatibilidad: la identidad previamente registrada sigue disponible
            // y una cuenta suspendida no se convierte en admin por este alta.
            administrador = await _db.Usuarios.IgnoreQueryFilters()
                .SingleOrDefaultAsync(u => u.Id == dto.AdministradorUsuarioId,
                    cancellationToken);
            if (administrador is null || administrador.Bloqueado || administrador.Eliminado)
                return BadRequest(ApiResponse<object>.Fail("El administrador solicitado no está habilitado."));
            pendiente = !administrador.Activo &&
                administrador.CreadoPorUsuarioId == actorId &&
                !await _db.Set<UsuarioEmpresa>().IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == administrador.Id, cancellationToken) &&
                !await _db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters().AsNoTracking()
                    .AnyAsync(g => g.UsuarioId == administrador.Id, cancellationToken);
            if (!administrador.Activo && !pendiente)
                return BadRequest(ApiResponse<object>.Fail("La cuenta del administrador está suspendida."));
        }

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

        // Usuario.RolId conserva el rol base NO administrador para los consumidores
        // legacy; únicamente UsuarioEmpresa.RolId concede autoridad dentro de
        // la empresa creada. No escribir UsuarioRolPlataforma.
        var rolBaseId = 0;
        if (nuevo is not null)
        {
            rolBaseId = await _db.Roles.IgnoreQueryFilters().AsNoTracking()
                .Where(rol => rol.Ambito == AmbitoAutorizacion.Empresa &&
                              rol.EmpresaId == null && rol.EsSistema &&
                              !rol.EsAdministrador && rol.Activo && !rol.Eliminado)
                .OrderBy(rol => rol.Id).Select(rol => rol.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (rolBaseId <= 0)
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail("No existe rol empresarial base no administrador."));
        }

        if (await _db.Set<Empresa>().IgnoreQueryFilters()
                .AsNoTracking().AnyAsync(e => e.Nombre == nombre, cancellationToken))
            return Conflict(ApiResponse<object>.Fail("Ya existe una empresa con el mismo nombre."));

        // El coste BCrypt se calcula antes de abrir la transacción, sin writes;
        // la creación del Usuario y toda su persistencia sí comparten el commit.
        var hashNuevo = nuevo is null ? null :
            BCrypt.Net.BCrypt.HashPassword(nuevo.Password, workFactor: 12);
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        if (nuevo is not null)
        {
            administrador = new Usuario
            {
                NombreUsuario = usuarioNuevo!,
                NombreCompleto = nombreCompletoNuevo!,
                PasswordHash = hashNuevo!,
                RolId = rolBaseId,
                Activo = true,
                CreadoPorUsuarioId = actorId
            };
            _db.Usuarios.Add(administrador);
            // La identidad adquiere ID dentro de la misma transacción MySQL.
            // Si fallan ConfigEmpresa, membresía o auditoría, también se revierte.
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (pendiente && administrador is not null)
        {
            administrador.Activo = true;
            administrador.ActualizadoPorUsuarioId = actorId;
            administrador.FechaActualizacion = DateTime.UtcNow;
        }
        var administradorId = administrador!.Id;

        var empresa = new Empresa(nombre)
        {
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        _db.Set<Empresa>().Add(empresa);
        await _db.SaveChangesAsync(cancellationToken);

        // Configuración tenant mínima, sin secretos ni permisos comerciales.
        // Se persiste antes de la auditoría para que un fallo en esta última
        // pruebe el rollback de datos ya escritos (pero no confirmados).
        var configEmpresa = new ConfigEmpresa(empresa.Id)
        {
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        _db.Set<ConfigEmpresa>().Add(configEmpresa);

        var membresia = new UsuarioEmpresa(
            administradorId, empresa.Id, rolEmpresaId)
        {
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = actor.NombreUsuario
        };
        _db.Set<UsuarioEmpresa>().Add(membresia);
        await _db.SaveChangesAsync(cancellationToken);

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
            Descripcion = nuevo is null
                ? "Alta controlada de empresa, configuración y membresía inicial de administrador."
                : "Alta atómica de usuario nuevo, empresa, configuración y membresía de administrador.",
            // Auditoría intencionalmente SIN password, hash ni otros secretos.
            ValoresNuevos = System.Text.Json.JsonSerializer.Serialize(new
            {
                EmpresaId = empresa.Id,
                AdministradorUsuarioId = administradorId,
                RolEmpresaId = rolEmpresaId,
                ConfigEmpresaId = configEmpresa.Id,
                UsuarioCreadoEnAlta = nuevo is not null
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
            administradorUsuarioId = administradorId,
            // El alta no implica que las operaciones comerciales estén
            // certificadas ni habilitadas; el middleware permanece vigente.
            operacionComercialHabilitada = false
        });
    }
}

public sealed class AltaEmpresaPlataformaRequest
{
    public string Nombre { get; set; } = string.Empty;
    // Modalidad legacy intacta, mutuamente excluyente con AdministradorNuevo.
    public int AdministradorUsuarioId { get; set; }
    public AdministradorNuevoEmpresaRequest? AdministradorNuevo { get; set; }
}

public sealed class AdministradorNuevoEmpresaRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class BorradorEmpresaPlataformaRequest
{
    public string Nombre { get; set; } = string.Empty;
}
