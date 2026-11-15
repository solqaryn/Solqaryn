using Solqaryn.Domain.AutomationV5;
using Guard = Solqaryn.Domain.AutomationV5.V5PrivateMySqlCutoverDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C12-04.M03.02: boundary for independent, private dev readiness review.
/// Callers MUST authenticate and authorize the actor server-side; none of these
/// parameter flags or an injected reader grants SQL, restore or cutover authority.
/// </summary>
public sealed record C1204VerifiedActor(
    string? ActorId,
    string? TenantId,
    bool JwtAuthenticatedByServer,
    bool MembershipCurrent,
    bool CanReviewCutover,
    bool RecentMfaVerified);

/// <summary>
/// A future Infrastructure adapter must independently verify the actual
/// origin, SHA, grants, CI logs and ephemeral MySQL proof represented here.
/// An in-memory fake used by a unit test is NOT external evidence.
/// </summary>
public sealed record C1204VerifiedEvidence(
    string ActorId,
    string TenantId,
    string CommitSha,
    Guard.TrustedSnapshot Snapshot);

public interface IC1204CutoverReadinessEvidenceReader
{
    Task<C1204VerifiedEvidence?> ReadVerifiedAsync(
        string trustedActorId,
        string trustedTenantId,
        string expectedCommitSha,
        CancellationToken cancellationToken);
}

public readonly record struct C1204ReadinessOutcome(string Code)
{
    public bool RequiresIndependentServerEnforcement =>
        Code == "TRUSTED_DEV_SANDBOX_REVIEW_REQUIRED";

    public bool AuthorizesSqlOrMigration => false;
    public bool AuthorizesCutover => false;
    public bool AuthorizesDeployment => false;
}

/// <summary>
/// No database, network connection, migration or user-facing endpoint is
/// registered by this service. Never accept client-provided VerifiedActor.
/// </summary>
public sealed class C1204PrivateMySqlReadinessApplicationService
{
    private readonly IC1204CutoverReadinessEvidenceReader _reader;

    public C1204PrivateMySqlReadinessApplicationService(
        IC1204CutoverReadinessEvidenceReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<C1204ReadinessOutcome> ReviewAsync(
        C1204VerifiedActor? trustedActor,
        Guard.Candidate? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (trustedActor is null || request is null ||
            string.IsNullOrWhiteSpace(trustedActor.ActorId) ||
            string.IsNullOrWhiteSpace(trustedActor.TenantId) ||
            !trustedActor.JwtAuthenticatedByServer ||
            !trustedActor.MembershipCurrent ||
            !trustedActor.CanReviewCutover ||
            !trustedActor.RecentMfaVerified)
            return new("VERIFIED_SERVER_ACTOR_REQUIRED");

        if (!string.Equals(request.Environment, "dev", StringComparison.Ordinal))
            return new("NON_DEV_ENVIRONMENT_DENIED");

        if (string.IsNullOrWhiteSpace(request.RequestedTenantId) ||
            !string.Equals(request.RequestedTenantId, trustedActor.TenantId,
                StringComparison.Ordinal) ||
            !request.SourceAndDestinationOwnedByTenant)
            return new("TENANT_OWNERSHIP_UNVERIFIED");

        if (request.ClientControlsConnectionOrTarget ||
            request.RequestsRealCutoverOrDualWrite ||
            request.RequestsDestructiveSchemaMutation ||
            request.RequestsGrantElevation ||
            request.RequestsSecretOrPersonalDataEmission ||
            request.AllowsCrossTenantFallback)
            return new("UNAUTHORIZED_RUNTIME_MUTATION_DENIED");

        if (request.HasUnmitigatedP0OrP1)
            return new("CRITICAL_FINDING_NO_GO");

        if (request.ExpectedCommitSha is not { Length: 40 } ||
            !request.ExpectedCommitSha.All(Uri.IsHexDigit) ||
            !string.Equals(request.ExpectedCommitSha, request.ManifestCommitSha,
                StringComparison.Ordinal))
            return new("EXACT_MANIFEST_SHA_REQUIRED");

        // No trusted evidence reader: no review. Never infer a pass from
        // the request, the caller's booleans or an application-side fallback.
        var evidence = await _reader.ReadVerifiedAsync(
            trustedActor.ActorId, trustedActor.TenantId,
            request.ExpectedCommitSha, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (evidence is null)
            return new("EVIDENCE_NOT_VERIFIED");

        if (!string.Equals(evidence.ActorId, trustedActor.ActorId,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.TenantId, trustedActor.TenantId,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.CommitSha, request.ExpectedCommitSha,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.Snapshot.AuthenticatedActorId, trustedActor.ActorId,
                StringComparison.Ordinal) ||
            !string.Equals(evidence.Snapshot.VerifiedTenantId, trustedActor.TenantId,
                StringComparison.Ordinal))
            return new("EVIDENCE_PROVENANCE_MISMATCH");

        // Do not overwrite missing evidence flags with caller claims. Domain
        // rules independently deny insufficient grants, CI, backup, restore,
        // fencing, current membership, RBAC, MFA and tenant ownership.
        var verdict = Guard.Evaluate(evidence.Snapshot, request);
        return new(verdict.Code);
    }
}
