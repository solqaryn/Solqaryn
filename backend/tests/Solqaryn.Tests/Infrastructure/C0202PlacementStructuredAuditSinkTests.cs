using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Queue = Solqaryn.Infrastructure.AutomationV5.C0202PlacementIsolatedDispatchQueue;
using Sink = Solqaryn.Infrastructure.AutomationV5.C0202PlacementStructuredAuditSink;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0202PlacementStructuredAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Secret = "SENSITIVE-TENANT-USER-DSN-NOT-FOR-LOG";
    private static readonly Guid Correlation =
        Guid.ParseExact("123456781234123412341234567890ab", "N");

    private sealed class Recorder : ILogger<Sink>
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
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException(Secret);
            EventIds.Add(id.Id);
            Messages.Add(formatter(state, exception));
        }
    }

    private static Sink.ObservedContext Context() => new(
        "tenant-A", "tenant-A", "actor-7", "actor-7", Sha, Sha,
        Correlation, true, true, false, false);

    private static Queue.Outcome Preview() =>
        new("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", 1);

    private static void NoAuthority(Sink.Receipt r)
    {
        Assert.False(r.GrantsTenantAccess);
        Assert.False(r.GrantsJwtOrRbac);
        Assert.False(r.StartsWorkerOrQueue);
        Assert.False(r.SelectsDatabase);
        Assert.False(r.AllowsMigrationOrCutover);
        Assert.False(r.CertifiesMySql);
        Assert.False(r.CertifiesDurableAudit);
        Assert.False(r.AllowsReleaseOrDeployment);
    }

    [Fact]
    public void PositivePreviewOnlyEmitsFixedNonGrantingFields()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(Preview(), Context());
        Assert.Equal("SAFE_PLACEMENT_PREVIEW_AUDIT_RECORDED", result.Code);
        Assert.True(result.LoggerAcceptedEvent);
        NoAuthority(result);
        Assert.Equal(202206, Assert.Single(rec.EventIds));
        Assert.Contains("SYNTHETIC_PREFLIGHT_ONLY_NO_AUTHORITY",
            Assert.Single(rec.Messages));
        Assert.Contains(Correlation.ToString("N"), rec.Messages[0]);
        Assert.DoesNotContain("tenant-A", rec.Messages[0]);
        Assert.DoesNotContain("actor-7", rec.Messages[0]);
        Assert.DoesNotContain(Sha, rec.Messages[0]);
    }

    [Fact]
    public void KnownDeniedResultNeverGrantsAuthority()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(
            new Queue.Outcome("QUEUE_PREFLIGHT_DENIED", 0), Context());
        Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("PLACEMENT_PREVIEW_DENIED_NO_AUTHORITY",
            Assert.Single(rec.Messages));
        NoAuthority(result);
    }

    [Fact]
    public void UnknownResultCannotInjectSecretsIntoLog()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(new Queue.Outcome(Secret, 0), Context());
        Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("UNTRUSTED_PLACEMENT_AUDIT_DENIED",
            Assert.Single(rec.Messages));
        Assert.Contains("REDACTED", rec.Messages[0]);
        Assert.DoesNotContain(Secret, rec.Messages[0]);
        NoAuthority(result);
    }

    [Fact]
    public void TenantBActorBAndRevocationRedactCorrelation()
    {
        foreach (var ctx in new[] {
            Context() with { ObservedSyntheticTenant = "tenant-B" },
            Context() with { ObservedSyntheticActor = "actor-8" },
            Context() with { PermissionRevoked = true },
            Context() with { CurrentMembershipVerified = false },
            Context() with { IsolatedBackendEvidenceConfirmed = false }
        })
        {
            var rec = new Recorder();
            var result = new Sink(rec).Record(Preview(), ctx);
            Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
            Assert.DoesNotContain(Correlation.ToString("N"), rec.Messages[0]);
            NoAuthority(result);
        }
    }

    [Fact]
    public void StaleOrNoncanonicalShaAndEmptyCorrelationAreDenied()
    {
        foreach (var ctx in new[] {
            Context() with { ObservedSourceSha = new string('f', 40) },
            Context() with { ExpectedSourceSha = Sha.ToUpperInvariant() },
            Context() with { CorrelationId = Guid.Empty }
        })
        {
            var rec = new Recorder();
            var result = new Sink(rec).Record(Preview(), ctx);
            Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        }
    }

    [Fact]
    public void ClientDsnOrCutoverClaimCannotMakeTrustedEvent()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(Preview(),
            Context() with { ClientRequestsDsnOrCutover = true });
        Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        NoAuthority(result);
    }

    [Fact]
    public void InvalidIdentityAndNullContextNeverClaimTrust()
    {
        foreach (var ctx in new Sink.ObservedContext?[] {
            null,
            Context() with { VerifiedSyntheticTenant = "../tenant" },
            Context() with { VerifiedSyntheticActor = "" }
        })
        {
            var rec = new Recorder();
            var result = new Sink(rec).Record(Preview(), ctx);
            Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        }
    }

    [Fact]
    public void ImpossibleQueueCountIsDeniedWithoutCorrelation()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(
            new Queue.Outcome("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", 0), Context());
        Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        NoAuthority(result);
    }

    [Fact]
    public void TooManySyntheticReceiptsCannotBeReportedAsVerified()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(
            new Queue.Outcome("QUEUE_SYNTHETIC_ACCEPTED_NO_WORKER", 99), Context());
        Assert.Equal("SAFE_PLACEMENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("REDACTED", Assert.Single(rec.Messages));
    }

    [Fact]
    public void DisabledLoggerNeverCertifiesDurableAudit()
    {
        var rec = new Recorder { Enabled = false };
        var result = new Sink(rec).Record(Preview(), Context());
        Assert.Equal("PLACEMENT_AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.False(result.LoggerAcceptedEvent);
        Assert.Empty(rec.Messages);
        NoAuthority(result);
    }

    [Fact]
    public void ThrowingLoggerDoesNotLeakException()
    {
        var rec = new Recorder { Throws = true };
        var result = new Sink(rec).Record(Preview(), Context());
        Assert.Equal("PLACEMENT_AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.False(result.LoggerAcceptedEvent);
        Assert.Empty(rec.Messages);
        NoAuthority(result);
    }

    [Fact]
    public void CancellationRunsBeforeLogging()
    {
        var rec = new Recorder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            new Sink(rec).Record(Preview(), Context(), cts.Token));
        Assert.Empty(rec.Messages);
    }

    [Fact]
    public void NullLoggerFailsClosedAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!));
    }
}
