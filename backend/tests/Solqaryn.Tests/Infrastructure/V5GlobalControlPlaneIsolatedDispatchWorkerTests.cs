using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5GlobalControlPlaneDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5GlobalControlPlaneAuthorizationGate;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5GlobalControlPlaneIsolatedDispatchWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5GlobalControlPlaneIsolatedDispatchWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 13, 0, 0, TimeSpan.Zero);

    private static Rules.TrustedSnapshot Snapshot() => new(
        "actor-a", "tenant-a", "tenant-a", true, true, true,
        false, false, true, true, true, "audit-42",
        false, false, null, null, null, false, null);

    private static Gate.Evidence Evidence() =>
        new(Sha, "actor-a", "tenant-a", true, true, true, false, Now, Snapshot());

    private static Gate.Request Request() =>
        new(Rules.Operation.OwnTenantMetadataRead, "tenant-a", Sha,
            null, false, false, false);

    private static Worker Create(Gate.Evidence? evidence = null) =>
        new(new V5GlobalControlPlaneIsolatedSnapshotAdapter(
            "actor-a", "tenant-a", Sha, evidence ?? Evidence()));

    private static Worker.Ticket Ticket(string key = "job_1") =>
        new(key, "actor-a", "tenant-a", Sha, Request(), true);

    private static ValueTask<IReadOnlyList<Worker.Receipt>> Preview(
        params Worker.Ticket?[] tickets) =>
        Create().PreviewAsync(tickets, "actor-a", "tenant-a", Sha);

    private static void NeverGrants(Worker.Receipt receipt)
    {
        Assert.False(receipt.Dispatched);
        Assert.False(receipt.GrantsAccess);
        Assert.False(receipt.GrantsPlatformDirectory);
        Assert.False(receipt.GrantsExceptionalLease);
        Assert.False(receipt.MutatesInstitutionalData);
        Assert.False(receipt.CertifiesAuthenticatedRuntime);
    }

    [Fact]
    public async Task NominalBatchNeedsRealAuthenticatedRuntimeAndGrantsNothing()
    {
        var receipt = Assert.Single(await Preview(Ticket()));
        Assert.Equal("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", receipt.Code);
        NeverGrants(receipt);
    }

    [Fact]
    public async Task TenantBActorMismatchAndRequestTenantSpoofFailClosed()
    {
        var batch = await Preview(
            Ticket("tenantB") with { ClaimedTenantId = "tenant-b" },
            Ticket("actorB") with { ClaimedActorId = "actor-b" },
            Ticket("targetB") with { Request = Request() with { RequestedTenantId = "tenant-b" } });
        Assert.Equal("CONTROL_PLANE_BATCH_TENANT_MISMATCH", batch[0].Code);
        Assert.Equal("CONTROL_PLANE_BATCH_ACTOR_MISMATCH", batch[1].Code);
        Assert.Equal("CONTROL_PLANE_BATCH_TENANT_MISMATCH", batch[2].Code);
        Assert.All(batch, NeverGrants);
    }

    [Fact]
    public async Task StaleShaAndCapturedStaleEvidenceAreRejected()
    {
        var batch = await Preview(
            Ticket("stale") with { ClaimedSourceSha = new string('f', 40) },
            Ticket("request") with { Request = Request() with { ExpectedSourceSha = new string('f', 40) } });
        Assert.Equal("CONTROL_PLANE_BATCH_STALE_SHA", batch[0].Code);
        Assert.Equal("CONTROL_PLANE_BATCH_STALE_SHA", batch[1].Code);
        var worker = Create(Evidence() with { ObservedSourceSha = new string('f', 40) });
        var result = Assert.Single(await worker.PreviewAsync(
            new Worker.Ticket?[] { Ticket() }, "actor-a", "tenant-a", Sha));
        Assert.Equal("ISOLATED_CONTROL_PLANE_SOURCE_SHA_MISMATCH", result.Code);
    }

    [Fact]
    public async Task DuplicateKeysAreOrdinalAndCannotDispatchTwice()
    {
        var batch = await Preview(Ticket("same_A"), Ticket("same_A"), Ticket("same_a"));
        Assert.Equal("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", batch[0].Code);
        Assert.Equal("CONTROL_PLANE_TICKET_DUPLICATE", batch[1].Code);
        Assert.Equal("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", batch[2].Code);
        Assert.All(batch, NeverGrants);
    }

    [Fact]
    public async Task UntrustedAndUnsafeTicketsAreDeniedBeforeAppGate()
    {
        var batch = await Preview(
            Ticket("untrusted") with { IsolatedBackendProvenanceClaim = false },
            Ticket("client") with { Request = Request() with { ClientAuthorityHint = true } },
            Ticket("export") with { Request = Request() with { WriteOrExport = true } },
            Ticket("sensitive") with { Request = Request() with { SensitiveData = true } });
        Assert.Equal("CONTROL_PLANE_UNVERIFIED_TICKET_SOURCE", batch[0].Code);
        Assert.All(batch.Skip(1), r => Assert.Equal("CONTROL_PLANE_UNSAFE_TICKET_DENIED", r.Code));
        Assert.All(batch, NeverGrants);
    }

    [Fact]
    public async Task RevocationMfaAndIndependentPlatformGrantStillRequired()
    {
        var revoked = Create(Evidence() with { PermissionRevoked = true });
        var revokedResult = Assert.Single(await revoked.PreviewAsync(
            new Worker.Ticket?[] { Ticket() }, "actor-a", "tenant-a", Sha));
        Assert.Equal("CONTROL_PLANE_CURRENT_BACKEND_AUTHORITY_REQUIRED", revokedResult.Code);

        var noMfa = Create(Evidence() with
        { Snapshot = Snapshot() with { RecentMfaVerified = false } });
        Assert.Equal("MFA_AND_REAUTHENTICATION_REQUIRED", Assert.Single(
            await noMfa.PreviewAsync(new Worker.Ticket?[] { Ticket() },
                "actor-a", "tenant-a", Sha)).Code);

        var directory = Ticket() with
        { Request = Request() with { Operation = Rules.Operation.PlatformTenantDirectoryRead } };
        var result = Assert.Single(await Preview(directory));
        Assert.Equal("INDEPENDENT_PLATFORM_GRANT_REQUIRED", result.Code);
        NeverGrants(result);
    }

    [Fact]
    public async Task MissingNullMalformedAndOverflowBatchFailClosed()
    {
        var worker = Create();
        Assert.Equal("CONTROL_PLANE_BATCH_REQUIRED", Assert.Single(
            await worker.PreviewAsync(null, "actor-a", "tenant-a", Sha)).Code);
        Assert.Equal("CONTROL_PLANE_BATCH_LIMIT_DENIED", Assert.Single(
            await Preview()).Code);
        Assert.Equal("CONTROL_PLANE_TICKET_INVALID", Assert.Single(
            await Preview(Ticket("../bad"))).Code);
        Assert.Equal("CONTROL_PLANE_TICKET_INVALID", Assert.Single(
            await Preview(null)).Code);
        var tooMany = Enumerable.Range(1, Worker.MaximumBatchSize + 1)
            .Select(i => (Worker.Ticket?)Ticket("job_" + i)).ToArray();
        Assert.Equal("CONTROL_PLANE_BATCH_LIMIT_DENIED", Assert.Single(
            await Preview(tooMany)).Code);
    }

    [Fact]
    public async Task InvalidVerifiedActorTenantOrShaIsDeniedBeforePreview()
    {
        var worker = Create();
        var one = new Worker.Ticket?[] { Ticket() };
        Assert.Equal("VERIFIED_CONTROL_PLANE_BATCH_SCOPE_REQUIRED", Assert.Single(
            await worker.PreviewAsync(one, "actor-a", "", Sha)).Code);
        Assert.Equal("VERIFIED_CONTROL_PLANE_BATCH_SCOPE_REQUIRED", Assert.Single(
            await worker.PreviewAsync(one, "actor-a", "tenant-a", Sha.ToUpperInvariant())).Code);
        Assert.Equal("VERIFIED_CONTROL_PLANE_BATCH_SCOPE_REQUIRED", Assert.Single(
            await worker.PreviewAsync(one, "../actor-a", "tenant-a", Sha)).Code);
    }

    [Fact]
    public async Task CancellationInterruptsWithoutDispatch()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await Create().PreviewAsync(new Worker.Ticket?[] { Ticket() },
                "actor-a", "tenant-a", Sha, cts.Token);
        });
    }
}
