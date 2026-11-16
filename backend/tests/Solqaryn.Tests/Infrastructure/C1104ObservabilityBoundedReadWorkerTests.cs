using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityBoundedReadWorker;
using Port = Solqaryn.Infrastructure.AutomationV5.IC1104BoundedObservabilityPreviewPort;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1104ObservabilityBoundedReadWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class FakePort : Port
    {
        public int Calls;
        public bool Throws;
        public string Code = "AUTHENTICATED_TENANT_SCOPED_ADAPTER_REQUIRED";
        public Task<C1104ObservabilityIsolatedSnapshotAdapter.Outcome> InspectAsync(
            C1104ObservabilityAuthorizationGate.Request request,
            string tenant, string observedSha, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("private tenant token");
            return Task.FromResult(new C1104ObservabilityIsolatedSnapshotAdapter.Outcome(Code));
        }
    }

    private static Worker.Ticket Ticket(
        string signal = "http_requests", string corr = "corr-01",
        string sha = Sha, string tenant = "tenant-A") =>
        new(signal, corr, sha, tenant, tenant);

    private static void NoGrants(Worker.Review r)
    {
        Assert.False(r.GrantsTenantRead);
        Assert.False(r.ExposesTelemetry);
        Assert.False(r.WritesData);
        Assert.False(r.StartsHostedWorker);
        Assert.False(r.ExportsMetrics);
        Assert.False(r.CertifiesSloOrCost);
        Assert.False(r.GrantsJwtOrRbac);
        Assert.False(r.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task TwoValidSyntheticEntriesNeedAuthenticatedRuntimeAdapter()
    {
        var fake = new FakePort();
        var result = await new Worker(fake).InspectBatchAsync(new[] {
            Ticket(), Ticket("error_rate", "corr-02")
        });
        Assert.Equal("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", result.Code);
        Assert.Equal(2, result.Examined);
        Assert.Equal(2, fake.Calls);
        NoGrants(result);
    }

    [Fact]
    public async Task NullEmptyAndOversizedBatchDoNotCallSource()
    {
        var fake = new FakePort();
        var worker = new Worker(fake);
        Assert.Equal("OBSERVABILITY_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.InspectBatchAsync(null)).Code);
        Assert.Equal("OBSERVABILITY_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.InspectBatchAsync(Array.Empty<Worker.Ticket>())).Code);
        var many = Enumerable.Range(0, 17).Select(i => Ticket(corr: "corr-" + i)).ToArray();
        Assert.Equal("OBSERVABILITY_BATCH_REQUIRED_OR_TOO_LARGE",
            (await worker.InspectBatchAsync(many)).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task ForeignTenantAndClientExportRequestDeniedBeforeIO()
    {
        var fake = new FakePort();
        foreach (var item in new[] {
            Ticket() with { ObservedSyntheticTenant = "tenant-B" },
            Ticket() with { RequestsExportOrRelease = true },
            Ticket() with { VerifiedSyntheticTenant = "../tenant" }
        })
            Assert.Equal("OBSERVABILITY_UNTRUSTED_OR_INVALID_TICKET",
                (await new Worker(fake).InspectBatchAsync(new[] { item })).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task MixedTenantOrShaCannotCombineTenantSnapshots()
    {
        var fake = new FakePort();
        Assert.Equal("OBSERVABILITY_MIXED_TENANT_OR_SOURCE_SHA",
            (await new Worker(fake).InspectBatchAsync(new[] {
                Ticket(), Ticket(corr: "corr-02", tenant: "tenant-B")
            })).Code);
        Assert.Equal("OBSERVABILITY_MIXED_TENANT_OR_SOURCE_SHA",
            (await new Worker(fake).InspectBatchAsync(new[] {
                Ticket(), Ticket(corr: "corr-02", sha: OtherSha)
            })).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task StaleMalformedOrUppercaseShaRejected()
    {
        var fake = new FakePort();
        foreach (var item in new[] {
            Ticket(sha: "bad"),
            Ticket(sha: Sha.ToUpperInvariant()),
            Ticket(corr: "bad correlation"),
            Ticket(signal: "raw_client_token")
        })
            Assert.Equal("OBSERVABILITY_UNTRUSTED_OR_INVALID_TICKET",
                (await new Worker(fake).InspectBatchAsync(new[] { item })).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task DuplicateSignalCorrelationRejectedOrdinally()
    {
        var fake = new FakePort();
        Assert.Equal("OBSERVABILITY_DUPLICATE_TICKET",
            (await new Worker(fake).InspectBatchAsync(new[] { Ticket(), Ticket() })).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task CaseDistinctCorrelationIsNotPermissivelyMerged()
    {
        var fake = new FakePort();
        var result = await new Worker(fake).InspectBatchAsync(new[] {
            Ticket(corr: "corr-a"), Ticket("error_rate", "CORR-A")
        });
        Assert.Equal(2, result.Examined);
        Assert.Equal(2, fake.Calls);
        NoGrants(result);
    }

    [Fact]
    public async Task InvalidLastEntryBlocksAllSourceReads()
    {
        var fake = new FakePort();
        Assert.Equal("OBSERVABILITY_UNTRUSTED_OR_INVALID_TICKET",
            (await new Worker(fake).InspectBatchAsync(new[] {
                Ticket(), Ticket("error_rate", "corr-02") with { RequestsExportOrRelease = true }
            })).Code);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public async Task ProviderDenialReturnsNoPartialAcceptedData()
    {
        var fake = new FakePort { Code = "NO_MEDIDO" };
        var result = await new Worker(fake).InspectBatchAsync(new[] { Ticket() });
        Assert.Equal("OBSERVABILITY_SOURCE_REJECTED", result.Code);
        Assert.Equal(0, result.Examined);
        NoGrants(result);
    }

    [Fact]
    public async Task ProviderExceptionIsOpaqueAndFailClosed()
    {
        var fake = new FakePort { Throws = true };
        var result = await new Worker(fake).InspectBatchAsync(new[] { Ticket() });
        Assert.Equal("OBSERVABILITY_SOURCE_UNAVAILABLE", result.Code);
        Assert.Equal(0, result.Examined);
        NoGrants(result);
    }

    [Fact]
    public async Task CancelledTokenStopsBeforeAnyProviderCall()
    {
        var fake = new FakePort();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Worker(fake).InspectBatchAsync(new[] { Ticket() }, cts.Token));
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public void NullDependencyDeniedAtConstruction() =>
        Assert.Throws<ArgumentNullException>(() => new Worker(null!));

    [Fact]
    public void AdapterPortRequiresIsolatedAdapter() =>
        Assert.Throws<ArgumentNullException>(() => new C1104ObservabilitySnapshotPreviewPort(null!));
}
