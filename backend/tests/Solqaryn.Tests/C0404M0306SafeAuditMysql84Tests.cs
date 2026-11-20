using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0404M0306SafeAuditMysql84Tests
{
    private sealed class CapturingLogger(bool fails = false) : ILogger<C0404DryRunAuditedQueue>
    {
        private sealed class Scope : IDisposable { public void Dispose() { } }
        public List<string> Messages { get; } = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Assert.Null(exception);
            if (fails) throw new InvalidOperationException("SENSITIVE_LOG_SINK_FAILURE");
            Messages.Add(formatter(state, exception));
        }
    }

    [Fact]
    public async Task RealMysql84TenantAbRedactedAuditAndNoPaymentEffects()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0306_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");
        var schema = "c0404_m0306_" + Guid.NewGuid().ToString("N");
        var scoped = raw.Replace("Database=c0404_bootstrap", "Database=" + schema,
            StringComparison.Ordinal);
        Assert.Contains("Database=" + schema, scoped, StringComparison.Ordinal);
        var options = new DbContextOptionsBuilder<C0404DryRunQueueDbContext>()
            .UseMySQL(scoped).Options;

        try
        {
            await using var db = new C0404DryRunQueueDbContext(options);
            await db.Database.EnsureCreatedAsync();
            await db.Database.OpenConnectionAsync();
            await using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await cmd.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true);
            }
            await db.Database.CloseConnectionAsync();

            var queue = new C0404DryRunDispatchQueue(db);
            var logger = new CapturingLogger();
            var audit = new C0404DryRunAuditedQueue(queue, logger);
            var a = new C0404DryRunDispatchQueue.Command(
                401, 88, "provider-test", new string('a', 64),
                new string('b', 64), 25.55m, "HNL");

            var initial = await audit.EnqueueAsync(a);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Queued, initial.Code);
            Assert.False(initial.MayContactProvider);
            Assert.False(initial.MayChargeOrRefund);
            Assert.False(initial.MayDispatchWorker);
            Assert.False(initial.CertifiesJwtRbacOrLivePayments);

            Assert.Equal(C0404DryRunDispatchQueue.Status.Replay,
                (await audit.EnqueueAsync(a)).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Conflict,
                (await audit.EnqueueAsync(a with { FacturaId = 89 })).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Denied,
                (await audit.EnqueueAsync(a with { EmpresaId = 0 })).Code);

            var b = a with { EmpresaId = 402, FacturaId = 99 };
            Assert.Equal(C0404DryRunDispatchQueue.Status.Missing,
                (await audit.InspectAsync(b)).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Queued,
                (await audit.EnqueueAsync(b)).Code);
            Assert.Equal(2, await db.Entries.CountAsync());
            Assert.All(await db.Entries.ToListAsync(),
                entry => Assert.Equal("DryRunOnly", entry.State));

            var brokenLogger = new C0404DryRunAuditedQueue(queue, new CapturingLogger(fails: true));
            Assert.Equal(C0404DryRunDispatchQueue.Status.Replay,
                (await brokenLogger.EnqueueAsync(a)).Code);
            Assert.Equal(2, await db.Entries.CountAsync());

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => audit.EnqueueAsync(a, cancelled.Token));

            var messages = string.Join("\n", logger.Messages);
            Assert.Contains("C0404_DRYRUN_RESULT", messages);
            Assert.Contains("Tenant=401", messages);
            Assert.Contains("Tenant=402", messages);
            Assert.DoesNotContain("provider-test", messages);
            Assert.DoesNotContain(new string('a', 64), messages);
            Assert.DoesNotContain(new string('b', 64), messages);
            Assert.DoesNotContain("25.55", messages);
            Assert.DoesNotContain("Factura", messages);
            Assert.DoesNotContain("SENSITIVE_LOG_SINK_FAILURE", messages);
        }
        finally
        {
            await using var cleanup = new C0404DryRunQueueDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
