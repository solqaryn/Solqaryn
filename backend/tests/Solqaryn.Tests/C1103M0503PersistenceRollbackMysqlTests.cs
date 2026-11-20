using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.API.Controllers;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C11-03.M05.03: physical Oracle EF/MySQL 8.4 transactional persistence and rollback.
/// Tenant membership is an explicit fixture; this does not certify JWT/HTTP/RBAC.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0503PersistenceRollbackMysqlTests
{
    private sealed class MemberOf(int tenantId) : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(
                empresaId == tenantId
                    ? new UsuarioTenantScopeActual(73, tenantId, 3, "Operador", false)
                    : null);
    }

    [Fact]
    public async Task PhysicalOutboxCommitRollbackAndIdempotencyPreserveTenantIsolation()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c1103_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, m =>
                m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("C11 M0503 transactional tenant A");
            var b = new Empresa("C11 M0503 transactional tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var serviceA = new MensajeOutboxService(
                new MensajeOutboxRepository(db), new MemberOf(a.Id));
            var requestA = new RegistrarMensajeOutboxRequest(
                "ventas.factura.emitida", "{\"tenant\":\"A\"}",
                "Factura", "73", "c1103-transactional");

            // An outbox intent staged by the application must not outlive
            // a failed caller transaction: no partial event is delivered.
            await using (var rollback = await db.Database.BeginTransactionAsync())
            {
                await serviceA.RegistrarAsync(a.Id, requestA, "transaction-key");
                await db.SaveChangesAsync();
                Assert.Equal(1, await db.MensajesOutbox.CountAsync());
                await rollback.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            await using (var independent = new AppDbContext(options))
            {
                Assert.Empty(await independent.MensajesOutbox.AsNoTracking().ToListAsync());
            }

            Guid committedEvent;
            await using (var committed = await db.Database.BeginTransactionAsync())
            {
                var first = await serviceA.RegistrarAsync(a.Id, requestA, "transaction-key");
                committedEvent = first.EventoId;
                await db.SaveChangesAsync();
                await committed.CommitAsync();
            }
            db.ChangeTracker.Clear();

            // Reopen another physical connection to prove durable COMMIT,
            // one-record replay, and server-side tenant authority.
            await using (var independent = new AppDbContext(options))
            {
                var independentRepo = new MensajeOutboxRepository(independent);
                var readerA = new MensajeOutboxService(independentRepo, new MemberOf(a.Id));
                var stored = Assert.Single(await independent.MensajesOutbox.AsNoTracking().ToListAsync());
                Assert.Equal(a.Id, stored.EmpresaId);
                Assert.Equal(committedEvent, stored.EventoId);

                var replay = await readerA.RegistrarAsync(a.Id, requestA, "transaction-key");
                Assert.Equal(committedEvent, replay.EventoId);
                await independent.SaveChangesAsync();
                Assert.Equal(1, await independent.MensajesOutbox.AsNoTracking().CountAsync());
                Assert.Null(await independentRepo.GetByClaveIdempotenciaAsync(
                    b.Id, "transaction-key"));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                    readerA.RegistrarAsync(b.Id, requestA, "transaction-key"));
                var divergent = new RegistrarMensajeOutboxRequest(
                    "ventas.factura.emitida", "{\"tenant\":\"B\"}",
                    "Factura", "73", "c1103-transactional");
                await Assert.ThrowsAsync<ConflictException>(() =>
                    readerA.RegistrarAsync(a.Id, divergent, "transaction-key"));
            }

            // A separate tenant can stage a matching opaque key, but rolling
            // back its own transaction must never affect tenant A's commit.
            await using (var another = new AppDbContext(options))
            {
                var serviceB = new MensajeOutboxService(
                    new MensajeOutboxRepository(another), new MemberOf(b.Id));
                var requestB = new RegistrarMensajeOutboxRequest(
                    "ventas.factura.emitida", "{\"tenant\":\"B\"}",
                    "Factura", "74", "c1103-tenant-b");
                await using var rollbackB = await another.Database.BeginTransactionAsync();
                await serviceB.RegistrarAsync(b.Id, requestB, "transaction-key");
                await another.SaveChangesAsync();
                Assert.Equal(2, await another.MensajesOutbox.AsNoTracking().CountAsync());
                await rollbackB.RollbackAsync();
            }
            await using (var finalRead = new AppDbContext(options))
            {
                var persisted = Assert.Single(await finalRead.MensajesOutbox.AsNoTracking().ToListAsync());
                Assert.Equal(a.Id, persisted.EmpresaId);
                Assert.Equal(committedEvent, persisted.EventoId);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
