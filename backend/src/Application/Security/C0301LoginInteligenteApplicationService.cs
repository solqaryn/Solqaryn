using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;

namespace Solqaryn.Application.Security;

/// <summary>Read a preference only from a trusted, user-bound server source.</summary>
public interface IC0301ServerTenantPreferenceReader
{
    Task<int?> ReadLastTenantIdAsync(int userId, CancellationToken cancellationToken);
}

/// <summary>
/// C03-01.M03.02 Application login preflight: verify credentials once, load
/// current backend memberships, enterprise and tenant roles, then resolve 0/1/N.
/// Not registered in DI/API. No JWT, session, RBAC grant or tenant access issued.
/// Subsequent endpoint and JWT issuance require fresh server enforcement.
/// </summary>
public sealed class C0301LoginInteligenteApplicationService
{
    private readonly IUsuarioRepository _users;
    private readonly IEmpresaRepository _companies;
    private readonly IRolRepository _roles;
    private readonly IC0301ServerTenantPreferenceReader _preferences;

    public C0301LoginInteligenteApplicationService(
        IUsuarioRepository users, IEmpresaRepository companies,
        IRolRepository roles, IC0301ServerTenantPreferenceReader preferences)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _companies = companies ?? throw new ArgumentNullException(nameof(companies));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    public sealed record Result(string Code, int? CandidateTenantId, IReadOnlyList<int> ValidTenantIds)
    {
        public bool IssuesJwt => false;
        public bool GrantsTenantAccess => false;
        public bool GrantsPlatformAccess => false;
    }

    private static Result Denied(string code) => new(code, null, Array.Empty<int>());

    public async Task<Result> PrepareAsync(
        string? username, string? password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var name = username?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256 ||
            string.IsNullOrEmpty(password) || password.Length > 1024)
            return Denied("INVALID_CREDENTIALS");

        try
        {
            var original = await _users.GetByNombreUsuarioAsync(name);
            cancellationToken.ThrowIfCancellationRequested();
            if (original is null || original.Id <= 0 || !original.Activo ||
                original.Bloqueado || original.Eliminado)
                return Denied("INVALID_CREDENTIALS");

            // AuthService.LoginAsync emits a legacy JWT before tenant selection.
            // Verify BCrypt exactly once here and do NOT emit any token.
            bool verified;
            try { verified = BCrypt.Net.BCrypt.Verify(password, original.PasswordHash); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            { return Denied("INVALID_CREDENTIALS"); }
            if (!verified)
                return Denied("INVALID_CREDENTIALS");

            var current = await _users.GetByIdAsync(original.Id);
            cancellationToken.ThrowIfCancellationRequested();
            if (current is null || current.Id != original.Id ||
                current.PasswordHash != original.PasswordHash ||
                !current.Activo || current.Bloqueado || current.Eliminado)
                return Denied("ACCOUNT_NO_LONGER_CURRENT");

            var memberships = await _users.GetEmpresasAsync(current.Id);
            cancellationToken.ThrowIfCancellationRequested();
            if (memberships is null)
                return Denied("CURRENT_MEMBERSHIPS_UNAVAILABLE");

            var snapshot = new List<C0301LoginTenantResolutionDomainRules.Membership>();
            foreach (var member in memberships)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member is null || member.UsuarioId != current.Id ||
                    member.EmpresaId <= 0 || member.RolId <= 0)
                    return Denied("FOREIGN_OR_INVALID_MEMBERSHIP_SNAPSHOT");

                Empresa? company = await _companies.GetByIdAsync(
                    member.EmpresaId, cancellationToken);
                Rol? role = await _roles.GetByIdAsync(member.RolId);
                cancellationToken.ThrowIfCancellationRequested();
                snapshot.Add(new C0301LoginTenantResolutionDomainRules.Membership(
                    current.Id, member.EmpresaId,
                    current.Activo && !current.Bloqueado && !current.Eliminado,
                    company is not null && company.Id == member.EmpresaId &&
                        company.Activa && !company.Eliminado,
                    member.Activa,
                    role is not null && role.Id == member.RolId &&
                        role.EsAsignableAEmpresa(member.EmpresaId),
                    true));
            }

