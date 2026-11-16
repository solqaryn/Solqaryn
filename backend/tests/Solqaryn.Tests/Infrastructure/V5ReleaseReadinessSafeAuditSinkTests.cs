using Microsoft.Extensions.Logging;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Sink = Solqaryn.Infrastructure.AutomationV5.V5ReleaseReadinessSafeAuditSink;
using Worker = Solqaryn.Application.AutomationV5.V5ReleaseReadinessBoundedReviewWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5ReleaseReadinessSafeAuditSinkTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Sink.VerifiedContext Valid() =>
        new(7, 10, 10, Sha, Sha, "dev", true, true, true, true, true, true, false);

    private sealed class Authority : IV5ReleaseReadinessSafeAuditAuthority
    {
        public Sink.VerifiedContext? Context = Valid();
        public int Calls;
        public bool Throws;
        public ValueTask<Sink.VerifiedContext?> CurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("SECRET_TENANT_TOKEN");
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
            if (Throws) throw new InvalidOperationException("SECRET_TENANT_TOKEN");
            Events.Add((level, id, formatter(state, exception)));
        }
    }

    private static Worker.Outcome Nominal(int count = 1) =>
        new("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", count);

    private static Sink Create(Authority source, CaptureLogger logger) => new(source, logger);

    private static void NeverAuthorizes(Sink.Receipt r)
    {
        Assert.False(r.GrantsAccess);
        Assert.False(r.ApprovesA5);
        Assert.False(r.AuthorizesDeployment);
        Assert.False(r.CertifiesRestore);
        Assert.False(r.CertifiesDurableAudit);
        Assert.False(r.WritesDatabase);
        Assert.False(r.AcknowledgesQueue);
        Assert.False(r.RunsProviderOrWorker);
        Assert.False(r.ExposesTenantData);
    }

    [Fact]
    public async Task NominalPreviewLogsOnlyFixedCodeAndNeverGrants()
    {
        var logger = new CaptureLogger();
        var receipt = await Create(new Authority(), logger).RecordAsync(Nominal());
        Assert.True(receipt.LoggerAccepted);
        NeverAuthorizes(receipt);
        var item = Assert.Single(logger.Events);
        Assert.Equal(LogLevel.Information, item.Level);
        Assert.Equal(14040306, item.Id.Id);
        Assert.Contains("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", item.Message);
        Assert.DoesNotContain(Sha, item.Message);
        Assert.DoesNotContain("tenant", item.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", item.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeniedWorkerOutcomeLogsWarningNotApproval()
    {
        var logger = new CaptureLogger();
        var receipt = await Create(new Authority(), logger).RecordAsync(
            new Worker.Outcome("REVIEW_EVIDENCE_DENIED", 1));
        Assert.True(receipt.LoggerAccepted);
        NeverAuthorizes(receipt);
        Assert.Equal(LogLevel.Warning, Assert.Single(logger.Events).Level);
    }

    [Fact]
    public async Task AllAllowedFailureCodesAreWarnings()
    {
        foreach (var code in new[] {
            "AUTHENTICATED_REVIEW_INBOX_UNAVAILABLE",
            "AUTHENTICATED_BOUNDED_DEV_INBOX_REQUIRED",
            "INVALID_READ_ONLY_WORK_ITEM",
            "DUPLICATE_WORK_ITEM_IN_BATCH",
            "READ_ONLY_SNAPSHOT_INSPECTION_UNAVAILABLE"
        })
        {
            var logger = new CaptureLogger();
            var receipt = await Create(new Authority(), logger).RecordAsync(new Worker.Outcome(code, 0));
            Assert.True(receipt.LoggerAccepted);
            Assert.Equal(LogLevel.Warning, Assert.Single(logger.Events).Level);
            NeverAuthorizes(receipt);
        }
    }

    [Fact]
    public async Task UnknownCodeOrInvalidCountCannotReadAuthorityOrLog()
    {
        var authority = new Authority();
        var logger = new CaptureLogger();
        foreach (var outcome in new[] {
            new Worker.Outcome("SECRET_TENANT_TOKEN", 0),
            new Worker.Outcome("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", -1),
            new Worker.Outcome("EXTERNAL_BROWSER_AND_A5_REVIEW_REQUIRED", 17)
        })
        {
            Assert.Equal("UNRECOGNIZED_OR_UNTRUSTED_REVIEW_OUTCOME",
                (await Create(authority, logger).RecordAsync(outcome)).Code);
        }
        Assert.Equal(0, authority.Calls);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task CrossTenantDeniedBeforeLogging()
    {
        var logger = new CaptureLogger();
        var authority = new Authority { Context = Valid() with { ResourceTenantId = 99 } };
        Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
            (await Create(authority, logger).RecordAsync(Nominal())).Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task MissingOrRevokedMembershipRoleAndReadPermissionDenied()
    {
        foreach (var state in new[] {
            Valid() with { MembershipCurrent = false },
            Valid() with { RoleCurrent = false },
            Valid() with { ExplicitReadPermissionCurrent = false },
            Valid() with { Revoked = true }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
                (await Create(new Authority { Context = state }, logger).RecordAsync(Nominal())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task SelfReviewAndMissingAuditFenceDenied()
    {
        foreach (var state in new[] {
            Valid() with { ReviewerDistinctFromAuthor = false },
            Valid() with { AuditFenceCurrent = false }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
                (await Create(new Authority { Context = state }, logger).RecordAsync(Nominal())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task UnauthenticatedOrUnknownActorRejected()
    {
        foreach (var state in new[] {
            Valid() with { BackendAuthenticated = false },
            Valid() with { ActorId = 0 },
            Valid() with { ActorTenantId = 0 }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
                (await Create(new Authority { Context = state }, logger).RecordAsync(Nominal())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task QaProdOrUppercaseDevNotAllowed()
    {
        foreach (var environment in new[] { "QA", "prod", "DEV", "" })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
                (await Create(new Authority { Context = Valid() with { Environment = environment } },
                    logger).RecordAsync(Nominal())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task StaleOrMalformedShaNotAccepted()
    {
        foreach (var state in new[] {
            Valid() with { ObservedSha = new string('f', 40) },
            Valid() with { SourceSha = Sha.ToUpperInvariant() },
            Valid() with { SourceSha = "bad" }
        })
        {
            var logger = new CaptureLogger();
            Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
                (await Create(new Authority { Context = state }, logger).RecordAsync(Nominal())).Code);
            Assert.Empty(logger.Events);
        }
    }

    [Fact]
    public async Task NullAuthorityFailsClosed()
    {
        var logger = new CaptureLogger();
        Assert.Equal("CURRENT_BACKEND_RELEASE_REVIEW_AUTHORIZATION_REQUIRED",
            (await Create(new Authority { Context = null }, logger).RecordAsync(Nominal())).Code);
        Assert.Empty(logger.Events);
    }

    [Fact]
    public async Task AuthExceptionProducesOpaqueError()
    {
        var logger = new CaptureLogger();
        var result = await Create(new Authority { Throws = true }, logger).RecordAsync(Nominal());
        Assert.Equal("AUDIT_AUTHORITY_UNAVAILABLE", result.Code);
        Assert.Empty(logger.Events);
        NeverAuthorizes(result);
    }

    [Fact]
    public async Task DisabledLoggerSkipsAuthorityRead()
    {
        var authority = new Authority();
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE",
            (await Create(authority, new CaptureLogger { Enabled = false }).RecordAsync(Nominal())).Code);
        Assert.Equal(0, authority.Calls);
    }

    [Fact]
    public async Task LoggerExceptionDoesNotRevealPrivateContext()
    {
        var logger = new CaptureLogger { Throws = true };
        var result = await Create(new Authority(), logger).RecordAsync(Nominal());
        Assert.Equal("AUDIT_LOGGER_UNAVAILABLE", result.Code);
        Assert.Empty(logger.Events);
        NeverAuthorizes(result);
    }

    [Fact]
    public async Task CancellationStopsBeforeBackendSourceRead()
    {
        var authority = new Authority();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Create(authority, new CaptureLogger()).RecordAsync(Nominal(), cts.Token));
        Assert.Equal(0, authority.Calls);
    }

    [Fact]
    public void DependenciesAreRequired()
    {
        Assert.Throws<ArgumentNullException>(() => new Sink(null!, new CaptureLogger()));
        Assert.Throws<ArgumentNullException>(() => new Sink(new Authority(), null!));
    }
}
