using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UxAuditStructuredEventSinkTests
{
    private sealed class CaptureLogger : ILogger<V5UxAuditStructuredEventSink>
    {
        public readonly List<(LogLevel Level, int EventId, string Text)> Entries = new();
        public bool InformationEnabled = true;
        public bool WarningEnabled = true;
        public bool ThrowInIsEnabled;
        public bool ThrowInLog;
        public bool ThrowOperationCanceled;
        public bool DisableAfterFirstEnabledCheck;
        public int EnabledCheckCalls;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Noop.Instance;
        public bool IsEnabled(LogLevel level)
        {
            EnabledCheckCalls++;
            if (ThrowInIsEnabled) throw new InvalidOperationException("SECRET_PROVIDER_DSN");
            if (DisableAfterFirstEnabledCheck && EnabledCheckCalls > 1) return false;
            return level switch
            {
                LogLevel.Information => InformationEnabled,
                LogLevel.Warning => WarningEnabled,
                _ => false
            };
        }

        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOperationCanceled) throw new OperationCanceledException("SECRET_CANCELLED_PROVIDER");
            if (ThrowInLog) throw new InvalidOperationException("SECRET_DATABASE_DSN");
            Entries.Add((level, id.Id, formatter(state, error)));
        }

        private sealed class Noop : IDisposable
        {
            public static readonly Noop Instance = new();
            public void Dispose() { }
        }
    }

    private static V5UxAuditBoundedReadOnlyWorker.ItemOutcome Item(
        int index, string code = "EXTERNAL_EVIDENCE_AND_A5_REQUIRED", int reviewed = 1) =>
        new(index, code, reviewed);

    private static V5UxAuditBoundedReadOnlyWorker.BatchOutcome Staged(int count = 2) =>
        new("STRUCTURAL_REVIEW_ONLY", count,
            Enumerable.Range(0, count).Select(i => Item(i)).ToArray());

    private static V5UxAuditBoundedReadOnlyWorker.BatchOutcome Denied() =>
        new("MIXED_TENANT_OR_SOURCE_BATCH", 0,
            Array.Empty<V5UxAuditBoundedReadOnlyWorker.ItemOutcome>());

    [Fact]
    public void StagedBatchEmitsOnlyBoundedOpaqueCountersAndNeverCertifiesBrowserOrA5()
    {
        var logger = new CaptureLogger();
        var receipt = new V5UxAuditStructuredEventSink(logger).Record(Staged());
        Assert.Equal("UX_AUDIT_STAGED_NOT_CERTIFIED", receipt.Code);
        Assert.True(receipt.Recorded);
        Assert.False(receipt.PersistsDurably);
        Assert.False(receipt.CertifiesBrowserMeasurement);
        Assert.False(receipt.CertifiesA5);
        Assert.False(receipt.GrantsAccess);
        Assert.False(receipt.AllowsInstitutionalWrite);
        Assert.False(receipt.AuthorizesProduction);
        var item = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, item.Level);
        Assert.Equal(150406, item.EventId);
        Assert.Contains("ItemsEvaluated=2", item.Text);
        Assert.DoesNotContain("tenant-A", item.Text);
        Assert.DoesNotContain("JWT", item.Text);
        Assert.DoesNotContain("SECRET", item.Text);
    }

    [Fact]
    public void RejectedBatchLogsWarningWithoutRawCodeOrPrivateData()
    {
        var logger = new CaptureLogger();
        var receipt = new V5UxAuditStructuredEventSink(logger).Record(Denied());
        Assert.Equal("UX_AUDIT_DENIED", receipt.Code);
        Assert.True(receipt.Recorded);
        var line = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, line.Level);
        Assert.Contains("ItemsEvaluated=0", line.Text);
        Assert.DoesNotContain("MIXED_TENANT", line.Text);
    }

    [Fact]
    public void IndividualDenialIsNotMisreportedAsAStagedSuccess()
    {
        var batch = new V5UxAuditBoundedReadOnlyWorker.BatchOutcome("SOME_ITEMS_DENIED", 2,
            new[] { Item(0), Item(1, "TRUSTED_TENANT_CONTEXT_MISMATCH", 0) });
        var logger = new CaptureLogger();
        var receipt = new V5UxAuditStructuredEventSink(logger).Record(batch);
        Assert.Equal("UX_AUDIT_DENIED", receipt.Code);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Entries).Level);
        Assert.False(receipt.CertifiesA5);
    }

    [Fact]
    public void ForgedSuccessAndInvalidCountsNeverReachLogger()
    {
        var logger = new CaptureLogger();
        var sink = new V5UxAuditStructuredEventSink(logger);
        var invalid = new V5UxAuditBoundedReadOnlyWorker.BatchOutcome?[] {
            null,
            new("EXTERNAL_CERTIFIED", 1, new[] { Item(0) }),
            new("STRUCTURAL_REVIEW_ONLY", 2, new[] { Item(0) }),
            new("STRUCTURAL_REVIEW_ONLY", 1, new[] { Item(1) }),
            new("STRUCTURAL_REVIEW_ONLY", 1, new[] { Item(0, "DENIED", 0) }),
            new("SOME_ITEMS_DENIED", 1, new[] { Item(0) }),
            new("BATCH_CAPACITY_DENIED", 1, new[] { Item(0) }),
            new("STRUCTURAL_REVIEW_ONLY", 33,
                Enumerable.Range(0, 33).Select(i => Item(i)).ToArray())
        };
        foreach (var batch in invalid)
        {
            var receipt = sink.Record(batch);
            Assert.Equal("UX_AUDIT_INVALID_OUTCOME", receipt.Code);
            Assert.False(receipt.Recorded);
        }
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void DisabledInformationOrWarningAndNullLoggerNeverClaimRecording()
    {
        var logger = new CaptureLogger { InformationEnabled = false, WarningEnabled = false };
        var sink = new V5UxAuditStructuredEventSink(logger);
        Assert.Equal("UX_AUDIT_SINK_DISABLED", sink.Record(Staged()).Code);
        Assert.False(sink.Record(Denied()).Recorded);
        Assert.Empty(logger.Entries);
        var noLogger = new V5UxAuditStructuredEventSink(
            NullLogger<V5UxAuditStructuredEventSink>.Instance);
        Assert.False(noLogger.Record(Staged()).Recorded);
        Assert.False(noLogger.Record(Denied()).Recorded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StatefulLoggerCannotSuppressLogAfterSuccessfulSinglePrecheck(bool staged)
    {
        var logger = new CaptureLogger { DisableAfterFirstEnabledCheck = true };
        var receipt = new V5UxAuditStructuredEventSink(logger)
            .Record(staged ? Staged() : Denied());

        Assert.True(receipt.Recorded);
        Assert.Equal(staged ? "UX_AUDIT_STAGED_NOT_CERTIFIED" : "UX_AUDIT_DENIED", receipt.Code);
        Assert.Equal(1, logger.EnabledCheckCalls);
        var emitted = Assert.Single(logger.Entries);
        Assert.Equal(staged ? LogLevel.Information : LogLevel.Warning, emitted.Level);
        Assert.Equal(V5UxAuditStructuredEventSink.AuditEventId, emitted.EventId);
        Assert.False(receipt.PersistsDurably);
        Assert.False(receipt.CertifiesA5);
    }

    [Fact]
    public void InitiallyDisabledStatefulLoggerDoesNotRecordOrInvokeLog()
    {
        var logger = new CaptureLogger
        {
            InformationEnabled = false,
            DisableAfterFirstEnabledCheck = true
        };
        var receipt = new V5UxAuditStructuredEventSink(logger).Record(Staged());
        Assert.Equal("UX_AUDIT_SINK_DISABLED", receipt.Code);
        Assert.False(receipt.Recorded);
        Assert.Equal(1, logger.EnabledCheckCalls);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void ProviderFailureAtCheckOrLogIsOpaqueAndNeverClaimedRecorded()
    {
        var logger = new CaptureLogger { ThrowInIsEnabled = true };
        var sink = new V5UxAuditStructuredEventSink(logger);
        var unavailable = sink.Record(Staged());
        Assert.Equal("UX_AUDIT_SINK_UNAVAILABLE", unavailable.Code);
        Assert.False(unavailable.Recorded);
        logger.ThrowInIsEnabled = false;
        logger.ThrowInLog = true;
        unavailable = sink.Record(Denied());
        Assert.Equal("UX_AUDIT_SINK_UNAVAILABLE", unavailable.Code);
        Assert.False(unavailable.Recorded);
        Assert.DoesNotContain("SECRET", unavailable.Code);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void ProviderCancellationWithoutCallerCancellationIsOpaque()
    {
        var logger = new CaptureLogger { ThrowOperationCanceled = true };
        var receipt = new V5UxAuditStructuredEventSink(logger).Record(Staged());
        Assert.Equal("UX_AUDIT_SINK_UNAVAILABLE", receipt.Code);
        Assert.False(receipt.Recorded);
        Assert.DoesNotContain("SECRET", receipt.Code);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void PreCancelledTokenDoesNotEmitOrInventARecord()
    {
        var logger = new CaptureLogger();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            new V5UxAuditStructuredEventSink(logger).Record(Staged(), cancellation.Token));
        Assert.Empty(logger.Entries);
    }
}