            // Freeze authority before another I/O boundary; tracked entities are mutable.
            var originalPasswordHash = current.PasswordHash;
            var originalMembershipKeys = memberships
                .Select(m => (m.UsuarioId, m.EmpresaId, m.RolId, m.Activa))
                .OrderBy(m => m.EmpresaId).ThenBy(m => m.RolId).ToArray();

            // Preference is NEVER taken from a caller-supplied tenant override.
            var preferred = await _preferences.ReadLastTenantIdAsync(
                current.Id, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var result = C0301LoginTenantResolutionDomainRules.Resolve(new(
                AuthenticatedUserId: current.Id,
                Memberships: snapshot,
                LastPreferredTenantId: preferred,
                PreferenceOwnerUserId: current.Id,
                IdentityVerifiedByServer: true,
                AccountLockedOrRevoked: false,
                PlatformAuthorityVerified: false,
                ExplicitPlatformConsoleGrantVerified: false,
                ClientTenantOverrideAttempted: false));
            // A preference await can race revocation and tenant/role reassignment.
            // Re-evaluate the complete evidence. This preflight never issues a JWT.
            var latestUser = await _users.GetByIdAsync(current.Id);
            var latestMembers = await _users.GetEmpresasAsync(current.Id);
            cancellationToken.ThrowIfCancellationRequested();
            if (latestUser is null || latestUser.Id != current.Id ||
                latestUser.PasswordHash != originalPasswordHash ||
                !latestUser.Activo || latestUser.Bloqueado || latestUser.Eliminado ||
                latestMembers is null || latestMembers.Any(m => m is null ||
                    m.UsuarioId != current.Id || m.EmpresaId <= 0 || m.RolId <= 0))
                return Denied("MEMBERSHIP_OR_ROLE_CHANGED_REAUTH_REQUIRED");

            var latestKeys = latestMembers
                .Select(m => (m.UsuarioId, m.EmpresaId, m.RolId, m.Activa))
                .OrderBy(m => m.EmpresaId).ThenBy(m => m.RolId).ToArray();
            if (!originalMembershipKeys.SequenceEqual(latestKeys))
                return Denied("MEMBERSHIP_OR_ROLE_CHANGED_REAUTH_REQUIRED");

            var latestEvidence = new List<C0301LoginTenantResolutionDomainRules.Membership>();
            foreach (var member in latestMembers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Empresa? company = await _companies.GetByIdAsync(
                    member.EmpresaId, cancellationToken);
                Rol? role = await _roles.GetByIdAsync(member.RolId);
                cancellationToken.ThrowIfCancellationRequested();
                latestEvidence.Add(new C0301LoginTenantResolutionDomainRules.Membership(
                    latestUser.Id, member.EmpresaId,
                    latestUser.Activo && !latestUser.Bloqueado && !latestUser.Eliminado,
                    company is not null && company.Id == member.EmpresaId &&
                        company.Activa && !company.Eliminado,
                    member.Activa,
                    role is not null && role.Id == member.RolId &&
                        role.EsAsignableAEmpresa(member.EmpresaId),
                    true));
            }
            if (!snapshot.OrderBy(m => m.TenantId).SequenceEqual(
                    latestEvidence.OrderBy(m => m.TenantId)))
                return Denied("MEMBERSHIP_OR_ROLE_CHANGED_REAUTH_REQUIRED");

            return new Result(result.Code, result.CandidateTenantId, result.ValidTenantIds);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No account enumeration or SQL/error details in returned result.
            return Denied("AUTHENTICATION_OR_MEMBERSHIP_SOURCE_UNAVAILABLE");
        }
    }
}
