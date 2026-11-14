using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Implements short-lived support access. Every grant, use, denial and revocation
/// is audited durably. Authorization is re-read from the platform role on each call.
/// </summary>
public sealed class PlataformaSoporteSessionService : IPlataformaSoporteSessionService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IPlataformaScopeService _platform;
    private readonly DbContextOptions<AppDbContext> _contextOptions;
    private IDbContextTransaction? _activeReadTransaction;

    public PlataformaSoporteSessionService(
        AppDbContext db,
        ICurrentUserService current,
        IPlataformaScopeService platform,
        DbContextOptions<AppDbContext> contextOptions)
    {
        _db = db;
        _current = current;
        _platform = platform;
        _contextOptions = contextOptions;
    }

    public async Task<PlatformSupportOperationResult> CreateAsync(
        int empresaId,
        ModuloSistema modulo,
        string? motivo,
        int duracionMinutos,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        var actor = await _platform.ObtenerActualAsync(cancellationToken);
        if (actor is null || !_current.EstaAutenticado ||
            _current.UsuarioId != actor.UsuarioId)
            return await DenyOperationAsync(
                "PLATFORM_AUTHORITY_REQUIRED", 403, empresaId, modulo,
                motivo, audit, cancellationToken);

        var reason = motivo?.Trim();
        if (empresaId <= 0 || !PlatformSupportPolicy.IsAllowedModule(modulo) ||
            reason is null || reason.Length is < 10 or > 500 ||
            duracionMinutos is < 1 or > 30)
            return await DenyOperationAsync(
                "INVALID_SUPPORT_SCOPE", 400, empresaId, modulo,
                reason, audit, cancellationToken, actor);

        try
        {
            var company = await _db.Set<Empresa>().IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Id == empresaId && x.Activa)
                .Select(x => x.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (company <= 0)
                return await DenyOperationAsync(
                    "ACTIVE_COMPANY_REQUIRED", 403, empresaId, modulo,
                    reason, audit, cancellationToken, actor);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(duracionMinutos);
            var token = CreateOpaqueToken();
            var session = new PlataformaSesionSoporte
            {
                OperadorUsuarioId = actor.UsuarioId,
                EmpresaId = empresaId,
                Modulo = modulo,
                Motivo = reason,
                TokenHash = HashToken(token),
                CreadaUtc = now,
                ExpiraUtc = expires,
                Version = 1
            };

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            _db.Set<PlataformaSesionSoporte>().Add(session);
            await _db.SaveChangesAsync(cancellationToken);
            _db.Set<RegistroAuditoria>().Add(CreateAudit(
                actor.UsuarioId, actor.NombreUsuario, empresaId, modulo,
                AccionPermiso.Crear, session.Id,
                "Creó una sesión temporal de soporte de solo lectura.",
                reason, audit, "Exito", null,
                AmbitoAutorizacion.Plataforma, OrigenAutorizacion.RolPlataforma));
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PlatformSupportOperationResult(
                true, 201, "SUPPORT_SESSION_CREATED", session.Id, token, expires);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportOperationResult(
                false, 503, "SUPPORT_SESSION_PERSISTENCE_UNAVAILABLE");
        }
    }

    public async Task<PlatformSupportOperationResult> RevokeAsync(
        int sessionId,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        var actor = await _platform.ObtenerActualAsync(cancellationToken);
        if (actor is null || !_current.EstaAutenticado ||
            _current.UsuarioId != actor.UsuarioId)
            return await DenyOperationAsync(
                "PLATFORM_AUTHORITY_REQUIRED", 403, null, null,
                null, audit, cancellationToken);

        if (sessionId <= 0)
            return await DenyOperationAsync(
                "INVALID_SUPPORT_SESSION", 400, null, null,
                null, audit, cancellationToken, actor);

        PlataformaSesionSoporte? session;
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            session = await FindSessionForUpdateAsync(sessionId, cancellationToken);
            if (session is null || session.RevocadaUtc is not null)
            {
                _db.Set<RegistroAuditoria>().Add(CreateAudit(
                    actor.UsuarioId, actor.NombreUsuario, null, SafeModule(null),
                    AccionPermiso.Desactivar, sessionId,
                    "Se rechazó una solicitud de revocación de sesión de soporte inexistente o ya revocada.",
                    null, audit, "Denegado", "SUPPORT_SESSION_NOT_ACTIVE",
                    AmbitoAutorizacion.Plataforma, OrigenAutorizacion.RolPlataforma));
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new PlatformSupportOperationResult(
                    false, 409, "SUPPORT_SESSION_NOT_ACTIVE", sessionId);
            }

            session.RevocadaUtc = DateTime.UtcNow;
            session.RevocadaPorUsuarioId = actor.UsuarioId;
            session.Version++;
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportOperationResult(
                false, 503, "SUPPORT_SESSION_REVOCATION_UNAVAILABLE", sessionId);
        }

        // Revocation is committed first. If its audit write fails, the session
        // remains revoked and the caller receives 503 rather than false success.
        try
        {
            _db.Set<RegistroAuditoria>().Add(CreateAudit(
                actor.UsuarioId, actor.NombreUsuario, session.EmpresaId, session.Modulo,
                AccionPermiso.Desactivar, session.Id,
                "Revocó una sesión temporal de soporte.",
                session.Motivo, audit, "Exito", null,
                AmbitoAutorizacion.Plataforma, OrigenAutorizacion.RolPlataforma));
            await _db.SaveChangesAsync(cancellationToken);
            return new PlatformSupportOperationResult(
                true, 200, "SUPPORT_SESSION_REVOKED", session.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportOperationResult(
                false, 503, "SUPPORT_SESSION_REVOKED_AUDIT_UNAVAILABLE", session.Id);
        }
    }

    public async Task<PlatformSupportReadResult> AuthorizeReadAsync(
        string? token,
        int? empresaId,
        ModuloSistema? modulo,
        bool routeIsExplicitRead,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default)
    {
        if (_activeReadTransaction is not null)
            return await DenyReadAsync(
                "SUPPORT_READ_ALREADY_ACTIVE", empresaId, modulo,
                null, audit, cancellationToken, null);

        var actor = await _platform.ObtenerActualAsync(cancellationToken);
        if (actor is null || !_current.EstaAutenticado ||
            _current.UsuarioId != actor.UsuarioId)
            return await DenyReadAsync(
                "PLATFORM_AUTHORITY_REQUIRED", empresaId, modulo,
                null, audit, cancellationToken, null);

        if (!routeIsExplicitRead || !HasValidTokenShape(token) ||
            empresaId is not > 0 || modulo is null ||
            !PlatformSupportPolicy.IsAllowedModule(modulo.Value))
            return await DenyReadAsync(
                "EXPLICIT_READ_SCOPE_REQUIRED", empresaId, modulo,
                actor, audit, cancellationToken, null);

        IDbContextTransaction? transaction = null;
        try
        {
            var tokenHash = HashToken(token!);
            transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            var session = await FindSessionByHashForUpdateAsync(tokenHash, cancellationToken);
            var now = DateTime.UtcNow;
            var activeCompany = await _db.Set<Empresa>().IgnoreQueryFilters()
                .AsNoTracking()
                .AnyAsync(x => x.Id == empresaId.Value && x.Activa, cancellationToken);

            var valid = session is not null &&
                        FixedHashMatches(session.TokenHash, tokenHash) &&
                        session.OperadorUsuarioId == actor.UsuarioId &&
                        session.EmpresaId == empresaId.Value &&
                        session.Modulo == modulo.Value &&
                        PlatformSupportPolicy.IsAllowedModule(session.Modulo) &&
                        session.RevocadaUtc is null &&
                        session.ExpiraUtc > now &&
                        activeCompany;

            // Persist the decision on an independent context before data is returned.
            // The request transaction keeps the session row locked until middleware
            // completes the read, so a concurrent revoke cannot overtake an in-flight GET.
            await using (var auditDb = new AppDbContext(_contextOptions))
            {
                auditDb.Set<RegistroAuditoria>().Add(CreateAudit(
                    actor.UsuarioId, actor.NombreUsuario,
                    activeCompany ? empresaId : null, modulo.Value,
                    AccionPermiso.Ver, session?.Id,
                    valid
                        ? "Autorizó una lectura mediante sesión temporal de soporte."
                        : "Se rechazó un intento de lectura con sesión temporal de soporte.",
                    valid ? session!.Motivo : null, audit,
                    valid ? "Exito" : "Denegado",
                    valid ? null : "SUPPORT_SCOPE_INVALID_OR_EXPIRED",
                    AmbitoAutorizacion.Plataforma, OrigenAutorizacion.RolPlataforma));
                await auditDb.SaveChangesAsync(cancellationToken);
            }

            if (!valid)
            {
                await transaction.RollbackAsync(cancellationToken);
                await transaction.DisposeAsync();
                transaction = null;
                return new PlatformSupportReadResult(
                    false, 403, "SUPPORT_SCOPE_INVALID_OR_EXPIRED");
            }

            _activeReadTransaction = transaction;
            transaction = null;
            return new PlatformSupportReadResult(
                true, 200, "SUPPORT_READ_AUTHORIZED", session!.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportReadResult(
                false, 503, "SUPPORT_AUDIT_UNAVAILABLE");
        }
        finally
        {
            if (transaction is not null)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { /* disposal still releases the provider transaction */ }
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task CompleteAuthorizedReadAsync(
        bool requestCompleted,
        CancellationToken cancellationToken = default)
    {
        var transaction = _activeReadTransaction;
        _activeReadTransaction = null;
        if (transaction is null)
            return;

        try
        {
            if (requestCompleted)
                await transaction.CommitAsync(cancellationToken);
            else
            {
                try { await transaction.RollbackAsync(cancellationToken); }
                catch { /* disposal still releases the provider transaction */ }
            }
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    private async Task<PlatformSupportOperationResult> DenyOperationAsync(
        string code,
        int statusCode,
        int? empresaId,
        ModuloSistema? modulo,
        string? motivo,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken,
        UsuarioPlataformaScopeActual? actor = null)
    {
        try
        {
            _db.Set<RegistroAuditoria>().Add(CreateAudit(
                actor?.UsuarioId ?? _current.UsuarioId,
                actor?.NombreUsuario ?? _current.NombreUsuario ?? "identidad-no-disponible",
                await ActiveCompanyOrNullAsync(empresaId, cancellationToken),
                SafeModule(modulo), AccionPermiso.Crear, null,
                "Se rechazó la creación de una sesión temporal de soporte.",
                motivo, audit, "Denegado", code,
                actor is null ? null : AmbitoAutorizacion.Plataforma,
                actor is null ? null : OrigenAutorizacion.RolPlataforma));
            await _db.SaveChangesAsync(cancellationToken);
            return new PlatformSupportOperationResult(false, statusCode, code);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportOperationResult(false, 503, "SUPPORT_AUDIT_UNAVAILABLE");
        }
    }

    private async Task<PlatformSupportReadResult> DenyReadAsync(
        string code,
        int? empresaId,
        ModuloSistema? modulo,
        UsuarioPlataformaScopeActual? actor,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken,
        int? sessionId)
    {
        try
        {
            _db.Set<RegistroAuditoria>().Add(CreateAudit(
                actor?.UsuarioId ?? _current.UsuarioId,
                actor?.NombreUsuario ?? _current.NombreUsuario ?? "identidad-no-disponible",
                await ActiveCompanyOrNullAsync(empresaId, cancellationToken),
                SafeModule(modulo), AccionPermiso.Ver, sessionId,
                "Se rechazó un intento de acceso de soporte fuera del alcance autorizado.",
                null, audit, "Denegado", code,
                actor is null ? null : AmbitoAutorizacion.Plataforma,
                actor is null ? null : OrigenAutorizacion.RolPlataforma));
            await _db.SaveChangesAsync(cancellationToken);
            return new PlatformSupportReadResult(false, 403, code);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new PlatformSupportReadResult(false, 503, "SUPPORT_AUDIT_UNAVAILABLE");
        }
    }

    private async Task<int?> ActiveCompanyOrNullAsync(
        int? empresaId, CancellationToken cancellationToken)
    {
        if (empresaId is not > 0)
            return null;

        return await _db.Set<Empresa>().IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Id == empresaId.Value && x.Activa)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<PlataformaSesionSoporte?> FindSessionForUpdateAsync(
        int sessionId, CancellationToken cancellationToken)
    {
        if (_db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
            return await _db.Set<PlataformaSesionSoporte>()
                .FromSqlInterpolated($"SELECT * FROM `PlataformaSesionesSoporte` WHERE `Id` = {sessionId} FOR UPDATE")
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);

        return await _db.Set<PlataformaSesionSoporte>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
    }

    private async Task<PlataformaSesionSoporte?> FindSessionByHashForUpdateAsync(
        string tokenHash, CancellationToken cancellationToken)
    {
        if (_db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
            return await _db.Set<PlataformaSesionSoporte>()
                .FromSqlInterpolated($"SELECT * FROM `PlataformaSesionesSoporte` WHERE `TokenHash` = {tokenHash} FOR UPDATE")
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(cancellationToken);

        return await _db.Set<PlataformaSesionSoporte>().IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    private static RegistroAuditoria CreateAudit(
        int? usuarioId,
        string nombreUsuario,
        int? empresaId,
        ModuloSistema modulo,
        AccionPermiso accion,
        int? referenceId,
        string description,
        string? reason,
        PlatformSupportAuditContext audit,
        string result,
        string? error,
        AmbitoAutorizacion? authority,
        OrigenAutorizacion? origin) => new()
    {
        UsuarioId = usuarioId,
        NombreUsuario = string.IsNullOrWhiteSpace(nombreUsuario)
            ? "identidad-no-disponible"
            : nombreUsuario[..Math.Min(nombreUsuario.Length, 150)],
        EmpresaId = empresaId,
        AmbitoAutoridad = authority,
        OrigenAutorizacion = origin,
        Modulo = modulo,
        Accion = accion,
        Entidad = nameof(PlataformaSesionSoporte),
        ReferenciaId = referenceId,
        Descripcion = description,
        Motivo = string.IsNullOrWhiteSpace(reason) ? null : reason[..Math.Min(reason.Length, 500)],
        Ip = string.IsNullOrWhiteSpace(audit.Ip) ? null : audit.Ip[..Math.Min(audit.Ip.Length, 64)],
        UserAgent = string.IsNullOrWhiteSpace(audit.UserAgent)
            ? null
            : audit.UserAgent[..Math.Min(audit.UserAgent.Length, 300)],
        CorrelationId = string.IsNullOrWhiteSpace(audit.CorrelationId)
            ? null
            : audit.CorrelationId[..Math.Min(audit.CorrelationId.Length, 64)],
        Resultado = result,
        Error = error
    };

    private static ModuloSistema SafeModule(ModuloSistema? modulo) =>
        modulo is not null && Enum.IsDefined(modulo.Value)
            ? modulo.Value
            : ModuloSistema.Dashboard;

    private static bool HasValidTokenShape(string? token) =>
        token is { Length: 43 } &&
        token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static string CreateOpaqueToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(token)))
            .ToLowerInvariant();

    private static bool FixedHashMatches(string persistedHash, string suppliedHash)
    {
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(persistedHash),
                Convert.FromHexString(suppliedHash));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}