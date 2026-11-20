using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C04-04.M05.04: positive acceptance of the provider-neutral RESERVATION
/// contract in isolated physical MySQL 8.4. No real payment, webhook, PSP,
/// signed-JWT HTTP, production AppDbContext or commercial integration.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0404M0504PositiveReservationAcceptanceTests
{
    [Fact]
    public async Task AcceptedTenantReservationPersistsReplaysAndRollsBackWithoutCharging()
    {
        var raw = Environment.GetEnvironmentVariable("C0404_M0504_MYSQL")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var connection = new MySqlConnectionStringBuilder(raw);
        if (connection.Server is not ("127.0.0.1" or "localhost") ||
            connection.Port != 3306 || connection.UserID != "root")
            throw new InvalidOperationException("LOCAL_EPHEMERAL_ROOT_ONLY");

        var schema = "c0404_m0504_" + Guid.NewGuid().ToString("N");
        var scoped = raw.Replace("Database=c0404_bootstrap", "Database=" + schema,
            StringComparison.Ordinal);
        Assert.Contains("Database=" + schema, scoped, StringComparison.Ordinal);
        var options = new DbContextOptionsBuilder<C0404PaymentReservationDbContext>()
            .UseMySQL(scoped).Options;
        try
        {
            await using (var db = new C0404PaymentReservationDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();
                await db.Database.OpenConnectionAsync();
                await using var versionCommand = db.Database.GetDbConnection().CreateCommand();
                versionCommand.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await versionCommand.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true,
                    $"Expected physical MySQL 8.4, observed {version}");
                await db.Database.CloseConnectionAsync();

                var store = new C0404PaymentReservationStore(db);
                var command = new C0404PaymentReservationStore.Command(
                    301, 901, "provider-neutral",
                    new string('a', 64), new string('b', 64), 120.50m, "HNL");

                var accepted = await store.ReserveAsync(command);
                Assert.Equal(C0404PaymentReservationStore.Status.Reserved, accepted.Code);
                Assert.True(accepted.ReservationId > 0);
                Assert.False(accepted.AuthorizesProviderCall);
                Assert.False(accepted.AuthorizesChargeOrRefund);
                Assert.False(accepted.CertifiesJwtOrCommercialIntegration);

                var replay = await store.ReserveAsync(command);
                Assert.Equal(C0404PaymentReservationStore.Status.Replay, replay.Code);
                Assert.Equal(accepted.ReservationId, replay.ReservationId);

                var otherTenant = command with { EmpresaId = 302, FacturaId = 902 };
                var acceptedB = await store.ReserveAsync(otherTenant);
                Assert.Equal(C0404PaymentReservationStore.Status.Reserved, acceptedB.Code);
                Assert.NotEqual(accepted.ReservationId, acceptedB.ReservationId);
                Assert.Equal(C0404PaymentReservationStore.Status.Replay,
                    (await store.InspectAsync(otherTenant)).Code);
                Assert.Equal(C0404PaymentReservationStore.Status.Conflict,
                    (await store.ReserveAsync(command with { Amount = 120.51m })).Code);
                Assert.Equal(2, await db.Reservations.CountAsync());
            }

            // Reopen the physical SQL database in a new DbContext: evidence
            // must survive process-local tracking and never invoke a PSP.
            await using (var reopened = new C0404PaymentReservationDbContext(options))
            {
                var store = new C0404PaymentReservationStore(reopened);
                var command = new C0404PaymentReservationStore.Command(
                    301, 901, "provider-neutral",
                    new string('a', 64), new string('b', 64), 120.50m, "HNL");
                var persisted = await store.InspectAsync(command);
                Assert.Equal(C0404PaymentReservationStore.Status.Replay, persisted.Code);
                Assert.NotNull(persisted.ReservationId);
                Assert.False(persisted.AuthorizesChargeOrRefund);
                Assert.Equal(2, await reopened.Reservations.CountAsync());
                var rollback = command with { KeyHash = new string('c', 64) };
                await using (var tx = await reopened.Database.BeginTransactionAsync())
                {
                    Assert.Equal(C0404PaymentReservationStore.Status.Reserved,
                        (await store.ReserveAsync(rollback)).Code);
                    await tx.RollbackAsync();
                }
                reopened.ChangeTracker.Clear();
                Assert.Equal(C0404PaymentReservationStore.Status.Missing,
                    (await store.InspectAsync(rollback)).Code);
                Assert.Equal(2, await reopened.Reservations.CountAsync());
            }
        }
        finally
        {
            await using var cleanup = new C0404PaymentReservationDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
