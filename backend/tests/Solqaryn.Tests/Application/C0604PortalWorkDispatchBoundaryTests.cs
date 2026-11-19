using Solqaryn.Application.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C0604PortalClienteAuthorizationGate;
using Boundary = Solqaryn.Application.AutomationV5.C0604PortalWorkDispatchBoundary;

namespace Solqaryn.Tests.Application;

public sealed class C0604PortalWorkDispatchBoundaryTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTime Utc = new(2026, 11, 18, 23, 0, 0, DateTimeKind.Utc);

    private sealed class Reader : IC0604PortalAuthorizationEvidenceReader
    {
        public int Reads { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Gate.Evidence?> ReadCurrentAsync(
            string resourceId, CancellationToken cancellationToken)
        {
            Reads++;
            if (Throws) throw new InvalidOperationException("private token or account data");
            return ValueTask.FromResult<Gate.Evidence?>(null);
        }
    }

    private sealed class Queue : IC0604PortalWorkQueue
    {
        public int Calls { get; private set; }
        public ValueTask EnqueueAsync(Boundary.Command command, CancellationToken cancellationToken)
        {
            Calls++;
            return ValueTask.CompletedTask;
        }
    }

    private static Boundary.Request Valid() => new(
        new Gate.Request("customer_101", Sha, 3, 10, false, false, false),
        "customer-preference-sync", "request_0001", Utc, false);

    [Fact]
    public async Task OptimisticCustomerInputNeverQueuesWithoutLiveAuthorization()
    {
        var reader = new Reader();
        var queue = new Queue();
        var result = await new Boundary(new Gate(reader), queue).TryEnqueueAsync(Valid());
        Assert.Equal("PORTAL_WORK_RUNTIME_AUTHORIZATION_REQUIRED", result.Code);
        Assert.False(result.Enqueued);
        Assert.Equal(1, reader.Reads);
        Assert.Equal(0, queue.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("request 0001")]
    [InlineData("../../../other")]
    public async Task InvalidIdempotencyKeysAreRejectedBeforeReader(string key)
    {
        var reader = new Reader();
        var queue = new Queue();
        var result = await new Boundary(new Gate(reader), queue)
            .TryEnqueueAsync(Valid() with { IdempotencyKey = key });
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED", result.Code);
        Assert.False(result.Enqueued);
        Assert.Equal(0, reader.Reads);
        Assert.Equal(0, queue.Calls);
    }

    [Fact]
    public async Task CrossBoundaryOperationsAndClientAuthorityAreDenied()
    {
        var reader = new Reader();
        var queue = new Queue();
        var target = new Boundary(new Gate(reader), queue);
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with { Operation = "transfer-funds" })).Code);
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with { ExternalDeliveryRequested = true })).Code);
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with
                { Authorization = Valid().Authorization! with { ClientSuppliedRole = true } })).Code);
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with
                { Authorization = Valid().Authorization! with { AttemptsWriteOrExport = true } })).Code);
        Assert.Equal(0, reader.Reads);
        Assert.Equal(0, queue.Calls);
    }

    [Fact]
    public async Task InvalidClockAndVersionDoNotAccessProviders()
    {
        var reader = new Reader();
        var target = new Boundary(new Gate(reader), new Queue());
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with
                { RequestedAtUtc = DateTime.SpecifyKind(Utc, DateTimeKind.Unspecified) })).Code);
        Assert.Equal("PORTAL_WORK_REQUEST_DENIED",
            (await target.TryEnqueueAsync(Valid() with
                { Authorization = Valid().Authorization! with { ExpectedVersion = 0 } })).Code);
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public async Task FailureIsOpaqueAndRepeatedRequestsDoNotDispatch()
    {
        var reader = new Reader { Throws = true };
        var queue = new Queue();
        var target = new Boundary(new Gate(reader), queue);
        for (var i = 0; i < 2; i++)
        {
            var result = await target.TryEnqueueAsync(Valid());
            Assert.Equal("PORTAL_WORK_TRUSTED_EVIDENCE_UNAVAILABLE", result.Code);
            Assert.False(result.Enqueued);
        }
        Assert.Equal(2, reader.Reads);
        Assert.Equal(0, queue.Calls);
    }

    [Fact]
    public async Task CancelledRequestCannotTouchQueueOrReader()
    {
        var reader = new Reader();
        var queue = new Queue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Boundary(new Gate(reader), queue).TryEnqueueAsync(Valid(), cts.Token));
        Assert.Equal(0, reader.Reads);
        Assert.Equal(0, queue.Calls);
    }

    [Fact]
    public void ConstructorRejectsMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new Boundary(null!, new Queue()));
        Assert.Throws<ArgumentNullException>(() => new Boundary(new Gate(new Reader()), null!));
    }
}
