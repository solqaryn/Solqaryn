using Microsoft.Extensions.Logging;
using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0402M0306StructuredAuditTests
{
    private static Service.ClientIntent Intent(int tenant = 101) =>
        new(7001, tenant, 2, Rules.ContractOperation.Suspend,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "private-caller-secret-do-not-log");

    private static Service MakeService(bool revoked = false, bool sourceThrows = false) =>
        new(new ActorSource(revoked, sourceThrows), new ContractSource());

    private static C0402ContractAuditedPreflight MakeAuditor(
        TestSink sink, TestLogger logger, bool revoked = false,
        bool sourceThrows = false) =>
        new(MakeService(revoked, sourceThrows), sink, logger);

    [Fact]
    public async Task PositivePreflightWritesSanitizedStructuredAuditWithoutEffects()
    {
        var sink = new TestSink();
        var logger = new TestLogger();
        var result = await MakeAuditor(sink, logger).PrepareAuditedAsync(Intent());
        Assert.Equal("REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED", result.Code);
        Assert.True(result.IsDomainCandidateOnly);
        Assert.False(result.AuthorizesContractMutation);
        Assert.False(result.AuthorizesPaymentOrRefund);
        Assert.False(result.PersistsData);
        Assert.Equal(1, sink.Attempts);
        Assert.Equal("C0402_CONTRACT_PREFLIGHT", sink.Last?.EventCode);
        Assert.Equal(result.Code, sink.Last?.OutcomeCode);
        Assert.Single(logger.Lines);
        Assert.Contains("C0402_CONTRACT_PREFLIGHT", logger.Lines[0]);
        Assert.DoesNotContain("private-caller-secret", logger.Lines[0]);
        Assert.DoesNotContain("7001", logger.Lines[0]);
        Assert.DoesNotContain("101", logger.Lines[0]);
        Assert.DoesNotContain("911", logger.Lines[0]);
    }

    [Theory]
    [InlineData(202, false, "CLIENT_TENANT_HINT_MISMATCH")]
    [InlineData(101, true, "LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED")]
    public async Task CrossTenantAndRevokedRequestsAreAuditedButDenied(
        int tenant, bool revoked, string expected)
    {
        var sink = new TestSink();
        var logger = new TestLogger();
        var result = await MakeAuditor(sink, logger, revoked).PrepareAuditedAsync(Intent(tenant));
        Assert.Equal(expected, result.Code);
        Assert.False(result.IsDomainCandidateOnly);
        Assert.Equal(expected, sink.Last?.OutcomeCode);
        Assert.Single(logger.Lines);
        Assert.DoesNotContain("private-caller-secret", logger.Lines[0]);
    }

    [Fact]
    public async Task ClientPaymentAuthorityIsRejectedAndAuditedWithoutLoggingCause()
    {
        var sink = new TestSink();
        var logger = new TestLogger();
        var result = await MakeAuditor(sink, logger).PrepareAuditedAsync(
            Intent() with { RequestsPaymentOrRefund = true });
        Assert.Equal("CLIENT_AUTHORITY_OR_SIDE_EFFECT_DENIED", result.Code);
        Assert.False(result.AuthorizesPaymentOrRefund);
        Assert.Equal(result.Code, sink.Last?.OutcomeCode);
        Assert.DoesNotContain("private-caller-secret", string.Join(";", logger.Lines));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedOrThrowingAuditSinkFailsClosed(bool throws)
    {
        var sink = new TestSink { Accept = false, Throw = throws };
        var logger = new TestLogger();
        var result = await MakeAuditor(sink, logger).PrepareAuditedAsync(Intent());
        Assert.Equal(throws ? "CONTRACT_AUDIT_OR_LOG_UNAVAILABLE" :
            "CONTRACT_AUDIT_SINK_UNAVAILABLE", result.Code);
        Assert.False(result.IsDomainCandidateOnly);
        Assert.False(result.AuthorizesContractMutation);
        Assert.Single(logger.Lines);
        Assert.Contains("audit attempt", logger.Lines[0]);
        Assert.DoesNotContain("private-", logger.Lines[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisabledOrThrowingStructuredLoggerFailsClosed(bool throws)
    {
        var sink = new TestSink();
        var logger = new TestLogger { Enabled = throws, Throw = throws };
        var result = await MakeAuditor(sink, logger).PrepareAuditedAsync(Intent());
        Assert.Equal(throws ? "CONTRACT_AUDIT_OR_LOG_UNAVAILABLE" :
            "CONTRACT_STRUCTURED_LOG_UNAVAILABLE", result.Code);
        Assert.False(result.IsDomainCandidateOnly);
        // Disabled logging must not leave a false-positive durable candidate.
        // Logging exceptions after persistence remain non-atomic by design.
        Assert.Equal(0, sink.Attempts);
        Assert.Null(sink.Last);
    }

    [Fact]
    public async Task ProviderExceptionIsSanitizedBeforeAuditAndLog()
    {
        var sink = new TestSink();
        var logger = new TestLogger();
        var result = await MakeAuditor(sink, logger, sourceThrows: true)
            .PrepareAuditedAsync(Intent());
        Assert.Equal("SERVER_ACTOR_SOURCE_UNAVAILABLE", result.Code);
        Assert.Equal(result.Code, sink.Last?.OutcomeCode);
        Assert.Single(logger.Lines);
        Assert.DoesNotContain("private-provider-secret", logger.Lines[0]);
    }

    [Fact]
    public async Task CancellationAfterAuditAwaitOnlyLogsPrecommitAttempt()
    {
        using var cts = new CancellationTokenSource();
        var sink = new TestSink { CancelAfterAppend = cts };
        var logger = new TestLogger();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await MakeAuditor(sink, logger).PrepareAuditedAsync(Intent(), cts.Token));
        Assert.Equal(1, sink.Attempts);
        Assert.Single(logger.Lines);
        Assert.Contains("audit attempt", logger.Lines[0]);
        Assert.DoesNotContain("private-", logger.Lines[0]);
    }

    private sealed class ActorSource(bool revoked, bool sourceThrows) :
        IC0402VerifiedContractActorSource
    {
        public ValueTask<Service.VerifiedServerActor?> ReadCurrentAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sourceThrows)
                throw new InvalidOperationException("private-provider-secret");
            return ValueTask.FromResult<Service.VerifiedServerActor?>(
                new(new Rules.VerifiedActor(101, 911, true, true, true, revoked),
                    true, true, true, true));
        }
    }

    private sealed class ContractSource : IC0402VerifiedContractSnapshotSource
    {
        public ValueTask<Service.VerifiedContractContext?> ReadOwnedAsync(
            int tenant, long contractId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (tenant != 101 || contractId != 7001)
                return ValueTask.FromResult<Service.VerifiedContractContext?>(null);
            return ValueTask.FromResult<Service.VerifiedContractContext?>(
                new(new Rules.ContractSnapshot(101, 7001, Rules.ContractState.Active, 2,
                        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
                    new Rules.CommercialEvidence(true, true, true, true, true, true, true),
                    true, true, true, true));
        }
    }

    private sealed class TestSink : IC0402ContractAuditSink
    {
        public int Attempts { get; private set; }
        public C0402ContractAuditEntry? Last { get; private set; }
        public bool Accept { get; init; } = true;
        public bool Throw { get; init; }
        public CancellationTokenSource? CancelAfterAppend { get; init; }

        public ValueTask<bool> AppendAsync(
            C0402ContractAuditEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Attempts++;
            Last = entry;
            if (Throw) throw new InvalidOperationException("private-sink-secret");
            CancelAfterAppend?.Cancel();
            return ValueTask.FromResult(Accept);
        }
    }

    private sealed class TestLogger : ILogger<C0402ContractAuditedPreflight>
    {
        public List<string> Lines { get; } = [];
        public bool Enabled { get; init; } = true;
        public bool Throw { get; init; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => Enabled;
        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (Throw) throw new InvalidOperationException("private-logger-secret");
            Lines.Add(formatter(state, exception));
        }
    }
}
