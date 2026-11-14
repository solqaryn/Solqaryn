using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5WorkItemAuditLoggerTests
{
    private const string Id = "C00-02.M03.06";
    private const string TaskId = "6af16bbad26881918c283abe06e1f32a";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly Guid Correlation = Guid.Parse("259da3c3-c428-8333-93f7-d1b6a41eea3a");

    private static V5WorkItemAuditLogger.AuditRecord Safe() =>
        new(Id, TaskId, "CLAIM", "PASS", Sha, Correlation);

    [Fact]
    public void ValidSuccess_LogsStructuredIdentifiersWithoutSecrets()
    {
        var logger = new CaptureLogger();
        var writer = new V5WorkItemAuditLogger(logger);
        Assert.True(writer.TryWrite(Safe()));
        Assert.Single(logger.Messages);
        Assert.Contains(Id, logger.Messages[0]);
        Assert.Contains(TaskId, logger.Messages[0]);
        Assert.Contains(Sha, logger.Messages[0]);
        Assert.DoesNotContain("Bearer", logger.Messages[0]);
        Assert.Equal(LogLevel.Information, logger.Levels[0]);
        Assert.Equal(506, logger.Ids[0].Id);
    }

    [Fact]
    public void SafeFailure_LogsOnlyAllowedErrorCodeWithoutExceptionText()
    {
        var logger = new CaptureLogger();
        var writer = new V5WorkItemAuditLogger(logger);
        Assert.True(writer.TryWrite(Safe() with {
            Operation = "AUDIT_A0_A4", Outcome = "FAIL", ErrorCode = "SECURITY_DENIED"
        }));
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Levels));
        Assert.Contains("SECURITY_DENIED", Assert.Single(logger.Messages));
    }

    [Fact]
    public void RejectsInvalidIdentifiersOperationsOutcomesAndCorrelationWithoutLogging()
    {
        var logger = new CaptureLogger();
        var writer = new V5WorkItemAuditLogger(logger);
        var invalid = new[] {
            Safe() with { WorkItemId = "C00-02.M03.06\nsecret" },
            Safe() with { TaskId = "external-actor" },
            Safe() with { CommitSha = "not-a-commit" },
            Safe() with { Operation = "PROMOTE_PROD" },
            Safe() with { Outcome = "SUCCESS\nJWT" },
            Safe() with { CorrelationId = Guid.Empty },
            Safe() with { ErrorCode = "password=secret" },
            Safe() with { Outcome = "FAIL" },
            Safe() with { Outcome = "FAIL", ErrorCode = "UNBOUNDED_EXCEPTION_TEXT" }
        };
        foreach (var item in invalid)
            Assert.False(writer.TryWrite(item));
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void ProviderFailureIsNotSilentlyConvertedToAuditSuccess()
    {
        var logger = new CaptureLogger { ThrowOnLog = true };
        var writer = new V5WorkItemAuditLogger(logger);
        Assert.Throws<InvalidOperationException>(() => writer.TryWrite(Safe()));
    }

    private sealed class CaptureLogger : ILogger<V5WorkItemAuditLogger>
    {
        public readonly List<string> Messages = new();
        public readonly List<LogLevel> Levels = new();
        public readonly List<EventId> Ids = new();
        public bool ThrowOnLog { get; init; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnLog)
                throw new InvalidOperationException("logger unavailable");
            Levels.Add(logLevel);
            Ids.Add(eventId);
            Messages.Add(formatter(state, exception));
        }
    }
}
