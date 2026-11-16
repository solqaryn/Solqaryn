using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Sink = Solqaryn.Infrastructure.AutomationV5.C1501RequirementCoverageSafeAuditSink;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1501RequirementCoverageBoundedReviewWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1501RequirementCoverageSafeAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Secret = "SECRET-TENANT-ACTOR-TOKEN-PRIVATE";
    private const string Correlation = "123456781234123412341234567890ab";
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
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => Scope.Instance;
        public bool IsEnabled(LogLevel level) => Enabled;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException(Secret);
            EventIds.Add(id.Id);
            Messages.Add(formatter(state, exception));
        }
    }
    private static Sink.Evidence Current() => new(
        31, 31, true, true, true, false, true, Sha, Sha, Correlation);
    private static Worker.Review Preview() => new(
        "REVIEW_PREVIEW_REQUIRES_INDEPENDENT_EVIDENCE",
        new[] { new Worker.Item("req.alpha", "C15-01.M03.01",
            "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED") });

    private static void NoGrant(Sink.Receipt r)
    {
        Assert.False(r.GrantsInstitutionalWrite);
        Assert.False(r.GrantsA5);
        Assert.False(r.CertifiesGlobalCoverage);
        Assert.False(r.AuthorizesTenantRead);
        Assert.False(r.AuthorizesQaOrProduction);
        Assert.False(r.CertifiesDurableAudit);
    }

    [Fact]
    public void PositivePreviewEmitsOnlyFixedRedactedFields()
    {
        var rec = new Recorder();
        var result = new Sink(rec).Record(Preview(), Current());
        Assert.Equal("SAFE_REQUIREMENT_PREVIEW_AUDIT_RECORDED", result.Code);
        Assert.Equal(1, result.ReviewedCount);
        Assert.Equal(150106, Assert.Single(rec.EventIds));
        Assert.Contains("REQUIREMENT_PREVIEW_NOT_CERTIFIED", Assert.Single(rec.Messages));
        Assert.Contains(Correlation, rec.Messages[0]);
        Assert.DoesNotContain("req.alpha", rec.Messages[0]);
        Assert.DoesNotContain("C15-01.M03.01", rec.Messages[0]);
        Assert.DoesNotContain(Sha, rec.Messages[0]);
        NoGrant(result);
    }

    [Fact]
    public void CrossTenantAndInvalidTenantDeny()
    {
        foreach (var ctx in new[] { Current() with { ResourceTenantId = 42 },
                                    Current() with { VerifiedTenantId = 0 } })
        {
            var rec = new Recorder();
            var result = new Sink(rec).Record(Preview(), ctx);
            Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED", result.Code);
            Assert.Equal(0, result.ReviewedCount);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
            NoGrant(result);
        }
    }

    [Fact]
    public void RevocationMembershipRoleAndTrustedBackendFailuresDeny()
    {
        foreach (var ctx in new[] {
            Current() with { PermissionRevoked = true },
            Current() with { MembershipActive = false },
            Current() with { ReviewerPermissionCurrent = false },
            Current() with { BackendIdentityVerified = false },
            Current() with { SourceAuthenticated = false }
        })
        {
            var rec = new Recorder();
            Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
                new Sink(rec).Record(Preview(), ctx).Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        }
    }

    [Fact]
    public void StaleShaNoncanonicalShaAndMaliciousCorrelationDeny()
    {
        foreach (var ctx in new[] {
            Current() with { ObservedHeadSha = new string('f', 40) },
            Current() with { ExpectedHeadSha = Sha.ToUpperInvariant() },
            Current() with { InternalCorrelationId = Secret },
            Current() with { InternalCorrelationId = null }
        })
        {
            var rec = new Recorder();
            Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
                new Sink(rec).Record(Preview(), ctx).Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        }
    }

    [Fact]
    public void UnknownReviewCodeNeverCertifiesGlobalCoverage()
    {
        var result = new Sink(new Recorder()).Record(
            Preview() with { Code = "GLOBAL_COVERAGE_CERTIFIED" }, Current());
        Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED", result.Code);
        NoGrant(result);
    }

    [Fact]
    public void ForgedItemCodeDoesNotLeakSecrets()
    {
        var rec = new Recorder();
        var fake = Preview() with { Items = new[] {
            new Worker.Item("req.alpha", "C15-01.M03.01", Secret) } };
        Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
            new Sink(rec).Record(fake, Current()).Code);
        Assert.DoesNotContain(Secret, Assert.Single(rec.Messages));
    }

    [Fact]
    public void DuplicateKeysAndWorkItemsRejectOrdinally()
    {
        foreach (var items in new[] {
            new[] { new Worker.Item("req.a", "C15-01.M03.01", "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED"),
                    new Worker.Item("req.a", "C15-01.M03.02", "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED") },
            new[] { new Worker.Item("req.a", "C15-01.M03.01", "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED"),
                    new Worker.Item("req.b", "C15-01.M03.01", "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED") }
        })
        {
            var rec = new Recorder();
            Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
                new Sink(rec).Record(Preview() with { Items = items }, Current()).Code);
            Assert.Contains("REDACTED", Assert.Single(rec.Messages));
        }
    }

    [Fact]
    public void EmptyAndSeventeenItemsFailMaximumSixteen()
    {
        var tooMany = Enumerable.Range(0, 17).Select(i =>
            new Worker.Item("req." + i, "C15-01.M03." + i.ToString("D2"),
                "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED")).ToArray();
        foreach (var items in new[] { Array.Empty<Worker.Item>(), tooMany })
        {
            var result = new Sink(new Recorder()).Record(Preview() with { Items = items }, Current());
            Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED", result.Code);
            Assert.Equal(0, result.ReviewedCount);
        }
    }

    [Fact]
    public void UnsafeIdentifierNeverAppearsInLog()
    {
        var rec = new Recorder();
        var fake = Preview() with { Items = new[] {
            new Worker.Item(Secret + "/../", "C15-01.M03.01",
                "EXTERNAL_CAUSAL_EVIDENCE_REQUIRED") } };
        Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
            new Sink(rec).Record(fake, Current()).Code);
        Assert.DoesNotContain(Secret, Assert.Single(rec.Messages));
    }

    [Fact]
    public void NullReviewOrEvidenceFailsClosed()
    {
        var sink = new Sink(new Recorder());
        Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
            sink.Record(null, Current()).Code);
        Assert.Equal("SAFE_REQUIREMENT_DENIAL_AUDIT_RECORDED",
            sink.Record(Preview(), null).Code);
        NoGrant(sink.Record(null, null));
    }

    [Fact]
    public void DisabledLoggerDoesNotClaimDurability()
    {
        var rec = new Recorder { Enabled = false };
        var result = new Sink(rec).Record(Preview(), Current());
        Assert.Equal("REQUIREMENT_AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.Empty(rec.Messages);
        NoGrant(result);
    }

    [Fact]
    public void LoggerExceptionNeverLeaksErrorContent()
    {
        var rec = new Recorder { Throws = true };
        var result = new Sink(rec).Record(Preview(), Current());
        Assert.Equal("REQUIREMENT_AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.Empty(rec.Messages);
        NoGrant(result);
    }

    [Fact]
    public void CancelledContextDoesNotTouchLogger()
    {
        var rec = new Recorder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            new Sink(rec).Record(Preview(), Current(), cts.Token));
        Assert.Empty(rec.Messages);
    }

    [Fact]
    public void NullLoggerRejectedAtConstruction() =>
        Assert.Throws<ArgumentNullException>(() => new Sink(null!));
}
