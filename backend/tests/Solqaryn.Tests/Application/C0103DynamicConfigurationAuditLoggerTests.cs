using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Xunit;
using AuditLogger = Solqaryn.Application.AutomationV5.C0103DynamicConfigurationAuditLogger;

namespace Solqaryn.Tests.Application;

public sealed class C0103DynamicConfigurationAuditLoggerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private sealed class CaptureLogger : ILogger<AuditLogger>
    {
        public readonly List<string> Messages = new();
        public LogLevel? LastLevel;
        public EventId? LastEventId;
        public bool Throws;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull =>
            new Scope();
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException("provider secret details");
            LastLevel = logLevel;
            LastEventId = eventId;
            Messages.Add(formatter(state, exception));
        }
        private sealed class Scope : IDisposable
        {
            public void Dispose() { }
        }
    }

    private static AuditLogger.AuditEvent Good() =>
        new(7, 10, "profile_theme", Sha, 3, "CONFIG_READ_PREFLIGHT",
            "CHECKED", Guid.Parse("cccccccc-0000-0000-0000-000000000007"));

    [Fact]
    public void ValidReadOnlyAuditWritesBoundedStructuredDataWithoutRawKey()
    {
        var provider = new CaptureLogger();
        var logger = new AuditLogger(provider);
        Assert.True(logger.TryWrite(Good()));
        Assert.Single(provider.Messages);
        Assert.Equal(LogLevel.Information, provider.LastLevel);
        Assert.Equal(10306, provider.LastEventId?.Id);
        var message = provider.Messages[0];
        Assert.Contains("CONFIG_READ_PREFLIGHT", message);
        Assert.Contains(Sha, message);
        Assert.DoesNotContain("profile_theme", message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectedOperationEmitsOnlyAllowlistedErrorCode()
    {
        var provider = new CaptureLogger();
        var logger = new AuditLogger(provider);
        Assert.True(logger.TryWrite(Good() with {
            Operation = "CONFIG_REJECTED", Outcome = "DENIED",
            ErrorCode = "RBAC_REVOKED"
        }));
        Assert.Equal(LogLevel.Warning, provider.LastLevel);
        Assert.Contains("RBAC_REVOKED", provider.Messages[0]);
    }

    [Fact]
    public void UntrustedActorTenantAndCorrelationNeverReachSink()
    {
        var provider = new CaptureLogger();
        var logger = new AuditLogger(provider);
        Assert.False(logger.TryWrite(null));
        Assert.False(logger.TryWrite(Good() with { VerifiedActorId = 0 }));
        Assert.False(logger.TryWrite(Good() with { VerifiedTenantId = -1 }));
        Assert.False(logger.TryWrite(Good() with { CorrelationId = Guid.Empty }));
        Assert.False(logger.TryWrite(Good() with { StoredVersion = 0 }));
        Assert.Empty(provider.Messages);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("jwt.key")]
    [InlineData("security.feature")]
    [InlineData("db..password")]
    [InlineData("token.secret")]
    [InlineData("profile key")]
    [InlineData("../file")]
    [InlineData("profile/role")]
    public void SensitiveAndMalformedKeysCannotBeLogged(string key)
    {
        var provider = new CaptureLogger();
        Assert.False(new AuditLogger(provider).TryWrite(Good() with { CanonicalKey = key }));
        Assert.Empty(provider.Messages);
    }

    [Fact]
    public void KeySizeBoundaryAndNoncanonicalShaAreStrict()
    {
        var provider = new CaptureLogger();
        var logger = new AuditLogger(provider);
        Assert.True(logger.TryWrite(Good() with { CanonicalKey = new string('a', 64) }));
        Assert.False(logger.TryWrite(Good() with { CanonicalKey = new string('a', 65) }));
        Assert.False(logger.TryWrite(Good() with { SourceSha = Sha.ToUpperInvariant() }));
        Assert.False(logger.TryWrite(Good() with { SourceSha = "bad" }));
        Assert.Single(provider.Messages);
    }

    [Fact]
    public void UnsupportedOperationOutcomeAndRawErrorNeverReachSink()
    {
        var provider = new CaptureLogger();
        var logger = new AuditLogger(provider);
        Assert.False(logger.TryWrite(Good() with { Operation = "SET_SECRET" }));
        Assert.False(logger.TryWrite(Good() with { Outcome = "SUCCESS" }));
        Assert.False(logger.TryWrite(Good() with { ErrorCode = "raw JWT=secret" }));
        Assert.False(logger.TryWrite(Good() with { Outcome = "ERROR", ErrorCode = null }));
        Assert.False(logger.TryWrite(Good() with { Outcome = "DENIED", ErrorCode = "SQL_RAW" }));
        Assert.Empty(provider.Messages);
    }

    [Fact]
    public void LoggerProviderExceptionsAreSanitizedWithoutPartialResult()
    {
        var provider = new CaptureLogger { Throws = true };
        Assert.False(new AuditLogger(provider).TryWrite(Good()));
        Assert.Empty(provider.Messages);
        Assert.Throws<ArgumentNullException>(() => new AuditLogger(null!));
    }
}
