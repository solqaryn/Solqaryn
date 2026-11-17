using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Adapter = Solqaryn.Infrastructure.AutomationV5.C1204IsolatedManifestAdapter;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1204SyntheticManifestReviewWorker;
using Sink = Solqaryn.Infrastructure.AutomationV5.C1204SyntheticManifestAuditSink;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1204SyntheticManifestAuditSinkTests
{
    private static readonly string Sha = new('a', 40);

    private sealed class CaptureLogger : ILogger<Sink>
    {
        public readonly List<(LogLevel Level, int Id, string Text)> Events = new();
        public int EnabledChecks;
        public bool Disabled;
        public bool DisableOnSecondCheck;
        public bool ThrowCheck;
        public bool ThrowLog;
        public bool CancelProvider;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Noop.Instance;
        public bool IsEnabled(LogLevel level)
        {
            EnabledChecks++;
            if (ThrowCheck) throw new InvalidOperationException("SECRET_MYSQL_DSN");
            return !Disabled && (!DisableOnSecondCheck || EnabledChecks == 1);
        }

        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (ThrowLog) throw new InvalidOperationException("SECRET_TOKEN_DATABASE");
            if (CancelProvider) throw new OperationCanceledException("SECRET_PROVIDER");
            Events.Add((level, eventId.Id, formatter(state, error)));
        }

        private sealed class Noop : IDisposable
        {
            public static readonly Noop Instance = new();
            public void Dispose() { }
        }
    }

    private static Worker.Review Matched(int count = 2) =>
        new("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF", count);

    [Fact]
    public async Task RealAdapterWorkerMatchProducesBoundedNonAuthoritativeStructuredAudit()
    {
        var actor = new C1204VerifiedActor("actor-01", "tenant-A", true, true, true, true);
        var adapter = new Adapter();
        var staged = adapter.Stage(actor, new Adapter.Draft("tenant-A", "restore-one",
            Sha, 0, new[] { Adapter.Checkpoint.SchemaInventoried,
                            Adapter.Checkpoint.RollbackOutlined }));
        Assert.Equal(1, staged.Revision);
        var worker = new Worker(new C1204SyntheticManifestReadPort(adapter));
        var result = await worker.ReviewAsync(actor,
            new[] { new Worker.Ticket("tenant-A", "restore-one", Sha, 1) });
        Assert.Equal("SYNTHETIC_MANIFEST_REVIEW_ONLY_NO_MYSQL_PROOF", result.Code);

        var logger = new CaptureLogger();
        var receipt = new Sink(logger).Record(result);
        Assert.Equal("SYNTHETIC_AUDIT_ATTEMPTED_NO_MYSQL_PROOF", receipt.Code);
        Assert.True(receipt.LogInvoked);
        Assert.False(receipt.PersistsDurably);
        Assert.False(receipt.CertifiesMysqlBackupOrRestore);
        Assert.False(receipt.AuthorizesSqlOrCutover);
        Assert.False(receipt.GrantsTenantAccess);
        Assert.False(receipt.CertifiesJwtOrHttp);
        Assert.False(receipt.AuthorizesQaOrProduction);
        var entry = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(Sink.AuditEventId, entry.Id);
        Assert.Contains("Examined=1", entry.Text);
        Assert.DoesNotContain("tenant-A", entry.Text);
        Assert.DoesNotContain("restore-one", entry.Text);
        Assert.DoesNotContain(Sha, entry.Text);
    }

    [Theory]
    [InlineData("CURRENT_VERIFIED_SERVER_ACTOR_REQUIRED")]
    [InlineData("INVALID_SYNTHETIC_MANIFEST_BATCH")]
    [InlineData("INVALID_OR_CROSS_TENANT_MANIFEST")]
    [InlineData("SYNTHETIC_SOURCE_UNAVAILABLE")]
    [InlineData("SYNTHETIC_MANIFEST_NOT_VERIFIED")]
    public void AllKnownDenialsUseOnlyBoundedAllowlistedReasonCodes(string code)
    {
        var logger = new CaptureLogger();
        var receipt = new Sink(logger).Record(new Worker.Review(code, 0));
        Assert.Equal("SYNTHETIC_AUDIT_DENIAL_ATTEMPTED", receipt.Code);
        Assert.True(receipt.LogInvoked);
        var entry = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(Sink.AuditEventId, entry.Id);
        Assert.Contains("Reason=" + code, entry.Text);
        Assert.DoesNotContain("tenant-A", entry.Text);
        Assert.DoesNotContain("SECRET", entry.Text);
    }

    [Fact]
    public void UnknownUnsafeCodesAndForgedCountsNeverReachLogger()
    {
        var logger = new CaptureLogger();
        var sink = new Sink(logger);
        foreach (var candidate in new[] {
            new Worker.Review("MYSQL_RESTORE_CERTIFIED", 1),
            new Worker.Review("PRIVATE_PASSWORD=SECRET", 0),
            Matched(0),
            Matched(Worker.MaximumBatch + 1),
            new Worker.Review("SYNTHETIC_SOURCE_UNAVAILABLE", 1),
            default(Worker.Review)
        })
        {
            var receipt = sink.Record(candidate);
            Assert.Equal("SYNTHETIC_AUDIT_INVALID_OUTCOME", receipt.Code);
            Assert.False(receipt.LogInvoked);
        }
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void DisabledLoggerIsFailClosedForPositiveAndNegativeOutcomes()
    {
        var logger = new CaptureLogger { Disabled = true };
        var sink = new Sink(logger);
        Assert.Equal("SYNTHETIC_AUDIT_SINK_DISABLED", sink.Record(Matched()).Code);
        Assert.False(sink.Record(new Worker.Review("SYNTHETIC_SOURCE_UNAVAILABLE", 0)).LogInvoked);
        Assert.Empty(logger.Events);
        var nullLogger = new Sink(NullLogger<Sink>.Instance);
        Assert.False(nullLogger.Record(Matched()).LogInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StatefulLoggerOnlyChecksEnabledOnceAndInvokesLog(bool positive)
    {
        var logger = new CaptureLogger { DisableOnSecondCheck = true };
        var result = new Sink(logger).Record(positive ? Matched(2)
            : new Worker.Review("INVALID_SYNTHETIC_MANIFEST_BATCH", 0));
        Assert.True(result.LogInvoked);
        Assert.Equal(1, logger.EnabledChecks);
        Assert.Single(logger.Events);
        Assert.False(result.PersistsDurably);
    }

    [Fact]
    public void ProviderExceptionsNeverRevealSecretsOrClaimLogging()
    {
        var logger = new CaptureLogger { ThrowCheck = true };
        var sink = new Sink(logger);
        var first = sink.Record(Matched());
        Assert.Equal("SYNTHETIC_AUDIT_SINK_UNAVAILABLE", first.Code);
        Assert.False(first.LogInvoked);
        logger.ThrowCheck = false;
        logger.ThrowLog = true;
        var second = sink.Record(new Worker.Review("SYNTHETIC_SOURCE_UNAVAILABLE", 0));
        Assert.Equal("SYNTHETIC_AUDIT_SINK_UNAVAILABLE", second.Code);
        Assert.False(second.LogInvoked);
        Assert.DoesNotContain("SECRET", second.Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void ProviderCancellationWithoutCallerCancellationIsOpaque()
    {
        var logger = new CaptureLogger { CancelProvider = true };
        var result = new Sink(logger).Record(Matched());
        Assert.Equal("SYNTHETIC_AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.LogInvoked);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void CallerCancellationPropagatesWithoutAnyLogging()
    {
        var logger = new CaptureLogger();
        using var token = new CancellationTokenSource();
        token.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            new Sink(logger).Record(Matched(), token.Token));
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void NullLoggerDependencyIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!));
    }
}
