using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class V5TenancySecurityAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class EvidenceReader : IV5TenancySecurityVerifiedAuthorizationEvidenceReader
    {
        public Gate.VerifiedEvidence? Current { get; set; } = Verified();
        public int Calls { get; private set; }
        public int? LastResourceId { get; private set; }

        public ValueTask<Gate.VerifiedEvidence?> ReadVerifiedAsync(
            int resourceId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastResourceId = resourceId;
            return ValueTask.FromResult(Current);
        }
    }

    private static Gate.VerifiedEvidence Verified() => new(
        ObservedSourceSha: Sha,
        VerifiedResourceId: 101,
        VerifiedTenantId: 7,
        VerifiedActorId: 11,
        ServerAuthenticationVerified: true,
        CurrentMembershipVerified: true,
        ExplicitPermissionVerified: true,
        AuthorizationReadFromTrustedBackend: true,
        PermissionRevoked: false,
        Authority: new Rules.TrustedAuthority(7, 11, 3, true, true, false, false),
        Resource: new Rules.PersistedResource(7, 7, 3, true, false, false));

    private static Gate.Request Request(
        Rules.Surface surface = Rules.Surface.BusinessRead) => new(
        ResourceId: 101,
        Surface: surface,
        ExpectedSourceSha: Sha,
        RequestsTenantFilterBypass: false,
        AttemptsCrossTenantEnumeration: false,
        RequestsInstitutionalWrite: false,
        IdempotencyKey: null);

    [Fact]
    public async Task VerifiedScopedReadStillNeedsAuthenticatedExecutionAdapter()
    {
        var source = new EvidenceReader();
        var result = await new Gate(source).EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedExecutionAdapter);
        Assert.Equal(101, source.LastResourceId);
        Assert.False(result.GrantsResourceRead);
        Assert.False(result.GrantsFileDownload);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsControlPlane);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
    }

    [Fact]
    public async Task MissingInvalidOrUntrustedRequestNeverReachesEvidence()
    {
        var source = new EvidenceReader();
        var gate = new Gate(source);
        Assert.Equal("AUTHORIZATION_REQUEST_REQUIRED",
            (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("RESOURCE_OR_SECURITY_SURFACE_INVALID",
            (await gate.EvaluateAsync(Request() with { ResourceId = -1 })).Code);
        Assert.Equal("RESOURCE_OR_SECURITY_SURFACE_INVALID",
            (await gate.EvaluateAsync(Request((Rules.Surface)999))).Code);
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED",
            (await gate.EvaluateAsync(Request() with { ExpectedSourceSha = "short" })).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task BypassEnumerationOrInstitutionalWriteFailsBeforeEvidence()
    {
        var source = new EvidenceReader();
        var gate = new Gate(source);
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Request() with { RequestsTenantFilterBypass = true })).Code);
        Assert.Equal("TENANT_QUERY_BYPASS_DENIED",
            (await gate.EvaluateAsync(Request() with { AttemptsCrossTenantEnumeration = true })).Code);
        Assert.Equal("INSTITUTIONAL_WRITE_OUT_OF_SCOPE",
            (await gate.EvaluateAsync(Request() with { RequestsInstitutionalWrite = true })).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task StaleOrMissingBackendEvidenceFailsClosed()
    {
        var source = new EvidenceReader { Current = Verified() with { ObservedSourceSha = OtherSha } };
        var gate = new Gate(source);
        Assert.Equal("TRUSTED_EVIDENCE_SOURCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = null;
        Assert.Equal("TRUSTED_EVIDENCE_SOURCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task RevocationMembershipPermissionAndUntrustedOriginDeny()
    {
        var source = new EvidenceReader();
        var gate = new Gate(source);
        source.Current = Verified() with { PermissionRevoked = true };
        Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { CurrentMembershipVerified = false };
        Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { ExplicitPermissionVerified = false };
        Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { AuthorizationReadFromTrustedBackend = false };
        Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { ServerAuthenticationVerified = false };
        Assert.Equal("CURRENT_VERIFIED_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task ForeignTenantAndSameTenantDifferentResourceCannotPass()
    {
        var source = new EvidenceReader();
        var gate = new Gate(source);
        source.Current = Verified() with { VerifiedTenantId = 8 };
        Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { VerifiedResourceId = 102 };
        Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { Resource = Verified().Resource! with { ParentTenantId = 8 } };
        Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { Resource = Verified().Resource! with { OwnershipReadFromPersistence = false } };
        Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
        source.Current = Verified() with { VerifiedActorId = 12 };
        Assert.Equal("RESOURCE_SCOPE_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Request())).Code);
    }

    [Fact]
    public async Task BranchFileJobAndControlPlaneRequireSeparateVerifiedAuthority()
    {
        var source = new EvidenceReader { Current = Verified() with
        { Resource = Verified().Resource! with { OwnerBranchId = 8 } } };
        var gate = new Gate(source);
        Assert.Equal("BRANCH_SCOPE_MISMATCH",
            (await gate.EvaluateAsync(Request(Rules.Surface.BranchRead))).Code);
        source.Current = Verified() with
        { Resource = Verified().Resource! with { SignedLinkExpired = true } };
        Assert.Equal("FILE_ACCESS_REVOKED_OR_EXPIRED",
            (await gate.EvaluateAsync(Request(Rules.Surface.FileDownload))).Code);
        source.Current = Verified();
        Assert.Equal("SERVICE_JOB_AUTHORITY_REQUIRED",
            (await gate.EvaluateAsync(Request(Rules.Surface.BackgroundJob))).Code);
        Assert.Equal("CONTROL_PLANE_AUTHORITY_REQUIRED",
            (await gate.EvaluateAsync(Request(Rules.Surface.ControlPlane))).Code);
    }

    [Fact]
    public async Task TrustedServiceJobWithIdempotencyStillDoesNotDispatchAnything()
    {
        var source = new EvidenceReader { Current = Verified() with
        {
            Authority = Verified().Authority! with
            {
                PrivilegedServiceGrantVerified = true,
                AuditedServiceContextVerified = true
            }
        } };
        var gate = new Gate(source);
        var result = await gate.EvaluateAsync(Request(Rules.Surface.BackgroundJob) with
        { IdempotencyKey = "v5-tenant-7-job-101" });
        Assert.True(result.NeedsAuthenticatedExecutionAdapter);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsInstitutionalWrite);
    }

    [Fact]
    public async Task CancellationAndNullDependencyAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Gate(null!));
        var source = new EvidenceReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(source).EvaluateAsync(Request(), cts.Token));
        Assert.Equal(0, source.Calls);
    }
}
