using Solqaryn.Application.AutomationV5;
using Xunit;
using Worker = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentReadOnlyDispatchWorker;
using Adapter = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentPersistenceProjectionAdapter;
using Rules = Solqaryn.Domain.AutomationV5.V5ContinuityIncidentDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class C0304IsolatedIncidentReadOnlyDispatchWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static Adapter.StoredMetadata Metadata() =>
        new(100, 7, 4, "Open", true, false, true, Sha);

    private static Worker.WorkTicket Ticket(string key = "incident_1") =>
        new(key, 100, 7, Sha, Metadata(), true,
            Rules.IncidentAction.ReadEvidence);

    private static ValueTask<IReadOnlyList<Worker.PreviewReceipt>> Preview(
        params Worker.WorkTicket?[] tickets) =>
        new Worker().PreviewAsync(tickets, 100, 7, Sha);

    [Fact]
    public async Task NominalReadOnlyPreviewNeverDispatchesOrGrants()
    {
        var results = await Preview(Ticket());
        var receipt = Assert.Single(results);
        Assert.Equal("AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED", receipt.Code);
        Assert.False(receipt.Dispatched);
        Assert.False(receipt.AuthorizesEvidenceDisclosure);
        Assert.False(receipt.AuthorizesIncidentMutation);
        Assert.False(receipt.AuthorizesBackupRestore);
        Assert.False(receipt.CertifiesJwtOrPersistence);
    }

    [Fact]
    public async Task TenantBAndForeignBranchAreDeniedBeforeProjection()
    {
        var results = await Preview(
            Ticket("tenant_b") with { ClaimedTenantId = 200 },
            Ticket("branch_b") with { ClaimedBranchId = 8 });
        Assert.Equal("INCIDENT_CROSS_TENANT_QUEUE_DENIED", results[0].Code);
        Assert.Equal("INCIDENT_CROSS_BRANCH_QUEUE_DENIED", results[1].Code);
        Assert.All(results, receipt => Assert.False(receipt.Dispatched));
    }

    [Fact]
    public async Task MissingAndStaleShaCannotBecomeReady()
    {
        var results = await Preview(
            Ticket("stale") with { ClaimedSourceSha = new string('f', 40) },
            Ticket("missing") with { ClaimedSourceSha = null },
            Ticket("source") with { Metadata = Metadata() with { CommitSha = new string('f', 40) } });
        Assert.Equal("INCIDENT_STALE_QUEUE_SHA_DENIED", results[0].Code);
        Assert.Equal("INCIDENT_STALE_QUEUE_SHA_DENIED", results[1].Code);
        Assert.Equal("EXACT_SOURCE_SHA_REQUIRED", results[2].Code);
    }

    [Fact]
    public async Task DuplicateKeysUseOrdinalComparisonAndNeverReplay()
    {
        var results = await Preview(Ticket("event_A"), Ticket("event_A"), Ticket("event_a"));
        Assert.Equal("AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED", results[0].Code);
        Assert.Equal("INCIDENT_TICKET_DUPLICATE", results[1].Code);
        Assert.Equal("AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED", results[2].Code);
        Assert.All(results, receipt => Assert.False(receipt.Dispatched));
    }

    [Fact]
    public async Task UntrustedSourceAndMissingPersistedEvidenceAreDenied()
    {
        var results = await Preview(
            Ticket("untrusted") with { IsolatedServerEvidenceConfirmed = false },
            Ticket("missing") with { Metadata = null },
            Ticket("unverified") with { Metadata = Metadata() with { OwnershipReadFromPersistence = false } });
        Assert.Equal("INCIDENT_UNVERIFIED_WORK_SOURCE", results[0].Code);
        Assert.Equal("VERIFIED_INCIDENT_SOURCE_REQUIRED", results[1].Code);
        Assert.Equal("PERSISTENCE_PROVENANCE_UNVERIFIED", results[2].Code);
    }

    [Theory]
    [InlineData(Rules.IncidentAction.Contain)]
    [InlineData(Rules.IncidentAction.Resolve)]
    [InlineData(Rules.IncidentAction.RequestSandboxRestore)]
    public async Task MutationAndRestoreCannotBeDispatched(Rules.IncidentAction action)
    {
        var result = Assert.Single(await Preview(Ticket() with { Action = action }));
        Assert.Equal("INCIDENT_MUTATION_OR_RESTORE_NOT_DISPATCHED", result.Code);
        Assert.False(result.AuthorizesBackupRestore);
        Assert.False(result.AuthorizesIncidentMutation);
    }

    [Fact]
    public async Task RevokedUnsanitizedAndBadVersionNeverDispatch()
    {
        var results = await Preview(
            Ticket("revoked") with { Metadata = Metadata() with { Revoked = true } },
            Ticket("unsafe") with { Metadata = Metadata() with { EvidenceSanitized = false } },
            Ticket("version") with { Metadata = Metadata() with { Version = -1 } });
        Assert.Equal("INCIDENT_ACCESS_REVOKED", results[0].Code);
        Assert.Equal("SANITIZED_EVIDENCE_REQUIRED", results[1].Code);
        Assert.Equal("INVALID_INCIDENT_VERSION", results[2].Code);
    }

    [Fact]
    public async Task NullEmptyMalformedAndOverLimitBatchesFailClosed()
    {
        var worker = new Worker();
        Assert.Equal("INCIDENT_BATCH_REQUIRED", Assert.Single(
            await worker.PreviewAsync(null, 100, 7, Sha)).Code);
        Assert.Equal("INCIDENT_BATCH_LIMIT_DENIED", Assert.Single(
            await Preview()).Code);
        Assert.Equal("INCIDENT_TICKET_INVALID", Assert.Single(
            await Preview(Ticket("../bad"))).Code);
        Assert.Equal("INCIDENT_TICKET_INVALID", Assert.Single(
            await Preview(new Worker.WorkTicket?[] { null })).Code);
        var excess = Enumerable.Range(1, Worker.MaximumBatchSize + 1)
            .Select(i => (Worker.WorkTicket?)Ticket("key_" + i)).ToArray();
        Assert.Equal("INCIDENT_BATCH_LIMIT_DENIED", Assert.Single(
            await Preview(excess)).Code);
    }

    [Fact]
    public async Task InvalidVerifiedScopeFailsBeforeAnyTicketIsPreviewed()
    {
        var worker = new Worker();
        var list = new Worker.WorkTicket?[] { Ticket() };
        Assert.Equal("VERIFIED_INCIDENT_WORKER_SCOPE_REQUIRED", Assert.Single(
            await worker.PreviewAsync(list, 200, 0, Sha)).Code);
        Assert.Equal("VERIFIED_INCIDENT_WORKER_SCOPE_REQUIRED", Assert.Single(
            await worker.PreviewAsync(list, 100, 7, Sha.ToUpperInvariant())).Code);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutDispatching()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await new Worker().PreviewAsync(
                new Worker.WorkTicket?[] { Ticket() }, 100, 7, Sha, cts.Token);
        });
    }
}
