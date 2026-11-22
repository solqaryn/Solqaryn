using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0402M0306DurableAuditMysql84Tests
{
    [Fact]
    public async Task ActualMysql84CommitsSanitizedAuditsAndDeniesUnsafeOperations()
    {
        var raw = Environment.GetEnvironmentVariable("C0402_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var root = new MySqlConnectionStringBuilder(raw);
        if (root.Server != "127.0.0.1" || root.Port != 3306 || root.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        var schema = "c0402_m0306_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await using (var version = admin.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync())!);
        }
        await using (var create = admin.CreateCommand())
        {
            create.CommandText = "CREATE DATABASE `" + schema + "`";
            await create.ExecuteNonQueryAsync();
        }
        var isolated = new MySqlConnectionStringBuilder(raw) { Database = schema }
            .ConnectionString;
        try
        {
            await ExecuteAsync(isolated, """
                CREATE TABLE V23C0402ContractAuditIsolated (
                    EventId CHAR(32) NOT NULL PRIMARY KEY,
                    EventCode VARCHAR(64) NOT NULL,
                    OutcomeCode VARCHAR(96) NOT NULL,
                    IsDomainCandidateOnly BOOLEAN NOT NULL,
                    OccurredUtc DATETIME(6) NOT NULL
                ) ENGINE=InnoDB
                """);
            var sink = new C0402IsolatedMySqlContractAuditSink(
                () => new MySqlConnection(isolated));
            var logger = new TestLogger();
            var preflight = new C0402ContractAuditedPreflight(
                new Service(new Actor(), new Contract()), sink, logger);
            var intent = new Service.ClientIntent(7001, 101, 2,
                Rules.ContractOperation.Suspend,
                new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                "private-client-cause-do-not-persist");
            var accepted = await preflight.PrepareAuditedAsync(intent);
            Assert.True(accepted.IsDomainCandidateOnly);
            var denied = await preflight.PrepareAuditedAsync(intent with { TenantHint = 202 });
            Assert.Equal("CLIENT_TENANT_HINT_MISMATCH", denied.Code);
            Assert.False(denied.IsDomainCandidateOnly);
            await using (var inspect = new MySqlConnection(isolated))
            {
                await inspect.OpenAsync();
                await using var query = inspect.CreateCommand();
                query.CommandText = """
                    SELECT OutcomeCode, IsDomainCandidateOnly
                    FROM V23C0402ContractAuditIsolated
                    ORDER BY IsDomainCandidateOnly DESC
                    """;
                await using var records = await query.ExecuteReaderAsync();
                Assert.True(await records.ReadAsync());
                Assert.Equal(accepted.Code, records.GetString(0));
                Assert.True(records.GetBoolean(1));
                Assert.True(await records.ReadAsync());
                Assert.Equal(denied.Code, records.GetString(0));
                Assert.False(records.GetBoolean(1));
                Assert.False(await records.ReadAsync());
            }
            Assert.DoesNotContain("private-client-cause", string.Join(";", logger.Lines));
            Assert.False(await sink.AppendAsync(
                new C0402ContractAuditEntry("UNAUTHORIZED_EVENT", "OK", true),
                CancellationToken.None));
            Assert.False(await sink.AppendAsync(
                new C0402ContractAuditEntry("C0402_CONTRACT_PREFLIGHT",
                    "private-leak", true), CancellationToken.None));
            Assert.False(await new C0402IsolatedMySqlContractAuditSink(
                () => new MySqlConnection(raw)).AppendAsync(
                new C0402ContractAuditEntry("C0402_CONTRACT_PREFLIGHT",
                    denied.Code, false), CancellationToken.None));
            // R2 adversarial audit-integrity invariant: no false-positive
            // candidate or disguised denied candidate may reach durable MySQL.
            Assert.False(await sink.AppendAsync(
                new C0402ContractAuditEntry("C0402_CONTRACT_PREFLIGHT",
                    "LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED", true),
                CancellationToken.None));
            Assert.False(await sink.AppendAsync(
                new C0402ContractAuditEntry("C0402_CONTRACT_PREFLIGHT",
                    "REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED",
                    false), CancellationToken.None));
            await using (var verify = new MySqlConnection(isolated))
            {
                await verify.OpenAsync();
                await using var count = verify.CreateCommand();
                count.CommandText = "SELECT COUNT(*) FROM V23C0402ContractAuditIsolated";
                Assert.Equal(2L, Convert.ToInt64(await count.ExecuteScalarAsync()));
            }
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await sink.AppendAsync(new C0402ContractAuditEntry(
                    "C0402_CONTRACT_PREFLIGHT", accepted.Code, true),
                    new CancellationToken(true)));
            await ExecuteAsync(isolated, "DROP TABLE V23C0402ContractAuditIsolated");
            var failClosed = await preflight.PrepareAuditedAsync(intent);
            Assert.Equal("CONTRACT_AUDIT_SINK_UNAVAILABLE", failClosed.Code);
            Assert.False(failClosed.IsDomainCandidateOnly);
            Assert.Equal(2, logger.Lines.Count);
        }
        finally
        {
            await using var cleanup = admin.CreateCommand();
            cleanup.CommandText = "DROP DATABASE `" + schema + "`";
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var db = new MySqlConnection(connectionString);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class Actor : IC0402VerifiedContractActorSource
    {
        public ValueTask<Service.VerifiedServerActor?> ReadCurrentAsync(
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Service.VerifiedServerActor?>(
                new(new Rules.VerifiedActor(101, 911, true, true, true, false),
                    true, true, true, true));
        }
    }

    private sealed class Contract : IC0402VerifiedContractSnapshotSource
    {
        public ValueTask<Service.VerifiedContractContext?> ReadOwnedAsync(
            int tenant, long id, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (tenant != 101 || id != 7001)
                return ValueTask.FromResult<Service.VerifiedContractContext?>(null);
            return ValueTask.FromResult<Service.VerifiedContractContext?>(
                new(new Rules.ContractSnapshot(101, 7001, Rules.ContractState.Active, 2,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
                    new Rules.CommercialEvidence(true, true, true, true, true, true, true),
                    true, true, true, true));
        }
    }

    private sealed class TestLogger : ILogger<C0402ContractAuditedPreflight>
    {
        public List<string> Lines { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception));
    }
}
