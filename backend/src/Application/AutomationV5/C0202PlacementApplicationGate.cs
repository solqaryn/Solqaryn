using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Trusted server-side evidence reader for C02-02.M03.02. An Infrastructure
/// implementation must retrieve current JWT/membership, persisted ownership,
/// binding catalog, revision and verified sandbox recovery evidence.
/// </summary>
public interface IC0202PlacementEvidenceReader
{
    ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
        string tenantId, CancellationToken cancellationToken);
}

/// <summary>
/// Non-authorizing shared/dedicated placement Application preflight.
/// Does not select a connection, perform migration or authorize cutover.
/// </summary>
public sealed class C0202PlacementApplicationGate
{
    private readonly IC0202PlacementEvidenceReader _reader;

    public C0202PlacementApplicationGate(IC0202PlacementEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Request(
        string? TenantId, string? SourceSha, DomainRules.Intent Intent,
        DomainRules.PlacementMode TargetMode, string? TargetBindingAlias,
        long ExpectedRevision, string? IdempotencyKey,
        bool ClientSuppliesDsn = false, bool RequestsLiveCutoverOrDualWrite = false,
        bool RequestsDestructiveSchema = false, bool CrossTenantFallback = false);

    public sealed record Evidence(
        string? SourceSha, string? VerifiedTenantId,
        string? PersistedOwnerTenantId, string? ApprovedTargetBindingAlias,
        bool TargetBindingFromTrustedCatalog, bool TargetOwnedByVerifiedTenant,
        bool VerifiedMigrationIntent, bool VerifiedRollbackTarget,
        bool VerifiedSandboxBackupRestore, bool VerifiedTenantConsistencyProof,
        bool CurrentJwtAndMembershipVerified, bool FreshRelationalRbacVerified,
        bool ServerTrustedReader, bool CurrentRevisionFenceVerified,
        DomainRules.TrustedPlacementSnapshot? Snapshot);

    public readonly record struct Result(string Code)
    {
        public bool RequiresSeparateAuthenticatedStorageAdapter =>
            Code == "AUTHENTICATED_STORAGE_ADAPTER_REQUIRED";
        public bool AuthorizesConnection => false;
        public bool AuthorizesMigration => false;
        public bool AuthorizesCutover => false;
        public bool AuthorizesProduction => false;
    }

    public async ValueTask<Result> EvaluateAsync(
        Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || !ValidSha(request.SourceSha) ||
            !ValidId(request.TenantId) || request.ExpectedRevision <= 0 ||
            !Enum.IsDefined(request.Intent) || !Enum.IsDefined(request.TargetMode))
            return new("INVALID_PLACEMENT_REQUEST_OR_SOURCE");

        if (request.ClientSuppliesDsn || request.RequestsLiveCutoverOrDualWrite ||
            request.RequestsDestructiveSchema || request.CrossTenantFallback)
            return new("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION");

        var evidence = await _reader.ReadAsync(request.TenantId!, cancellationToken);
        if (evidence is null ||
            !string.Equals(evidence.SourceSha, request.SourceSha, StringComparison.Ordinal) ||
            !string.Equals(evidence.VerifiedTenantId, request.TenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.PersistedOwnerTenantId, request.TenantId,
                StringComparison.Ordinal))
            return new("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED");

        if (!evidence.ServerTrustedReader ||
            !evidence.CurrentJwtAndMembershipVerified ||
            !evidence.FreshRelationalRbacVerified ||
            !evidence.CurrentRevisionFenceVerified ||
            evidence.Snapshot is null ||
            !string.Equals(evidence.Snapshot.VerifiedTenantId,
                evidence.VerifiedTenantId, StringComparison.Ordinal) ||
            !string.Equals(evidence.Snapshot.AssignmentOwnerTenantId,
                evidence.PersistedOwnerTenantId, StringComparison.Ordinal))
            return new("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED");

        // Only server-verified target binding and recovery evidence are passed
        // to Domain. A browser-supplied alias never selects a connection.
        if (request.Intent == DomainRules.Intent.ProposeMigration &&
            !string.Equals(request.TargetBindingAlias,
                evidence.ApprovedTargetBindingAlias, StringComparison.Ordinal))
            return new("TARGET_BINDING_NOT_SERVER_APPROVED");

        var candidate = new DomainRules.PlacementCandidate
        {
            Intent = request.Intent,
            RequestedTenantId = request.TenantId,
            TargetMode = request.TargetMode,
            TargetBindingAlias = evidence.ApprovedTargetBindingAlias,
            TargetBindingFromTrustedCatalog = evidence.TargetBindingFromTrustedCatalog,
            TargetOwnedByVerifiedTenant = evidence.TargetOwnedByVerifiedTenant,
            ExpectedAssignmentRevision = request.ExpectedRevision,
            UsesVerifiedMigrationIntent = evidence.VerifiedMigrationIntent,
            RollbackTargetVerified = evidence.VerifiedRollbackTarget,
            VerifiedSandboxBackupRestore = evidence.VerifiedSandboxBackupRestore,
            TenantDataConsistencyProofVerified = evidence.VerifiedTenantConsistencyProof,
            IdempotencyKey = request.IdempotencyKey
        };
        var verdict = DomainRules.Evaluate(evidence.Snapshot, candidate);
        if (!verdict.RequiresServerEnforcement)
            return new(verdict.Code);
        return new("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED");
    }

    private static bool ValidSha(string? value) =>
        value is { Length: 40 } && value.All(Uri.IsHexDigit);
    private static bool ValidId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => (c >= 'a' && c <= 'z') ||
                       (c >= 'A' && c <= 'Z') ||
                       (c >= '0' && c <= '9') || c is '-' or '_');
}
