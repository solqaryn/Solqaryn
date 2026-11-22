using Solqaryn.Application.AutomationV5;
using Xunit;
using Audit = Solqaryn.Application.AutomationV5.V5UniversalCaseFileQaEvidenceAudit;

namespace Solqaryn.Tests.Application;

public sealed class C0104M0306CaseFileQaAuditSafetyTests
{
    private const string Id = "C01-04.M03.06";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Utc =
        new(2026, 11, 18, 20, 0, 0, TimeSpan.Zero);

    private static Audit.Observation Good() => new(
        Id, "casefile.timeline", Sha, "tenant-A", "case-42", true, true,
        "verify-solqaryn-project-scope", "completed", "success", Utc,
        "private token=secret; case details and client PII");

    private sealed class FrozenClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Utc;
    }

    private sealed class Source(Audit.Observation? observation, Exception? error = null)
        : IV5UniversalCaseFileQaEvidenceSource
    {
        public int Reads { get; private set; }
        public ValueTask<Audit.Observation?> ReadAsync(
            string id, string route, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            if (error is not null) throw error;
            return ValueTask.FromResult(observation);
        }
    }

    private sealed class Sink(Exception? error = null) : IV5UniversalCaseFileQaAuditSink
    {
        public List<Audit.SafeAuditEntry> Entries { get; } = new();
        public ValueTask WriteAsync(Audit.SafeAuditEntry entry, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (error is not null) throw error;
            Entries.Add(entry);
            return ValueTask.CompletedTask;
        }
    }

    private static ValueTask<Audit.Verdict> Capture(
        Source source, Sink sink,
        string? id = Id, string? route = "casefile.timeline",
        string? sha = Sha, string? tenant = "tenant-A", string? caseId = "case-42",
        CancellationToken ct = default) =>
        new Audit(source, sink, new FrozenClock()).CaptureAsync(id, route, sha, tenant, caseId, ct);

    [Fact]
    public async Task TrustedAuditProducesOnlyRedactedStructuredEnvelope()
    {
        var sink = new Sink();
        var result = await Capture(new Source(Good()), sink);
        Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", result.Code);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal(Id, entry.MicrotaskId);
        Assert.Equal("casefile.timeline", entry.RouteKey);
        Assert.Equal(Sha, entry.SourceSha);
        Assert.Equal("UNVERIFIED_EXTERNAL_REVIEW_REQUIRED", entry.VerificationClass);
        var rendered = System.Text.Json.JsonSerializer.Serialize(entry);
        Assert.DoesNotContain("private", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant-A", rendered);
        Assert.DoesNotContain("case-42", rendered);
        Assert.False(result.CertifiesCI);
        Assert.False(result.CertifiesA5);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.AuthorizesCaseWrite);
        Assert.False(result.AuthorizesAttachmentRead);
        Assert.False(result.AllowsProduction);
    }

    [Theory]
    [InlineData("C01-04.M03.06;DROP")]
    [InlineData("")]
    [InlineData("C01-04.M03.06\nsecret")]
    public async Task MalformedMicrotaskNeverQueriesEvidence(string id)
    {
        var source = new Source(Good()); var sink = new Sink();
        Assert.Equal("CASEFILE_QA_SCOPE_INVALID",
            (await Capture(source, sink, id: id)).Code);
        Assert.Equal(0, source.Reads);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task RouteIsAllowlistedAndShaMustBeExact()
    {
        var source = new Source(Good()); var sink = new Sink();
        Assert.Equal("CASEFILE_QA_SCOPE_INVALID",
            (await Capture(source, sink, route: "casefile/42?token=secret")).Code);
        Assert.Equal("CASEFILE_QA_SCOPE_INVALID",
            (await Capture(source, sink, sha: new string('a', 39))).Code);
        Assert.Equal(0, source.Reads);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CrossTenantOrCaseAndRevokedEvidenceCannotWrite()
    {
        var sink = new Sink();
        foreach (var invalid in new[]
        {
            Good() with { VerifiedTenantScope = "tenant-B" },
            Good() with { VerifiedCaseFileScope = "case-43" },
            Good() with { SourceAuthenticated = false },
            Good() with { SourceOwnedByTenant = false },
            Good() with { SourceSha = new string('b', 40) }
        })
        {
            var result = await Capture(new Source(invalid), sink);
            Assert.Equal("CASEFILE_QA_TRUSTED_EVIDENCE_REQUIRED", result.Code);
            Assert.Empty(sink.Entries);
        }
    }

    [Theory]
    [InlineData("completed", "pending")]
    [InlineData("in_progress", "success")]
    [InlineData("completed", "approved")]
    [InlineData("skipped", "success")]
    public async Task CheckStateCannotPretendSuccess(string status, string conclusion)
    {
        var sink = new Sink();
        Assert.Equal("CASEFILE_QA_CHECK_OR_TIME_INVALID",
            (await Capture(new Source(Good() with
            {
                CheckStatus = status, CheckConclusion = conclusion
            }), sink)).Code);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CancelledAndSkippedExternalResultsRemainUncertifiedObservations()
    {
        foreach (var resultCode in new[] { "cancelled", "skipped", "failure" })
        {
            var sink = new Sink();
            var verdict = await Capture(new Source(Good() with
            {
                CheckConclusion = resultCode
            }), sink);
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", verdict.Code);
            Assert.Equal(resultCode, Assert.Single(sink.Entries).CheckConclusion);
            Assert.False(verdict.CertifiesCI);
            Assert.False(verdict.CertifiesA5);
        }
    }

    [Theory]
    [InlineData(-600, true)]
    [InlineData(-601, false)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public async Task FrozenClockPreservesBoundaryAndRejectsStaleOrFutureEvidence(int offsetSeconds, bool allowed)
    {
        var sink = new Sink();
        var result = await Capture(new Source(Good() with
        {
            ObservedAtUtc = Utc.AddSeconds(offsetSeconds)
        }), sink);
        Assert.Equal(allowed ? "CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING" :
            "CASEFILE_QA_CHECK_OR_TIME_INVALID", result.Code);
        if (allowed) Assert.Single(sink.Entries);
        else Assert.Empty(sink.Entries);
        Assert.False(result.CertifiesCI);
        Assert.False(result.CertifiesA5);
    }

    private sealed class CancellingButReturningSource(
        Audit.Observation observation, CancellationTokenSource cancellation)
        : IV5UniversalCaseFileQaEvidenceSource
    {
        public ValueTask<Audit.Observation?> ReadAsync(
            string id, string route, CancellationToken cancellationToken)
        {
            // Simulates a non-cooperative provider that finishes after caller cancellation.
            cancellation.Cancel();
            return ValueTask.FromResult<Audit.Observation?>(observation);
        }
    }

    private sealed class CancellationIgnoringSink : IV5UniversalCaseFileQaAuditSink
    {
        public bool WasCalled { get; private set; }
        public ValueTask WriteAsync(Audit.SafeAuditEntry entry, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task CancellationDuringProviderReadCannotWriteEvenWhenSinkIgnoresCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var sink = new CancellationIgnoringSink();
        var audit = new Audit(
            new CancellingButReturningSource(Good(), cancellation),
            sink, new FrozenClock());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await audit.CaptureAsync(
                Id, "casefile.timeline", Sha, "tenant-A", "case-42", cancellation.Token));

        Assert.False(sink.WasCalled);
    }

    private sealed class CancellingWithoutWritingSink(CancellationTokenSource cancellation)
        : IV5UniversalCaseFileQaAuditSink
    {
        public bool WasCalled { get; private set; }
        public ValueTask WriteAsync(Audit.SafeAuditEntry entry, CancellationToken cancellationToken)
        {
            WasCalled = true;
            cancellation.Cancel();
            // A non-cooperative sink reports success without persisting the event.
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task CancellationDuringSinkWriteCannotProduceFalseSuccessfulReceipt()
    {
        using var cancellation = new CancellationTokenSource();
        var sink = new CancellingWithoutWritingSink(cancellation);
        var audit = new Audit(new Source(Good()), sink, new FrozenClock());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await audit.CaptureAsync(
                Id, "casefile.timeline", Sha, "tenant-A", "case-42", cancellation.Token));
        Assert.True(sink.WasCalled);
    }

    [Fact]
    public async Task NonUtcTimestampIsRejected()
    {
        var sink = new Sink();
        Assert.Equal("CASEFILE_QA_CHECK_OR_TIME_INVALID",
            (await Capture(new Source(Good() with
            {
                ObservedAtUtc = Utc.ToOffset(TimeSpan.FromHours(-6))
            }), sink)).Code);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task SourceAndSinkFailuresAreSanitized()
    {
        var source = new Source(Good(), new InvalidOperationException("private bearer=secret"));
        var sink = new Sink();
        var fromSource = await Capture(source, sink);
        Assert.Equal("CASEFILE_QA_SOURCE_UNAVAILABLE", fromSource.Code);
        Assert.DoesNotContain("secret", fromSource.Code);
        Assert.Empty(sink.Entries);
        var brokenSink = new Sink(new InvalidOperationException("sensitive payload"));
        var fromSink = await Capture(new Source(Good()), brokenSink);
        Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE", fromSink.Code);
        Assert.DoesNotContain("payload", fromSink.Code);
    }

    [Fact]
    public async Task CallerCancellationPropagatesBeforeAnyRead()
    {
        var source = new Source(Good()); var sink = new Sink();
        using var ct = new CancellationTokenSource();
        ct.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Capture(source, sink, ct: ct.Token));
        Assert.Equal(0, source.Reads);
        Assert.Empty(sink.Entries);
    }

    [Theory]
    [InlineData("1", "9")]
    [InlineData("9", "1")]
    [InlineData("7", "42")]
    public async Task CanonicalPositiveOneDigitSqlScopesAreAcceptedAsUnverifiedOnly(
        string tenant, string caseFile)
    {
        var sink = new Sink();
        var trusted = Good() with
        {
            VerifiedTenantScope = tenant,
            VerifiedCaseFileScope = caseFile
        };
        var verdict = await Capture(
            new Source(trusted), sink, tenant: tenant, caseId: caseFile);
        Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", verdict.Code);
        Assert.Single(sink.Entries);
        Assert.False(verdict.CertifiesCI);
        Assert.False(verdict.CertifiesA5);
        Assert.False(verdict.GrantsTenantAccess);
        Assert.False(verdict.AuthorizesCaseWrite);
    }

    [Theory]
    [InlineData("0", "case-42")]
    [InlineData("00", "case-42")]
    [InlineData("01", "case-42")]
    [InlineData("001", "case-42")]
    [InlineData("+1", "case-42")]
    [InlineData("-1", "case-42")]
    [InlineData("a", "case-42")]
    [InlineData("tenant-A", "0")]
    [InlineData("tenant-A", "0009")]
    [InlineData("tenant-A", "9 ")]
    public async Task NoncanonicalNumericOrOneLetterScopeFailsBeforeSourceIo(
        string tenant, string caseFile)
    {
        var source = new Source(Good());
        var sink = new Sink();
        var verdict = await Capture(source, sink, tenant: tenant, caseId: caseFile);
        Assert.Equal("CASEFILE_QA_SCOPE_INVALID", verdict.Code);
        Assert.Equal(0, source.Reads);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task TrustedSourceUsingPaddedTenantCannotMasqueradeAsCanonicalScope()
    {
        var sink = new Sink();
        var trusted = Good() with
        {
            VerifiedTenantScope = "01", VerifiedCaseFileScope = "9"
        };
        var verdict = await Capture(
            new Source(trusted), sink, tenant: "1", caseId: "9");
        Assert.Equal("CASEFILE_QA_TRUSTED_EVIDENCE_REQUIRED", verdict.Code);
        Assert.Empty(sink.Entries);
    }
}
