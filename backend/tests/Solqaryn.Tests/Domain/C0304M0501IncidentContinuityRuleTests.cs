using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

/// <summary>C03-04.M05.01 real domain rule contract; fixture authority is not a signed JWT/HTTP test.</summary>
public sealed class C0304M0501IncidentContinuityRuleTests
{
    private static V5ContinuityIncidentDomainRules.ServerAuthority Authority() =>
        new(101, 11, 2, true, true, true, true, true);
    private static V5ContinuityIncidentDomainRules.PersistedIncident Incident(
        V5ContinuityIncidentDomainRules.IncidentState state =
            V5ContinuityIncidentDomainRules.IncidentState.Open) =>
        new(11, 2, 4, state, true, false, true);
    private static V5ContinuityIncidentDomainRules.Request Read() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence,
            null, null, null, null, false, false);
    private static V5ContinuityIncidentDomainRules.Request Contain() =>
        new(V5ContinuityIncidentDomainRules.IncidentAction.Contain,
            null, 4, "incident-11-v4", "verified evidence", true, false);
    private static V5ContinuityIncidentDomainRules.Request Resolve() =>
        Contain() with { Action = V5ContinuityIncidentDomainRules.IncidentAction.Resolve, MitigationEvidenceVerified = true };
    private static V5ContinuityIncidentDomainRules.Verdict Check(
        V5ContinuityIncidentDomainRules.ServerAuthority? a = null,
        V5ContinuityIncidentDomainRules.PersistedIncident? i = null,
        V5ContinuityIncidentDomainRules.Request? r = null) =>
        V5ContinuityIncidentDomainRules.Evaluate(a ?? Authority(), i ?? Incident(), r ?? Read());
    private static void Denied(V5ContinuityIncidentDomainRules.Verdict v, string code)
    {
        Assert.Equal(code, v.Code);
        Assert.False(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
        Assert.False(v.AuthorizesBackupRestore);
    }

    [Fact]
    public void PositiveReadStillNeedsRealEnforcer()
    {
        var v = Check();
        Assert.True(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
        Assert.False(v.AuthorizesBackupRestore);
    }

    [Fact]
    public void PositiveContainRequiresEnforcerAndDoesNotMutate()
    {
        var v = Check(r: Contain());
        Assert.True(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
    }

    [Fact]
    public void PositiveResolveAfterMitigationRequiresEnforcer()
    {
        var v = Check(i: Incident(V5ContinuityIncidentDomainRules.IncidentState.Contained), r: Resolve());
        Assert.True(v.RequiresTrustedServiceEnforcement);
        Assert.False(v.AuthorizesIncidentMutation);
    }

    [Theory]
    [InlineData("jwt")]
    [InlineData("membership")]
    [InlineData("permission")]
    [InlineData("scope")]
    [InlineData("actor")]
    [InlineData("tenant")]
    public void UntrustedAuthorityCannotRead(string missing)
    {
        var a = Authority();
        a = missing switch
        {
            "jwt" => a with { JwtVerified = false },
            "membership" => a with { ActiveMembershipVerified = false },
            "permission" => a with { ExactPermissionVerified = false },
            "scope" => a with { ServerResolvedScope = false },
            "actor" => a with { ActorId = null },
            "tenant" => a with { TenantId = 0 },
            _ => throw new ArgumentOutOfRangeException(nameof(missing))
        };
        Denied(Check(a: a), "VERIFIED_SERVER_AUTHORITY_REQUIRED");
    }

    [Theory]
    [InlineData(22, true, 4)]
    [InlineData(0, true, 4)]
    [InlineData(11, false, 4)]
    [InlineData(11, true, -1)]
    public void CrossTenantUnownedOrInvalidVersionCannotRead(int owner, bool store, long version) =>
        Denied(Check(i: Incident() with
        { OwnerTenantId = owner, OwnershipReadFromPersistence = store, Version = version }),
        "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED");

    [Theory]
    [InlineData(3)]
    [InlineData(0)]
    public void WrongOrInvalidBranchDenied(int branch) =>
        Denied(Check(i: Incident() with { OwnerBranchId = branch }), "INCIDENT_BRANCH_MISMATCH");

    [Fact]
    public void ClientTenantHeaderCannotCrossTenants() =>
        Denied(Check(r: Read() with { ClientTenantOverride = 22 }), "CLIENT_TENANT_OVERRIDE_DENIED");

    [Fact]
    public void RevokedIncidentAndRawEvidenceAreDenied()
    {
        Denied(Check(i: Incident() with { IncidentRevoked = true }), "INCIDENT_ACCESS_REVOKED");
        Denied(Check(i: Incident() with { EvidenceSanitized = false }), "SANITIZED_EVIDENCE_REQUIRED");
    }

    [Fact]
    public void TrustedFlagCannotTriggerBackupRestore() =>
        Denied(Check(r: Read() with
        {
            Action = V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore,
            RecoveryApprovalFromTrustedSource = true
        }), "RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER");

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void OptimisticVersionMismatchCannotContain(long version) =>
        Denied(Check(r: Contain() with { ExpectedVersion = version }), "INCIDENT_VERSION_CONFLICT");

    [Fact]
    public void MissingVersionDeniesContain() =>
        Denied(Check(r: Contain() with { ExpectedVersion = null }), "INCIDENT_VERSION_CONFLICT");

    [Fact]
    public void UnsafeIdempotencyAndReasonCannotAuthorizeMutation()
    {
        foreach (var key in new string?[] { null, "", "line\nbreak", new string('a', 129) })
            Denied(Check(r: Contain() with { IdempotencyKey = key }),
                "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED");
        foreach (var reason in new string?[] { null, "", "control\0byte", new string('x', 257) })
            Denied(Check(r: Contain() with { Reason = reason }),
                "AUDIT_REASON_AND_IDEMPOTENCY_REQUIRED");
    }

    [Fact]
    public void IndependentDecisionRequired() =>
        Denied(Check(a: Authority() with { IndependentDecisionAuthorityVerified = false },
            r: Contain()), "INDEPENDENT_DECISION_AUTHORITY_REQUIRED");

    [Fact]
    public void ResolveRequiresContainmentAndMitigation()
    {
        Denied(Check(r: Resolve()), "MITIGATION_AND_CONTAINMENT_REQUIRED");
        Denied(Check(i: Incident(V5ContinuityIncidentDomainRules.IncidentState.Contained),
            r: Resolve() with { MitigationEvidenceVerified = false }),
            "MITIGATION_AND_CONTAINMENT_REQUIRED");
    }

    [Fact]
    public void CannotContainAfterContainedOrResolved()
    {
        foreach (var state in new[]
        {
            V5ContinuityIncidentDomainRules.IncidentState.Contained,
            V5ContinuityIncidentDomainRules.IncidentState.Resolved
        })
            Denied(Check(i: Incident(state), r: Contain()), "INVALID_CONTAINMENT_TRANSITION");
    }

    [Fact]
    public void UnknownActionAndStateCannotPass()
    {
        Denied(Check(r: Read() with
        { Action = (V5ContinuityIncidentDomainRules.IncidentAction)999 }),
            "UNKNOWN_INCIDENT_STATE_OR_ACTION");
        Denied(Check(i: Incident() with
        { State = (V5ContinuityIncidentDomainRules.IncidentState)999 }),
            "UNKNOWN_INCIDENT_STATE_OR_ACTION");
    }

    [Fact]
    public void NullAuthorityIncidentOrRequestFailClosed()
    {
        Denied(V5ContinuityIncidentDomainRules.Evaluate(null, Incident(), Read()),
            "VERIFIED_SERVER_AUTHORITY_REQUIRED");
        Denied(V5ContinuityIncidentDomainRules.Evaluate(Authority(), null, Read()),
            "VERIFIED_SERVER_AUTHORITY_REQUIRED");
        Denied(V5ContinuityIncidentDomainRules.Evaluate(Authority(), Incident(), null),
            "VERIFIED_SERVER_AUTHORITY_REQUIRED");
    }
}
