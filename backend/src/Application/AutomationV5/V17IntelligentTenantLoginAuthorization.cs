namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Implement ONLY from the current authenticated backend session, membership
/// store and per-user preference. Never trust browser/route/host/role hints.
/// </summary>
public interface ITrustedTenantLoginAuthorityReader
{
    ValueTask<V17IntelligentTenantLoginAuthorization.TrustedSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// C02-01.M03.03: fail-closed Application decision for 0/1/N enterprise context.
/// This policy does not issue JWTs, authorize HTTP or install a tenant context.
/// </summary>
public sealed class V17IntelligentTenantLoginAuthorization
{
    private readonly ITrustedTenantLoginAuthorityReader _reader;

    public V17IntelligentTenantLoginAuthorization(ITrustedTenantLoginAuthorityReader reader)
        => _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Membership(
        int ActorId, int TenantId, string DisplayName,
        bool FromPersistence, bool Active, bool TenantActive,
        bool CurrentRelationalRoleVerified, bool Revoked);

    public sealed record TrustedSnapshot(
        int ActorId, bool JwtValidatedByBackend, bool UserActive,
        bool AuthorityStoreAvailable, bool GlobalPrivilegeSeparatelyVerified,
        bool SessionValid, int? SessionTenantId,
        int? PreferredTenantId, int? PreferenceOwnerActorId,
        bool PreferenceFromPersistence, bool PreferenceRevoked,
        IReadOnlyCollection<Membership>? Memberships);

    public sealed record Request(
        int? SelectedTenantId = null, bool SwitchingEnterprise = false,
        bool ClientClaimsGlobalRole = false, bool ClientClaimsMembership = false,
        bool ClientClaimsHostAuthority = false);

    public sealed record Choice(int TenantId, string DisplayName);

    public sealed record Decision(string Code, int? ProposedTenantId,
        IReadOnlyList<Choice> Choices, bool DiscardStalePreference = false)
    {
        public bool AuthorizesResourceAccess => false;
        public bool IssuesJwt => false;
        public bool InstallsTenantContext => false;
        public bool AuthorizesGlobalConsole => false;
        public bool RequiresAuthenticatedBackendReissue =>
            Code is "ONE_TENANT_REISSUE_REQUIRED" or "PREFERENCE_REISSUE_REQUIRED"
                or "CHOICE_REISSUE_REQUIRED" or "SWITCH_REISSUE_REQUIRED";
        public bool RequiresOldTenantCachePurge => Code == "SWITCH_REISSUE_REQUIRED";
    }

    private static Decision Deny(string reason, bool clear = false)
        => new(reason, null, Array.Empty<Choice>(), clear);

    public async ValueTask<Decision> ResolveAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.ClientClaimsGlobalRole ||
            request.ClientClaimsMembership || request.ClientClaimsHostAuthority)
            return Deny("CLIENT_OR_MISSING_AUTHORITY_DENIED");
        if (request.SelectedTenantId is <= 0)
            return Deny("INVALID_SELECTED_TENANT");
        if (request.SwitchingEnterprise && !request.SelectedTenantId.HasValue)
            return Deny("EXPLICIT_SWITCH_TARGET_REQUIRED");

        TrustedSnapshot? s;
        try { s = await _reader.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("CURRENT_AUTHORITY_STORE_UNAVAILABLE");
        }
        if (s is null || s.ActorId <= 0 || !s.JwtValidatedByBackend ||
            !s.UserActive || !s.AuthorityStoreAvailable || s.Memberships is null)
            return Deny("AUTHENTICATED_CURRENT_MEMBERSHIPS_REQUIRED");
        if (s.Memberships.Count > 128)
            return Deny("MEMBERSHIP_SNAPSHOT_CAPACITY_INVALID");

        var eligible = new List<Membership>();
        var seen = new HashSet<int>();
        foreach (Membership? m in s.Memberships)
        {
            if (m is null || m.ActorId != s.ActorId || m.TenantId <= 0)
                return Deny("CROSS_ACTOR_MEMBERSHIP_SNAPSHOT_DENIED");
            if (!m.FromPersistence || !m.Active || !m.TenantActive ||
                !m.CurrentRelationalRoleVerified || m.Revoked)
                continue;
            if (string.IsNullOrWhiteSpace(m.DisplayName) ||
                m.DisplayName.Length > 120 || m.DisplayName.Any(char.IsControl) ||
                !seen.Add(m.TenantId))
                return Deny("AMBIGUOUS_OR_INVALID_ACTIVE_MEMBERSHIP");
            eligible.Add(m);
        }

        if (s.SessionTenantId.HasValue &&
            (!s.SessionValid ||
             !eligible.Any(m => m.TenantId == s.SessionTenantId.Value)))
            return Deny("STALE_OR_REVOKED_SESSION_DENIED");
        if (request.SwitchingEnterprise &&
            (!s.SessionValid || !s.SessionTenantId.HasValue))
            return Deny("ACTIVE_SESSION_REQUIRED_TO_SWITCH");

        bool preferenceValid = s.PreferenceFromPersistence &&
            !s.PreferenceRevoked && s.PreferenceOwnerActorId == s.ActorId &&
            s.PreferredTenantId.HasValue &&
            eligible.Any(m => m.TenantId == s.PreferredTenantId.Value);
        bool discardPreference = s.PreferredTenantId.HasValue && !preferenceValid;

        if (eligible.Count == 0)
            return Deny(s.GlobalPrivilegeSeparatelyVerified
                ? "GLOBAL_CONSOLE_AUTHENTICATED_ADAPTER_REQUIRED"
                : "NO_AUTHORIZED_ENTERPRISE", discardPreference);

        if (request.SelectedTenantId.HasValue &&
            !eligible.Any(m => m.TenantId == request.SelectedTenantId.Value))
            return Deny("FOREIGN_OR_REVOKED_TENANT_DENIED", discardPreference);

        if (request.SwitchingEnterprise)
            return new("SWITCH_REISSUE_REQUIRED", request.SelectedTenantId,
                Array.Empty<Choice>(), discardPreference);

        if (s.SessionValid && s.SessionTenantId.HasValue)
        {
            if (request.SelectedTenantId.HasValue &&
                request.SelectedTenantId != s.SessionTenantId)
                return Deny("EXPLICIT_SWITCH_REQUIRED");
            return new("CURRENT_SESSION_BACKEND_CHECK_REQUIRED", s.SessionTenantId,
                Array.Empty<Choice>(), discardPreference);
        }

        if (request.SelectedTenantId.HasValue)
            return new("CHOICE_REISSUE_REQUIRED", request.SelectedTenantId,
                Array.Empty<Choice>(), discardPreference);
        if (eligible.Count == 1)
            return new("ONE_TENANT_REISSUE_REQUIRED", eligible[0].TenantId,
                Array.Empty<Choice>(), discardPreference);
        if (preferenceValid)
            return new("PREFERENCE_REISSUE_REQUIRED", s.PreferredTenantId,
                Array.Empty<Choice>());

        return new("SELECT_VERIFIED_TENANT", null,
            eligible.OrderBy(m => m.TenantId)
                .Select(m => new Choice(m.TenantId, m.DisplayName.Trim()))
                .ToArray(), discardPreference);
    }
}
