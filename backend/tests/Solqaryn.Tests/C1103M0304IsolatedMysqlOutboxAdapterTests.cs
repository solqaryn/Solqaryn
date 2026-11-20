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
/// C11-03.M03.04: physical Oracle EF / MySQL 8.4 integration, never mocked SQL.
/// Only a GitHub-hosted loopback MySQL service is accepted; unique DB is dropped.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0304IsolatedMysqlOutboxAdapterTests
{
    [Fact]
    public async Task TenantBoundEvidenceAndDurableUniquenessRollbackAreReal()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        var disposableName = "c1103_m0304_" + Guid.NewGuid().ToString("N");
        var scoped = new MySqlConnectionStringBuilder(raw)
        {
            Database = disposableName
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(scoped,
                migration => migration.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("C11 Tenant A");
            var b = new Empresa("C11 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var first = MensajeOutbox.Crear(a.Id, "ventas.factura.emitida",
                "{\"test\":\"A\"}", "c11-m0304-shared-key");
            var second = MensajeOutbox.Crear(b.Id, "ventas.factura.emitida",
                "{\"test\":\"B\"}", "c11-m0304-shared-key");
            db.MensajesOutbox.AddRange(first, second);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var adapter = new C1103OutboxTenantEvidenceAdapter(db);
            var foundA = await adapter.ReadByEventAsync(a.Id, first.EventoId);
            Assert.NotNull(foundA);
            Assert.Equal(a.Id, foundA.EmpresaId);
            Assert.Equal(first.EventoId, foundA.EventoId);
            Assert.Equal(EstadoMensajeOutbox.Pendiente, foundA.Estado);
            Assert.Equal(0, foundA.Intentos);
            Assert.NotNull(await adapter.ReadByEventAsync(b.Id, second.EventoId));
            Assert.Null(await adapter.ReadByEventAsync(b.Id, first.EventoId));
            Assert.Null(await adapter.ReadByEventAsync(a.Id, second.EventoId));
            Assert.Null(await adapter.ReadByEventAsync(0, first.EventoId));
            Assert.Null(await adapter.ReadByEventAsync(a.Id, Guid.Empty));

            using (var cancelled = new CancellationTokenSource())
            {
                cancelled.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => adapter.ReadByEventAsync(a.Id, first.EventoId, cancelled.Token));
            }

            // A failed transaction must leave no intent; original tenant rows survive.
            var rolledBack = MensajeOutbox.Crear(a.Id, "ventas.factura.emitida",
                "{\"test\":\"rollback\"}", "c11-m0304-rollback-key");
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                db.MensajesOutbox.Add(rolledBack);
                await db.SaveChangesAsync();
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            Assert.Null(await adapter.ReadByEventAsync(a.Id, rolledBack.EventoId));
            Assert.NotNull(await adapter.ReadByEventAsync(a.Id, first.EventoId));
            Assert.NotNull(await adapter.ReadByEventAsync(b.Id, second.EventoId));

            // Actual physical Oracle EF tenant/key UNIQUE must reject replay,
            // even if application lookup is bypassed; do this last after rollback.
            db.MensajesOutbox.Add(MensajeOutbox.Crear(a.Id,
                "ventas.factura.emitida", "{\"test\":\"altered\"}",
                "c11-m0304-shared-key"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
