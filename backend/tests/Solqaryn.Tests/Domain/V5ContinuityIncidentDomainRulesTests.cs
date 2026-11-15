using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5ContinuityIncidentDomainRulesTests
{
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

    private static string Check(
        V5ContinuityIncidentDomainRules.ServerAuthority? authority = null,
        V5ContinuityIncidentDomainRules.PersistedIncident? incident = null,
        V5ContinuityIncidentDomainRules.Request? request = null) =>
        V5ContinuityIncidentDomainRules.Evaluate(
            authority ?? Authority(), incident ?? Incident(), request ?? Read()).Code;

    [Fact]
    public void VerifiedSnapshotIsOnlyReadyForTrustedAdapter()
    {
        var result = V5ContinuityIncidentDomainRules.Evaluate(
            Authority(), Incident(), Read());
        Assert.True(result.RequiresTrustedServiceEnforcement);
        Assert.False(result.AuthorizesIncidentMutation);
        Assert.False(result.AuthorizesBackupRestore);
        Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", Check(request: Contain()));
    }

    [Fact]
    public void InvalidJwtMembershipOrPermissionAlwaysDenied()
    {
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { JwtVerified = false }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ActiveMembershipVerified = false }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ExactPermissionVerified = false }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            Check(authority: Authority() with { ServerResolvedScope = false }));
    }

    [Fact]
    public void CrossTenantAndLegacyUnownedIncidentsFailClosed()
    {
        Assert.Equal("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED",
            Check(incident: Incident() with { OwnerTenantId = 200 }));
        Assert.Equal("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED",
            Check(incident: Incident() with { OwnerTenantId = null }));
        Assert.Equal("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED",
            Check(incident: Incident() with { OwnershipReadFromPersistence = false }));
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            Check(request: Read() with { ClientTenantOverride = 200 }));
    }

    [Fact]
    public void BranchRevocationAndUnsafeEvidenceAreDenied()
    {
        Assert.Equal("INCIDENT_BRANCH_MISMATCH",
            Check(authority: Authority() with { BranchId = 8 }));
        Assert.Equal("INCIDENT_ACCESS_REVOKED",
            Check(incident: Incident() with { IncidentRevoked = true }));
        Assert.Equal("SANITIZED_EVIDENCE_REQUIRED",
            Check(incident: Incident() with { EvidenceSanitized = false }));
    }

    [Fact]
    public void RestoreCannotBeStartedFromDomainSnapshot()
    {
        Assert.Equal("RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER",
            Check(request: Read() with
            {
                Action = V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore,
                RecoveryApprovalFromTrustedSource = true
            }));
    }

    [Fact]
    public void VersionKeyAndIndependentDecisionAreRequired()
    {
        Assert.Equal("INCIDENT_VERSION_CONFLICT",
            Check(request: Contain() with { ExpectedVersion = 3 }));
        Assert.Equal("AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED",
            Check(request: Contain() with { IdempotencyKey = "bad\nkey" }));
        Assert.Equal("AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED",
            Check(request: Contain() with { Reason = "" }));
        Assert.Equal("INDEPENDENT_DECISION_AUTHORITY_REQUIRED",
            Check(authority: Authority() with
                { IndependentDecisionAuthorityVerified = false }, request: Contain()));
    }

    [Fact]
    public void IncidentLifecycleCannotSkipContainment()
    {
        var resolve = Contain() with
        {
            Action = V5ContinuityIncidentDomainRules.IncidentAction.Resolve
        };
        Assert.Equal("MITIGATION_AND_CONTAINMENT_REQUIRED",
            Check(request: resolve));
        Assert.Equal("MITIGATION_AND_CONTAINMENT_REQUIRED",
            Check(incident: Incident() with
                { State = V5ContinuityIncidentDomainRules.IncidentState.Contained },
                request: resolve with { MitigationEvidenceVerified = false }));
        Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT",
            Check(incident: Incident() with
                { State = V5ContinuityIncidentDomainRules.IncidentState.Contained },
                request: resolve with { MitigationEvidenceVerified = true }));
        Assert.Equal("INVALID_CONTAINMENT_TRANSITION",
            Check(incident: Incident() with
                { State = V5ContinuityIncidentDomainRules.IncidentState.Resolved },
                request: Contain()));
    }

    [Fact]
    public void UnsupportedStatesAndNullInputsCannotPass()
    {
        Assert.Equal("UNKNOWN_INCIDENT_STATE_OR_ACTION",
            Check(request: Read() with
            { Action = (V5ContinuityIncidentDomainRules.IncidentAction)123 }));
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            V5ContinuityIncidentDomainRules.Evaluate(null, Incident(), Read()).Code);
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED",
            V5ContinuityIncidentDomainRules.Evaluate(Authority(), null, Read()).Code);
    }
}
