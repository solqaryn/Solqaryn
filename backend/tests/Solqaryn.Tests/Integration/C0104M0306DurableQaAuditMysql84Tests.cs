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
                  EvidenceKey CHAR(64) CHARACTER SET ascii NOT NULL,
                  ProviderWorkflowId BIGINT NULL,
                  ProviderRunId BIGINT NULL,
                  ProviderAttempt INT NULL,
                  ProviderJobId BIGINT NULL,
                  ProviderCheckRunId BIGINT NULL,
                  ProviderCheckSuiteId BIGINT NULL,
                  PRIMARY KEY(TenantId,CaseFileId,EvidenceKey)
                ) ENGINE=InnoDB
                """);

            var utc = DateTimeOffset.UtcNow;
            var a = new Audit(
                new Source(Observation(utc, "101", "42")),
                new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), 101, 42,
                    new FrozenClock(utc)));
            var b = new Audit(
                new Source(Observation(utc, "202", "42")),
                new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), 202, 42,
                    new FrozenClock(utc)));

            var first = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "101", "42");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", first.Code);
            Assert.False(first.CertifiesCI);
            Assert.False(first.CertifiesA5);
            Assert.False(first.GrantsTenantAccess);
            Assert.False(first.AllowsProduction);
            Assert.Equal(1L, await Count(isolated, 101, 42));
            Assert.Equal(0L, await Count(isolated, 202, 42));

            var second = await b.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "202", "42");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", second.Code);
            Assert.Equal(1L, await Count(isolated, 101, 42));
            Assert.Equal(1L, await Count(isolated, 202, 42));
            Assert.Equal(0L, await Count(isolated, 101, 43));

            // Same exact-SHA event cannot silently overwrite or masquerade as a new receipt.
            var replay = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "101", "42");
            Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE", replay.Code);
            Assert.Equal(1L, await Count(isolated, 101, 42));

            // Caller scope mismatch is rejected BEFORE the durable sink is called.
            var mismatch = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "202", "42");
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_MISMATCH", mismatch.Code);
            Assert.Equal(2L, await CountAll(isolated));

            // The trusted observation and request may agree with each other,
            // but an A-scoped durable sink must not store B's SQL receipt.
            // This was previously possible because SafeAuditEntry has no tenant ID.
            var misbound = new Audit(
                new Source(Observation(utc, "202", "42")),
                new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), 101, 42,
                    new FrozenClock(utc)));
            var denied = await misbound.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "202", "42");
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_MISMATCH", denied.Code);
            Assert.Equal(1L, await Count(isolated, 101, 42));
            Assert.Equal(1L, await Count(isolated, 202, 42));

            // Opaque scopes must never become SQL receipts via guessed mappings.
            var opaque = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "tenant-A", "case-42");
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_MISMATCH", opaque.Code);
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
            // Exact identifiers must not accept a final line feed before SQL.
            var invalidId = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with { MicrotaskId = safe.MicrotaskId + "\n" }, default));
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_INVALID", invalidId.Message);
            var invalidSha = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(safe with { SourceSha = safe.SourceSha + "\n" }, default));
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_INVALID", invalidSha.Message);
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

            // New R2 hypothesis: two independent writers racing the same receipt
            // must produce one durable row and one sanitized duplicate rejection.
            // Unlike the prior sequential replay test, these calls overlap on MySQL 8.4.
            var contested = safe with { ObservedAtUtc = utc.AddSeconds(1) };
            async Task<bool> AttemptConcurrentReceipt()
            {
                try
                {
                    await direct.WriteAsync(contested, CancellationToken.None);
                    return true;
                }
                catch (InvalidOperationException ex)
                {
                    Assert.Equal("CASEFILE_QA_DURABLE_WRITE_REJECTED", ex.Message);
                    return false;
                }
            }

            var race = await Task.WhenAll(
                AttemptConcurrentReceipt(), AttemptConcurrentReceipt());
            Assert.Equal(1, race.Count(wasWritten => wasWritten));
            Assert.Equal(1, race.Count(wasWritten => !wasWritten));
            Assert.Equal(2L, await Count(isolated, 101, 42));
            Assert.Equal(1L, await Count(isolated, 202, 42));
            Assert.Equal(3L, await CountAll(isolated));

            // Admit the actual directed job for this ID without certifying its source.
            var directed = new Audit(new Source(Observation(utc, "101", "42") with
                { CheckName = "C01-04.M03.06 - directed durable MySQL 8.4 QA proof" }), direct, new FrozenClock(utc));
            var directedReceipt = await directed.CaptureAsync(Microtask, "casefile.timeline", Sha, "101", "42");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", directedReceipt.Code);
            Assert.False(directedReceipt.CertifiesCI);
            Assert.False(directedReceipt.CertifiesA5);
            var directedEntry = safe with { CheckName = "C01-04.M03.06 - directed durable MySQL 8.4 QA proof" };
            var wrongTask = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(directedEntry with { MicrotaskId = "C01-04.M03.05" }, default));
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_INVALID", wrongTask.Message);
            var alteredName = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(directedEntry with { CheckName = directedEntry.CheckName + "\n" }, default));
            Assert.Equal("CASEFILE_QA_DURABLE_SCOPE_INVALID", alteredName.Message);
            Assert.Equal(4L, await CountAll(isolated));
            await using (var verify = new MySqlConnection(isolated))
            {
                await verify.OpenAsync();
                await using var cmd = verify.CreateCommand();
                cmd.CommandText = $"SELECT COUNT(*) FROM {Table} WHERE TenantId=101 AND CaseFileId=42 AND MicrotaskId=@task AND CheckName=@name";
                cmd.Parameters.AddWithValue("@task", Microtask);
                cmd.Parameters.AddWithValue("@name", directedEntry.CheckName);
                Assert.Equal(1L, Convert.ToInt64(await cmd.ExecuteScalarAsync()));
            }

            // Synthetic provider identity exercises real MySQL storage and replay only.
            // It does not certify the provider or authenticated tenant ownership.
            var provider = new Audit.ProviderJobIdentity(Microtask, Sha, 100, 200,
                1, 300, 400, 500, directedEntry.CheckName, "completed", "failure");
            async Task<Audit.Verdict> CaptureProvider(Audit.ProviderJobIdentity job,
                DateTimeOffset observed, string tenant)
            {
                var sink = new V5UniversalCaseFileQaDurableMySqlAuditSink(
                    () => new MySqlConnection(isolated), int.Parse(tenant), 42, new FrozenClock(utc));
                return await new Audit(new Source(Observation(observed, tenant, "42") with
                {
                    CheckName = job.CheckName, CheckConclusion = job.Conclusion, ProviderJob = job
                }), sink, new FrozenClock(utc)).CaptureAsync(Microtask, "casefile.timeline", Sha, tenant, "42");
            }
            var firstBuild = await CaptureProvider(provider, utc, "101");
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING", firstBuild.Code);
            Assert.False(firstBuild.CertifiesCI);
            Assert.False(firstBuild.CertifiesA5);
            Assert.False(firstBuild.GrantsTenantAccess);
            Assert.Equal("CASEFILE_QA_AUDIT_SINK_UNAVAILABLE",
                (await CaptureProvider(provider, utc.AddSeconds(1), "101")).Code);
            // Same run/SHA/timestamp, distinct attempt/job: preserve both outcomes.
            var secondAttempt = provider with { Attempt = 2, JobId = 301, CheckRunId = 401, Conclusion = "success" };
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING",
                (await CaptureProvider(secondAttempt, utc, "101")).Code);
            var secondRun = provider with { RunId = 201, JobId = 302, CheckRunId = 402 };
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING",
                (await CaptureProvider(secondRun, utc, "101")).Code);
            Assert.Equal("CASEFILE_QA_ENVELOPE_WRITTEN_REVIEW_PENDING",
                (await CaptureProvider(provider, utc, "202")).Code);
            Assert.Equal("CASEFILE_QA_CHECK_OR_TIME_INVALID",
                (await CaptureProvider(provider with { SourceSha = new string('f', 40) }, utc, "101")).Code);
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await direct.WriteAsync(directedEntry with { ProviderJob = provider with { Attempt = 0 } }, default));
            Assert.Equal(8L, await CountAll(isolated));
            await using (var verifyProvider = new MySqlConnection(isolated))
            {
                await verifyProvider.OpenAsync();
                await using var cmd = verifyProvider.CreateCommand();
                cmd.CommandText = $"SELECT ProviderWorkflowId,ProviderRunId,ProviderAttempt,ProviderJobId,ProviderCheckRunId,ProviderCheckSuiteId,CheckConclusion FROM {Table} WHERE TenantId=101 AND CaseFileId=42 AND ProviderRunId=200 ORDER BY ProviderAttempt";
                await using var reader = await cmd.ExecuteReaderAsync();
                Assert.True(await reader.ReadAsync());
                Assert.Equal((100L, 200L, 1, 300L, 400L, 500L, "failure"),
                    (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6)));
                Assert.True(await reader.ReadAsync());
                Assert.Equal((100L, 200L, 2, 301L, 401L, 500L, "success"),
                    (reader.GetInt64(0), reader.GetInt64(1), reader.GetInt32(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5), reader.GetString(6)));
                Assert.False(await reader.ReadAsync());
            }

            // Storage outage or schema absence cannot create a successful receipt.
            await Execute(isolated, $"DROP TABLE {Table}");
            var unavailable = await a.CaptureAsync(
                Microtask, "casefile.timeline", Sha, "101", "42");
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
