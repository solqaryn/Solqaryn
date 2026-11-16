using Solqaryn.Infrastructure.AutomationV5;
using App = Solqaryn.Application.AutomationV5.C0501DesignTokenApplicationService;
using Rules = Solqaryn.Domain.AutomationV5.C0501DesignTokenDomainRules;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.C0501DesignTokenBoundedReviewWorker;
using Service = Solqaryn.Infrastructure.AutomationV5.C0501DesignTokenSafeAuditService;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0501DesignTokenSafeAuditServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class Sink : IC0501DesignTokenAuditSink
    {
        public readonly List<C0501DesignTokenAuditRecord> Records = new();
        public bool Fail { get; set; }
        public bool Throw { get; set; }

        public ValueTask<bool> WriteAsync(C0501DesignTokenAuditRecord record, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (Throw) throw new InvalidOperationException("SECRET_SHOULD_NOT_LEAK");
            Records.Add(record);
            return ValueTask.FromResult(!Fail);
        }
    }

    private sealed class Reader : IC0501DesignTokenAuditPreviewReader
    {
        public int Calls { get; private set; }
        public bool Throw { get; set; }
        public Worker.Result? Forced { get; set; }
        public TaskCompletionSource<bool>? Wait { get; set; }

        public async ValueTask<Worker.Result> ReadAsync(
            IReadOnlyList<Worker.Input> batch, int actorId, int tenantId,
            long revision, int epoch, string sha, CancellationToken ct)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("PRIVATE_TENANT_KEY");
            if (Wait is not null) await Wait.Task.WaitAsync(ct);
            return Forced ?? new Worker.Result("THEME_PREVIEW_ONLY_REAUTH_REQUIRED",
                batch.Select((_, i) => new Worker.Item(i,
                    "AUTHENTICATED_THEME_ADAPTER_REQUIRED")).ToArray());
        }
    }

    private static App.ActorSnapshot Actor() =>
        new(new Rules.TrustedContext(7, 11, true, true, true, false, 7, 9),
            true, true, true, true);

    private static App.CatalogSnapshot Catalog() =>
        new(11, "primary-color", 7,
            new HashSet<string>(StringComparer.Ordinal) { "primary-color" }, true, true);

    private static Service WithRealReader(Sink sink) =>
        new(new C0501DesignTokenBoundedAuditReader(
            new Worker(new C0501DesignTokenIsolatedSnapshotAdapter(Actor(), Catalog(), Sha))), sink);

    private static Worker.Input Input(string key = "attempt-001", int tenant = 11,
        bool directWrite = false, string css = "#123AbC") =>
        new(key, new App.ClientIntent(tenant, 9, 7, "primary-color", "COLOR", css,
            "2c85f621-314a-4f81-88c4-8054e61f812d", false, directWrite));

    private static ValueTask<Service.Result> Check(Service service,
        IReadOnlyList<Worker.Input>? batch = null, string operation = "operation-001",
        int actor = 7, int tenant = 11, long revision = 7, int epoch = 9,
        string? sha = Sha, CancellationToken ct = default) =>
        service.ReviewAsync(operation, batch ?? new[] { Input() },
            actor, tenant, revision, epoch, sha, ct);

    [Fact]
    public async Task RealIsolatedWorkerIsAuditedWithoutAuthorizingAnyEffect()
    {
        var sink = new Sink();
        var result = await Check(WithRealReader(sink));
        Assert.Equal("THEME_AUDIT_EXTERNAL_AUTH_REQUIRED", result.Code);
        Assert.Equal(1, result.ReviewedCount);
        Assert.False(result.AuthorizesThemeMutation);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.GrantsRbacOrJwt);
        Assert.False(result.AppliesCss);
        Assert.False(result.ReadsOrWritesMysql);
        Assert.False(result.PersistsAudit);
        Assert.False(result.StartsHostedWorker);
        Assert.False(result.CertifiesAccessibility);
        Assert.False(result.CertifiesProduction);
        Assert.Single(sink.Records);
        Assert.Equal(1, sink.Records[0].ReviewedCount);
        Assert.DoesNotContain("primary-color", sink.Records[0].ToString());
        Assert.DoesNotContain("#123AbC", sink.Records[0].ToString());
    }

    [Fact]
    public async Task MissingIdentityAndInvalidShaFailBeforeReader()
    {
        var reader = new Reader();
        var service = new Service(reader, new Sink());
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await Check(service, actor: 0)).Code);
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await Check(service, tenant: 0)).Code);
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await Check(service, sha: "short")).Code);
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await Check(service, epoch: 0)).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task EmptyAndOversizedBatchesDenyWithoutReading()
    {
        var reader = new Reader();
        var service = new Service(reader, new Sink());
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await service.ReviewAsync("operation-001", Array.Empty<Worker.Input>(),
                7, 11, 7, 9, Sha)).Code);
        var over = Enumerable.Range(0, 13).Select(i => Input($"attempt-{i:000}")).ToArray();
        Assert.Equal("THEME_AUDIT_TRUSTED_CONTEXT_REQUIRED",
            (await Check(service, over)).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task TenantBOrDirectCssWriteIsRejectedBeforeReader()
    {
        var reader = new Reader();
        var service = new Service(reader, new Sink());
        Assert.Equal("THEME_AUDIT_MIXED_SCOPE_DENIED",
            (await Check(service, new[] { Input(tenant: 22) })).Code);
        Assert.Equal("THEME_AUDIT_MIXED_SCOPE_DENIED",
            (await Check(service, new[] { Input(directWrite: true) })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task DuplicateOperationIsIdempotentlyDenied()
    {
        var reader = new Reader();
        var service = new Service(reader, new Sink());
        Assert.Equal("THEME_AUDIT_EXTERNAL_AUTH_REQUIRED", (await Check(service)).Code);
        Assert.Equal("THEME_AUDIT_DUPLICATE_OPERATION_DENIED", (await Check(service)).Code);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task ReaderExceptionDoesNotLeakSensitiveValues()
    {
        var service = new Service(new Reader { Throw = true }, new Sink());
        var result = await Check(service);
        Assert.Equal("THEME_AUDIT_READER_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("PRIVATE_TENANT_KEY", result.ToString());
    }

    [Fact]
    public async Task FailedAuditSinkPreventsSuccessAndMasksItsException()
    {
        var result = await Check(new Service(new Reader(), new Sink { Fail = true }));
        Assert.Equal("THEME_AUDIT_SINK_UNAVAILABLE", result.Code);
        var thrown = await Check(new Service(new Reader(), new Sink { Throw = true }));
        Assert.Equal("THEME_AUDIT_SINK_UNAVAILABLE", thrown.Code);
        Assert.DoesNotContain("SECRET_SHOULD_NOT_LEAK", thrown.ToString());
    }

    [Fact]
    public async Task ForgedWorkerAuthorityOrWrongCountIsDenied()
    {
        var forged = new Reader { Forced = new Worker.Result("THEME_AUTHORIZED",
            new[] { new Worker.Item(0, "AUTHENTICATED_THEME_ADAPTER_REQUIRED") }) };
        Assert.Equal("THEME_AUDIT_AUTHORITY_DENIED",
            (await Check(new Service(forged, new Sink()))).Code);
        var incomplete = new Reader { Forced = new Worker.Result(
            "THEME_PREVIEW_ONLY_REAUTH_REQUIRED", Array.Empty<Worker.Item>()) };
        Assert.Equal("THEME_AUDIT_AUTHORITY_DENIED",
            (await Check(new Service(incomplete, new Sink()))).Code);
    }

    [Fact]
    public async Task OverlappingCallsAreRejectedWhileFirstReadIsActive()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reader = new Reader { Wait = gate };
        var service = new Service(reader, new Sink());
        var first = Check(service).AsTask();
        for (var i = 0; i < 50 && reader.Calls == 0; i++)
            await Task.Yield();
        Assert.Equal(1, reader.Calls);
        var other = await Check(service, operation: "operation-002");
        Assert.Equal("THEME_AUDIT_CONCURRENT_DENIED", other.Code);
        gate.SetResult(true);
        Assert.Equal("THEME_AUDIT_EXTERNAL_AUTH_REQUIRED", (await first).Code);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task CancellationIsPropagatedWithoutSuccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Check(new Service(new Reader(), new Sink()),
                ct: cancellation.Token));
    }

    [Fact]
    public async Task CapacityOf128RequiresNewReviewSession()
    {
        var reader = new Reader();
        var service = new Service(reader, new Sink());
        for (var i = 0; i < Service.MaximumOperations; i++)
        {
            var result = await Check(service, operation: $"attempt-{i:000}");
            Assert.Equal("THEME_AUDIT_EXTERNAL_AUTH_REQUIRED", result.Code);
        }
        Assert.Equal("THEME_AUDIT_CAPACITY_REAUTH_REQUIRED",
            (await Check(service, operation: "attempt-128")).Code);
        Assert.Equal(Service.MaximumOperations, reader.Calls);
    }
}
