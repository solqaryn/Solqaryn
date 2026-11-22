using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C02-01.M05.04: live read-only membership input for the intelligent-login
/// decision. Requires an already authenticated backend principal and reuses
/// the current relational user/membership/role/tenant authorization service.
/// This reader NEVER issues JWTs, installs a tenant context or saves preference.
/// It is not registered in production DI until the reissue and routing flow
/// receives its own real end-to-end security proof.
/// </summary>
public sealed class C0201M0504CurrentMembershipSqlReader(
    AppDbContext database,
    ICurrentUserService currentUser,
    IUsuarioScopeService scope) : ITrustedTenantLoginAuthorityReader
{
    public async ValueTask<Gate.TrustedSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!currentUser.EstaAutenticado ||
            currentUser.UsuarioId is not int actorId || actorId <= 0)
            return null;

        // Claims identify the actor only. Never use the JWT role or a browser
        // selected tenant as evidence of effective membership.
        var active = await database.Usuarios.AsNoTracking().AnyAsync(
            user => user.Id == actorId && user.Activo &&
                    !user.Bloqueado && !user.Eliminado,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!active) return null;

        // The existing service performs the relational UsuarioEmpresa,
        // Empresa, Usuario and Rol checks using the current backend identity.
        // An exception remains a store failure and is denied by the Application
        // gate, rather than turning into a fallback tenant.
        var available = await scope.ListarEmpresasSeleccionablesAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (available is null || available.Count > 128) return null;

        // Membership resolution re-reads the principal after an await. Never
        // bind its companies to the original actor if identity has changed.
        if (!currentUser.EstaAutenticado || currentUser.UsuarioId != actorId)
            return null;

        var seen = new HashSet<int>();
        var memberships = new List<Gate.Membership>(available.Count);
        foreach (var company in available)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (company.EmpresaId <= 0 ||
                string.IsNullOrWhiteSpace(company.Nombre) ||
                company.Nombre.Length > 120 ||
                company.Nombre.Any(char.IsControl) ||
                !seen.Add(company.EmpresaId))
                return null;

            memberships.Add(new Gate.Membership(
                actorId, company.EmpresaId, company.Nombre,
                FromPersistence: true, Active: true, TenantActive: true,
                CurrentRelationalRoleVerified: true, Revoked: false));
        }
        cancellationToken.ThrowIfCancellationRequested();
        // Guard again after enumerating a potentially custom/lazy scope list.
        if (!currentUser.EstaAutenticado || currentUser.UsuarioId != actorId)
            return null;

        // An initial list may become stale while relational membership, role or
        // tenant activity changes during an await. Re-read the canonical SQL
        // scope and reject any changed set/name, never propose a revoked tenant.
        // This is a bounded read-only check, not atomic JWT reissuance.
        var freshCompanies = await scope.ListarEmpresasSeleccionablesAsync(
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (freshCompanies is null || freshCompanies.Count != memberships.Count)
            return null;
        var expectedNames = memberships.ToDictionary(
            member => member.TenantId, member => member.DisplayName);
        var freshIds = new HashSet<int>();
        foreach (var company in freshCompanies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!freshIds.Add(company.EmpresaId) ||
                !expectedNames.TryGetValue(company.EmpresaId, out var name) ||
                !string.Equals(company.Nombre, name, StringComparison.Ordinal))
                return null;
        }

        // A current user can lose authority while membership SQL is awaited.
        // Revalidate the actor in the same database before returning a proposal.
        // This does not pretend that JWT reissue is atomic.
        var stillActive = await database.Usuarios.AsNoTracking().AnyAsync(
            user => user.Id == actorId && user.Activo &&
                    !user.Bloqueado && !user.Eliminado,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (!stillActive || !currentUser.EstaAutenticado ||
            currentUser.UsuarioId != actorId)
            return null;

        // A repeated, cached scope response is not a second authoritative SQL
        // read. Verify the COMPLETE current membership/role/tenant set directly
        // against Oracle EF/MySQL immediately before proposing a tenant.
        // A revoked role with a stale scope list MUST fail closed.
        var verifiedRows = await (
            from membership in database.UsuarioEmpresas.AsNoTracking()
            join tenant in database.Set<Empresa>().AsNoTracking()
                on membership.EmpresaId equals tenant.Id
            join role in database.Roles.AsNoTracking()
                on membership.RolId equals role.Id
            where membership.UsuarioId == actorId && membership.Activa &&
                  tenant.Activa && role.Activo && !role.Eliminado &&
                  role.Ambito == AmbitoAutorizacion.Empresa &&
                  (role.EmpresaId == tenant.Id ||
                   (role.EmpresaId == null && role.EsSistema))
            select new { membership.EmpresaId, tenant.Nombre }
        ).ToListAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (verifiedRows.Count != memberships.Count ||
            verifiedRows.Select(row => row.EmpresaId).Distinct().Count() !=
                verifiedRows.Count ||
            verifiedRows.Any(row =>
                !expectedNames.TryGetValue(row.EmpresaId, out var name) ||
                !string.Equals(row.Nombre, name, StringComparison.Ordinal)) ||
            !currentUser.EstaAutenticado || currentUser.UsuarioId != actorId)
            return null;

        // No persisted last-tenant preference or active session reissue is
        // available from this read-only reader. Never fabricate either.
        return new Gate.TrustedSnapshot(
            actorId, JwtValidatedByBackend: true, UserActive: true,
            AuthorityStoreAvailable: true,
            GlobalPrivilegeSeparatelyVerified: false,
            SessionValid: false, SessionTenantId: null,
            PreferredTenantId: null, PreferenceOwnerActorId: null,
            PreferenceFromPersistence: false, PreferenceRevoked: false,
            Memberships: memberships);
    }
}
