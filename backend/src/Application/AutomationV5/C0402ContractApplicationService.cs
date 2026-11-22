using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>Future authoritative server actor lookup, not a JWT validator by itself.</summary>
public interface IC0402VerifiedContractActorSource
{
    ValueTask<C0402ContractApplicationService.VerifiedServerActor?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Future authoritative tenant-filtered persisted contract and commercial policy.
/// Client-provided tenant, prices, permissions or policy flags must never be trusted.
/// No MySQL adapter is installed by this application-only microtask.
/// </summary>
public interface IC0402VerifiedContractSnapshotSource
{
    ValueTask<C0402ContractApplicationService.VerifiedContractContext?> ReadOwnedAsync(
        int authenticatedTenantId, long contractIdHint, CancellationToken cancellationToken);
}

/// <summary>
/// C04-02.M03.02: fail-closed application preflight, no DI registration, API,
/// JWT creation, persistent transaction, legal approval, charge or refund.
/// Caller must revalidate live authority, contract version and commercial rules
/// atomically in a later independently authorized runtime integration.
/// </summary>
public sealed class C0402ContractApplicationService
{
    private readonly IC0402VerifiedContractActorSource _actors;
    private readonly IC0402VerifiedContractSnapshotSource _contracts;

    public C0402ContractApplicationService(
        IC0402VerifiedContractActorSource actors,
        IC0402VerifiedContractSnapshotSource contracts)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
    }

    public sealed record ClientIntent(
        long ContractIdHint, int? TenantHint, long ExpectedVersion,
        Rules.ContractOperation Operation, DateTime EffectiveUtc, string Cause,
        bool RequestsDirectMutation = false, bool RequestsPaymentOrRefund = false,
        bool SuppliesRoleOrPolicyAuthority = false);

    public sealed record VerifiedServerActor(
        Rules.VerifiedActor Scope, bool TrustedServerSource,
        bool SignedJwtEstablished, bool RelationalRbacFresh, bool MembershipFresh);

    public sealed record VerifiedContractContext(
        Rules.ContractSnapshot Contract, Rules.CommercialEvidence CommercialPolicy,
        bool ServerTenantFiltered, bool ResourceOwnedByActorTenant,
        bool SnapshotVersionFresh, bool PolicyProvenanceVerified);

    public readonly record struct Preview(string Code, bool IsDomainCandidateOnly)
    {
        public bool AuthorizesContractMutation => false;
        public bool AuthorizesPaymentOrRefund => false;
        public bool AuthorizesPrivateRead => false;
        public bool CertifiesJwtHttpMysql => false;
        public bool PersistsData => false;
    }

    public async ValueTask<Preview> PrepareAsync(
        ClientIntent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || intent.ContractIdHint <= 0 ||
            intent.ExpectedVersion <= 0 || intent.TenantHint is <= 0 ||
            intent.EffectiveUtc.Kind != DateTimeKind.Utc ||
            string.IsNullOrWhiteSpace(intent.Cause) || intent.Cause.Length > 160 ||
            intent.Cause.Any(char.IsControl) ||
            !Enum.IsDefined(intent.Operation))
            return Deny("CLIENT_CONTRACT_INTENT_INVALID");

        if (intent.RequestsDirectMutation || intent.RequestsPaymentOrRefund ||
            intent.SuppliesRoleOrPolicyAuthority)
            return Deny("CLIENT_AUTHORITY_OR_SIDE_EFFECT_DENIED");

        VerifiedServerActor? actor;
        try
        {
            actor = await _actors.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("SERVER_ACTOR_SOURCE_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        var scope = actor?.Scope;
        if (scope is null || scope.EmpresaId <= 0 || scope.ActorId <= 0 ||
            !actor!.TrustedServerSource || !actor.SignedJwtEstablished ||
            !actor.RelationalRbacFresh || !actor.MembershipFresh ||
            !scope.JwtVerified || !scope.MembershipActive ||
            !scope.ExplicitContractRbac || scope.PermissionRevoked)
            return Deny("LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED");

        if (intent.TenantHint.HasValue && intent.TenantHint != scope.EmpresaId)
            return Deny("CLIENT_TENANT_HINT_MISMATCH");

        VerifiedContractContext? context;
        try
        {
            context = await _contracts.ReadOwnedAsync(
                scope.EmpresaId, intent.ContractIdHint, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("SERVER_CONTRACT_SOURCE_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (context is null || context.Contract is null || context.CommercialPolicy is null ||
            !context.ServerTenantFiltered || !context.ResourceOwnedByActorTenant ||
            !context.SnapshotVersionFresh || !context.PolicyProvenanceVerified ||
            context.Contract.EmpresaId != scope.EmpresaId ||
            context.Contract.ContractId != intent.ContractIdHint ||
            context.Contract.Version != intent.ExpectedVersion)
            return Deny("VERIFIED_OWNED_CONTRACT_VERSION_AND_POLICY_REQUIRED");

        // An actor may be revoked or moved to another tenant while the trusted
        // contract snapshot source awaits MySQL/IO. A successful first read
        // does not certify authority at the time of the eventual preview.
        // This second check is a fail-closed preflight, NOT atomic authorization
        // for money, contract mutation, or an external production operation.
        VerifiedServerActor? refreshedActor;
        try
        {
            refreshedActor = await _actors.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("SERVER_ACTOR_SOURCE_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();
        var refreshedScope = refreshedActor?.Scope;
        if (refreshedScope is null ||
            refreshedScope.EmpresaId != scope.EmpresaId ||
            refreshedScope.ActorId != scope.ActorId ||
            !refreshedActor!.TrustedServerSource ||
            !refreshedActor.SignedJwtEstablished ||
            !refreshedActor.RelationalRbacFresh ||
            !refreshedActor.MembershipFresh ||
            !refreshedScope.JwtVerified ||
            !refreshedScope.MembershipActive ||
            !refreshedScope.ExplicitContractRbac ||
            refreshedScope.PermissionRevoked)
            return Deny("LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED");

        var request = new Rules.TransitionRequest(
            intent.ContractIdHint, scope.EmpresaId, intent.ExpectedVersion,
            intent.Operation, intent.EffectiveUtc, intent.Cause);

        var decision = Rules.Evaluate(scope, context.Contract, request,
            context.CommercialPolicy);

        // Even with preconditions fulfilled, no side effects are ever authorized.
        return decision.Code is
            "DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED" or
            "RENEWAL_PROPOSAL_ONLY_NO_EFFECT"
            ? new Preview("REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED", true)
            : Deny(decision.Code);
    }

    private static Preview Deny(string code) => new(code, false);
}
