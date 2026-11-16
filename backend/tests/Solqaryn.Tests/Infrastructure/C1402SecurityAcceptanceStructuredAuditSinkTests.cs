using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C1402SecurityAcceptanceAuthorizationGate;
using Rules = Solqaryn.Domain.AutomationV5.V5SecurityAcceptanceDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1402SecurityAcceptanceStructuredAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Sensitive = "SECRET-TENANT-PII-DO-NOT-LOG";
    private static readonly Guid Correlation =
        Guid.ParseExact("11111111111111111111111111111111", "N");

    private static Gate.Request Request() =>
        new("security-acceptance-101", Sha, Rules.Surface.BusinessRead);

    private static Gate.Decision Decision() =>
        new("AUTHENTICATED_SECURITY_ACCEPTANCE_ADAPTER_REQUIRED");

    private static C1402SecurityAcceptanceStructuredAuditSink.ObservedContext Context() =>
        new("security-acceptance-101", Sha, 7, 11, 11, Correlation, true);

    private sealed class Recorder : ILogger<C1402SecurityAcceptanceStructuredAuditSink>
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
        public bool IsEnabled(LogLevel logLevel) => Enabled;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? ex, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException(Sensitive);
            EventIds.Add(id.Id);
            Messages.Add(formatter(state, ex));
        }
    }

    private static C1402SecurityAcceptanceStructuredAuditSink Sink(Recorder log) =>
        new(log);

    private static void NoGrant(C1402SecurityAcceptanceStructuredAuditSink.Receipt value)
    {
        Assert.False(value.GrantsAccess);
        Assert.False(value.GrantsFileOrJob);
        Assert.False(value.GrantsInstitutionalWrite);
        Assert.False(value.AllowsRestorationOrDeployment);
        Assert.False(value.CertifiesSecurityAcceptance);
        Assert.False(value.CertifiesDurableAudit);
    }

    [Fact]
    public void NominalAcceptsOnlyNonAuthorizingFixedSchema()
    {
        var log = new Recorder();
        var result = Sink(log).Record(Request(), Decision(), Context());
        Assert.Equal("SAFE_AUDIT_PREVIEW_RECORDED", result.Code);
        Assert.True(result.LoggerAcceptedEvent);
        NoGrant(result);
        Assert.Equal(140206, Assert.Single(log.EventIds));
        Assert.Contains("PREFLIGHT_ONLY_NO_PERMISSION", Assert.Single(log.Messages));
        Assert.Contains(Correlation.ToString("N"), log.Messages[0]);
        Assert.DoesNotContain("security-acceptance-101", log.Messages[0]);
        Assert.DoesNotContain(Sha, log.Messages[0]);
    }

    [Fact]
    public void RawDecisionAndUserProvidedErrorsNeverReachLog()
    {
        var log = new Recorder();
        var result = Sink(log).Record(Request(), new Gate.Decision(Sensitive), Context());
        Assert.Equal("SAFE_AUDIT_PREVIEW_RECORDED", result.Code);
        Assert.Contains("DENIED_NO_PERMISSION", Assert.Single(log.Messages));
        Assert.DoesNotContain(Sensitive, log.Messages[0]);
        NoGrant(result);
    }

    [Fact]
    public void TenantBAndRevokedBackendProofAreRedacted()
    {
        foreach (var ctx in new[] {
            Context() with { ObservedTenantId = 22 },
            Context() with { VerifiedTenantId = 0 },
            Context() with { BackendActorId = 0 },
            Context() with { CurrentBackendEvidenceVerified = false }
        })
        {
            var log = new Recorder();
            var result = Sink(log).Record(Request(), Decision(), ctx);
            Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED", result.Code);
            Assert.Contains("REJECTED_UNTRUSTED_AUDIT", Assert.Single(log.Messages));
            Assert.DoesNotContain(Correlation.ToString("N"), log.Messages[0]);
            NoGrant(result);
        }
    }

    [Fact]
    public void ResourceSpoofAndStaleShaFailWithoutDisclosure()
    {
        foreach (var ctx in new[] {
            Context() with { VerifiedResourceKey = "other" },
            Context() with { SourceSha = new string('a', 40) },
            Context() with { CorrelationId = Guid.Empty }
        })
        {
            var log = new Recorder();
            var result = Sink(log).Record(Request(), Decision(), ctx);
            Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED", result.Code);
            Assert.DoesNotContain(Correlation.ToString("N"), Assert.Single(log.Messages));
        }
    }

    [Fact]
    public void TenantRoleOverrideAndRestoreBypassCannotBeAccepted()
    {
        foreach (var request in new[] {
            Request() with { ClientControlsTenantOrRole = true },
            Request() with { RequestsBypassOrRestore = true }
        })
        {
            var log = new Recorder();
            Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED",
                Sink(log).Record(request, Decision(), Context()).Code);
            NoGrant(Sink(log).Record(request, Decision(), Context()));
        }
    }

    [Fact]
    public void InvalidKeyAndUppercaseShaCannotBeTrustworthy()
    {
        var log = new Recorder();
        Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED",
            Sink(log).Record(Request() with { ResourceKey = "../bad" },
                Decision(), Context()).Code);
        Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED",
            Sink(log).Record(Request() with { ExpectedCommitSha = Sha.ToUpperInvariant() },
                Decision(), Context()).Code);
    }

    [Fact]
    public void UnknownSurfaceAndNullRequestAreDenied()
    {
        var log = new Recorder();
        Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED",
            Sink(log).Record(null, Decision(), Context()).Code);
        Assert.Equal("SAFE_AUDIT_DENIAL_RECORDED",
            Sink(log).Record(Request() with { Surface = (Rules.Surface)999 },
                Decision(), Context()).Code);
        Assert.Equal(2, log.Messages.Count);
    }

    [Fact]
    public void DisabledLoggerNeverPretendsDurability()
    {
        var log = new Recorder { Enabled = false };
        var result = Sink(log).Record(Request(), Decision(), Context());
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.False(result.LoggerAcceptedEvent);
        Assert.Empty(log.Messages);
        NoGrant(result);
    }

    [Fact]
    public void ThrowingLoggerDoesNotLeakExceptionMessage()
    {
        var log = new Recorder { Throws = true };
        var result = Sink(log).Record(Request(), Decision(), Context());
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.False(result.LoggerAcceptedEvent);
        Assert.Empty(log.Messages);
        NoGrant(result);
    }

    [Fact]
    public void CancellationStopsBeforeLogging()
    {
        var log = new Recorder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            Sink(log).Record(Request(), Decision(), Context(), cts.Token));
        Assert.Empty(log.Messages);
    }

    [Fact]
    public void MissingLoggerFailsClosedAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C1402SecurityAcceptanceStructuredAuditSink(null!));
    }
}
