using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C04-04.M06.04: physical MySQL 8.4 rollback and cancellation proof.
/// Isolated test-only store. Not a live payment deactivation mechanism.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0404M0604RollbackAndDeactivationMySqlTests
{
    [Fact]
    public async Task PhysicalMySql84RollbackAndCancellationPreserveTenantABReservations()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0604_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");

        var schema = "c0404_m0604_" + Guid.NewGuid().ToString("N");
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
            await using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await command.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true);
            }
            await db.Database.CloseConnectionAsync();

            var store = new C0404PaymentReservationStore(db);
            var tenantA = new C0404PaymentReservationStore.Command(
                601, 9001, "provider-neutral", new string('a', 64),
                new string('b', 64), 12.50m, "HNL");
            var tenantB = tenantA with
            {
                EmpresaId = 602, FacturaId = 9002,
                KeyHash = new string('c', 64),
                PayloadDigest = new string('d', 64),
                Amount = 35.75m
            };
            var savedA = await store.ReserveAsync(tenantA);
            var savedB = await store.ReserveAsync(tenantB);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, savedA.Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, savedB.Code);
            Assert.True(savedA.ReservationId > 0);
            Assert.True(savedB.ReservationId > 0);
            Assert.NotEqual(savedA.ReservationId, savedB.ReservationId);
            Assert.Equal(2, await db.Reservations.CountAsync());

            // Both original rows must survive two later operations rolled back.
            var abandonedA = tenantA with
            {
                FacturaId = 9011, KeyHash = new string('e', 64),
                PayloadDigest = new string('f', 64)
            };
            var abandonedB = tenantB with
            {
                FacturaId = 9012, KeyHash = new string('1', 64),
                PayloadDigest = new string('2', 64)
            };
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                Assert.Equal(C0404PaymentReservationStore.Status.Reserved,
                    (await store.ReserveAsync(abandonedA)).Code);
                Assert.Equal(C0404PaymentReservationStore.Status.Reserved,
                    (await store.ReserveAsync(abandonedB)).Code);
                Assert.Equal(4, await db.Reservations.CountAsync());
                await transaction.RollbackAsync();
            }
            db.ChangeTracker.Clear();

            // Independent EF context reads physical SQL, not tracked state.
            await using var observer = new C0404PaymentReservationDbContext(options);
            var recovery = new C0404PaymentReservationStore(observer);
            Assert.Equal(2, await observer.Reservations.CountAsync());
            var restoredA = await recovery.InspectAsync(tenantA);
            var restoredB = await recovery.InspectAsync(tenantB);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay, restoredA.Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Replay, restoredB.Code);
            Assert.Equal(savedA.ReservationId, restoredA.ReservationId);
            Assert.Equal(savedB.ReservationId, restoredB.ReservationId);
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await recovery.InspectAsync(abandonedA)).Code);
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await recovery.InspectAsync(abandonedB)).Code);

            var crossTenantGuess = tenantA with { EmpresaId = tenantB.EmpresaId };
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await recovery.InspectAsync(crossTenantGuess)).Code);

            // Aborted request must not mutate either surviving reservation.
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => recovery.ReserveAsync(abandonedA, canceled.Token));
            Assert.Equal(2, await observer.Reservations.CountAsync());

            var resumed = await recovery.ReserveAsync(abandonedA);
            Assert.Equal(C0404PaymentReservationStore.Status.Reserved, resumed.Code);
            Assert.Equal(3, await observer.Reservations.CountAsync());
            Assert.Equal(savedA.ReservationId,
                (await recovery.InspectAsync(tenantA)).ReservationId);
            Assert.Equal(savedB.ReservationId,
                (await recovery.InspectAsync(tenantB)).ReservationId);
            Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                (await recovery.InspectAsync(abandonedB)).Code);

            foreach (var result in new[] { savedA, savedB, resumed })
            {
                Assert.False(result.AuthorizesProviderCall);
                Assert.False(result.AuthorizesChargeOrRefund);
                Assert.False(result.CertifiesJwtOrCommercialIntegration);
            }
        }
        finally
        {
            // Destructive cleanup applies only to unique temporary CI schema.
            await using var cleanup = new C0404PaymentReservationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
