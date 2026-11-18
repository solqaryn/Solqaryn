using Solqaryn.Application.AutomationV5;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C02-02.M05.04 — positive, non-authorizing placement acceptance over
/// synthetic trusted snapshots. A returned adapter requirement is NOT access.
/// </summary>
public sealed class C0202M0504PlacementPositiveAcceptanceTests
{
    private const string SourceSha = "0123456789abcdef0123456789abcdef01234567";

    private static DomainRules.TrustedPlacementSnapshot Snapshot(string tenant, long revision) => new()
    {
        VerifiedTenantId = tenant,
        AuthenticatedActorId = "actor-verified",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = tenant,
        CurrentAssignmentRevision = revision,
        CurrentBindingAlias = "shared-server-alias",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static C0202PlacementApplicationGate.Evidence Evidence(string tenant, long revision) => new(
        SourceSha, tenant, tenant, "dedicated-server-alias",
        true, true, true, true, true, true, true, true, true, true,
        Snapshot(tenant, revision));

    private static C0202PlacementApplicationGate.Request Request(
        string tenant, long revision, DomainRules.Intent intent,
        DomainRules.PlacementMode mode, string key = "intent-001") =>
        new(tenant, SourceSha, intent, mode, "dedicated-server-alias", revision, key);

    private sealed class Reader(C0202PlacementApplicationGate.Evidence trustedEvidence)
        : IC0202PlacementEvidenceReader
    {
        public int Calls { get; private set; }
        public List<string> Tenants { get; } = [];

        public ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Tenants.Add(tenantId);
            return ValueTask.FromResult<C0202PlacementApplicationGate.Evidence?>(trustedEvidence);
        }
    }

    private static void AdapterOnly(C0202PlacementApplicationGate.Result result)
    {
        Assert.Equal("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedStorageAdapter);
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesProduction);
    }

    [Theory]
    [InlineData("tenant-A", DomainRules.PlacementMode.Shared)]
    [InlineData("tenant-A", DomainRules.PlacementMode.Dedicated)]
    [InlineData("tenant-B", DomainRules.PlacementMode.Shared)]
    [InlineData("tenant-B", DomainRules.PlacementMode.Dedicated)]
    public async Task VerifiedReadOnlyInspectionRequiresAnotherAuthenticatedAdapter(
        string tenant, DomainRules.PlacementMode mode)
    {
        var reader = new Reader(Evidence(tenant, 17));
        var gate = new C0202PlacementApplicationGate(reader);
        var result = await gate.EvaluateAsync(
            Request(tenant, 17, DomainRules.Intent.InspectExistingAssignment, mode));
        AdapterOnly(result);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(new[] { tenant }, reader.Tenants);
    }

    [Theory]
    [InlineData("tenant-A", DomainRules.PlacementMode.Shared)]
    [InlineData("tenant-A", DomainRules.PlacementMode.Dedicated)]
    [InlineData("tenant-B", DomainRules.PlacementMode.Shared)]
    [InlineData("tenant-B", DomainRules.PlacementMode.Dedicated)]
    public async Task VerifiedMigrationProposalDoesNotExecuteOrAuthorizeMigration(
        string tenant, DomainRules.PlacementMode mode)
    {
        var reader = new Reader(Evidence(tenant, 17));
        var result = await new C0202PlacementApplicationGate(reader).EvaluateAsync(
            Request(tenant, 17, DomainRules.Intent.ProposeMigration, mode));
        AdapterOnly(result);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task MaxTenantAndIdempotencyLengthPreserveFailClosedPositiveBoundary()
    {
        var tenant = new string('a', 128);
        var idempotency = new string('k', 128);
        var result = await new C0202PlacementApplicationGate(
            new Reader(Evidence(tenant, long.MaxValue))).EvaluateAsync(
            Request(tenant, long.MaxValue, DomainRules.Intent.ProposeMigration,
                DomainRules.PlacementMode.Dedicated, idempotency));
        AdapterOnly(result);
    }

    [Fact]
    public async Task RepeatingAnIdenticalSnapshotCannotImplicitlyGrantCutover()
    {
        var reader = new Reader(Evidence("tenant-A", 17));
        var gate = new C0202PlacementApplicationGate(reader);
        var request = Request("tenant-A", 17, DomainRules.Intent.ProposeMigration,
            DomainRules.PlacementMode.Dedicated);
        AdapterOnly(await gate.EvaluateAsync(request));
        AdapterOnly(await gate.EvaluateAsync(request));
        Assert.Equal(2, reader.Calls);
        Assert.Equal(new[] { "tenant-A", "tenant-A" }, reader.Tenants);
    }

    [Fact]
    public async Task SameTrustedProviderCannotBeReusedForDifferentTenant()
    {
        var reader = new Reader(Evidence("tenant-A", 17));
        var result = await new C0202PlacementApplicationGate(reader).EvaluateAsync(
            Request("tenant-B", 17, DomainRules.Intent.InspectExistingAssignment,
                DomainRules.PlacementMode.Shared));
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", result.Code);
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesProduction);
        Assert.Equal(new[] { "tenant-B" }, reader.Tenants);
    }

    [Fact]
    public async Task PositiveEvidenceNeverBypassesRevisionFence()
    {
        var reader = new Reader(Evidence("tenant-A", 17));
        var result = await new C0202PlacementApplicationGate(reader).EvaluateAsync(
            Request("tenant-A", 16, DomainRules.Intent.ProposeMigration,
                DomainRules.PlacementMode.Dedicated));
        Assert.Equal("STALE_PLACEMENT_REVISION", result.Code);
        Assert.False(result.AuthorizesMigration);
        Assert.Equal(1, reader.Calls);
    }
}
