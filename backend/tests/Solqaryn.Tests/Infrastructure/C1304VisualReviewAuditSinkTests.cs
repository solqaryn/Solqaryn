using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1304VisualReviewAuditSinkTests
{
    private sealed class SinkLogger : ILogger<C1304VisualReviewAuditSink>
    {
        public ConcurrentBag<(LogLevel Level, EventId Event, string Message)> Messages { get; } = new();
        public bool ThrowProviderSecret;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Scope.Instance;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowProviderSecret) throw new InvalidOperationException("SECRET_PROD_DSN");
            Messages.Add((level, id, formatter(state, exception)));
        }

        private sealed class Scope : IDisposable
        {
            public static readonly Scope Instance = new();
            public void Dispose() { }
        }
    }

    private static C1304VisualReviewBoundedWorker.Review Staged(int count = 1) =>
        new("VISUAL_REVIEW_STAGED_BROWSER_A5_REQUIRED", count);

    [Fact]
    public void ValidStagingLogsStructuredSafeEventButNeverGrantsPermissions()
    {
        var logger = new SinkLogger();
        var receipt = new C1304VisualReviewAuditSink(logger).Record(11, Staged(2));
        Assert.Equal("AUDIT_STAGED_NOT_CERTIFIED", receipt.Code);
        Assert.True(receipt.Recorded);
        Assert.False(receipt.GrantsJwtOrRbac);
        Assert.False(receipt.AuthorizesTenantAccess);
        Assert.False(receipt.CertifiesBrowserOrAccessibility);
        Assert.False(receipt.AuthorizesQaOrProduction);
        Assert.False(receipt.PersistsDurably);
        var log = Assert.Single(logger.Messages);
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(C1304VisualReviewAuditSink.AuditEventId, log.Event.Id);
        Assert.Contains("TenantId=11", log.Message);
        Assert.Contains("ItemsChecked=2", log.Message);
        Assert.DoesNotContain("JWT", log.Message);
        Assert.DoesNotContain("SECRET", log.Message);
    }

    [Theory]
    [InlineData("VISUAL_REVIEW_SOURCE_REJECTED")]
    [InlineData("VISUAL_REVIEW_SOURCE_UNAVAILABLE")]
    [InlineData("VISUAL_REVIEW_INVALID_BATCH")]
    [InlineData("VISUAL_REVIEW_MIXED_TENANT_OR_SOURCE")]
    [InlineData("VISUAL_REVIEW_DUPLICATE_TICKET")]
    [InlineData("VISUAL_REVIEW_INVALID_TICKET")]
    public void RejectionsAreOpaqueAndUseWarningSeverity(string code)
    {
        var logger = new SinkLogger();
        var result = new C1304VisualReviewAuditSink(logger)
            .Record(22, new C1304VisualReviewBoundedWorker.Review(code, 0));
        Assert.Equal("AUDIT_REJECTED", result.Code);
        Assert.True(result.Recorded);
        var item = Assert.Single(logger.Messages);
        Assert.Equal(LogLevel.Warning, item.Level);
        Assert.Contains("TenantId=22", item.Message);
        Assert.DoesNotContain(code, item.Message);
    }

    [Fact]
    public void ForgedSuccessCountsUnknownCodesAndInvalidTenantFailBeforeLogging()
    {
        var logger = new SinkLogger();
        var sink = new C1304VisualReviewAuditSink(logger);
        Assert.Equal("AUDIT_INVALID_TENANT", sink.Record(0, Staged()).Code);
        foreach (var review in new[] {
            Staged(0), Staged(C1304VisualReviewBoundedWorker.MaximumBatch + 1),
            new C1304VisualReviewBoundedWorker.Review("VISUAL_REVIEW_SOURCE_REJECTED", 1),
            new C1304VisualReviewBoundedWorker.Review("FAKE_PROVIDER_SUCCESS", 0)
        })
        {
            var result = sink.Record(11, review);
            Assert.Equal("AUDIT_INVALID_REVIEW", result.Code);
            Assert.False(result.Recorded);
        }
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void CancellationPreventsEmissionAndProviderFailureIsOpaque()
    {
        var logger = new SinkLogger();
        var sink = new C1304VisualReviewAuditSink(logger);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => sink.Record(11, Staged(), cancelled.Token));
        Assert.Empty(logger.Messages);
        logger.ThrowProviderSecret = true;
        var result = sink.Record(11, Staged());
        Assert.Equal("AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.Recorded);
        Assert.DoesNotContain("SECRET", result.Code);
    }

    [Fact]
    public void ConcurrentIndependentTenantsRemainDistinctAndBounded()
    {
        var logger = new SinkLogger();
        var sink = new C1304VisualReviewAuditSink(logger);
        Parallel.For(0, 32, i =>
        {
            var result = sink.Record(i % 2 == 0 ? 11 : 22, Staged());
            Assert.Equal("AUDIT_STAGED_NOT_CERTIFIED", result.Code);
        });
        Assert.Equal(32, logger.Messages.Count);
        Assert.Equal(16, logger.Messages.Count(m => m.Message.Contains("TenantId=11")));
        Assert.Equal(16, logger.Messages.Count(m => m.Message.Contains("TenantId=22")));
        Assert.All(logger.Messages, item => Assert.Equal(1, item.Message.Split("ItemsChecked=1").Length - 1));
    }
}
