using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0202PlacementIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Tenant = "tenant-A";
    private const string Actor = "user-7";

    private static DomainRules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = Tenant,
        AuthenticatedActorId = Actor,
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = Tenant,
        CurrentAssignmentRevision = 8,
        CurrentBindingAlias = "shared-cluster",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };
    private static Gate.Evidence Evidence() =>
        new(Sha, Tenant, Tenant, "dedicated-tenant-A",
            true, true, true, true, true, true, true, true, true, true, Snapshot());
    private static Gate.Request Request(
        DomainRules.Intent intent = DomainRules.Intent.InspectExistingAssignment) =>
        new(Tenant, Sha, intent, DomainRules.PlacementMode.Shared,
            "dedicated-tenant-A", 8, "idempotency-123");
    private static C0202PlacementIsolatedSnapshotAdapter Adapter(Gate.Evidence? evidence = null) =>
        new(Tenant, Actor, Sha, 8, evidence ?? Evidence());
    private static async Task<Gate.Result> Check(
        C0202PlacementIsolatedSnapshotAdapter adapter, Gate.Request? request = null,
        string? observedTenant = Tenant, string? observedActor = Actor,
        long observedRevision = 8, string? observedSha = Sha) =>
        await adapter.EvaluateReadOnlyAsync(request ?? Request(),
            observedTenant, observedActor, observedRevision, observedSha);

    [Fact]
    public async Task NominalInspectionNeverGrantsAStorageConnection()
    {
        var adapter = Adapter();
        var result = await Check(adapter);
        Assert.Equal("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedStorageAdapter);
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesProduction);
        Assert.False(adapter.IsLiveStorageProvider);
        Assert.False(adapter.AuthorizesConnection);
        Assert.False(adapter.AuthorizesMigration);
        Assert.False(adapter.AuthorizesCutover);
        Assert.False(adapter.AuthorizesDualWrite);
        Assert.False(adapter.AuthorizesCrossTenantFallback);
        Assert.False(adapter.CanReadOrWriteTenantDatabase);
        Assert.False(adapter.CertifiesJwtMySqlRestoreOrProduction);
    }

    [Fact]
    public void InvalidConstructorArgumentsFailClosed()
    {
        Assert.Throws<ArgumentException>(() => new C0202PlacementIsolatedSnapshotAdapter("../tenant", Actor, Sha, 8, Evidence()));
        Assert.Throws<ArgumentException>(() => new C0202PlacementIsolatedSnapshotAdapter(Tenant, "../actor", Sha, 8, Evidence()));
        Assert.Throws<ArgumentException>(() => new C0202PlacementIsolatedSnapshotAdapter(Tenant, Actor, "short", 8, Evidence()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C0202PlacementIsolatedSnapshotAdapter(Tenant, Actor, Sha, 0, Evidence()));
        Assert.Throws<ArgumentNullException>(() => new C0202PlacementIsolatedSnapshotAdapter(Tenant, Actor, Sha, 8, null!));
    }

    [Fact]
    public async Task NullRequestIsRejectedBeforeReader()
    {
        var result = await Adapter().EvaluateReadOnlyAsync(null, Tenant, Actor, 8, Sha);
        Assert.Equal("ISOLATED_PLACEMENT_REQUEST_REQUIRED", result.Code);
    }

    [Fact]
    public async Task CrossTenantObservedAndRequestedAreRejected()
    {
        Assert.Equal("ISOLATED_PLACEMENT_TENANT_MISMATCH",
            (await Check(Adapter(), observedTenant: "tenant-B")).Code);
        Assert.Equal("ISOLATED_PLACEMENT_TENANT_MISMATCH",
            (await Check(Adapter(), request: Request() with { TenantId = "tenant-B" })).Code);
    }

    [Fact]
    public async Task ForgedActorAndStaleRevisionAreRejected()
    {
        Assert.Equal("ISOLATED_PLACEMENT_ACTOR_MISMATCH",
            (await Check(Adapter(), observedActor: "user-8")).Code);
        Assert.Equal("ISOLATED_PLACEMENT_REVISION_MISMATCH",
            (await Check(Adapter(), observedRevision: 7)).Code);
        Assert.Equal("ISOLATED_PLACEMENT_REVISION_MISMATCH",
            (await Check(Adapter(), Request() with { ExpectedRevision = 7 })).Code);
    }

    [Fact]
    public async Task OldSourceShaDoesNotPass()
    {
        Assert.Equal("ISOLATED_PLACEMENT_SHA_MISMATCH",
            (await Check(Adapter(), observedSha: new string('a', 40))).Code);
        Assert.Equal("ISOLATED_PLACEMENT_SHA_MISMATCH",
            (await Check(Adapter(), Request() with { SourceSha = new string('f', 40) })).Code);
    }

    [Fact]
    public async Task ForgedCapturedOwnerAndActorCannotBeLaundered()
    {
        var items = new[]
        {
            Evidence() with { VerifiedTenantId = "tenant-B" },
            Evidence() with { PersistedOwnerTenantId = "tenant-B" },
            Evidence() with { SourceSha = new string('f', 40) },
            Evidence() with { Snapshot = Snapshot() with { AuthenticatedActorId = "user-8" } },
            Evidence() with { Snapshot = Snapshot() with { AssignmentOwnerTenantId = "tenant-B" } },
            Evidence() with { Snapshot = Snapshot() with { CurrentAssignmentRevision = 7 } }
        };
        foreach (var evidence in items)
            Assert.Equal("ISOLATED_PLACEMENT_CAPTURED_EVIDENCE_MISMATCH",
                (await Check(Adapter(evidence))).Code);
    }

    [Fact]
    public async Task DsnLiveCutoverDestructiveSchemaAndTenantFallbackDenied()
    {
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await Check(Adapter(), Request() with { ClientSuppliesDsn = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await Check(Adapter(), Request() with { RequestsLiveCutoverOrDualWrite = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await Check(Adapter(), Request() with { RequestsDestructiveSchema = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await Check(Adapter(), Request() with { CrossTenantFallback = true })).Code);
    }

    [Fact]
    public async Task JwtMembershipAndRbacRevocationDenyPlacement()
    {
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(Adapter(Evidence() with { CurrentJwtAndMembershipVerified = false }))).Code);
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(Adapter(Evidence() with { FreshRelationalRbacVerified = false }))).Code);
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            (await Check(Adapter(Evidence() with { Snapshot = Snapshot() with { TenantSuspendedOrRevoked = true } }))).Code);
    }

    [Fact]
    public async Task UntrustedBindingAndStaleRoleAreDenied()
    {
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED",
            (await Check(Adapter(Evidence() with { TargetBindingFromTrustedCatalog = false }),
                Request(DomainRules.Intent.ProposeMigration))).Code);
        Assert.Equal("PLACEMENT_REVIEW_ROLE_REQUIRED",
            (await Check(Adapter(Evidence() with
                { Snapshot = Snapshot() with { CurrentRoleCanReviewPlacement = false } }))).Code);
        Assert.Equal("TARGET_BINDING_NOT_SERVER_APPROVED",
            (await Check(Adapter(), Request(DomainRules.Intent.ProposeMigration)
                with { TargetBindingAlias = "untrusted-binding" })).Code);
    }

    [Fact]
    public async Task MissingRollbackAndSandboxRestoreBlockProposals()
    {
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            (await Check(Adapter(Evidence() with { VerifiedSandboxBackupRestore = false }),
                Request(DomainRules.Intent.ProposeMigration))).Code);
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            (await Check(Adapter(Evidence() with { VerifiedTenantConsistencyProof = false }),
                Request(DomainRules.Intent.ProposeMigration))).Code);
    }

    [Fact]
    public async Task MigrationStillNeedsFreshIdempotencyAndGrant()
    {
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            (await Check(Adapter(), Request(DomainRules.Intent.ProposeMigration)
                with { IdempotencyKey = "" })).Code);
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            (await Check(Adapter(Evidence() with
                { Snapshot = Snapshot() with { CurrentRoleCanProposeMigration = false } }),
                Request(DomainRules.Intent.ProposeMigration))).Code);
    }

    [Fact]
    public async Task CancellationPreventsAnyProviderWork()
    {
        var adapter = Adapter();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await adapter.EvaluateReadOnlyAsync(Request(), Tenant, Actor, 8, Sha, cts.Token));
        Assert.False(adapter.AuthorizesConnection);
    }
}
