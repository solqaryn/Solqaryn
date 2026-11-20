using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C04-04.M05.05: adversarial negative contract in isolated physical MySQL 8.4.
/// Store does NOT authenticate users or perform live payments; this scope
/// verifies only fail-closed input/SQL fences, not signed JWT/RBAC/HTTP.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0404M0505NegativeSecurityMySqlTests
{
    [Fact]
    public async Task PhysicalMySqlRejectsCrossTenantLookupMalformedInputAndSqlFenceBypass()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0505_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var conn = new MySqlConnectionStringBuilder(raw);
        if (conn.Server is not ("127.0.0.1" or "localhost") ||
            conn.Port != 3306 || conn.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");
        var schema = "c0404_m0505_" + Guid.NewGuid().ToString("N");
        var scoped = raw.Replace("Database=c0404_bootstrap", "Database=" + schema,
            StringComparison.Ordinal);
        Assert.Contains("Database=" + schema, scoped, StringComparison.Ordinal);
        var options = new DbContextOptionsBuilder<C0404PaymentReservationDbContext>()
            .UseMySQL(scoped).Options;
        try
        {
            await using var db = new C0404PaymentReservationDbContext(options);
            await db.Database.EnsureCreatedAsync();
            await db.Database.OpenConnectionAsync();
            await using (var cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await cmd.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true);
            }
            await db.Database.CloseConnectionAsync();
            var store = new C0404PaymentReservationStore(db);
            var a = new C0404PaymentReservationStore.Command(
                501, 1001, "provider-neutral", new string('a', 64),
                new string('b', 64), 100.50m, "HNL");
            var saved = await store.ReserveAsync(a);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, saved.Code);
            Assert.True(saved.ReservationId > 0);

            // A guessed key from A does not disclose A to a B-scoped lookup.
            var b = a with { EmpresaId = 502, FacturaId = 1002 };
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await store.InspectAsync(b)).Code);
            var savedB = await store.ReserveAsync(b);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, savedB.Code);
            Assert.NotEqual(saved.ReservationId, savedB.ReservationId);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay,
                (await store.InspectAsync(a)).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay,
                (await store.InspectAsync(b)).Code);

            // Same tenant/provider/hash with changed identity/amount/payload:
            // no replay or payment authorization may be inferred.
            foreach (var attack in new[]
            {
                a with { FacturaId = 1003 },
                a with { Amount = 101.00m },
                a with { PayloadDigest = new string('c', 64) }
            })
            {
                var result = await store.ReserveAsync(attack);
                Assert.Equal(C0404PaymentReservationStore.Status.Conflict, result.Code);
                Assert.False(result.AuthorizesProviderCall);
                Assert.False(result.AuthorizesChargeOrRefund);
            }

            foreach (var malformed in new[]
            {
                a with { EmpresaId = 0 },
                a with { FacturaId = 0 },
                a with { Provider = "x'; DROP TABLE V22C0404PaymentReservations;--" },
                a with { Provider = "unsafe space" },
                a with { KeyHash = "not-a-sha" },
                a with { PayloadDigest = "bad" },
                a with { Amount = 0m },
                a with { Amount = -1m },
                a with { Amount = 0.001m },
                a with { Currency = "hNl" }
            })
            {
                var result = await store.ReserveAsync(malformed);
                Assert.Equal(C0404PaymentReservationStore.Status.Denied, result.Code);
                Assert.False(result.AuthorizesProviderCall);
                Assert.False(result.AuthorizesChargeOrRefund);
            }
            Assert.Equal(2, await db.Reservations.CountAsync());

            // Bypass the application precheck to prove a PHYSICAL MySQL CHECK.
            await using (var bypass = new C0404PaymentReservationDbContext(options))
            {
                bypass.Reservations.Add(new C0404PaymentReservation
                {
                    EmpresaId = 0, FacturaId = 1004, Provider = "provider-neutral",
                    KeyHash = new string('d', 64), PayloadDigest = new string('e', 64),
                    Amount = 50m, Currency = "HNL", CreatedUtc = DateTime.UtcNow
                });
                await Assert.ThrowsAsync<DbUpdateException>(
                    () => bypass.SaveChangesAsync());
            }
            db.ChangeTracker.Clear();
            Assert.Equal(2, await db.Reservations.CountAsync());

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => store.ReserveAsync(a, cancelled.Token));
            Assert.Equal(2, await db.Reservations.CountAsync());
        }
        finally
        {
            await using var cleanup = new C0404PaymentReservationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
