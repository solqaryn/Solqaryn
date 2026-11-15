using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using AuditRules = Solqaryn.Domain.AutomationV5.V5OperationalAuditDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5OperationalAuditReadOnlyProviderGatewayTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private const string Revision = "source-v5-r1";
    private const string Id = "C15-05.M03.05";

    private static readonly HashSet<string> Tasks = new(StringComparer.Ordinal)
    {
        "6adbb9fff1548191a1bd5b30d9e9984a",
        "6af16bb9492c8191b8e8c7d606f11507",
        "6adc2e4bd8a48191ac7d8641ed95a1a3",
        "6aeacb7820ac8191b7d768a6f33dfa09",
        "6adc2e55f47c81919f6374e561687076",
        "6af16bbad26881918c283abe06e1f32a"
    };

    private sealed class Provider : IV5OperationalAuditReadOnlyEvidenceProvider
    {
        public V5OperationalAuditEvidenceSnapshot? Snapshot { get; set; } = Complete();
        public bool Throw { get; set; }
        public int Calls { get; private set; }
        public string? SeenTenant { get; private set; }
        public ValueTask<V5OperationalAuditEvidenceSnapshot?> ReadScopedAsync(
            string serverTenantId, CancellationToken cancellationToken)
        {
            Calls++;
            SeenTenant = serverTenantId;
            if (Throw)
                throw new InvalidOperationException("private-provider-token");
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static V5OperationalAuditEvidenceSnapshot Complete() => new(
        "tenant-A", Revision, Sha,
        new HashSet<string>(StringComparer.Ordinal) { Id },
        new HashSet<string>(Tasks, StringComparer.Ordinal),
        new[] { new AuditRules.WorkItem(
            Id, "LISTO", "6af16bbad26881918c283abe06e1f32a",
            Sha, "6af16bb9492c8191b8e8c7d606f11507",
            "check:exact-sha", "a5:independent") });

    private static V5OperationalAuditSecurityGate.Context Context() => new(
        PrincipalId: "actor-11",
        ServerResolvedTenantId: "tenant-A",
        MembershipTenantId: "tenant-A",
        TargetResourceTenantId: "tenant-A",
        ClientRequestedTenantId: null,
        EvidenceSha: Sha,
        IsAuthenticated: true,
        TenantResolvedOnServer: true,
        MembershipActive: true,
        HasExplicitAuditPermission: true,
        PermissionRevoked: false,
        IsServiceActor: false,
        ServiceActorGrantVerified: false,
        RequestedQueryFilterBypass: false,
        RequestedInstitutionalWrite: false);

    [Fact]
    public async Task CompleteReadOnlySnapshotStillRequiresExternalAuthenticatedEvidence()
    {
        var source = new Provider();
        var result = await new V5OperationalAuditReadOnlyProviderGateway(source)
            .PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks);
        Assert.Equal("AUTHENTICATED_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsExamined);
        Assert.Equal("tenant-A", source.SeenTenant);
        Assert.True(result.RequiresIndependentExternalEvidence);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
        Assert.False(result.AllowsEventDispatch);
        Assert.False(result.CertifiesA5);
    }

    [Fact]
    public async Task InvalidShaAndTaskCatalogNeverReadProvider()
    {
        var source = new Provider();
        var gateway = new V5OperationalAuditReadOnlyProviderGateway(source);
        Assert.Equal("CHECK_SHA_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(Context(), OtherSha, Revision, Tasks)).Code);
        Assert.Equal("JAVIER_SIX_TASK_CATALOG_REQUIRED",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision,
                new HashSet<string> { "some-task" })).Code);
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, "", Tasks)).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task CrossTenantRevocationAndFilterBypassCannotReadProvider()
    {
        var source = new Provider();
        var gateway = new V5OperationalAuditReadOnlyProviderGateway(source);
        Assert.Equal("TENANT_MEMBERSHIP_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(
                Context() with { MembershipTenantId = "tenant-B" }, Sha, Revision, Tasks)).Code);
        Assert.Equal("REVOKED_OR_INACTIVE_MEMBERSHIP",
            (await gateway.PrepareReadOnlyAsync(
                Context() with { PermissionRevoked = true }, Sha, Revision, Tasks)).Code);
        Assert.Equal("QUERY_FILTER_BYPASS_DENIED",
            (await gateway.PrepareReadOnlyAsync(
                Context() with { RequestedQueryFilterBypass = true }, Sha, Revision, Tasks)).Code);
        Assert.Equal("INSTITUTIONAL_WRITE_OUT_OF_SCOPE",
            (await gateway.PrepareReadOnlyAsync(
                Context() with { RequestedInstitutionalWrite = true }, Sha, Revision, Tasks)).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task ForeignProviderTenantOrWrongTaskCatalogFailClosed()
    {
        var source = new Provider { Snapshot = Complete() with { TenantId = "tenant-B" } };
        var gateway = new V5OperationalAuditReadOnlyProviderGateway(source);
        Assert.Equal("EVIDENCE_PROVIDER_TENANT_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks)).Code);
        source.Snapshot = Complete() with
        { AuthorizedTaskIds = new HashSet<string>(StringComparer.Ordinal) { "external" } };
        Assert.Equal("TASK_CATALOG_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks)).Code);
    }

    [Fact]
    public async Task StaleSourceMissingA5AndProviderFailureNeverCertify()
    {
        var source = new Provider { Snapshot = Complete() with { SourceRevision = "old" } };
        var gateway = new V5OperationalAuditReadOnlyProviderGateway(source);
        Assert.Equal("SOURCE_REVISION_MISMATCH",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks)).Code);
        source.Snapshot = Complete() with
        { Items = new[] { Complete().Items.Single() with { ReviewerTaskId = null } } };
        Assert.Equal("SOURCE_OR_A5_INCOMPLETE",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks)).Code);
        source.Throw = true;
        var unavailable = await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks);
        Assert.Equal("EVIDENCE_PROVIDER_UNAVAILABLE", unavailable.Code);
        Assert.DoesNotContain("token", unavailable.Code);
        source.Throw = false;
        source.Snapshot = null;
        Assert.Equal("EVIDENCE_PROVIDER_UNAVAILABLE",
            (await gateway.PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks)).Code);
    }

    [Fact]
    public async Task CancellationAndConstructorAreFailClosed()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new V5OperationalAuditReadOnlyProviderGateway(null!));
        var source = new Provider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new V5OperationalAuditReadOnlyProviderGateway(source)
                .PrepareReadOnlyAsync(Context(), Sha, Revision, Tasks, cts.Token));
        Assert.Equal(0, source.Calls);
    }
}
