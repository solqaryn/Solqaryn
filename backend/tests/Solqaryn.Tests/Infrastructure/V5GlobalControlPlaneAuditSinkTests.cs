using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Sink = Solqaryn.Infrastructure.AutomationV5.V5GlobalControlPlaneAuditSink;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5GlobalControlPlaneIsolatedDispatchWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5GlobalControlPlaneAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static Sink.VerifiedContext Valid() =>
        new("actor-a", "tenant-a", "tenant-a", Sha, Sha,
            true, true, true, true, true, false);

    private sealed class Authority : IV5GlobalControlPlaneAuditAuthority
    {
        public Sink.VerifiedContext? Context = Valid();
        public bool Throws;
        public int Calls;
        public ValueTask<Sink.VerifiedContext?> CurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("PRIVATE-JWT-tenant-secret");
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
            public void Dispose() { }
        }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            new Scope();
        public bool IsEnabled(LogLevel level) => Enabled;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (Throws) throw new InvalidOperationException("PRIVATE-JWT-tenant-secret");
            Events.Add((level, id, formatter(state, exception)));
        }
    }

    private static Worker.Receipt Outcome(string code) => new("sensitive-idempotency", code);
    private static Worker.Receipt Good() =>
        Outcome("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED");
    private static Sink Create(Authority authority, CaptureLogger logger) =>
        new(authority, logger);
    private static void NeverGrants(Sink.Receipt result)
    {
        Assert.False(result.GrantsAccess);
        Assert.False(result.GrantsPlatformDirectory);
        Assert.False(result.GrantsExceptionalLease);
        Assert.False(result.CertifiesAuthenticatedRuntime);
        Assert.False(result.CertifiesDurableAudit);
        Assert.False(result.WritesDatabase);
        Assert.False(result.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task NominalPreviewLogsFixedCodeButGrantsNothing()
    {
        var logger = new CaptureLogger();
        var result = await Create(new Authority(), logger).RecordAsync(Good());
        Assert.True(result.LoggerAccepted);
        NeverGrants(result);
        var entry = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(204306, entry.Id.Id);
        Assert.Contains("AUTHENTICATED_GLOBAL_CONTROL_PLANE_ADAPTER_REQUIRED", entry.Message);
        Assert.DoesNotContain("sensitive-idempotency", entry.Message);
        Assert.DoesNotContain(Sha, entry.Message);
        Assert.DoesNotContain("tenant-a", entry.Message);
    }

    [Fact]
    public async Task DenialLogsWarningButNeverGrants()
    {
        var logger = new CaptureLogger();
        var result = await Create(new Authority(), logger).RecordAsync(
            Outcome("CONTROL_PLANE_BATCH_TENANT_MISMATCH"));
        Assert.True(result.LoggerAccepted);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Events).Level);
        NeverGrants(result);
    }

    [Fact]
    public async Task UnknownOrInjectedOutcomeIsRejectedBeforeAuthorityRead()
    {
        var authority = new Authority();
        var logger = new CaptureLogger();
        var result = await Create(authority, logger).RecordAsync(
            Outcome("JWT:secret-{tenant-b}"));
        Assert.Equal("CONTROL_PLANE_AUDIT_UNKNOWN_OUTCOME", result.Code);
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
        NeverGrants(result);
    }

    [Fact]
    public async Task MissingAuthorityCannotLog()
    {
        var logger = new CaptureLogger();
        var result = await Create(new Authority { Context = null }, logger).RecordAsync(Good());
        Assert.Equal("CURRENT_CONTROL_PLANE_BACKEND_AUTHORITY_REQUIRED", result.Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task TenantBOrActorSpoofCannotLog()
    {
        foreach (var bad in new[] {
            Valid() with { TargetTenantId = "tenant-b" },
            Valid() with { ActorTenantId = "tenant-b" },
            Valid() with { ActorId = "../actor-a" },
            Valid() with { ActorId = "" }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_CONTROL_PLANE_BACKEND_AUTHORITY_REQUIRED",
                (await Create(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task RevocationMfaAndBackendGrantsAreIndependentlyRequired()
    {
        foreach (var bad in new[] {
            Valid() with { BackendAuthenticated = false },
            Valid() with { CurrentPlatformRole = false },
            Valid() with { RecentMfaAndReauthentication = false },
            Valid() with { CurrentAuditPermission = false },
            Valid() with { AuditFenceCurrent = false },
            Valid() with { Revoked = true }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_CONTROL_PLANE_BACKEND_AUTHORITY_REQUIRED",
                (await Create(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task StaleOrNoncanonicalShaFailsClosed()
    {
        foreach (var bad in new[] {
            Valid() with { ObservedSourceSha = new string('f', 40) },
            Valid() with { SourceSha = Sha.ToUpperInvariant() },
            Valid() with { SourceSha = "not-a-sha" },
            Valid() with { ActorTenantId = "tenant a" }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_CONTROL_PLANE_BACKEND_AUTHORITY_REQUIRED",
                (await Create(new Authority { Context = bad }, logger).RecordAsync(Good())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task AuthorityExceptionReturnsOpaqueCode()
    {
        var logger = new CaptureLogger();
        var result = await Create(new Authority { Throws = true }, logger).RecordAsync(Good());
        Assert.Equal("CONTROL_PLANE_AUDIT_AUTHORITY_UNAVAILABLE", result.Code);
        Assert.Empty(logger.Events);
        NeverGrants(result);
    }

    [Fact]
    public async Task DisabledLoggerSkipsTrustedRead()
    {
        var authority = new Authority();
        var logger = new CaptureLogger { Enabled = false };
        Assert.Equal("CONTROL_PLANE_AUDIT_LOGGER_UNAVAILABLE",
            (await Create(authority, logger).RecordAsync(Good())).Code);
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task LoggerExceptionDoesNotExposePayload()
    {
        var logger = new CaptureLogger { Throws = true };
        var result = await Create(new Authority(), logger).RecordAsync(Good());
        Assert.Equal("CONTROL_PLANE_AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.Empty(logger.Events);
        NeverGrants(result);
    }

    [Fact]
    public async Task CancellationPrecedesTrustedRead()
    {
        var authority = new Authority();
        var logger = new CaptureLogger();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Create(authority, logger).RecordAsync(Good(), cts.Token));
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public void ConstructorRequiresBothDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!, new CaptureLogger()));
        Assert.Throws<ArgumentNullException>(() => new Sink(new Authority(), null!));
    }

    [Fact]
    public async Task BoundaryOnlyAllowsFixedWarningCodes()
    {
        var logger = new CaptureLogger();
        var sink = Create(new Authority(), logger);
        Assert.True((await sink.RecordAsync(Outcome("MFA_AND_REAUTHENTICATION_REQUIRED"))).LoggerAccepted);
        Assert.True((await sink.RecordAsync(Outcome("INDEPENDENT_PLATFORM_GRANT_REQUIRED"))).LoggerAccepted);
        Assert.False((await sink.RecordAsync(Outcome("INDEPENDENT_PLATFORM_GRANT_REQUIRED:tenant-b"))).LoggerAccepted);
        Assert.Equal(2, logger.Events.Count);
        Assert.All(logger.Events, x => Assert.Equal(LogLevel.Warning, x.Level));
    }
}
