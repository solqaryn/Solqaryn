using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5ContinuityIncidentApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private static readonly V5ContinuityIncidentApplicationService Service = new();

    private static V5ContinuityIncidentDomainRules.ServerAuthority Authority() =>
        new(10, 100, 7, true, true, true, true, true);

    private static V5ContinuityIncidentDomainRules.PersistedIncident Incident() =>
        new(100, 7, 4, V5ContinuityIncidentDomainRules.IncidentState.Open,
            true, false, true);

    private static V5ContinuityIncidentDomainRules.Request Read() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence,
            null, null, null, null, false, false);

    private static V5ContinuityIncidentDomainRules.Request Contain() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.Contain,
            null, 4, "incident-100-v4", "contain-with-evidence", true, false);

    private static V5ContinuityIncidentApplicationService.Request Valid() =>
        new(Sha, Sha, "git-proof:dev-verified", Authority(), Incident(), Read());

    [Fact]
    public void ReadPreflightAlwaysDefersEvidenceAccessToTrustedAdapter()
    {
        var result = Service.Prepare(Valid());
        Assert.Equal("AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsIncidentMutation);
        Assert.False(result.AllowsBackupRestore);
        Assert.False(result.AllowsEvidenceDisclosure);
    }

    [Fact]
    public void ContainmentIntentNeverGrantsMutation()
    {
        var result = Service.Prepare(Valid() with { Command = Contain() });
        Assert.Equal("AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsIncidentMutation);
    }

    [Fact]
    public void NullOrMismatchedSourceShaFailsClosed()
    {
        Assert.Equal("INCIDENT_REQUEST_REQUIRED", Service.Prepare(null).Code);
        Assert.Equal("INCIDENT_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ExpectedCommitSha = null }).Code);
        Assert.Equal("INCIDENT_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedCommitSha = OtherSha }).Code);
        Assert.Equal("INCIDENT_SOURCE_SHA_UNVERIFIED",
            Service.Prepare(Valid() with { ObservedCommitSha = "not-a-sha" }).Code);
    }

    [Fact]
    public void UnboundedOrSensitiveReferencesAreDenied()
    {
        Assert.Equal("INCIDENT_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = "" }).Code);
        Assert.Equal("INCIDENT_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with
            {
                SanitizedEvidenceReference = "https://private/incident"
            }).Code);
        Assert.Equal("INCIDENT_EVIDENCE_REFERENCE_REQUIRED",
            Service.Prepare(Valid() with { SanitizedEvidenceReference = "a@b" }).Code);
    }

    [Fact]
    public void RevokedClaimsAndForeignTenantOrBranchAreDenied()
    {
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { ActiveMembershipVerified = false }
            }).Code);
        Assert.Equal("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED",
            Service.Prepare(Valid() with
            {
                Incident = Incident() with { OwnerTenantId = 200 }
            }).Code);
        Assert.Equal("INCIDENT_BRANCH_MISMATCH",
            Service.Prepare(Valid() with
            {
                Authority = Authority() with { BranchId = 8 }
            }).Code);
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            Service.Prepare(Valid() with
            {
                Command = Read() with { ClientTenantOverride = 200 }
            }).Code);
    }

    [Fact]
    public void RestoreRequestRequiresSeparateRunnerEvenWhenApproved()
    {
        var result = Service.Prepare(Valid() with
        {
            Command = Read() with
            {
                Action = V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore,
                RecoveryApprovalFromTrustedSource = true
            }
        });
        Assert.Equal("RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER", result.Code);
        Assert.False(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsBackupRestore);
    }

    [Fact]
    public void VersionOrIdempotencyFailureIsNeverConvertedToSuccess()
    {
        Assert.Equal("INCIDENT_VERSION_CONFLICT",
            Service.Prepare(Valid() with
            {
                Command = Contain() with { ExpectedVersion = 3 }
            }).Code);
        Assert.Equal("AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Command = Contain() with { IdempotencyKey = null }
            }).Code);
        Assert.Equal("INDEPENDENT_DECISION_AUTHORITY_REQUIRED",
            Service.Prepare(Valid() with
            {
                Command = Contain(),
                Authority = Authority() with { IndependentDecisionAuthorityVerified = false }
            }).Code);
    }

    [Fact]
    public void RevokedIncidentOrUnsanitizedEvidenceCannotProceed()
    {
        Assert.Equal("INCIDENT_ACCESS_REVOKED",
            Service.Prepare(Valid() with
            {
                Incident = Incident() with { IncidentRevoked = true }
            }).Code);
        Assert.Equal("SANITIZED_EVIDENCE_REQUIRED",
            Service.Prepare(Valid() with
            {
                Incident = Incident() with { EvidenceSanitized = false }
            }).Code);
    }

    [Fact]
    public void ResolveRequiresContainmentAndVerifiedMitigation()
    {
        Assert.Equal("MITIGATION_AND_CONTAINMENT_REQUIRED",
            Service.Prepare(Valid() with
            {
                Command = Contain() with
                {
                    Action = V5ContinuityIncidentDomainRules.IncidentAction.Resolve
                }
            }).Code);
    }
}
