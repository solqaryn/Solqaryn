using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0404M0503PaymentPersistenceConcurrencyTests
{
    [Fact]
    public async Task RealMySql84PersistenceConcurrencyReplayRollbackAndNoPaymentEffects()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0503_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");
        var schema = "c0404_m0503_" + Guid.NewGuid().ToString("N");
        // Avoid a MySqlConnector -> Oracle MySql.Data SSL enum spelling change.
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
                401, 88, "provider-test", new string('a', 64),
                new string('b', 64), 25.55m, "HNL");
            var first = await store.ReserveAsync(a);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, first.Code);
            Assert.True(first.ReservationId > 0);
            Assert.False(first.AuthorizesProviderCall);
            Assert.False(first.AuthorizesChargeOrRefund);
            Assert.False(first.CertifiesJwtOrCommercialIntegration);
            var replay = await store.ReserveAsync(a);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay, replay.Code);
            Assert.Equal(first.ReservationId, replay.ReservationId);
            Assert.Equal(C0404PaymentReservationStore.Status.Conflict,
                (await store.ReserveAsync(a with { Amount = 25.56m })).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Conflict,
                (await store.ReserveAsync(a with { FacturaId = 89 })).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Conflict,
                (await store.ReserveAsync(a with { PayloadDigest = new string('c', 64) })).Code);

            var b = a with { EmpresaId = 402, FacturaId = 99, Amount = 11.75m };
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved,
                (await store.ReserveAsync(b)).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await store.InspectAsync(a with { KeyHash = new string('d', 64) })).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay,
                (await store.InspectAsync(a)).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Denied,
                (await store.ReserveAsync(a with { EmpresaId = 0 })).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Denied,
                (await store.ReserveAsync(a with { KeyHash = "bad" })).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Denied,
                (await store.ReserveAsync(a with { Amount = 10.123m })).Code);
            Assert.Equal(2, await db.Reservations.CountAsync());

            var rollback = a with { KeyHash = new string('e', 64) };
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                Assert.Equal(C0404PaymentReservationStore.Status.Reserved,
                    (await store.ReserveAsync(rollback)).Code);
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await store.InspectAsync(rollback)).Code);
            Assert.Equal(2, await db.Reservations.CountAsync());

            // Bypass the application precheck to verify the physical SQL UNIQUE.
            await using var duplicate = new C0404PaymentReservationDbContext(options);
            duplicate.Reservations.Add(new C0404PaymentReservation
            {
                EmpresaId = a.EmpresaId,
                FacturaId = 999,
                Provider = a.Provider,
                KeyHash = a.KeyHash,
                PayloadDigest = new string('f', 64),
                Currency = "HNL",
                Amount = 99m,
                CreatedUtc = DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
            // M05.03 new causal integration: two independent EF DbContexts compete
            // for one physical MySQL UNIQUE reservation under the same tenant/key.
            // Exactly one INSERT may win; the other must re-read the same ID.
            var racing = a with { KeyHash = new string('e', 64), FacturaId = 100 };
            await using var racerDb1 = new C0404PaymentReservationDbContext(options);
            await using var racerDb2 = new C0404PaymentReservationDbContext(options);
            var racingResults = await Task.WhenAll(
                new C0404PaymentReservationStore(racerDb1).ReserveAsync(racing),
                new C0404PaymentReservationStore(racerDb2).ReserveAsync(racing));
            Assert.Single(racingResults, result =>
                result.Code == C0404PaymentReservationStore.Status.Reserved);
            Assert.Single(racingResults, result =>
                result.Code == C0404PaymentReservationStore.Status.Replay);
            Assert.Equal(racingResults[0].ReservationId, racingResults[1].ReservationId);
            Assert.All(racingResults, result =>
            {
                Assert.False(result.AuthorizesProviderCall);
                Assert.False(result.AuthorizesChargeOrRefund);
                Assert.False(result.CertifiesJwtOrCommercialIntegration);
            });
            db.ChangeTracker.Clear();
            Assert.Equal(3, await db.Reservations.CountAsync());

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => store.InspectAsync(a, cancelled.Token));
        }
        finally
        {
            await using var cleanup = new C0404PaymentReservationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
