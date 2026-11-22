using MySql.Data.MySqlClient;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Audit = Solqaryn.Application.AutomationV5.V5UniversalCaseFileQaEvidenceAudit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// A real, disposable MySQL 8.4 test, not an authenticated case-file API test.
/// The in-test evidence source is synthetic; JWT/RBAC/HTTP and live DI remain unproven.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0104M0306DurableQaAuditMysql84Tests
{
    private const string Microtask = "C01-04.M03.06";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Table = "V23C0104CaseFileQaAuditIsolated";

    private sealed class Source(Audit.Observation observation) : IV5UniversalCaseFileQaEvidenceSource
    {
        public ValueTask<Audit.Observation?> ReadAsync(
            string microtaskId, string routeKey, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult<Audit.Observation?>(observation);
        }
    }

    private sealed class FrozenClock(DateTimeOffset utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utc;
    }

    private static Audit.Observation Observation(
        DateTimeOffset utc, string tenant, string caseFile) => new(
        Microtask, "casefile.timeline", Sha, tenant, caseFile,
        true, true, "verify-solqaryn-project-scope", "completed",
        "success", utc, "PII: bearer=secret@example.com");

    [Fact]
    public async Task DurableSinkCommitsTenantAAndBWithoutLeakingPayloadAndRejectsReplay()
    {
        var raw = RequiredEphemeralMysql();
        var schema = "c0104_qa_" + Guid.NewGuid().ToString("N");
        await using var admin = new MySqlConnection(raw);
        await admin.OpenAsync();
        await using (var version = admin.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            Assert.StartsWith("8.4.", Convert.ToString(await version.ExecuteScalarAsync())!);
        }
        await Execute(admin, "CREATE DATABASE " + Quote(schema));
        var isolated = new MySqlConnectionStringBuilder(raw) { Database = schema }.ConnectionString;
        try
        {
            await Execute(isolated, $"""
                CREATE TABLE {Table} (
                  TenantId INT NOT NULL,
                  CaseFileId INT NOT NULL,
                  MicrotaskId VARCHAR(20) NOT NULL,
                  RouteKey VARCHAR(40) NOT NULL,
                  SourceSha CHAR(40) NOT NULL,
                  CheckName VARCHAR(96) NOT NULL,
                  CheckStatus VARCHAR(16) NOT NULL,
                  CheckConclusion VARCHAR(16) NOT NULL,
                  ObservedUtc DATETIME(6) NOT NULL,
                  VerificationClass VARCHAR(64) NOT NULL,
                  PRIMARY KEY(TenantId,CaseFileId,SourceSha,RouteKey,CheckName,ObservedUtc)
                ) ENGINE=InnoDB
                """);

            var utc = DateTimeOffset.UtcNow;
            var a = new Audit(
                new Source(Observation(utc, "tenant-A", "case-42")),
                new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), 101, 42,
                    new FrozenClock(utc)));
            var b = new Audit(
                new Source(Observation(utc, "tenant-B", "case-42")),
                new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), 202, 42,
                    new FrozenClock(utc)));

            var first = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-A", "case-42");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", first.Code);
            Assert.False(first.CertifiesCI);
            Assert.False(first.CertifiesA5);
            Assert.False(first.GrantsTenantAccess);
            Assert.False(first.AllowsProduction);
            Assert.Equal(1L, await Count(isolated, 101, 42));
            Assert.Equal(0L, await Count(isolated, 202, 42));

            var second = await b.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-B", "case-42");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", second.Code);
            Assert.Equal(1L, await Count(isolated, 101, 42));
            Assert.Equal(1L, await Count(isolated, 202, 42));
            Assert.Equal(0L, await Count(isolated, 101, 43));

            // Same exact-SHA event cannot silently overwrite or masquerade as a new receipt.
            var replay = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-A", "case-42");
            Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE", replay.Code);
            Assert.Equal(1L, await Count(isolated, 101, 42));

            // Caller scope mismatch is rejected BEFORE the durable sink is called.
            var mismatch = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-B", "case-42");
            Assert.Equal("CASEFILE_QA_TRUSTED_EVIDENCE_REQUIRED", mismatch.Code);
            Assert.Equal(2L, await CountAll(isolated));

            var direct = new V5UniversalCaseFileQaDurableMySqlAuditSink(
                () => new MySqlConnection(isolated), 101, 42,
                new FrozenClock(utc));
            var safe = new Audit.SafeAuditEntry(
                Microtask, "casefile.timeline", Sha,
                "verify-solqaryn-project-scope", "completed", "success",
                utc, "UNVERIFIED_EXTERNAL_REVIEW_REQUIRED");
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with { CheckName = "Bearer secret" }, default));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with { CheckStatus = "skipped" }, default));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with { VerificationClass = "PASS" }, default));
            // A direct sink call must not bypass the Application freshness boundary.
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with
                {
                    ObservedAtUtc = utc.AddMinutes(-11)
                }, default));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with
                {
                    ObservedAtUtc = utc.AddSeconds(6)
                }, default));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with
                {
                    ObservedAtUtc = utc.ToOffset(TimeSpan.FromHours(-6))
                }, default));
            Assert.Equal(2L, await CountAll(isolated));

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await direct.WriteAsync(safe, cancelled.Token));
            Assert.Equal(2L, await CountAll(isolated));

            await using (var rows = new MySqlConnection(isolated))
            {
                await rows.OpenAsync();
                await using var select = rows.CreateCommand();
                select.CommandText = $"SELECT MicrotaskId,SourceSha,VerificationClass FROM {Table} WHERE TenantId=@tenant";
                select.Parameters.AddWithValue("@tenant", 101);
                await using var reader = await select.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                var rendered = reader.GetString(0) + reader.GetString(1) + reader.GetString(2);
                Assert.DoesNotContain("secret", rendered, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("tenant-A", rendered);
                Assert.Equal("UNVERIFIED_EXTERNAL_REVIEW_REQUIRED", reader.GetString(2));
                Assert.False(await reader.ReadAsync());
            }

            // Storage outage or schema absence cannot create a successful receipt.
            await Execute(isolated, $"DROP TABLE {Table}");
            var unavailable = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-A", "case-42");
            Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE", unavailable.Code);
        }
        finally
        {
            await Execute(admin, "DROP DATABASE " + Quote(schema));
        }
    }

    private static string RequiredEphemeralMysql()
    {
        var raw = Environment.GetEnvironmentVariable("C0104_M0306_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var info = new MySqlConnectionStringBuilder(raw);
        if (info.Server is not ("localhost" or "127.0.0.1") ||
            info.Port != 3306 || info.UserID != "root" || info.Database != "solqaryn_ci")
            throw new InvalidOperationException("LOOPBACK_MYSQL84_ONLY");
        return raw;
    }

    private static string Quote(string identifier) => "`" + identifier + "`";

    private static async Task Execute(string raw, string sql)
    {
        await using var connection = new MySqlConnection(raw);
        await connection.OpenAsync();
        await Execute(connection, sql);
    }

    private static async Task Execute(MySqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long> Count(string raw, int tenant, int caseFile)
    {
        await using var db = new MySqlConnection(raw);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {Table} WHERE TenantId=@tenant AND CaseFileId=@case";
        cmd.Parameters.AddWithValue("@tenant", tenant);
        cmd.Parameters.AddWithValue("@case", caseFile);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static async Task<long> CountAll(string raw)
    {
        await using var db = new MySqlConnection(raw);
        await db.OpenAsync();
        await using var cmd = db.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {Table}";
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
}
