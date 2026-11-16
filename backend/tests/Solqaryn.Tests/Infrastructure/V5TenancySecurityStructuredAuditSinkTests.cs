using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.V5TenancySecurityAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5TenancySecurityDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5TenancySecurityStructuredAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Secret = "SECRET-PRIVATE-PII-DO-NOT-LOG";
    private static readonly Guid Trace = Guid.ParseExact("0123456789abcdef0123456789abcdef", "N");

    private static Gate.Request Request() =>
        new(101, Rules.Surface.BusinessRead, Sha, false, false, false, Secret);
    private static Gate.Result Preflight() =>
        new("AUTHENTICATED_EXECUTION_ADAPTER_REQUIRED");
    private static V5TenancySecurityStructuredAuditSink.TrustedAuditContext Context() =>
        new(11, 11, 7, Sha, Trace, true);

    private sealed class Recorder : ILogger<V5TenancySecurityStructuredAuditSink>
    {
        public bool Enabled = true;
        public bool Throws;
        public readonly List<string> Messages = new();
        public readonly List<int> EventIds = new();
        private sealed class Scope : IDisposable
        {
            public static readonly Scope Instance = new();
            public void Dispose() { }
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            Scope.Instance;
        public bool IsEnabled(LogLevel level) => Enabled;
        public void Log<TState>(
            LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException(Secret);
            EventIds.Add(eventId.Id);
            Messages.Add(formatter(state, exception));
        }
    }

    private static V5TenancySecurityStructuredAuditSink Sink(Recorder log) => new(log);

    private static void NoGrant(V5TenancySecurityStructuredAuditSink.Receipt result)
    {
        Assert.False(result.GrantsResourceRead);
        Assert.False(result.GrantsFileDownload);
        Assert.False(result.GrantsBackgroundJob);
        Assert.False(result.GrantsControlPlane);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
        Assert.False(result.CertifiesDurableAudit);
        Assert.False(result.AuthorizesQaOrProduction);
    }

    [Fact]
    public void NominalPreflightWritesFixedStructuredAuditButNeverGrants()
    {
        var logger = new Recorder();
        var result = Sink(logger).Record(Request(), Preflight(), Context());
        Assert.True(result.WasRecorded);
        Assert.Equal("AUDIT_RECORDED_NON_AUTHORIZING", result.Code);
        NoGrant(result);
        Assert.Single(logger.Messages);
        Assert.Equal(150306, Assert.Single(logger.EventIds));
        Assert.Contains("PREFLIGHT_NON_AUTHORIZING", logger.Messages[0]);
        Assert.Contains(Trace.ToString("N"), logger.Messages[0]);
        Assert.Contains(Sha, logger.Messages[0]);
        Assert.DoesNotContain(Secret, logger.Messages[0]);
    }

    [Fact]
    public void ExplicitDenialNeverLogsUntrustedReasonOrIdempotency()
    {
        var logger = new Recorder();
        var result = Sink(logger).Record(Request(), new Gate.Result(Secret), Context());
        Assert.True(result.WasRecorded);
        Assert.Contains("DENIED_NON_AUTHORIZING", Assert.Single(logger.Messages));
        Assert.DoesNotContain(Secret, logger.Messages[0]);
        NoGrant(result);
    }

    [Fact]
    public void TenantBAndRevokedEvidenceFailClosedWithoutIdentifiers()
    {
        foreach (var context in new[]
        {
            Context() with { ObservedTenantId = 22 },
            Context() with { VerifiedTenantId = 0 },
            Context() with { VerifiedActorId = 0 },
            Context() with { BackendEvidenceVerified = false }
        })
        {
            var logger = new Recorder();
            var result = Sink(logger).Record(Request(), Preflight(), context);
            Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED", result.Code);
            Assert.Contains("INVALID_TRUSTED_CONTEXT", Assert.Single(logger.Messages));
            Assert.DoesNotContain(Sha, logger.Messages[0]);
            Assert.DoesNotContain("22", logger.Messages[0]);
            NoGrant(result);
        }
    }

    [Fact]
    public void UnsafeShaAndMissingCorrelationFailClosed()
    {
        foreach (var context in new[]
        {
            Context() with { SourceSha = "secret-source" },
            Context() with { SourceSha = Sha.ToUpperInvariant() },
            Context() with { CorrelationId = Guid.Empty },
            Context() with { SourceSha = new string('f', 40) }
        })
        {
            var logger = new Recorder();
            var result = Sink(logger).Record(Request(), Preflight(), context);
            Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED", result.Code);
            Assert.DoesNotContain(Secret, Assert.Single(logger.Messages));
            Assert.DoesNotContain(Sha, logger.Messages[0]);
        }
    }

    [Fact]
    public void ClientBypassEnumerationAndWriteCannotBecomeValidAudit()
    {
        foreach (var request in new[]
        {
            Request() with { RequestsTenantFilterBypass = true },
            Request() with { AttemptsCrossTenantEnumeration = true },
            Request() with { RequestsInstitutionalWrite = true }
        })
        {
            var logger = new Recorder();
            var result = Sink(logger).Record(request, Preflight(), Context());
            Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED", result.Code);
            NoGrant(result);
        }
    }

    [Fact]
    public void MissingOrMalformedRequestIsNotAccepted()
    {
        var logger = new Recorder();
        Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED",
            Sink(logger).Record(null, Preflight(), Context()).Code);
        Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED",
            Sink(logger).Record(Request() with { ResourceId = -1 }, Preflight(), Context()).Code);
        Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED",
            Sink(logger).Record(Request() with { ExpectedSourceSha = "not-sha" }, Preflight(), Context()).Code);
        Assert.Equal(3, logger.Messages.Count);
    }

    [Fact]
    public void UndefinedSurfaceIsDeniedWithoutEchoingClientText()
    {
        var logger = new Recorder();
        var result = Sink(logger).Record(Request() with { Surface = (Rules.Surface)9999 },
            Preflight(), Context());
        Assert.Equal("UNVERIFIED_CONTEXT_DENIED_AND_RECORDED", result.Code);
        Assert.DoesNotContain(Secret, Assert.Single(logger.Messages));
    }

    [Fact]
    public void DisabledLoggerDoesNotClaimDurableAudit()
    {
        var logger = new Recorder { Enabled = false };
        var result = Sink(logger).Record(Request(), Preflight(), Context());
        Assert.Equal("AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.WasRecorded);
        Assert.Empty(logger.Messages);
        NoGrant(result);
    }

    [Fact]
    public void ThrowingLoggerDoesNotLeakExceptionMessage()
    {
        var logger = new Recorder { Throws = true };
        var result = Sink(logger).Record(Request(), Preflight(), Context());
        Assert.Equal("AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.WasRecorded);
        Assert.Empty(logger.Messages);
        NoGrant(result);
    }

    [Fact]
    public void CancellationStopsBeforeAnyAudit()
    {
        var logger = new Recorder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            Sink(logger).Record(Request(), Preflight(), Context(), cts.Token));
        Assert.Empty(logger.Messages);
    }

    [Fact]
    public void MissingLoggerIsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new V5TenancySecurityStructuredAuditSink(null!));
    }
}
