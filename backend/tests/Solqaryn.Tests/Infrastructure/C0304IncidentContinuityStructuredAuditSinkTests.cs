using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Sink = Solqaryn.Infrastructure.AutomationV5.C0304IncidentContinuityStructuredAuditSink;
using Worker = Solqaryn.Application.AutomationV5.C0304IsolatedIncidentReadOnlyDispatchWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0304IncidentContinuityStructuredAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Sensitive = "SENSITIVE-TENANT-IDEMPOTENCY-KEY";
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
            if (Throws) throw new InvalidOperationException(Sensitive);
            EventIds.Add(id.Id);
            Messages.Add(formatter(state, exception));
        }
    }

    private static Sink.ObservedContext Context() =>
        new(11, 11, 7, Sha, Sha, Correlation, true, false, false);
    private static IReadOnlyList<Worker.PreviewReceipt> Preview() =>
        new[] { new Worker.PreviewReceipt(Sensitive,
            "AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED") };
    private static Sink Create(Recorder recorder) => new(recorder);

    private static void NoGrants(Sink.AuditReceipt receipt)
    {
        Assert.False(receipt.GrantsIncidentEvidence);
        Assert.False(receipt.GrantsMutation);
        Assert.False(receipt.GrantsBackupRestore);
        Assert.False(receipt.StartsJobOrWorker);
        Assert.False(receipt.CertifiesJwtOrPersistence);
        Assert.False(receipt.CertifiesDurableAudit);
        Assert.False(receipt.AllowsReleaseOrDeployment);
    }

    [Fact]
    public void NominalPreviewLogsOnlyFixedNonAuthorizingFields()
    {
        var recorder = new Recorder();
        var receipt = Create(recorder).RecordBatch(Preview(), Context());
        Assert.Equal("SAFE_INCIDENT_PREVIEW_AUDIT_RECORDED", receipt.Code);
        Assert.True(receipt.LoggerAcceptedEvent);
        NoGrants(receipt);
        Assert.Equal(30406, Assert.Single(recorder.EventIds));
        Assert.Contains("INCIDENT_PREFLIGHT_ONLY_NO_PERMISSION", Assert.Single(recorder.Messages));
        Assert.Contains(Correlation.ToString("N"), recorder.Messages[0]);
        Assert.Contains("1", recorder.Messages[0]);
        Assert.DoesNotContain(Sensitive, recorder.Messages[0]);
        Assert.DoesNotContain(Sha, recorder.Messages[0]);
        Assert.DoesNotContain("11", recorder.Messages[0]);
    }

    [Fact]
    public void KnownDenialDoesNotGrantOrLeakOriginalTicket()
    {
        var recorder = new Recorder();
        var receipt = Create(recorder).RecordBatch(new[] {
            new Worker.PreviewReceipt(Sensitive, "INCIDENT_CROSS_TENANT_QUEUE_DENIED")
        }, Context());
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", receipt.Code);
        Assert.Contains("INCIDENT_PREVIEW_DENIED_NO_PERMISSION", Assert.Single(recorder.Messages));
        Assert.DoesNotContain(Sensitive, recorder.Messages[0]);
        NoGrants(receipt);
    }

    [Fact]
    public void UnknownOrClientSuppliedOutcomeIsRedacted()
    {
        var recorder = new Recorder();
        var receipt = Create(recorder).RecordBatch(new[] {
            new Worker.PreviewReceipt(Sensitive, Sensitive)
        }, Context());
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", receipt.Code);
        Assert.Contains("UNVERIFIED_INCIDENT_AUDIT_DENIED", Assert.Single(recorder.Messages));
        Assert.Contains("REDACTED", recorder.Messages[0]);
        Assert.DoesNotContain(Sensitive, recorder.Messages[0]);
    }

    [Fact]
    public void CrossTenantAndRevokedContextNeverLogCorrelation()
    {
        foreach (var context in new[] {
            Context() with { ObservedTenantId = 22 },
            Context() with { PermissionRevoked = true },
            Context() with { VerifiedTenantId = 0 },
            Context() with { CurrentBackendEvidenceConfirmed = false },
            Context() with { BackendActorId = 0 }
        })
        {
            var recorder = new Recorder();
            var receipt = Create(recorder).RecordBatch(Preview(), context);
            Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", receipt.Code);
            Assert.Contains("REDACTED", Assert.Single(recorder.Messages));
            Assert.DoesNotContain(Correlation.ToString("N"), recorder.Messages[0]);
            NoGrants(receipt);
        }
    }

    [Fact]
    public void SourceShaAndCorrelationMustMatchTrustedScope()
    {
        foreach (var context in new[] {
            Context() with { ObservedCommitSha = new string('f', 40) },
            Context() with { ExpectedCommitSha = Sha.ToUpperInvariant() },
            Context() with { CorrelationId = Guid.Empty }
        })
        {
            var recorder = new Recorder();
            var receipt = Create(recorder).RecordBatch(Preview(), context);
            Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", receipt.Code);
            Assert.Contains("REDACTED", Assert.Single(recorder.Messages));
        }
    }

    [Fact]
    public void ClientRestorationOrAuthorityOverrideIsAlwaysDenied()
    {
        var recorder = new Recorder();
        var result = Create(recorder).RecordBatch(Preview(),
            Context() with { ClientClaimsAuthorityOrRestore = true });
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", result.Code);
        NoGrants(result);
    }

    [Fact]
    public void EmptyNullAndOversizedBatchAreDenied()
    {
        var recorder = new Recorder();
        var sink = Create(recorder);
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED",
            sink.RecordBatch(null, Context()).Code);
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED",
            sink.RecordBatch(Array.Empty<Worker.PreviewReceipt>(), Context()).Code);
        var many = Enumerable.Repeat(Preview()[0],
            Worker.MaximumBatchSize + 1).ToArray();
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED",
            sink.RecordBatch(many, Context()).Code);
    }

    [Fact]
    public void UnknownContextIsDeniedWithNoCorrelationLeak()
    {
        var recorder = new Recorder();
        var receipt = Create(recorder).RecordBatch(Preview(), null);
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", receipt.Code);
        Assert.Contains("REDACTED", Assert.Single(recorder.Messages));
    }

    [Fact]
    public async Task ComposesWithRealIsolatedWorkerForDeniedBatch()
    {
        var worker = new Worker();
        var receipts = await worker.PreviewAsync(
            Array.Empty<Worker.WorkTicket?>(), 11, null, Sha);
        var recorder = new Recorder();
        var result = Create(recorder).RecordBatch(receipts, Context());
        Assert.Equal("SAFE_INCIDENT_DENIAL_AUDIT_RECORDED", result.Code);
        Assert.Contains("INCIDENT_PREVIEW_DENIED_NO_PERMISSION",
            Assert.Single(recorder.Messages));
        NoGrants(result);
    }

    [Fact]
    public void DisabledLoggerCannotClaimDurability()
    {
        var recorder = new Recorder { Enabled = false };
        var receipt = Create(recorder).RecordBatch(Preview(), Context());
        Assert.Equal("INCIDENT_AUDIT_LOGGER_UNAVAILABLE", receipt.Code);
        Assert.False(receipt.LoggerAcceptedEvent);
        Assert.Empty(recorder.Messages);
        NoGrants(receipt);
    }

    [Fact]
    public void LoggerFailureHasOpaqueNonGrantingReceipt()
    {
        var recorder = new Recorder { Throws = true };
        var receipt = Create(recorder).RecordBatch(Preview(), Context());
        Assert.Equal("INCIDENT_AUDIT_LOGGER_UNAVAILABLE", receipt.Code);
        Assert.False(receipt.LoggerAcceptedEvent);
        Assert.Empty(recorder.Messages);
        NoGrants(receipt);
    }

    [Fact]
    public void CancellationOccursBeforeLogging()
    {
        var recorder = new Recorder();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            Create(recorder).RecordBatch(Preview(), Context(), cts.Token));
        Assert.Empty(recorder.Messages);
    }

    [Fact]
    public void MissingLoggerRejectedAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!));
    }
}
