using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0404M0305DryRunQueueMysql84Tests
{
    [Fact]
    public async Task RealMysql84TenantIdempotencyConflictRollbackAndNoProviderDispatch()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0305_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");

        var schema = "c0404_m0305_" + Guid.NewGuid().ToString("N");
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
            var a = new C0404DryRunDispatchQueue.Command(
                401, 88, "provider-test", new string('a', 64),
                new string('b', 64), 25.55m, "HNL");
            var first = await queue.EnqueueDryRunAsync(a);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Queued, first.Code);
            Assert.True(first.EntryId > 0);
            Assert.False(first.MayContactProvider);
            Assert.False(first.MayChargeOrRefund);
            Assert.False(first.MayDispatchWorker);
            Assert.False(first.CertifiesJwtRbacOrLivePayments);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Replay,
                (await queue.EnqueueDryRunAsync(a)).Code);
            Assert.Equal(first.EntryId, (await queue.InspectAsync(a)).EntryId);

            // Reuse of one tenant/provider/key with changed payload must fail.
            Assert.Equal(C0404DryRunDispatchQueue.Status.Conflict,
                (await queue.EnqueueDryRunAsync(a with { FacturaId = 89 })).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Conflict,
                (await queue.EnqueueDryRunAsync(a with { Amount = 25.56m })).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Conflict,
                (await queue.EnqueueDryRunAsync(a with { PayloadDigest = new string('c', 64) })).Code);

            // Same hash in independent tenant must never read tenant A.
            var b = a with { EmpresaId = 402, FacturaId = 99, Amount = 11.75m };
            Assert.Equal(C0404DryRunDispatchQueue.Status.Missing,
                (await queue.InspectAsync(b)).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Queued,
                (await queue.EnqueueDryRunAsync(b)).Code);
            Assert.Equal(2, await db.Entries.CountAsync());
            Assert.All(await db.Entries.ToListAsync(),
                row => Assert.Equal("DryRunOnly", row.State));

            Assert.Equal(C0404DryRunDispatchQueue.Status.Denied,
                (await queue.EnqueueDryRunAsync(a with { EmpresaId = 0 })).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Denied,
                (await queue.EnqueueDryRunAsync(a with { KeyHash = "cleartext" })).Code);
            Assert.Equal(C0404DryRunDispatchQueue.Status.Denied,
                (await queue.EnqueueDryRunAsync(a with { Amount = 10.123m })).Code);

            var rolledBack = a with { KeyHash = new string('d', 64) };
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                Assert.Equal(C0404DryRunDispatchQueue.Status.Queued,
                    (await queue.EnqueueDryRunAsync(rolledBack)).Code);
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            Assert.Equal(C0404DryRunDispatchQueue.Status.Missing,
                (await queue.InspectAsync(rolledBack)).Code);
            Assert.Equal(2, await db.Entries.CountAsync());

            // SQL UNIQUE, not only application preflight, enforces idempotency.
            await using var duplicate = new C0404DryRunQueueDbContext(options);
            duplicate.Entries.Add(new C0404DryRunQueueEntry
            {
                EmpresaId = a.EmpresaId, FacturaId = a.FacturaId,
                Provider = a.Provider, KeyHash = a.KeyHash,
                PayloadDigest = a.PayloadDigest, Currency = "HNL",
                Amount = 25.55m, State = "DryRunOnly", CreatedUtc = DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => queue.EnqueueDryRunAsync(a, cancelled.Token));
        }
        finally
        {
            await using var cleanup = new C0404DryRunQueueDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
