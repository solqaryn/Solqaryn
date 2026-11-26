using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Audit = Solqaryn.Application.AutomationV5.V5UniversalCaseFileQaEvidenceAudit;

namespace Solqaryn.Tests.Application;

public sealed class C0104M0306StructuredLoggerSinkTests
{
    private const string Id = "C01-04.M03.06";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Utc = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    private static Audit.SafeAuditEntry GoodEntry() => new(
        Id, "casefile.timeline", Sha, "verify-solqaryn-project-scope",
        "completed", "success", Utc, "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED");

    private static Audit.Observation GoodObservation() => new(
        Id, "casefile.timeline", Sha, "tenant-A", "case-42", true, true,
        "verify-solqaryn-project-scope", "completed", "success", Utc,
        "private bearer token=secret; patient and client PII");

    private sealed class FrozenClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Utc;
    }

    private sealed class Source(Audit.Observation observation) : IV5UniversalCaseFileQaEvidenceSource
    {
        public ValueTask<Audit.Observation?> ReadAsync(
            Audit.EvidenceSelection selection, CancellationToken cancellationToken) =>
            ValueTask.FromResult<Audit.Observation?>(observation);
    }

    private sealed class ProbeLogger(bool enabled = true) : ILogger<V5UniversalCaseFileQaStructuredLoggerSink>
    {
        public List<(LogLevel Level, int EventId, string Text, KeyValuePair<string, object?>[] Fields)> Calls { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => enabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var fields = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToArray()
                : Array.Empty<KeyValuePair<string, object?>>();
            Calls.Add((logLevel, eventId.Id, formatter(state, exception), fields));
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();
            public void Dispose() { }
        }
    }

    [Fact]
    public async Task TrustedObservationEmitsActualStructuredLogWithNoPIIOrPermission()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        var result = await new Audit(new Source(GoodObservation()), sink, new FrozenClock()).CaptureAsync(
            Id, "casefile.timeline", Sha, "tenant-A", "case-42");

        Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", result.Code);
        var call = Assert.Single(logger.Calls);
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal(1040306, call.EventId);
        Assert.NotEqual(Guid.Empty, result.CorrelationId);
        Assert.Contains(call.Fields, p => p.Key == "CorrelationId" &&
            Equals(p.Value, result.CorrelationId.ToString("N")));
        Assert.Contains(call.Fields, p => p.Key == "MicrotaskId" && Equals(p.Value, Id));
        Assert.Contains(call.Fields, p => p.Key == "SourceSha" && Equals(p.Value, Sha));
        Assert.Contains(call.Fields, p => p.Key == "VerificationClass" &&
            Equals(p.Value, "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED"));
        var emitted = call.Text + string.Join(" ", call.Fields.Select(p => $"{p.Key}={p.Value}"));
        Assert.DoesNotContain("tenant-A", emitted);
        Assert.DoesNotContain("case-42", emitted);
        Assert.DoesNotContain("secret", emitted, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("patient", emitted, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.CertifiesCI);
        Assert.False(result.CertifiesA5);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.AuthorizesCaseWrite);
    }

    [Fact]
    public async Task DirectedJobLogsExactNameOnlyForItsOwnMicrotask()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        var directed = GoodEntry() with
        {
            CheckName = Audit.DirectedDurableCheckName,
            ProviderJob = new Audit.ProviderJobIdentity(Id, Sha, 100, 200, 1, 300, 400, 500,
                Audit.DirectedDurableCheckName, "completed", "success")
        };
        await sink.WriteAsync(directed, default);
        Assert.Contains(Assert.Single(logger.Calls).Fields,
            p => p.Key == "CheckName" && Equals(p.Value, directed.CheckName));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sink.WriteAsync(directed with { MicrotaskId = "C01-04.M03.05" }, default));
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await sink.WriteAsync(directed with { CheckName = directed.CheckName + "\n" }, default));
        Assert.Single(logger.Calls);
    }

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("cancelled")]
    [InlineData("skipped")]
    public async Task CompletedDirectedReceiptWithoutIdentityCannotLog(string conclusion)
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        await Assert.ThrowsAsync<ArgumentException>(async () => await sink.WriteAsync(
            GoodEntry() with
            {
                CheckName = Audit.DirectedDurableCheckName, CheckConclusion = conclusion
            }, default));
        Assert.Empty(logger.Calls);
    }

    [Fact]
    public async Task ProviderIdentityIsStructuredAndBoundToTheAuditEnvelope()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        var name = Audit.DirectedDurableCheckName;
        var job = new Audit.ProviderJobIdentity(Id, Sha, 100, 200, 2, 300, 400, 500,
            name, "completed", "success");
        var observation = GoodObservation() with { CheckName = name, ProviderJob = job };
        var result = await new Audit(new Source(observation), sink, new FrozenClock()).CaptureAsync(
            Id, "casefile.timeline", Sha, "tenant-A", "case-42");
        Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", result.Code);
        Assert.False(result.CertifiesCI);
        Assert.False(result.GrantsTenantAccess);
        var call = Assert.Single(logger.Calls);
        Assert.Contains(call.Fields, p => p.Key == "ProviderRunId" && Equals(p.Value, 200L));
        Assert.Contains(call.Fields, p => p.Key == "ProviderAttempt" && Equals(p.Value, 2));
        Assert.Contains(call.Fields, p => p.Key == "ProviderCheckRunId" && Equals(p.Value, 400L));
        Assert.DoesNotContain("secret", call.Text, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<ArgumentException>(async () => await sink.WriteAsync(
            GoodEntry() with { CheckName = name, ProviderJob = job with { SourceSha = new string('f', 40) } }, default));
        Assert.Single(logger.Calls);
    }

    [Fact]
    public async Task DirectSinkRejectsForgedSensitiveAndUnverifiedFieldsWithoutLogging()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        foreach (var invalid in new[]
        {
            GoodEntry() with { MicrotaskId = "C01-04.M03.06\nPII" },
            GoodEntry() with { MicrotaskId = Id + "\n" },
            GoodEntry() with { SourceSha = Sha + "\n" },
            GoodEntry() with { RouteKey = "casefile.timeline?token=secret" },
            GoodEntry() with { SourceSha = new string('f', 39) },
            GoodEntry() with { CheckName = "provider bearer secret" },
            GoodEntry() with { CheckStatus = "queued", CheckConclusion = "success" },
            GoodEntry() with { CheckConclusion = "skipped", VerificationClass = "A5_PASS" },
            GoodEntry() with { ObservedAtUtc = Utc.ToOffset(TimeSpan.FromHours(-6)) }
        })
        {
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await sink.WriteAsync(invalid, CancellationToken.None));
        }
        Assert.Empty(logger.Calls);
    }

    [Fact]
    public async Task DisabledInformationLoggingFailsClosedWithoutFakeAuditReceipt()
    {
        var logger = new ProbeLogger(enabled: false);
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        var result = await new Audit(new Source(GoodObservation()), sink, new FrozenClock()).CaptureAsync(
            Id, "casefile.timeline", Sha, "tenant-A", "case-42");

        Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.CertifiesCI);
        Assert.False(result.CertifiesA5);
        Assert.False(result.GrantsTenantAccess);
        Assert.Empty(logger.Calls);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sink.WriteAsync(GoodEntry(), CancellationToken.None));
        Assert.Equal("CASEFILE_QA_AUDIT_LOGGING_DISABLED", error.Message);
    }

    [Fact]
    public async Task CrossTenantOrRevokedEvidenceNeverReachesLogSink()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        foreach (var observation in new[]
        {
            GoodObservation() with { VerifiedTenantScope = "tenant-B" },
            GoodObservation() with { SourceAuthenticated = false },
            GoodObservation() with { SourceOwnedByTenant = false },
            GoodObservation() with { VerifiedCaseFileScope = "case-43" }
        })
        {
            var result = await new Audit(new Source(observation), sink, new FrozenClock()).CaptureAsync(
                Id, "casefile.timeline", Sha, "tenant-A", "case-42");
            Assert.Equal("CASEFILE_QA_TRUSTED_EVIDENCE_REQUIRED", result.Code);
        }
        Assert.Empty(logger.Calls);
    }


    [Theory]
    [InlineData(-600, true)]
    [InlineData(-601, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public async Task StaleOrFutureProviderObservationCannotCreateQaAuditReceipt(
        int secondsFromFrozenUtc, bool shouldWrite)
    {
        var observation = GoodObservation() with
        {
            ObservedAtUtc = Utc.AddSeconds(secondsFromFrozenUtc)
        };
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        var audit = new Audit(new Source(observation), sink, new FrozenClock());
        var result = await audit.CaptureAsync(
            Id, "casefile.timeline", Sha, "tenant-A", "case-42");

        Assert.Equal(
            shouldWrite ? "CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING" :
                "CASEFILE_QA_CHECK_OR_TIME_INVALID",
            result.Code);
        Assert.False(result.CertifiesCI);
        Assert.False(result.CertifiesA5);
        if (shouldWrite) Assert.Single(logger.Calls);
        else Assert.Empty(logger.Calls);
    }

    [Fact]
    public async Task CancellationDoesNotEmitLog()
    {
        var logger = new ProbeLogger();
        var sink = new V5UniversalCaseFileQaStructuredLoggerSink(logger);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sink.WriteAsync(GoodEntry(), cts.Token));
        Assert.Empty(logger.Calls);
    }
}
