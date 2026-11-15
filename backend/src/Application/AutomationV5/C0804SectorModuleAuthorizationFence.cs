using Rules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>Only the trusted backend can provide the current actor and relational permissions.</summary>
public interface IC0804CurrentActorSource
{
    ValueTask<C0804SectorModuleAuthorizationFence.ActorEvidence?> ReadCurrentAsync(CancellationToken ct);
}
/// <summary>A future backend adapter must scope reads by authenticated tenant and exact module.</summary>
public interface IC0804TenantModuleSource
{
    ValueTask<C0804SectorModuleAuthorizationFence.ModuleEvidence?> ReadAsync(
        string authenticatedTenantId, string exactModuleKey, CancellationToken ct);
}
/// <summary>
/// C08-04.M03.03: isolated, read-only authorization fence for sector modules.
/// No endpoint, DI registration, entitlement, DB mutation, provider call, or actual activation.
/// </summary>
public sealed class C0804SectorModuleAuthorizationFence
{
    private readonly IC0804CurrentActorSource _actor;
    private readonly IC0804TenantModuleSource _module;
    public C0804SectorModuleAuthorizationFence(
        IC0804CurrentActorSource actor, IC0804TenantModuleSource module)
    {
        _actor = actor ?? throw new ArgumentNullException(nameof(actor));
        _module = module ?? throw new ArgumentNullException(nameof(module));
    }
    public sealed record Intent(string? ModuleKey, string? TenantAssertion,
        string? ExpectedSourceSha, Rules.Operation Operation,
        long ExpectedRevision, string? IdempotencyKey,
        bool ClientClaimsPermissions = false, bool ClientRequestsGlobalMutation = false);

    public sealed record ActorEvidence(string? TenantId,
        bool CurrentJwtVerified, bool MembershipCurrent, bool RbacGrantCurrent,
        bool TrustedBackendSource, Rules.VerifiedActorAndPolicy? Policy);

    public sealed record ModuleEvidence(string? ModuleKey, string? TenantOwner,
        string? SourceSha, Rules.State State, long Revision,
        bool PersistedTenantReadVerified, bool AuditFenceAvailable);

    public readonly record struct Result(string Code)
    {
        public bool NeedsFutureAuthenticatedCasAdapter =>
            Code == "AUTHENTICATED_MODULE_CAS_ADAPTER_REQUIRED";
        public bool GrantsAccess => false;
        public bool ActivatesModule => false;
        public bool WritesToDatabase => false;
        public bool AuthorizesRelease => false;
    }

    public async ValueTask<Result> InspectAsync(Intent? intent, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (intent is null || string.IsNullOrWhiteSpace(intent.ModuleKey) ||
            intent.ModuleKey.Length > 96 || string.IsNullOrWhiteSpace(intent.TenantAssertion) ||
            intent.ExpectedSourceSha is not { Length: 40 } ||
            !intent.ExpectedSourceSha.All(Uri.IsHexDigit) ||
            intent.ExpectedRevision <= 0 || !Enum.IsDefined(intent.Operation))
            return new("VALID_MODULE_INTENT_REQUIRED");
        if (intent.ClientClaimsPermissions || intent.ClientRequestsGlobalMutation)
            return new("UNTRUSTED_CLIENT_PRIVILEGE_DENIED");

        ActorEvidence? actor;
        try { actor = await _actor.ReadCurrentAsync(ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("CURRENT_ACTOR_SOURCE_UNAVAILABLE"); }

        var policy = actor?.Policy;
        if (actor is null || policy is null || !actor.TrustedBackendSource ||
            !actor.CurrentJwtVerified || !actor.MembershipCurrent || !actor.RbacGrantCurrent ||
            !policy.AuthenticatedJwtVerifiedByBackend ||
            !policy.CurrentMembershipVerified || !policy.CurrentExplicitRelationalRbacGrant ||
            policy.UserOrTenantOrRoleRevoked || string.IsNullOrWhiteSpace(actor.TenantId) ||
            !string.Equals(actor.TenantId, policy.VerifiedTenantId, StringComparison.Ordinal))
            return new("CURRENT_BACKEND_IDENTITY_AND_RBAC_REQUIRED");

        if (!string.Equals(intent.TenantAssertion, actor.TenantId, StringComparison.Ordinal))
            return new("CROSS_TENANT_CLIENT_ASSERTION_DENIED");

        ct.ThrowIfCancellationRequested();
        ModuleEvidence? module;
        try { module = await _module.ReadAsync(actor.TenantId!, intent.ModuleKey!, ct); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return new("TENANT_MODULE_SOURCE_UNAVAILABLE"); }

        if (module is null || !module.PersistedTenantReadVerified ||
            !module.AuditFenceAvailable ||
            !string.Equals(module.TenantOwner, actor.TenantId, StringComparison.Ordinal) ||
            !string.Equals(module.ModuleKey, intent.ModuleKey, StringComparison.Ordinal) ||
            !string.Equals(module.SourceSha, intent.ExpectedSourceSha, StringComparison.Ordinal) ||
            module.Revision != policy.CurrentModuleRevision ||
            !string.Equals(policy.ResourceOwnerTenantId, module.TenantOwner, StringComparison.Ordinal) ||
            !string.Equals(policy.CurrentCapabilityKey, module.ModuleKey, StringComparison.Ordinal))
            return new("TRUSTED_TENANT_MODULE_SNAPSHOT_REQUIRED");

        var evidence = new C0804SectorModuleApplicationGate.VerifiedEvidence(
            module.SourceSha, module.ModuleKey, actor.TenantId, module.TenantOwner,
            true, true, true, module.AuditFenceAvailable, module.State, policy);
        var gate = new C0804SectorModuleApplicationGate(new StaticEvidenceReader(evidence));
        var result = await gate.EvaluateAsync(
            new C0804SectorModuleApplicationGate.Request(
                intent.ModuleKey, intent.TenantAssertion, intent.ExpectedSourceSha,
                intent.Operation, intent.ExpectedRevision, intent.IdempotencyKey), ct);

        // Even an accepted preflight must be revalidated inside a real backend
        // transaction with optimistic CAS, fresh roles, audit and independent A5.
        return result.RequiresAuthenticatedMutationAdapter
            ? new("AUTHENTICATED_MODULE_CAS_ADAPTER_REQUIRED")
            : new(result.Code);
    }

    private sealed class StaticEvidenceReader(
        C0804SectorModuleApplicationGate.VerifiedEvidence evidence)
        : IC0804SectorModuleEvidenceReader
    {
        public ValueTask<C0804SectorModuleApplicationGate.VerifiedEvidence?> ReadAsync(
            string moduleKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ValueTask.FromResult<C0804SectorModuleApplicationGate.VerifiedEvidence?>(evidence);
        }
    }
}
