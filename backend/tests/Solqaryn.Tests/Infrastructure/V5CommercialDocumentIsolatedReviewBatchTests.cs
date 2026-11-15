using System.Linq;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReviewBatch;
using Adapter = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReadAdapter;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5CommercialDocumentIsolatedReviewBatchTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Digest = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private sealed class Principals : IV5CommercialTrustedPrincipalSource
    {
        public Adapter.PrincipalSnapshot? State { get; set; } = new(
            7, 11, Sha, "review-11-12", true, true, true, true, false,
            new HashSet<Rules.Operation> { Rules.Operation.ConfirmOrder },
            false, false);
        public int Reads { get; private set; }
        public ValueTask<Adapter.PrincipalSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            return ValueTask.FromResult(State);
        }
    }
    private sealed class Documents : IV5CommercialTenantDocumentSource
    {
        public Adapter.DocumentSnapshot? State { get; set; } = new(
            12, 11, 11, 11, "BORRADOR", "HNL", 12.50m, 5, Sha, true, false);
        public int Reads { get; private set; }
        public int? TenantRead { get; private set; }
        public ValueTask<Adapter.DocumentSnapshot?> ReadAsync(int tenant, int doc, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            TenantRead = tenant;
            return ValueTask.FromResult(State);
        }
    }
    private static (Worker Batch, Principals Principals, Documents Documents) Setup()
    {
        var principals = new Principals();
        var documents = new Documents();
        return (new Worker(new Adapter(principals, documents)), principals, documents);
    }
    private static Rules.Command Command() => new(
        Rules.Operation.ConfirmOrder, 5, 12.50m, "HNL", "idem-key-12", Digest);
    private static Worker.Ticket Ticket(int documentId = 12) =>
        new(documentId, Command(), Sha);

    [Fact] public async Task NominalReviewNeverGrantsAnyFinancialOperation()
    {
        var s = Setup();
        var v = await s.Batch.InspectBatchAsync(new Worker.Ticket?[] { Ticket() });
        Assert.Equal("REVIEW_RESULTS_ONLY_NO_LIVE_EFFECT", v.Code);
        Assert.Equal("REVIEW_NEEDS_SEPARATE_DURABLE_ADAPTER", Assert.Single(v.Receipts).Code);
        Assert.False(v.IsHostedWorker);
        Assert.False(v.IsDurable);
        Assert.False(v.AuthorizesCommercialAction);
        Assert.False(v.Receipts[0].GrantsPayment);
        Assert.False(v.Receipts[0].GrantsFiscalIssuance);
        Assert.False(v.Receipts[0].GrantsMutation);
        Assert.False(v.Receipts[0].GrantsExport);
        Assert.False(v.Receipts[0].CertifiesLiveJwtMySqlOrPsp);
        Assert.Equal(11, s.Documents.TenantRead);
    }

    [Fact] public async Task NullEmptyAndTooManyTicketsFailBeforeSourceRead()
    {
        var s = Setup();
        Assert.Equal("REVIEW_BATCH_SIZE_REQUIRED", (await s.Batch.InspectBatchAsync(null)).Code);
        Assert.Equal("REVIEW_BATCH_SIZE_REQUIRED",
            (await s.Batch.InspectBatchAsync(Array.Empty<Worker.Ticket?>())).Code);
        Assert.Equal("REVIEW_BATCH_SIZE_REQUIRED",
            (await s.Batch.InspectBatchAsync(Enumerable.Repeat<Worker.Ticket?>(Ticket(), 17).ToArray())).Code);
        Assert.Equal(0, s.Principals.Reads);
    }

    [Fact] public async Task NullTicketInvalidShaOrDigestFailsWithoutReader()
    {
        var s = Setup();
        var result = await s.Batch.InspectBatchAsync(new Worker.Ticket?[]
        {
            null, Ticket() with { DocumentId = 0 },
            Ticket() with { SourceSha = "bad" },
            Ticket() with { Command = Command() with { PayloadSha256 = "bad" } }
        });
        Assert.Equal(4, result.Receipts.Count);
        Assert.All(result.Receipts, x => Assert.Equal("REVIEW_TICKET_INVALID", x.Code));
        Assert.Equal(0, s.Principals.Reads);
    }

    [Fact] public async Task DuplicateReplayIsNotSubmittedTwice()
    {
        var s = Setup();
        var result = await s.Batch.InspectBatchAsync(new Worker.Ticket?[] { Ticket(), Ticket() });
        Assert.Equal("REVIEW_NEEDS_SEPARATE_DURABLE_ADAPTER", result.Receipts[0].Code);
        Assert.Equal("REVIEW_DUPLICATE_NO_ACTION", result.Receipts[1].Code);
        Assert.Equal(1, s.Principals.Reads);
    }

    [Fact] public async Task SameKeyDifferentPayloadOrDocumentConflicts()
    {
        var s = Setup();
        var x = await s.Batch.InspectBatchAsync(new Worker.Ticket?[]
        {
            Ticket(), Ticket() with { DocumentId = 13 },
            Ticket() with { Command = Command() with { Amount = 16.25m } }
        });
        Assert.Equal("REVIEW_IDEMPOTENCY_CONFLICT", x.Receipts[1].Code);
        Assert.Equal("REVIEW_IDEMPOTENCY_CONFLICT", x.Receipts[2].Code);
        Assert.Equal(1, s.Principals.Reads);
    }

    [Fact] public async Task TenantBCannotBeLaunderedViaDocumentOwnership()
    {
        var s = Setup();
        s.Documents.State = s.Documents.State! with { DocumentTenantId = 22 };
        var result = await s.Batch.InspectBatchAsync(new Worker.Ticket?[] { Ticket() });
        Assert.StartsWith("REVIEW_DENIED_", Assert.Single(result.Receipts).Code);
        Assert.False(result.Receipts[0].GrantsTransaction);
    }

    [Fact] public async Task RevokedMembershipAndGrantsFailClosed()
    {
        foreach (var invalid in new[]
        {
            new Principals().State! with { MembershipActive = false },
            new Principals().State! with { GrantsRevoked = true }
        })
        {
            var s = Setup();
            s.Principals.State = invalid;
            var result = await s.Batch.InspectBatchAsync(new Worker.Ticket?[] { Ticket() });
            Assert.StartsWith("REVIEW_DENIED_", Assert.Single(result.Receipts).Code);
            Assert.Equal(0, s.Documents.Reads);
        }
    }

    [Fact] public async Task VersionAmountCurrencyAndFiscalOperationsRemainRejected()
    {
        foreach (var invalid in new[]
        {
            Command() with { ExpectedVersion = 4 },
            Command() with { Amount = 1m },
            Command() with { Currency = "USD" },
            Command() with { RequestedOperation = Rules.Operation.IssueInvoice }
        })
        {
            var s = Setup();
            var r = await s.Batch.InspectBatchAsync(new Worker.Ticket?[]
            { Ticket() with { Command = invalid } });
            Assert.StartsWith("REVIEW_DENIED_", Assert.Single(r.Receipts).Code);
        }
    }

    [Fact] public async Task UpperBoundSixteenRemainsReviewOnly()
    {
        var s = Setup();
        var tickets = Enumerable.Range(1, Worker.MaximumBatch)
            .Select(i => new Worker.Ticket(12, Command() with { IdempotencyKey = "key-" + i }, Sha))
            .Cast<Worker.Ticket?>().ToArray();
        var r = await s.Batch.InspectBatchAsync(tickets);
        Assert.Equal(16, r.Receipts.Count);
        Assert.All(r.Receipts, x => Assert.Equal("REVIEW_NEEDS_SEPARATE_DURABLE_ADAPTER", x.Code));
    }

    [Fact] public async Task CancelledBatchNeverCallsReader()
    {
        var s = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await s.Batch.InspectBatchAsync(new Worker.Ticket?[] { Ticket() }, cts.Token));
        Assert.Equal(0, s.Principals.Reads);
    }
}