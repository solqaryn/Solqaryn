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
/// C11-03.M06.06: real Oracle EF/MySQL 8.4 evidence for ambiguous transport
/// outcome, ordered retry and tenant isolation. The transport is deliberately
/// scripted: this is NOT a live provider or a global phase/release certificate.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0606ReplayOrderMysql84Tests
{
    private sealed class Recipient(
        Func<int, Guid, string, string, string, CancellationToken, Task> deliver)
        : IC1103OutboxIsolatedTransport
    {
        public Task SendAsync(int tenantId, Guid eventId, string kind,
            string payload, string key, CancellationToken cancellationToken)
            => deliver(tenantId, eventId, kind, payload, key, cancellationToken);
    }

    [Fact]
    public async Task AmbiguousAcceptanceReplaysSameKeyInOrderWithoutCrossTenantAck()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var bootstrap = new MySqlConnectionStringBuilder(raw);
        if (bootstrap.Server is not ("127.0.0.1" or "localhost")
            || bootstrap.Port != 3306 || bootstrap.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        bootstrap.Database = "c1103_m0606_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(bootstrap.ConnectionString,
                opt => opt.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var tenantA = new Empresa("C11 M0606 A");
            var tenantB = new Empresa("C11 M0606 B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();

            var now = DateTime.UtcNow;
            var uncertain = MensajeOutbox.Crear(tenantA.Id, "outbox.event",
                "{\"tenant\":\"A\"}", "c11-m0606-uncertain",
                creadoEnUtc: now.AddMinutes(-4));
            var ordinary = MensajeOutbox.Crear(tenantA.Id, "outbox.event",
                "{\"tenant\":\"A\"}", "c11-m0606-ordinary",
                creadoEnUtc: now.AddMinutes(-3));
            var foreign = MensajeOutbox.Crear(tenantB.Id, "outbox.event",
                "{\"tenant\":\"B\"}", "c11-m0606-foreign",
                creadoEnUtc: now.AddMinutes(-2));
            db.MensajesOutbox.AddRange(uncertain, ordinary, foreign);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // A receiving service may accept a message and then time out.
            // At-least-once delivery is unavoidable: only a real consumer's
            // tenant/key deduplication can prevent duplicate external effects.
            var attempts = new List<string>();
            var accepted = new HashSet<string>(StringComparer.Ordinal);
            var relay = new C1103OutboxIsolatedRelayWorker(
                new MensajeOutboxRepository(db),
                new Recipient((tenant, eventId, kind, payload, key, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    Assert.Equal(tenantA.Id, tenant);
                    Assert.NotEqual(foreign.EventoId, eventId);
                    Assert.Equal("outbox.event", kind);
                    Assert.Contains("\"A\"", payload);
                    attempts.Add(key);
                    accepted.Add(tenant + ":" + key);
                    if (key == "c11-m0606-uncertain" &&
                        attempts.Count(x => x == key) == 1)
                        throw new TimeoutException("REMOTE_RESULT_UNKNOWN_SECRET");
                    return Task.CompletedTask;
                }));

            var first = await relay.RunTenantAsync(
                tenantA.Id, now, 1, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(
                1, 0, 1, 0, 0), first);
            db.ChangeTracker.Clear();
            var failed = await db.MensajesOutbox.AsNoTracking()
                .SingleAsync(x => x.Id == uncertain.Id);
            Assert.Equal(EstadoMensajeOutbox.Fallido, failed.Estado);
            Assert.Equal("OUTBOX_TRANSPORT_FAILURE", failed.UltimoError);
            Assert.DoesNotContain("SECRET", failed.UltimoError);

            // The scheduled retry has not matured. The other A message must
            // be processed in original order; B is never enumerated.
            var second = await relay.RunTenantAsync(
                tenantA.Id, now, 1, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(
                1, 1, 0, 0, 0), second);
            var third = await relay.RunTenantAsync(
                tenantA.Id, now.AddSeconds(61), 1, 2, TimeSpan.FromMinutes(1));
            Assert.Equal(new C1103OutboxIsolatedRelayWorker.BatchResult(
                1, 1, 0, 0, 0), third);

            Assert.Equal(new[] { "c11-m0606-uncertain",
                "c11-m0606-ordinary", "c11-m0606-uncertain" }, attempts);
            Assert.Equal(2, accepted.Count); // test recipient deduplicated replay
            db.ChangeTracker.Clear();
            Assert.Equal(EstadoMensajeOutbox.Entregado,
                (await db.MensajesOutbox.AsNoTracking()
                    .SingleAsync(x => x.Id == uncertain.Id)).Estado);
            Assert.Equal(EstadoMensajeOutbox.Entregado,
                (await db.MensajesOutbox.AsNoTracking()
                    .SingleAsync(x => x.Id == ordinary.Id)).Estado);
            Assert.Equal(EstadoMensajeOutbox.Pendiente,
                (await db.MensajesOutbox.AsNoTracking()
                    .SingleAsync(x => x.Id == foreign.Id)).Estado);

            var repository = new MensajeOutboxRepository(db);
            Assert.False(await repository.MarcarEntregadoAsync(
                tenantA.Id, uncertain.Id, 1, now.AddSeconds(62)));
            Assert.False(await repository.MarcarEntregadoAsync(
                tenantB.Id, uncertain.Id, 2, now.AddSeconds(62)));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
