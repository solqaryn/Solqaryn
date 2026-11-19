using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C03-04.M05.05 adversarial domain fixture, NOT JWT signed, HTTP, MySQL or real recovery.
/// </summary>
public sealed class C0304M0505IncidentAdversarialTests
{
    private static V5ContinuityIncidentDomainRules.ServerAuthority Authority() =>
        new(101, 21, 2, true, true, true, true, true);
    private static V5ContinuityIncidentDomainRules.PersistedIncident Incident() =>
        new(21, 2, 7, V5ContinuityIncidentDomainRules.IncidentState.Open, true, false, true);
    private static V5ContinuityIncidentDomainRules.Request Contain() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.Contain,
            null, 7, "incident-21-v7", "verified audit trail", false, false);

    [Theory]
    [InlineData("missing-jwt", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("revoked-membership", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("missing-rbac", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("unresolved-server-scope", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("anonymous-actor", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("zero-actor", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("zero-tenant", "VERIFIED_SERVER_AUTHORITY_REQUIRED")]
    [InlineData("client-tenant-spoof", "CLIENT_TENANT_OVERRIDE_DENIED")]
    [InlineData("cross-company", "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED")]
    [InlineData("missing-company", "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED")]
    [InlineData("untrusted-owner-source", "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED")]
    [InlineData("negative-incident-version", "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED")]
    [InlineData("cross-branch", "INCIDENT_BRANCH_MISMATCH")]
    [InlineData("invalid-branch", "INCIDENT_BRANCH_MISMATCH")]
    [InlineData("no-actor-branch", "INCIDENT_BRANCH_MISMATCH")]
    [InlineData("revoked-incident", "INCIDENT_ACCESS_REVOKED")]
    [InlineData("raw-evidence", "SANITIZED_EVIDENCE_REQUIRED")]
    [InlineData("sandbox-restore-even-if-approved", "RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER")]
    [InlineData("stale-version", "INCIDENT_VERSION_CONFLICT")]
    [InlineData("missing-version", "INCIDENT_VERSION_CONFLICT")]
    [InlineData("missing-idempotency", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("control-idempotency", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("oversize-idempotency", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("blank-reason", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("control-reason", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("oversize-reason", "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED")]
    [InlineData("independent-review-missing", "INDEPENDENT_DECISION_AUTHORITY_REQUIRED")]
    [InlineData("contain-already-contained", "INVALID_CONTAINMENT_TRANSITION")]
    [InlineData("contain-already-resolved", "INVALID_CONTAINMENT_TRANSITION")]
    [InlineData("resolve-before-contain", "MITIGATION_AND_CONTAINMENT_REQUIRED")]
    [InlineData("resolve-without-mitigation", "MITIGATION_AND_CONTAINMENT_REQUIRED")]
    [InlineData("unknown-action", "UNKNOWN_INCIDENT_STATE_OR_ACTION")]
    [InlineData("unknown-state", "UNKNOWN_INCIDENT_STATE_OR_ACTION")]
    public void AbusePathCannotAuthorizeMutationOrRestore(string fault, string expected)
    {
        var a = Authority();
        var i = Incident();
        var request = Contain();
        switch (fault)
        {
            case "missing-jwt": a = a with { JwtVerified = false }; break;
            case "revoked-membership": a = a with { ActiveMembershipVerified = false }; break;
            case "missing-rbac": a = a with { ExactPermissionVerified = false }; break;
            case "unresolved-server-scope": a = a with { ServerResolvedScope = false }; break;
            case "anonymous-actor": a = a with { ActorId = null }; break;
            case "zero-actor": a = a with { ActorId = 0 }; break;
            case "zero-tenant": a = a with { TenantId = 0 }; break;
            case "client-tenant-spoof": request = request with { ClientTenantOverride = 22 }; break;
            case "cross-company": i = i with { OwnerTenantId = 22 }; break;
            case "missing-company": i = i with { OwnerTenantId = null }; break;
            case "untrusted-owner-source": i = i with { OwnershipReadFromPersistence = false }; break;
            case "negative-incident-version": i = i with { Version = -1 }; break;
            case "cross-branch": i = i with { OwnerBranchId = 3 }; break;
            case "invalid-branch": i = i with { OwnerBranchId = 0 }; break;
            case "no-actor-branch": a = a with { BranchId = null }; break;
            case "revoked-incident": i = i with { IncidentRevoked = true }; break;
            case "raw-evidence": i = i with { EvidenceSanitized = false }; break;
            case "sandbox-restore-even-if-approved":
                request = request with {
                    Action = V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore,
                    RecoveryApprovalFromTrustedSource = true
                }; break;
            case "stale-version": request = request with { ExpectedVersion = 6 }; break;
            case "missing-version": request = request with { ExpectedVersion = null }; break;
            case "missing-idempotency": request = request with { IdempotencyKey = null }; break;
            case "control-idempotency": request = request with { IdempotencyKey = "bad\nkey" }; break;
            case "oversize-idempotency": request = request with { IdempotencyKey = new string('i', 129) }; break;
            case "blank-reason": request = request with { Reason = " " }; break;
            case "control-reason": request = request with { Reason = "bad\0reason" }; break;
            case "oversize-reason": request = request with { Reason = new string('r', 257) }; break;
            case "independent-review-missing":
                a = a with { IndependentDecisionAuthorityVerified = false }; break;
            case "contain-already-contained":
                i = i with { State = V5ContinuityIncidentDomainRules.IncidentState.Contained }; break;
            case "contain-already-resolved":
                i = i with { State = V5ContinuityIncidentDomainRules.IncidentState.Resolved }; break;
            case "resolve-before-contain":
                request = request with {
                    Action = V5ContinuityIncidentDomainRules.IncidentAction.Resolve,
                    MitigationEvidenceVerified = true
                }; break;
            case "resolve-without-mitigation":
                i = i with { State = V5ContinuityIncidentDomainRules.IncidentState.Contained };
                request = request with {
                    Action = V5ContinuityIncidentDomainRules.IncidentAction.Resolve,
                    MitigationEvidenceVerified = false
                }; break;
            case "unknown-action":
                request = request with {
                    Action = (V5ContinuityIncidentDomainRules.IncidentAction)999
                }; break;
            case "unknown-state":
                i = i with { State = (V5ContinuityIncidentDomainRules.IncidentState)999 }; break;
            default: throw new ArgumentOutOfRangeException(nameof(fault));
        }

        var v = V5ContinuityIncidentDomainRules.Evaluate(a, i, request);
        Assert.Equal(expected, v.Code);
        Assert.False(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
        Assert.False(v.AuthorizesBackupRestore);
    }

    [Fact]
    public void FullyTrustedPreflightStillNeverPerformsStateChange()
    {
        var v = V5ContinuityIncidentDomainRules.Evaluate(Authority(), Incident(), Contain());
        Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", v.Code);
        Assert.True(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
        Assert.False(v.AuthorizesBackupRestore);
    }

    [Fact]
    public void NullDependenciesNeverGrantIncidentOrBackupAuthority()
    {
        var a = Authority();
        var i = Incident();
        var request = Contain();
        foreach (var v in new[] {
            V5ContinuityIncidentDomainRules.Evaluate(null, i, request),
            V5ContinuityIncidentDomainRules.Evaluate(a, null, request),
            V5ContinuityIncidentDomainRules.Evaluate(a, i, null)
        })
        {
            Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", v.Code);
            Assert.False(v.RequiresTrustedServiceEnforcement);
            Assert.False(v.AuthorizesIncidentMutation);
            Assert.False(v.AuthorizesBackupRestore);
        }
    }
}
