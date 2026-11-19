using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C03-04.M05.04: positive, read-only acceptance of actual incident preflight.
/// Ready means a trusted authenticated adapter is still mandatory; no effects.
/// </summary>
public sealed class C0304M0504IncidentPositiveAcceptanceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5ContinuityIncidentApplicationService Service = new();
    private static V5ContinuityIncidentDomainRules.ServerAuthority Authority(int tenant, int branch) =>
        new(101, tenant, branch, true, true, true, true, true);
    private static V5ContinuityIncidentDomainRules.PersistedIncident Incident(
        int tenant, int branch, V5ContinuityIncidentDomainRules.IncidentState state) =>
        new(tenant, branch, 4, state, true, false, true);
    private static V5ContinuityIncidentDomainRules.Request Command(
        V5ContinuityIncidentDomainRules.IncidentAction action, long? version = null) =>
        new(action, null, version,
            version is null ? null : "incident-v4-acceptance",
            version is null ? null : "verified containment evidence",
            action == V5ContinuityIncidentDomainRules.IncidentAction.Resolve, false);
    private static V5ContinuityIncidentApplicationService.Request Request(
        int tenant, int branch, V5ContinuityIncidentDomainRules.IncidentState state,
        V5ContinuityIncidentDomainRules.IncidentAction action) =>
        new(Sha, Sha, "git-proof:incident-acceptance",
            Authority(tenant, branch), Incident(tenant, branch, state),
            Command(action, action == V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence
                ? null : 4));
    private static void PositiveOnlyAsTrustedAdapter(
        V5ContinuityIncidentApplicationService.Request request)
    {
        var verdict = Service.Prepare(request);
        Assert.Equal("AUTHENTICATED_INCIDENT_ADAPTER_REQUIRED", verdict.Code);
        Assert.True(verdict.NeedsTrustedAdapter);
        Assert.False(verdict.AllowsIncidentMutation);
        Assert.False(verdict.AllowsEvidenceDisclosure);
        Assert.False(verdict.AllowsBackupRestore);
    }

    [Theory]
    [InlineData(11, 2)]
    [InlineData(11, 4)]
    [InlineData(22, 2)]
    [InlineData(22, 4)]
    public void MatchingTenantBranchReadPreflightRequiresRealAuthenticatedAdapter(int tenant, int branch) =>
        PositiveOnlyAsTrustedAdapter(Request(tenant, branch,
            V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence));

    [Theory]
    [InlineData(11, 2)]
    [InlineData(22, 4)]
    public void ValidContainmentIntentStillCannotMutate(int tenant, int branch) =>
        PositiveOnlyAsTrustedAdapter(Request(tenant, branch,
            V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.Contain));

    [Theory]
    [InlineData(11, 2)]
    [InlineData(22, 4)]
    public void VerifiedResolutionIntentStillNeedsAdapterAndDoesNotResolve(int tenant, int branch) =>
        PositiveOnlyAsTrustedAdapter(Request(tenant, branch,
            V5ContinuityIncidentDomainRules.IncidentState.Contained,
            V5ContinuityIncidentDomainRules.IncidentAction.Resolve));

    [Theory]
    [InlineData(11, 3)]
    [InlineData(22, 7)]
    public void ForeignBranchCannotBeAcceptedEvenForRead(int tenant, int actorBranch)
    {
        var request = Request(tenant, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence);
        var result = Service.Prepare(request with
        { Authority = Authority(tenant, actorBranch) });
        Assert.Equal("INCIDENT_BRANCH_MISMATCH", result.Code);
        Assert.False(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsEvidenceDisclosure);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void IncompleteRealAuthorityAlwaysDeniesPositiveRead(
        bool jwt, bool membership, bool permission)
    {
        var request = Request(11, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence);
        var result = Service.Prepare(request with
        {
            Authority = Authority(11, 2) with
            {
                JwtVerified = jwt,
                ActiveMembershipVerified = membership,
                ExactPermissionVerified = permission
            }
        });
        Assert.Equal("VERIFIED_SERVER_AUTHORITY_REQUIRED", result.Code);
        Assert.False(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsIncidentMutation);
    }

    [Fact]
    public void SourceCommitMustBeCausallyIdentical()
    {
        var request = Request(11, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence);
        var result = Service.Prepare(request with
        { ObservedCommitSha = "abcdef0123456789abcdef0123456789abcdef01" });
        Assert.Equal("INCIDENT_SOURCE_SHA_UNVERIFIED", result.Code);
        Assert.False(result.NeedsTrustedAdapter);
    }

    [Fact]
    public void SanitizedReferenceDoesNotBecomeReadableEvidence()
    {
        var request = Request(11, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence);
        var result = Service.Prepare(request);
        Assert.True(result.NeedsTrustedAdapter);
        Assert.False(result.AllowsEvidenceDisclosure);
    }

    [Fact]
    public void RestoreMustNeverBeInferredFromPositiveRead()
    {
        var request = Request(11, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.ReadEvidence);
        var result = Service.Prepare(request with
        {
            Command = Command(V5ContinuityIncidentDomainRules.IncidentAction.RequestSandboxRestore)
                with { RecoveryApprovalFromTrustedSource = true }
        });
        Assert.Equal("RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER", result.Code);
        Assert.False(result.AllowsBackupRestore);
        Assert.False(result.NeedsTrustedAdapter);
    }

    [Fact]
    public void PositiveContainmentNeverSkipsOptimisticVersionCheck()
    {
        var request = Request(11, 2, V5ContinuityIncidentDomainRules.IncidentState.Open,
            V5ContinuityIncidentDomainRules.IncidentAction.Contain);
        var result = Service.Prepare(request with
        { Command = Command(V5ContinuityIncidentDomainRules.IncidentAction.Contain, 3) });
        Assert.Equal("INCIDENT_VERSION_CONFLICT", result.Code);
        Assert.False(result.AllowsIncidentMutation);
    }
}
