using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Sink = Solqaryn.Infrastructure.AutomationV5.V5UiLightnessQueueAuditSink;
using Queue = Solqaryn.Infrastructure.AutomationV5.V5UiLightnessBoundedReviewQueue;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UiLightnessQueueAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static Sink.VerifiedContext Valid() =>
        new(7, 11, 11, Sha, Sha, true, true, true, true, true, false);

    private sealed class Authority : IV5UiLightnessAuditAuthority
    {
        public Sink.VerifiedContext? Context = Valid();
        public int Calls;
        public bool Throws;
        public ValueTask<Sink.VerifiedContext?> CurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("secret-jwt-tenant-pii");
            return ValueTask.FromResult(Context);
        }
    }

    private sealed class CaptureLogger : ILogger<Sink>
    {
        public bool Enabled = true;
        public bool Throws;
        public readonly List<(LogLevel Level, EventId Id, string Message)> Events = new();
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
            if (Throws) throw new InvalidOperationException("secret-jwt-tenant-pii");
            Events.Add((level, id, formatter(state, exception)));
        }
    }

    private static Queue.Outcome Good() =>
        new("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED");
    private static Sink SinkFor(Authority a, CaptureLogger l) => new(a, l);

    private static void NeverGrants(Sink.Receipt result)
    {
        Assert.False(result.GrantsAccess);
        Assert.False(result.RunsWorkerOrProvider);
        Assert.False(result.CertifiesBrowserOrCoreWebVitals);
        Assert.False(result.CertifiesDurableAudit);
        Assert.False(result.WritesDatabase);
        Assert.False(result.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task PositivePreviewIsStructuredAndDoesNotCertifyRealBrowser()
    {
        var logger = new CaptureLogger();
        var receipt = await SinkFor(new Authority(), logger).RecordAsync(Good());
        Assert.True(receipt.LoggerAccepted);
        NeverGrants(receipt);
        var entry = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(504306, entry.Id.Id);
        Assert.Contains("EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED", entry.Message);
        Assert.DoesNotContain(Sha, entry.Message);
        Assert.DoesNotContain("tenant", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jwt", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DenialEmitsWarningWithFixedResultOnly()
    {
        var logger = new CaptureLogger();
        var receipt = await SinkFor(new Authority(), logger).RecordAsync(
            new Queue.Outcome("IDEMPOTENCY_KEY_CONFLICT_DENIED"));
        Assert.True(receipt.LoggerAccepted);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Events).Level);
        NeverGrants(receipt);
    }

    [Fact]
    public async Task MissingItemDoesNotBecomeApprovedAudit()
    {
        var logger = new CaptureLogger();
        var receipt = await SinkFor(new Authority(), logger).RecordAsync(
            new Queue.Outcome("NO_PENDING_ISOLATED_REVIEW"));
        Assert.True(receipt.LoggerAccepted);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Events).Level);
        NeverGrants(receipt);
    }

    [Fact]
    public async Task UnknownOutcomeFailsBeforeTrustedSourceRead()
    {
        var a = new Authority();
        var logger = new CaptureLogger();
        var r = await SinkFor(a, logger).RecordAsync(new Queue.Outcome("SECRET:token"));
        Assert.Equal("UNRECOGNIZED_OR_UNTRUSTED_QUEUE_OUTCOME", r.Code);
        Assert.Equal(0, a.Calls);
        Assert.Empty(logger.Events);
        NeverGrants(r);
    }

    [Fact]
    public async Task ForeignTenantFailsClosedAndNeverLogs()
    {
        var a = new Authority { Context = Valid() with { ResourceTenantId = 22 } };
        var logger = new CaptureLogger();
        var r = await SinkFor(a, logger).RecordAsync(Good());
        Assert.Equal("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED", r.Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task RevokedMembershipOrRoleOrGrantDenies()
    {
        foreach (var bad in new[] {
            Valid() with { MembershipCurrent = false },
            Valid() with { RoleCurrent = false },
            Valid() with { ExplicitReadPermissionCurrent = false },
            Valid() with { Revoked = true }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED",
                (await SinkFor(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task BackendIdentityAndAuditFenceRequired()
    {
        foreach (var bad in new[] {
            Valid() with { BackendAuthenticated = false },
            Valid() with { AuditFenceCurrent = false },
            Valid() with { ActorId = 0 },
            Valid() with { ActorTenantId = 0 }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED",
                (await SinkFor(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task StaleOrUppercaseShaNeverLogs()
    {
        foreach (var bad in new[] {
            Valid() with { ObservedSourceSha = new string('a', 40) },
            Valid() with { SourceSha = Sha.ToUpperInvariant() },
            Valid() with { SourceSha = "bad" }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED",
                (await SinkFor(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task MissingAuthorityFailsClosed()
    {
        var logger = new CaptureLogger();
        Assert.Equal("CURRENT_BACKEND_TENANT_AUTHORIZATION_REQUIRED",
            (await SinkFor(new Authority { Context = null }, logger).RecordAsync(Good())).Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task AuthorityErrorNeverLeaksException()
    {
        var logger = new CaptureLogger();
        var receipt = await SinkFor(new Authority { Throws = true }, logger).RecordAsync(Good());
        Assert.Equal("AUDIT_AUTHORITY_UNAVAILABLE", receipt.Code);
        Assert.Empty(logger.Events);
        NeverGrants(receipt);
    }

    [Fact]
    public async Task DisabledLoggerAvoidsAuthorityRead()
    {
        var authority = new Authority();
        var logger = new CaptureLogger { Enabled = false };
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE",
            (await SinkFor(authority, logger).RecordAsync(Good())).Code);
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task LoggerExceptionDoesNotLeakPrivateData()
    {
        var logger = new CaptureLogger { Throws = true };
        var result = await SinkFor(new Authority(), logger).RecordAsync(Good());
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.Empty(logger.Events);
        NeverGrants(result);
    }

    [Fact]
    public async Task CancellationStopsBeforeAuthorityRead()
    {
        var authority = new Authority();
        var logger = new CaptureLogger();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await SinkFor(authority, logger).RecordAsync(Good(), cts.Token));
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void ConstructorDependenciesAreRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!, new CaptureLogger()));
        Assert.Throws<ArgumentNullException>(() => new Sink(new Authority(), null!));
    }
}
