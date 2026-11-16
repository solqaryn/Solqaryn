using DomainRules = Solqaryn.Domain.AutomationV5.V5SectorModuleLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C08-04.M03.02: non-authorizing Application preflight.
/// The evidence reader must obtain fresh trusted backend identity, persisted
/// tenant ownership, current relational permission and audit/concurrency fence.
/// </summary>
public interface IC0804SectorModuleEvidenceReader
{
    ValueTask<C0804SectorModuleApplicationGate.VerifiedEvidence?> ReadAsync(
        string moduleKey, CancellationToken cancellationToken);
}

public sealed class C0804SectorModuleApplicationGate
{
    private readonly IC0804SectorModuleEvidenceReader _reader;

    public C0804SectorModuleApplicationGate(IC0804SectorModuleEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? ModuleKey, string? RequestedTenantId,
        string? ExpectedSourceSha, DomainRules.Operation Operation,
        long ExpectedRevision, string? IdempotencyKey,
        bool ClientControlsCatalogOrPermission = false,
        bool ClientControlsTenantOrResource = false,
        bool RequestsGlobalGrantOrSchemaMutation = false);

    public sealed record VerifiedEvidence(
        string? ObservedSourceSha, string? PersistedModuleKey,
        string? VerifiedTenantId, string? PersistedOwnerTenantId,
        bool CurrentBackendJwtVerified, bool CurrentMembershipAndRbacVerified,
        bool VerifiedFromTrustedBackend, bool AuditAndConcurrencyFenceAvailable,
        DomainRules.State CurrentState,
        DomainRules.VerifiedActorAndPolicy? Policy);

    public readonly record struct Result(string Code)
    {
        public bool RequiresAuthenticatedMutationAdapter =>
            Code == "TRUSTED_MUTATION_ADAPTER_REQUIRED";
        public bool AuthorizesModuleActivation => false;
        public bool AuthorizesTenantAccess => false;
        public bool WritesToDatabase => false;
        public bool AuthorizesRelease => false;
    }

    public async ValueTask<Result> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || string.IsNullOrWhiteSpace(request.ModuleKey) ||
            request.ModuleKey.Length > 96 || !ValidSha(request.ExpectedSourceSha) ||
            request.ExpectedRevision <= 0 || !Enum.IsDefined(request.Operation))
            return new("INVALID_REQUEST_OR_SOURCE_SHA");

        if (request.ClientControlsCatalogOrPermission ||
            request.ClientControlsTenantOrResource ||
            request.RequestsGlobalGrantOrSchemaMutation)
            return new("UNTRUSTED_PRIVILEGE_OR_SCHEMA_OVERRIDE");

        VerifiedEvidence? evidence;
        try
        {
            evidence = await _reader.ReadAsync(request.ModuleKey, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Untrusted evidence failures must not expose exception messages,
            // credentials, resource identifiers or partial authorization.
            return new("TRUSTED_MODULE_EVIDENCE_UNAVAILABLE");
        }
        if (evidence is null || !string.Equals(evidence.ObservedSourceSha,
                request.ExpectedSourceSha, StringComparison.Ordinal) ||
            !string.Equals(evidence.PersistedModuleKey, request.ModuleKey,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.VerifiedTenantId, request.RequestedTenantId,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.PersistedOwnerTenantId,
                evidence.VerifiedTenantId, StringComparison.Ordinal))
            return new("TRUSTED_MODULE_AND_TENANT_EVIDENCE_REQUIRED");

        if (!evidence.CurrentBackendJwtVerified ||
            !evidence.CurrentMembershipAndRbacVerified ||
            !evidence.VerifiedFromTrustedBackend ||
            !evidence.AuditAndConcurrencyFenceAvailable ||
            evidence.Policy is null ||
            !string.Equals(evidence.Policy.VerifiedTenantId,
                evidence.VerifiedTenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.Policy.ResourceOwnerTenantId,
                evidence.PersistedOwnerTenantId, StringComparison.Ordinal))
            return new("FRESH_AUTHORIZATION_AND_AUDIT_REQUIRED");

        var transition = new DomainRules.ProposedTransition
        {
            RequestedTenantId = request.RequestedTenantId,
            CapabilityKey = evidence.PersistedModuleKey,
            ServerAllowedCapabilityKey = evidence.Policy.CurrentCapabilityKey,
            CurrentState = evidence.CurrentState,
            Operation = request.Operation,
            ExpectedRevision = request.ExpectedRevision,
            IdempotencyKey = request.IdempotencyKey,
            RetainsHistoryAndOwnership = true
        };
        var verdict = DomainRules.Evaluate(evidence.Policy, transition);
        if (!verdict.RequiresApplicationEnforcement)
            return new(verdict.Code);

        return new("TRUSTED_MUTATION_ADAPTER_REQUIRED");
    }

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
}
