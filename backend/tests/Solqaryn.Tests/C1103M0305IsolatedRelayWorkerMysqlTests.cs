using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Oracle EF + physical ephemeral MySQL 8.4; scripted transport is NOT a provider.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0305IsolatedRelayWorkerMysqlTests
{
    private sealed class ScriptedTransport(
        Func<int, Guid, string, string, string, CancellationToken, Task> handler)
        : IC1103OutboxIsolatedTransport
    {
        public Task SendAsync(
            int tenantId, Guid eventId, string kind, string payload,
            string key, CancellationToken cancellationToken) =>
            handler(tenantId, eventId, kind, payload, key, cancellationToken);
    }

    [Fact]
    public async Task TenantIsolationOrderedClaimsPartialFailureRetryDeadletterAndStaleCasAreDurable()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var original = new MySqlConnectionStringBuilder(raw);
        if (original.Server is not ("127.0.0.1" or "localhost") ||
            original.Port != 3306 || original.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var temporary = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c1103_m0305_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(temporary,
                m => m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var tenantA = new Empresa("C11 M0305 Tenant A");
            var tenantB = new Empresa("C11 M0305 Tenant B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();

            var start = DateTime.UtcNow;
            var retry = MensajeOutbox.Crear(tenantA.Id, "ventas.factura.emitida",
                "{\"tenant\":\"A\"}", "c11-m0305-retry", creadoEnUtc: start.AddMinutes(-3));
            var ok = MensajeOutbox.Crear(tenantA.Id, "ventas.factura.emitida",
                "{\"tenant\":\"A\"}", "c11-m0305-ok", creadoEnUtc: start.AddMinutes(-2));
            var foreign = MensajeOutbox.Crear(tenantB.Id, "ventas.factura.emitida",
                "{\"tenant\":\"B\"}", "c11-m0305-foreign", creadoEnUtc: start.AddMinutes(-1));
            db.MensajesOutbox.AddRange(retry, ok, foreign);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var deliveries = new List<string>();
            var relay = new C1103OutboxIsolatedRelayWorker(
                new MensajeOutboxRepository(db),
                new ScriptedTransport((tenantId, eventId, kind, payload, key, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    Assert.Equal(tenantA.Id, tenantId);
                    Assert.NotEqual(foreign.EventoId, eventId);
                    Assert.Equal("ventas.factura.emitida", kind);
                    Assert.Contains("\"A\"", payload);
                    deliveries.Add(key);
                    if (key == "c11-m0305-retry" &&
                        deliveries.Count(x => x == key) == 1)
                        throw new InvalidOperationException("untrusted provider secret/PII");
                    return Task.CompletedTask;
                }));

            var one = await relay.RunTenantAsync(tenantA.Id, start, 2, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(2, 1, 1, 0, 0), one);
            var stateRetry = await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == retry.Id);
            var stateOk = await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == ok.Id);
            var stateB = await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == foreign.Id);
            Assert.Equal(EstadoMensajeOutbox.Fallido, stateRetry.Estado);
            Assert.Equal(1, stateRetry.Intentos);
            Assert.Equal("OUTBOX_TRANSPORT_FAILURE", stateRetry.UltimoError);
            Assert.DoesNotContain("secret", stateRetry.UltimoError);
            Assert.Equal(EstadoMensajeOutbox.Entregado, stateOk.Estado);
            Assert.Equal(EstadoMensajeOutbox.Pendiente, stateB.Estado);

            var premature = await relay.RunTenantAsync(
                tenantA.Id, start.AddSeconds(30), 2, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(0, premature.Claimed);
            var second = await relay.RunTenantAsync(
                tenantA.Id, start.AddSeconds(61), 2, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(1, 1, 0, 0, 0), second);
            Assert.Equal(EstadoMensajeOutbox.Entregado,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == retry.Id)).Estado);
            Assert.False(await new MensajeOutboxRepository(db).MarcarEntregadoAsync(
                tenantA.Id, retry.Id, 1, start.AddSeconds(62)));
            Assert.False(await new MensajeOutboxRepository(db).MarcarEntregadoAsync(
                tenantB.Id, retry.Id, 2, start.AddSeconds(62)));

            var poison = MensajeOutbox.Crear(tenantA.Id, "ventas.factura.emitida",
                "{\"tenant\":\"A\"}", "c11-m0305-poison", creadoEnUtc: start);
            db.MensajesOutbox.Add(poison);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var dead = new C1103OutboxIsolatedRelayWorker(
                new MensajeOutboxRepository(db),
                new ScriptedTransport((_, _, _, _, _, _) =>
                    throw new InvalidOperationException("untrusted provider PII")));
            var third = await dead.RunTenantAsync(
                tenantA.Id, start.AddSeconds(62), 2, 1, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(1, 0, 0, 1, 0), third);
            var failed = await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == poison.Id);
            Assert.Equal(EstadoMensajeOutbox.DeadLetter, failed.Estado);
            Assert.Equal("OUTBOX_TRANSPORT_FAILURE", failed.UltimoError);
            Assert.Equal(0, (await dead.RunTenantAsync(
                tenantA.Id, start.AddMinutes(2), 2, 1, TimeSpan.FromMinutes(1))).Claimed);
            Assert.Equal(EstadoMensajeOutbox.Pendiente,
                (await db.MensajesOutbox.AsNoTracking().SingleAsync(x => x.Id == foreign.Id)).Estado);
            Assert.Equal(new[] {"c11-m0305-retry", "c11-m0305-ok", "c11-m0305-retry"},
                deliveries);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                relay.RunTenantAsync(tenantA.Id, start, 2, 2,
                    TimeSpan.FromMinutes(1), cancelled.Token));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                relay.RunTenantAsync(0, start, 2, 2, TimeSpan.FromMinutes(1)));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
