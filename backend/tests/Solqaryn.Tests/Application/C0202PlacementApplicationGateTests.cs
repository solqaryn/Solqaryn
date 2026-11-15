using Solqaryn.Application.AutomationV5;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0202PlacementApplicationGateTests
{
    private const string Tenant = "tenant-A";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static DomainRules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = Tenant, AuthenticatedActorId = "user-7",
        JwtValidatedByBackend = true, CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = Tenant,
        CurrentAssignmentRevision = 8,
        CurrentBindingAlias = "shared-cluster",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };
    private static C0202PlacementApplicationGate.Evidence Evidence() => new(
        Sha, Tenant, Tenant, "dedicated-tenant-A", true, true,
        true, true, true, true, true, true, true, true, Snapshot());

    private static C0202PlacementApplicationGate.Request Request(
        DomainRules.Intent intent = DomainRules.Intent.InspectExistingAssignment) =>
        new(Tenant, Sha, intent, DomainRules.PlacementMode.Shared,
            "dedicated-tenant-A", 8, "idempotency-123");

    private sealed class Reader(C0202PlacementApplicationGate.Evidence? evidence)
        : IC0202PlacementEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(evidence);
        }
    }
    private static async Task<C0202PlacementApplicationGate.Result> Check(
        C0202PlacementApplicationGate.Request? request = null,
        C0202PlacementApplicationGate.Evidence? evidence = null)
    {
        var gate = new C0202PlacementApplicationGate(new Reader(evidence ?? Evidence()));
        return await gate.EvaluateAsync(request ?? Request());
    }

    [Fact]
    public async Task ValidInspectionDoesNotSelectAConnection()
    {
        var v = await Check();
        Assert.True(v.RequiresSeparateAuthenticatedStorageAdapter);
        Assert.False(v.AuthorizesConnection);
        Assert.False(v.AuthorizesMigration);
        Assert.False(v.AuthorizesCutover);
        Assert.False(v.AuthorizesProduction);
    }
    [Fact]
    public async Task VerifiedDedicatedProposalStillDoesNotMigrate()
    {
        var v = await Check(request: Request(DomainRules.Intent.ProposeMigration)
            with { TargetMode = DomainRules.PlacementMode.Dedicated });
        Assert.True(v.RequiresSeparateAuthenticatedStorageAdapter);
        Assert.False(v.AuthorizesMigration);
    }
    [Fact]
    public async Task InvalidShaRevisionAndEnumsFailBeforeReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(Request() with { SourceSha = "short" })).Code);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(Request() with { ExpectedRevision = 0 })).Code);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(Request() with { Intent = (DomainRules.Intent)999 })).Code);
        Assert.Equal(0, reader.Calls);
    }
    [Fact]
    public async Task ClientDsnCutoverAndFallbackAreDeniedBeforeReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await gate.EvaluateAsync(Request() with { ClientSuppliesDsn = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await gate.EvaluateAsync(Request() with { RequestsLiveCutoverOrDualWrite = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await gate.EvaluateAsync(Request() with { CrossTenantFallback = true })).Code);
        Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION",
            (await gate.EvaluateAsync(Request() with { RequestsDestructiveSchema = true })).Code);
        Assert.Equal(0, reader.Calls);
    }
    [Fact]
    public async Task TenantBOrForgedOwnershipFails()
    {
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED",
            (await Check(request: Request() with { TenantId = "tenant-B" })).Code);
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED",
            (await Check(evidence: Evidence() with { PersistedOwnerTenantId = "tenant-B" })).Code);
    }
    [Fact]
    public async Task OldShaOrMissingReaderEvidenceFails()
    {
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED",
            (await Check(evidence: Evidence() with { SourceSha = new string('a', 40) })).Code);
        var gate = new C0202PlacementApplicationGate(new Reader(null));
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED",
            (await gate.EvaluateAsync(Request())).Code);
    }
    [Fact]
    public async Task RevokedIdentityOrRoleMustFail()
    {
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentJwtAndMembershipVerified = false })).Code);
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(evidence: Evidence() with { FreshRelationalRbacVerified = false })).Code);
        Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED",
            (await Check(evidence: Evidence() with { Snapshot = Snapshot() with { TenantSuspendedOrRevoked = true } })).Code);
    }
    [Fact]
    public async Task NoTrustedReaderOrFenceNoAcceptance()
    {
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(evidence: Evidence() with { ServerTrustedReader = false })).Code);
        Assert.Equal("FRESH_BACKEND_STORAGE_AUTHORITY_REQUIRED",
            (await Check(evidence: Evidence() with { CurrentRevisionFenceVerified = false })).Code);
    }
    [Fact]
    public async Task TargetAliasMustMatchServerCatalog()
    {
        Assert.Equal("TARGET_BINDING_NOT_SERVER_APPROVED",
            (await Check(request: Request(DomainRules.Intent.ProposeMigration)
                with { TargetBindingAlias = "untrusted-other-cluster" })).Code);
        Assert.Equal("TRUSTED_TARGET_BINDING_REQUIRED",
            (await Check(request: Request(DomainRules.Intent.ProposeMigration),
                evidence: Evidence() with { TargetBindingFromTrustedCatalog = false })).Code);
    }
    [Fact]
    public async Task MissingVerifiedRestoreOrConsistencyBlocksProposal()
    {
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            (await Check(request: Request(DomainRules.Intent.ProposeMigration),
                evidence: Evidence() with { VerifiedSandboxBackupRestore = false })).Code);
        Assert.Equal("RECOVERY_AND_CONSISTENCY_EVIDENCE_REQUIRED",
            (await Check(request: Request(DomainRules.Intent.ProposeMigration),
                evidence: Evidence() with { VerifiedTenantConsistencyProof = false })).Code);
    }
    [Fact]
    public async Task MismatchedCasAndMissingMigrationIdempotencyFail()
    {
        Assert.Equal("STALE_PLACEMENT_REVISION",
            (await Check(request: Request() with { ExpectedRevision = 7 })).Code);
        Assert.Equal("MIGRATION_INTENT_AUTHORITY_REQUIRED",
            (await Check(request: Request(DomainRules.Intent.ProposeMigration)
                with { IdempotencyKey = "" })).Code);
    }
    [Fact]
    public async Task CancelledBeforeReadPropagates()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await gate.EvaluateAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
