using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1401FunctionalAcceptanceAuditServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private sealed class TrustedReader : IC1401FunctionalAcceptanceAuthorityReader
    {
        public bool Fail;
        public C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext? Snapshot =
            new("functional_acceptance", Sha, 7, 11, true, true, true, true,
                false, new HashSet<string>(StringComparer.Ordinal) { "AF01" },
                new HashSet<string>(StringComparer.Ordinal) { "READ_OWN" });

        public ValueTask<C1401FunctionalAcceptanceAuthorizationGate.VerifiedContext?> ReadAsync(
            string resourceKey, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new InvalidOperationException("secret-token-must-not-log");
            return ValueTask.FromResult(Snapshot);
        }
    }

    private sealed class CapturingLogger : ILogger<C1401FunctionalAcceptanceAuditService>
    {
        public bool Enabled = true;
        public bool ThrowOnWrite;
        public int Writes;
        public string? LastMessage;
        public List<string> FieldNames = new();

        private sealed class Scope : IDisposable { public void Dispose() { } }
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();
        public bool IsEnabled(LogLevel level) => Enabled;

        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnWrite) throw new InvalidOperationException("audit-sink-secret");
            Assert.Equal(LogLevel.Information, level);
            Assert.Equal(140106, eventId.Id);
            Assert.Null(exception);
            Writes++;
            LastMessage = formatter(state, exception);
            if (state is IEnumerable<KeyValuePair<string, object?>> properties)
                FieldNames = properties.Select(p => p.Key).ToList();
        }
    }

    private static C1401FunctionalAcceptanceAuthorizationGate.Request Intent(int tenant = 11) =>
        new("functional_acceptance", Sha, tenant,
            new V5FunctionalAcceptanceDomainRules.AcceptanceEvidence?[]
            {
                new("AF01", "READ_OWN", 11, Sha, "ci:isolated:proof",
                    true, true, false, false, true, true)
            });

    private static C1401FunctionalAcceptanceAuditService Build(
        TrustedReader reader, CapturingLogger logger) =>
        new(new C1401FunctionalAcceptanceAuthorizationGate(reader), logger);

    [Fact]
    public async Task PositivePreflightIsStructuredAndStillCannotGrantAnyAuthority()
    {
        var logger = new CapturingLogger();
        var result = await Build(new TrustedReader(), logger).InspectAndAuditAsync(Intent());
        Assert.Equal("AUTHENTICATED_FUNCTIONAL_ACCEPTANCE_ADAPTER_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsChecked);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesFunctionalAcceptance);
        Assert.False(result.AuthorizesQaOrProduction);
        Assert.Equal(1, logger.Writes);
        Assert.Contains("OutcomeCode", logger.FieldNames);
        Assert.Contains("ItemsChecked", logger.FieldNames);
        Assert.DoesNotContain("TenantId", logger.FieldNames);
        Assert.DoesNotContain("ResourceKey", logger.FieldNames);
        Assert.DoesNotContain("SourceCommitSha", logger.FieldNames);
        Assert.DoesNotContain(Sha, logger.LastMessage);
        Assert.DoesNotContain("functional_acceptance", logger.LastMessage);
    }

    [Fact]
    public async Task CrossTenantAssertionAndRevocationRemainFailClosedAndAudited()
    {
        var logger = new CapturingLogger();
        var reader = new TrustedReader();
        var service = Build(reader, logger);
        var foreign = await service.InspectAndAuditAsync(Intent(tenant: 22));
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED", foreign.Code);
        Assert.False(foreign.GrantsTenantOrResourceAccess);
        reader.Snapshot = reader.Snapshot! with { SessionOrRoleRevoked = true };
        var revoked = await service.InspectAndAuditAsync(Intent());
        Assert.Equal("CURRENT_BACKEND_JWT_RBAC_MEMBERSHIP_OWNERSHIP_REQUIRED", revoked.Code);
        Assert.False(revoked.AllowsInstitutionalWrite);
        Assert.Equal(2, logger.Writes);
    }

    [Fact]
    public async Task SourceFailureIsOpaqueAndNeverLeaksSecretsIntoAudit()
    {
        var logger = new CapturingLogger();
        var result = await Build(new TrustedReader { Fail = true }, logger)
            .InspectAndAuditAsync(Intent());
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_SOURCE_UNAVAILABLE", result.Code);
        Assert.False(result.GrantsTenantOrResourceAccess);
        Assert.Equal(1, logger.Writes);
        Assert.DoesNotContain("secret-token-must-not-log", logger.LastMessage);
    }

    [Fact]
    public async Task DisabledOrThrowingAuditSinkDeniesInsteadOfClaimingSuccess()
    {
        var logger = new CapturingLogger { Enabled = false };
        var service = Build(new TrustedReader(), logger);
        var disabled = await service.InspectAndAuditAsync(Intent());
        Assert.Equal("AUDIT_SINK_UNAVAILABLE", disabled.Code);
        Assert.Equal(0, logger.Writes);
        logger.Enabled = true;
        logger.ThrowOnWrite = true;
        var failed = await service.InspectAndAuditAsync(Intent());
        Assert.Equal("AUDIT_SINK_UNAVAILABLE", failed.Code);
        Assert.False(failed.AuthorizesQaOrProduction);
    }

    [Fact]
    public async Task CancellationStopsBeforePreflightAndEmitsNoAudit()
    {
        var logger = new CapturingLogger();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Build(new TrustedReader(), logger)
                .InspectAndAuditAsync(Intent(), cancel.Token));
        Assert.Equal(0, logger.Writes);
    }

    [Fact]
    public async Task BoundaryOversizedBatchDeniesAndRecordsOnlySafeCode()
    {
        var logger = new CapturingLogger();
        var original = Intent();
        var tooMany = Enumerable.Repeat(original.Items!.Single(), 65).ToArray();
        var result = await Build(new TrustedReader(), logger)
            .InspectAndAuditAsync(original with { Items = tooMany });
        Assert.Equal("ACCEPTANCE_BATCH_CAPACITY_INVALID", result.Code);
        Assert.Equal(0, result.ItemsChecked);
        Assert.Equal(1, logger.Writes);
    }
}
