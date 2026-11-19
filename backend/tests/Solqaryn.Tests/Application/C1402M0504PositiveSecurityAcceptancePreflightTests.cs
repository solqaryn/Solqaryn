using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C14-02.M05.04: positive acceptance of Application preflight only.
/// Not signed JWT HTTP or MySQL acceptance.
/// </summary>
public sealed class C1402M0504PositiveSecurityAcceptancePreflightTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Resource = "security-tenant-A";
    private static readonly V5SecurityAcceptanceDomainRules.VerifiedAuthority Authorized =
        new(77, 11, 2, Sha, true, true, true, true, true, false, false);
    private static readonly V5SecurityAcceptanceDomainRules.PersistedTarget Owned =
        new(11, 2, 11, true, true, false, false);

    private sealed class Reader(C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence evidence)
        : IC1402SecurityAcceptanceEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(Resource, resourceKey);
            Calls++;
            return ValueTask.FromResult<C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence?>(evidence);
        }
    }

    private static C1402SecurityAcceptanceAuthorizationGate.VerifiedEvidence Evidence(
        V5SecurityAcceptanceDomainRules.Surface surface, bool trustedJob = false)
    {
        var authority = trustedJob
            ? Authorized with { ServiceActorVerified = true, ServiceGrantVerified = true }
            : Authorized;
        var attempt = new V5SecurityAcceptanceDomainRules.Attempt(
            surface, null, Sha, false, false, trustedJob);
        return new(Resource, Sha, true, true, true, true, true,
            "git-proof:dev", authority, Owned, attempt);
    }

    private static C1402SecurityAcceptanceAuthorizationGate.Request Request(
        V5SecurityAcceptanceDomainRules.Surface surface) => new(Resource, Sha, surface);

    [Theory]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BusinessRead)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BusinessWrite)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.BranchRead)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.HostResolve)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.FileDownload)]
    [InlineData(V5SecurityAcceptanceDomainRules.Surface.CacheRead)]
    public async Task ConsistentEvidenceOnSixSurfacesRequiresRealAuthenticatedAdapter(
        V5SecurityAcceptanceDomainRules.Surface surface)
    {
        var reader = new Reader(Evidence(surface));
        var decision = await new C1402SecurityAcceptanceAuthorizationGate(reader)
            .CheckAsync(Request(surface));

        Assert.Equal(1, reader.Calls);
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", decision.Code);
        Assert.True(decision.RequiresSeparateAuthenticatedAdapter);
        Assert.False(decision.GrantsTenantOrResourceAccess);
        Assert.False(decision.AllowsInstitutionalWrite);
        Assert.False(decision.AllowsRestoreOrDeployment);
        Assert.False(decision.CertifiesSecurityAcceptance);
    }

    [Fact]
    public async Task JobPositiveRequiresServerGrantAndIdempotencyButCannotGrantExecution()
    {
        const V5SecurityAcceptanceDomainRules.Surface surface =
            V5SecurityAcceptanceDomainRules.Surface.BackgroundJob;
        var reader = new Reader(Evidence(surface, trustedJob: true));
        var result = await new C1402SecurityAcceptanceAuthorizationGate(reader)
            .CheckAsync(Request(surface));
        Assert.Equal(1, reader.Calls);
        Assert.Equal("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedAdapter);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.False(result.AllowsInstitutionalWrite);
    }

    [Fact]
    public async Task SameTenantButWrongBranchIsRejected()
    {
        const V5SecurityAcceptanceDomainRules.Surface surface =
            V5SecurityAcceptanceDomainRules.Surface.BranchRead;
        var evidence = Evidence(surface) with { Target = Owned with { OwnerBranchId = 3 } };
        var result = await new C1402SecurityAcceptanceAuthorizationGate(new Reader(evidence))
            .CheckAsync(Request(surface));
        Assert.Equal("BRANCH_OWNER_MISMATCH", result.Code);
        Assert.False(result.RequiresSeparateAuthenticatedAdapter);
    }

    [Fact]
    public async Task TrustedSnapshotStillCannotAuthorizeRestore()
    {
        const V5SecurityAcceptanceDomainRules.Surface surface =
            V5SecurityAcceptanceDomainRules.Surface.BackupRestore;
        var reader = new Reader(Evidence(surface));
        var result = await new C1402SecurityAcceptanceAuthorizationGate(reader)
            .CheckAsync(Request(surface));
        Assert.Equal("CLIENT_AUTHORITY_OR_RESTORE_OVERRIDE_DENIED", result.Code);
        Assert.Equal(0, reader.Calls);
        Assert.False(result.AllowsRestoreOrDeployment);
    }
}
