using System.Linq;
using Gate = Solqaryn.Application.AutomationV5.C0202PlacementApplicationGate;
using DomainRules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0202PlacementSandboxRehearsalJournalTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Tenant = "tenant-A";
    private const string Actor = "user-7";

    private static DomainRules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = Tenant, AuthenticatedActorId = Actor,
        JwtValidatedByBackend = true, CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true, CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true, AssignmentOwnerTenantId = Tenant,
        CurrentAssignmentRevision = 8, CurrentBindingAlias = "shared-cluster",
        BindingFromTrustedCatalog = true, UniqueActiveAssignmentVerified = true
    };
    private static Gate.Evidence Evidence() =>
        new(Sha, Tenant, Tenant, "dedicated-tenant-A",
            true, true, true, true, true, true, true, true, true, true, Snapshot());
    private static Gate.Request Request() =>
        new(Tenant, Sha, DomainRules.Intent.ProposeMigration,
            DomainRules.PlacementMode.Dedicated, "dedicated-tenant-A", 8, "idem-12345");
    private static C0202PlacementSandboxRehearsalJournal Journal(Gate.Evidence? e = null) =>
        new(new C0202PlacementIsolatedSnapshotAdapter(Tenant, Actor, Sha, 8, e ?? Evidence()),
            Tenant, Actor, Sha, 8, "shared-cluster");
    private static Task<C0202PlacementSandboxRehearsalJournal.Outcome> Stage(
        C0202PlacementSandboxRehearsalJournal journal, Gate.Request? request = null,
        string tenant = Tenant, string actor = Actor, long revision = 8,
        string sha = Sha, CancellationToken cancellationToken = default) =>
        journal.StageAsync(request ?? Request(), tenant, actor, revision, sha, cancellationToken);

    [Fact] public async Task StagingIsOnlySyntheticAndDoesNotGrantStorage()
    {
        var j = Journal();
        var result = await Stage(j);
        Assert.Equal("SANDBOX_STAGE_RECORDED_NO_LIVE_EFFECT", result.Code);
        Assert.Equal(9, result.SandboxRevision);
        Assert.False(result.GrantsStorageConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.WritesMySqlOrEf);
        Assert.False(result.IsDurable);
        Assert.False(result.CertifiesProductionOrJwt);
    }

    [Fact] public async Task TenantBAndForgedActorCannotSeeOrStageTenantA()
    {
        var j = Journal();
        Assert.Equal("SANDBOX_TENANT_OR_ACTOR_DENIED",
            j.InspectSynthetic("tenant-B", Actor).Code);
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, tenant: "tenant-B")).Code);
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, actor: "user-8")).Code);
        Assert.Equal(8, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task StaleRevisionAndSourceShaDoNotChangeProjection()
    {
        var j = Journal();
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, revision: 7)).Code);
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, sha: new string('f', 40))).Code);
        Assert.Equal(8, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task ClientDsnDualWriteCutoverAndDestructiveFlagsFailClosed()
    {
        foreach (var request in new[]
        {
            Request() with { ClientSuppliesDsn = true },
            Request() with { RequestsLiveCutoverOrDualWrite = true },
            Request() with { RequestsDestructiveSchema = true },
            Request() with { CrossTenantFallback = true }
        })
            Assert.Equal("SANDBOX_PREFLIGHT_DENIED", (await Stage(Journal(), request)).Code);
    }

    [Fact] public async Task RevokedJwtRbacOrMissingRestoreEvidenceBlocksStage()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { CurrentJwtAndMembershipVerified = false },
            Evidence() with { FreshRelationalRbacVerified = false },
            Evidence() with { VerifiedSandboxBackupRestore = false },
            Evidence() with { Snapshot = Snapshot() with { TenantSuspendedOrRevoked = true } }
        })
            Assert.Equal("SANDBOX_PREFLIGHT_DENIED",
                (await Stage(Journal(evidence))).Code);
    }

    [Fact] public async Task InvalidTargetAndIdempotencyNeverPassToAdapter()
    {
        var j = Journal();
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, Request() with { TargetBindingAlias = "../dsn" })).Code);
        Assert.Equal("SANDBOX_UNTRUSTED_COMMAND_DENIED",
            (await Stage(j, Request() with { IdempotencyKey = "" })).Code);
        Assert.Equal(8, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task RepeatedKeyIsIdempotentButDifferentPayloadConflicts()
    {
        var j = Journal();
        Assert.Equal("SANDBOX_STAGE_RECORDED_NO_LIVE_EFFECT", (await Stage(j)).Code);
        Assert.Equal("SANDBOX_ALREADY_STAGED", (await Stage(j)).Code);
        Assert.Equal("SANDBOX_IDEMPOTENCY_CONFLICT",
            (await Stage(j, Request() with { TargetMode = DomainRules.PlacementMode.Shared })).Code);
        Assert.Equal(9, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task DifferentKeyCannotClaimSameSingleAttempt()
    {
        var j = Journal();
        await Stage(j);
        Assert.Equal("SANDBOX_SINGLE_ATTEMPT_ONLY",
            (await Stage(j, Request() with { IdempotencyKey = "idem-other" })).Code);
        Assert.Equal(9, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task ConcurrentCasOnlyOneDistinctStageWins()
    {
        var j = Journal();
        var tasks = Enumerable.Range(0, 24).Select(i => Task.Run(async () =>
            await Stage(j, Request() with { IdempotencyKey = "idem-" + i })));
        var outcomes = await Task.WhenAll(tasks);
        Assert.Single(outcomes.Where(x => x.Code == "SANDBOX_STAGE_RECORDED_NO_LIVE_EFFECT"));
        Assert.Equal(9, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public async Task RollbackRequiresStageKeyTenantActorAndMonotonicCas()
    {
        var j = Journal();
        await Stage(j);
        Assert.Equal("SANDBOX_ROLLBACK_IDENTITY_DENIED",
            (await j.RollbackAsync(Request(), "tenant-B", Actor, Sha, 9)).Code);
        Assert.Equal("SANDBOX_UNKNOWN_ROLLBACK_KEY",
            (await j.RollbackAsync(Request() with { IdempotencyKey = "idem-other" },
                Tenant, Actor, Sha, 9)).Code);
        Assert.Equal("SANDBOX_ROLLBACK_CAS_MISMATCH",
            (await j.RollbackAsync(Request(), Tenant, Actor, Sha, 8)).Code);
        var result = await j.RollbackAsync(Request(), Tenant, Actor, Sha, 9);
        Assert.Equal("SANDBOX_ROLLBACK_RECORDED_NO_LIVE_EFFECT", result.Code);
        Assert.Equal(10, result.SandboxRevision);
        Assert.Equal("SANDBOX_ROLLBACK_ALREADY_RECORDED",
            (await j.RollbackAsync(Request(), Tenant, Actor, Sha, 10)).Code);
        Assert.Equal("SANDBOX_ALREADY_ROLLED_BACK", (await Stage(j)).Code);
    }

    [Fact] public async Task CancelledStageAndRollbackCannotAlterSandbox()
    {
        var j = Journal();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Stage(j, cancellationToken: cts.Token));
        Assert.Equal(8, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
        await Stage(j);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await j.RollbackAsync(Request(), Tenant, Actor, Sha, 9, cts.Token));
        Assert.Equal(9, j.InspectSynthetic(Tenant, Actor).SandboxRevision);
    }

    [Fact] public void ConstructorRejectsUnsafeIdentityShaAndRevision()
    {
        var adapter = new C0202PlacementIsolatedSnapshotAdapter(Tenant, Actor, Sha, 8, Evidence());
        Assert.Throws<ArgumentException>(() => new C0202PlacementSandboxRehearsalJournal(
            adapter, "../tenant", Actor, Sha, 8, "shared"));
        Assert.Throws<ArgumentException>(() => new C0202PlacementSandboxRehearsalJournal(
            adapter, Tenant, Actor, "bad", 8, "shared"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new C0202PlacementSandboxRehearsalJournal(
            adapter, Tenant, Actor, Sha, 0, "shared"));
    }
}