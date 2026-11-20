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
/// C11-03.M05.05 negative security and abuse tests on real Oracle EF +
/// ephemeral MySQL 8.4. Membership is an explicit scoped fixture, not real
/// JWT or live UsuarioEmpresa/RBAC integration.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0505NegativeSecurityMysqlTests
{
    private sealed class MemberOf(int tenantId) : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);
        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(
                empresaId == tenantId
                    ? new UsuarioTenantScopeActual(91, tenantId, 3, "Operador", false)
                    : null);
    }

    [Fact]
    public async Task TenantBoundaryRejectsForeignReplayAndDatabaseUniqueRejectsAbusiveDuplicates()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");
        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c1103_m0505_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, m =>
                m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("C11 M0505 adversarial tenant A");
            var b = new Empresa("C11 M0505 adversarial tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var repo = new MensajeOutboxRepository(db);
            var scopedA = new MensajeOutboxService(repo, new MemberOf(a.Id));
            var scopedB = new MensajeOutboxService(repo, new MemberOf(b.Id));
            var request = new RegistrarMensajeOutboxRequest(
                "ventas.factura.emitida", "{\"tenant\":\"A\"}",
                "Factura", "99", "c11-negative");
            var changed = new RegistrarMensajeOutboxRequest(
                "ventas.factura.emitida", "{\"tenant\":\"B\"}",
                "Factura", "99", "c11-negative");

            var first = await scopedA.RegistrarAsync(a.Id, request, "negative-idem-key");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Membership A may never borrow scope B, even with a known replay key.
            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                scopedA.RegistrarAsync(b.Id, request, "negative-idem-key"));
            await Assert.ThrowsAsync<ConflictException>(() =>
                scopedA.RegistrarAsync(a.Id, changed, "negative-idem-key"));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                scopedA.RegistrarAsync(0, request, "negative-idem-key"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                scopedA.RegistrarAsync(a.Id, request, "invalid\nkey"));

            // Same tenant key cannot be inserted via another EF context bypassing
            // service validation: the physical unique index is the final guard.
            await using (var adversary = new AppDbContext(options))
            {
                adversary.MensajesOutbox.Add(MensajeOutbox.Crear(
                    a.Id, request.TipoEvento, changed.PayloadJson, "negative-idem-key",
                    request.TipoAgregado, request.IdAgregado, request.CorrelationId));
                await Assert.ThrowsAsync<DbUpdateException>(() =>
                    adversary.SaveChangesAsync());
            }

            // Independent authorized B can use the same opaque key safely.
            var ownB = await scopedB.RegistrarAsync(b.Id, changed, "negative-idem-key");
            await db.SaveChangesAsync();
            Assert.NotEqual(first.EventoId, ownB.EventoId);
            var records = await db.MensajesOutbox.AsNoTracking().ToListAsync();
            Assert.Equal(2, records.Count);
            Assert.Single(records.Where(x => x.EmpresaId == a.Id));
            Assert.Single(records.Where(x => x.EmpresaId == b.Id));
            Assert.Equal(first.EventoId,
                Assert.Single(records.Where(x => x.EmpresaId == a.Id)).EventoId);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
