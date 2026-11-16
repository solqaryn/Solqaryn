using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityBoundedReadWorker;
using Sink = Solqaryn.Infrastructure.AutomationV5.C1104ObservabilityStructuredAuditSink;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C1104ObservabilityStructuredAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Sink.TrustedContext Trusted() =>
        new(Sha, Sha, true, true, true, true, true);

    private sealed class Authority : IC1104ObservabilityAuditEvidenceSource
    {
        public Sink.TrustedContext? Evidence = Trusted();
        public int Calls;
        public bool Throws;
        public ValueTask<Sink.TrustedContext?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("secret-token-and-private-tenant");
            return ValueTask.FromResult(Evidence);
        }
    }

    private sealed class Log : ILogger<Sink>
    {
        public bool Enabled = true;
        public bool Throws;
        public readonly List<(LogLevel Level, EventId Id, string Text)> Entries = new();
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
            if (Throws) throw new InvalidOperationException("raw-jwt-should-never-escape");
            Entries.Add((level, id, formatter(state, exception)));
        }
    }

    private static Worker.Review Good(int count = 2) =>
        new("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", count);

    private static void NoAuthority(Sink.Receipt receipt)
    {
        Assert.False(receipt.GrantsTenantRead);
        Assert.False(receipt.ExposesMetricsOrPayload);
        Assert.False(receipt.CertifiesSloOrCost);
        Assert.False(receipt.CertifiesDurableAudit);
        Assert.False(receipt.AuthorizesQaOrProduction);
        Assert.False(receipt.StartsWorkerOrExporter);
    }

    [Fact]
    public async Task TrustedPreviewLogsOnlyAllowlistedStructuredCodeAndCount()
    {
        var log = new Log();
        var receipt = await new Sink(new Authority(), log).WriteAsync(Good());
        Assert.True(receipt.LoggerAcceptedEvent);
        NoAuthority(receipt);
        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(110406, entry.Id.Id);
        Assert.Contains("OBSERVABILITY_PREVIEW_REQUIRES_AUTHENTICATED_API", entry.Text);
        Assert.Contains("2", entry.Text);
        Assert.DoesNotContain(Sha, entry.Text);
        Assert.DoesNotContain("tenant", entry.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jwt", entry.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task KnownDenialIsWarningWithNoGrants()
    {
        var log = new Log();
        var result = await new Sink(new Authority(), log).WriteAsync(
            new("OBSERVABILITY_DUPLICATE_TICKET", 0));
        Assert.True(result.LoggerAcceptedEvent);
        Assert.Equal(LogLevel.Warning, Assert.Single(log.Entries).Level);
        NoAuthority(result);
    }

    [Fact]
    public async Task SourceFailureIsOpaqueFixedWarning()
    {
        var log = new Log();
        var result = await new Sink(new Authority(), log).WriteAsync(
            new("OBSERVABILITY_SOURCE_UNAVAILABLE", 0));
        Assert.True(result.LoggerAcceptedEvent);
        Assert.Contains("OBSERVABILITY_SOURCE_UNAVAILABLE", Assert.Single(log.Entries).Text);
        NoAuthority(result);
    }

    [Fact]
    public async Task UnknownResultCannotReachLoggerOrTrustedPort()
    {
        var a = new Authority();
        var log = new Log();
        var result = await new Sink(a, log).WriteAsync(new("SECRET_123", 0));
        Assert.Equal("UNTRUSTED_OR_INVALID_AUDIT_RESULT", result.Code);
        Assert.Equal(0, a.Calls);
        Assert.Empty(log.Entries);
        NoAuthority(result);
    }

    [Fact]
    public async Task PositiveResultRequiresBoundedNonzeroCount()
    {
        var a = new Authority();
        var log = new Log();
        var sink = new Sink(a, log);
        foreach (var n in new[] { -1, 0, 17, int.MaxValue })
            Assert.Equal("UNTRUSTED_OR_INVALID_AUDIT_RESULT",
                (await sink.WriteAsync(Good(n))).Code);
        Assert.Empty(log.Entries);
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public async Task DenialCannotClaimInspectedEntries()
    {
        var a = new Authority();
        var log = new Log();
        Assert.Equal("UNTRUSTED_OR_INVALID_AUDIT_RESULT",
            (await new Sink(a, log).WriteAsync(
                new("OBSERVABILITY_SOURCE_REJECTED", 1))).Code);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public async Task TenantMismatchFailsClosedWithoutLogging()
    {
        var a = new Authority { Evidence = Trusted() with { TenantPartitionMatches = false } };
        var log = new Log();
        var result = await new Sink(a, log).WriteAsync(Good());
        Assert.Equal("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED", result.Code);
        Assert.Empty(log.Entries);
        NoAuthority(result);
    }

    [Fact]
    public async Task RevokedMembershipOrRbacOrAuditFenceIsDenied()
    {
        foreach (var context in new[] {
            Trusted() with { MembershipCurrent = false },
            Trusted() with { ExplicitReadPermissionCurrent = false },
            Trusted() with { AuditFenceCurrent = false },
            Trusted() with { BackendAuthenticated = false }
        })
        {
            var log = new Log();
            Assert.Equal("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED",
                (await new Sink(new Authority { Evidence = context }, log)
                    .WriteAsync(Good())).Code);
            Assert.Empty(log.Entries);
        }
    }

    [Fact]
    public async Task WrongShaOrUppercaseShaIsDenied()
    {
        foreach (var context in new[] {
            Trusted() with { ObservedSourceSha = new string('a', 40) },
            Trusted() with { ExpectedSourceSha = Sha.ToUpperInvariant() },
            Trusted() with { ExpectedSourceSha = "bad" }
        })
        {
            var log = new Log();
            Assert.Equal("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED",
                (await new Sink(new Authority { Evidence = context }, log)
                    .WriteAsync(Good())).Code);
            Assert.Empty(log.Entries);
        }
    }

    [Fact]
    public async Task MissingAuthorityReturnsOpaqueDenial()
    {
        var log = new Log();
        Assert.Equal("CURRENT_AUTHORITY_AND_TENANT_FENCE_REQUIRED",
            (await new Sink(new Authority { Evidence = null }, log)
                .WriteAsync(Good())).Code);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public async Task AuthorityExceptionNeverLeaksPrivateData()
    {
        var log = new Log();
        var result = await new Sink(new Authority { Throws = true }, log).WriteAsync(Good());
        Assert.Equal("TRUSTED_AUDIT_EVIDENCE_UNAVAILABLE", result.Code);
        Assert.Empty(log.Entries);
        NoAuthority(result);
    }

    [Fact]
    public async Task LoggerDisabledAndThrowingAreFailClosed()
    {
        var authority = new Authority();
        var disabled = new Log { Enabled = false };
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE",
            (await new Sink(authority, disabled).WriteAsync(Good())).Code);
        Assert.Equal(0, authority.Calls);
        var broken = new Log { Throws = true };
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE",
            (await new Sink(authority, broken).WriteAsync(Good())).Code);
        Assert.Empty(broken.Entries);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeExternalRead()
    {
        var a = new Authority();
        var log = new Log();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Sink(a, log).WriteAsync(Good(), cts.Token));
        Assert.Equal(0, a.Calls);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public void DependenciesMustNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!, new Log()));
        Assert.Throws<ArgumentNullException>(() => new Sink(new Authority(), null!));
    }
}
