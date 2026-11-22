using System.Data;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C01-03.M03.06 V24: real, dev-only, opt-in audit for an existing JWT/RBAC
/// tenant configuration GET. Denies actor, tenant or grant from client hints.
/// A durable receipt and readback are REQUIRED; no configuration write granted.
/// </summary>
public sealed class C0103DurableTenantConfigurationAudit(
    AppDbContext database,
    ICurrentUserService current,
    IUsuarioScopeService scopes,
    C0103DynamicConfigurationAuditLogger logger)
{
    public async Task<bool> TryRecordReadAsync(
        int tenantId, long version, string? sourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!current.EstaAutenticado || current.UsuarioId is not int actorId ||
            actorId <= 0 || tenantId <= 0 || version <= 0 ||
            sourceSha is not { Length: 40 } ||
            sourceSha.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            return false;
        try
        {
            await using var transaction = await database.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var authority = await scopes.ObtenerActualAsync(tenantId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (authority is null || authority.UsuarioId != actorId ||
                authority.EmpresaId != tenantId || authority.RolId <= 0 ||
                !current.EstaAutenticado || current.UsuarioId != actorId)
                return false;

            // Fresh SQL user, membership, tenant, role AND permission check.
            var granted = await (
                from membership in database.UsuarioEmpresas.AsNoTracking()
                join user in database.Usuarios.AsNoTracking()
                    on membership.UsuarioId equals user.Id
                join company in database.Set<Empresa>().AsNoTracking()
                    on membership.EmpresaId equals company.Id
                join role in database.Roles.AsNoTracking()
                    on membership.RolId equals role.Id
                join grant in database.RolPermisos.AsNoTracking()
                    on role.Id equals grant.RolId
                join permission in database.Set<Permiso>().AsNoTracking()
                    on grant.PermisoId equals permission.Id
                where membership.UsuarioId == actorId &&
                    membership.EmpresaId == tenantId &&
                    membership.RolId == authority.RolId && membership.Activa &&
                    user.Activo && !user.Eliminado && !user.Bloqueado &&
                    company.Activa && role.Activo && !role.Eliminado &&
                    role.Ambito == AmbitoAutorizacion.Empresa &&
                    (role.EmpresaId == tenantId ||
                     (role.EmpresaId == null && role.EsSistema)) &&
                    permission.Activo && !permission.Eliminado &&
                    permission.Ambito == AmbitoAutorizacion.Empresa &&
                    permission.Modulo == ModuloSistema.Configuracion &&
                    permission.Accion == AccionPermiso.Ver
                select grant.Id).AnyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!granted || !current.EstaAutenticado || current.UsuarioId != actorId)
                return false;

            var stored = await database.Set<ConfigEmpresa>().AsNoTracking()
                .AnyAsync(x => x.EmpresaId == tenantId && x.Version == version,
                    cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!stored || current.UsuarioId != actorId) return false;

            var correlationId = Guid.NewGuid();
            var receipt = new RegistroAuditoria {
                UsuarioId=actorId, EmpresaId=tenantId,
                NombreUsuario="C0103_verified_actor",
                Modulo=ModuloSistema.Configuracion,
                Accion=AccionPermiso.Ver,
                Entidad="ConfigEmpresa", ReferenciaId=tenantId,
                Descripcion="C0103 authenticated config read; version=" +
                    version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CorrelationId=correlationId.ToString("N"), Resultado="Exito"
            };
            database.Set<RegistroAuditoria>().Add(receipt);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            var readback = await database.Set<RegistroAuditoria>()
                .IgnoreQueryFilters().AsNoTracking().AnyAsync(x =>
                    x.Id == receipt.Id && x.EmpresaId == tenantId &&
                    x.UsuarioId == actorId &&
                    x.CorrelationId == receipt.CorrelationId &&
                    x.Modulo == ModuloSistema.Configuracion &&
                    x.Accion == AccionPermiso.Ver && x.Resultado == "Exito",
                    cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!readback) return false;

            // Logging only after SQL receipt. Disabled providers do not
            // produce a false positive; no raw key or secret is emitted.
            return logger.TryWrite(new C0103DynamicConfigurationAuditLogger.AuditEvent(
                actorId, tenantId, "tenant_operational", sourceSha, version,
                "CONFIG_READ_PREFLIGHT", "CHECKED", correlationId));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }
}
