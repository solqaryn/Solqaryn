using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.API.Controllers;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C11-03.M05.04: positive application acceptance against physical ephemeral MySQL 8.4.
/// No HTTP/JWT or real UsuarioEmpresa permission certification is claimed here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1103M0504PositiveAcceptanceMysqlTests
{
    private sealed class FixedScope(int companyId) : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);
        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioTenantScopeActual?>(
                empresaId == companyId
                    ? new UsuarioTenantScopeActual(71, companyId, 3, "Operador", false)
                    : null);
    }

    [Fact]
    public async Task PositiveAcceptancePersistsOnceAndReplaysWithoutTenantBleed()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var identity = new MySqlConnectionStringBuilder(raw);
        if (identity.Server is not ("127.0.0.1" or "localhost") ||
            identity.Port != 3306 || identity.UserID != "root")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c1103_m0504_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, m =>
                m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("C11 M0504 tenant A");
            var b = new Empresa("C11 M0504 tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            Assert.True(a.Id > 0);
            Assert.True(b.Id > 0);
            Assert.NotEqual(a.Id, b.Id);

            var service = new MensajeOutboxService(
                new MensajeOutboxRepository(db), new FixedScope(a.Id));
            var request = new RegistrarMensajeOutboxRequest(
                "ventas.factura.emitida", "{\"tenant\":\"A\"}",
                "Factura", "42", "c1103-acceptance");
            var first = await service.RegistrarAsync(
                a.Id, request, "positive-replay-key");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var replay = await service.RegistrarAsync(
                a.Id, request, "positive-replay-key");
            await db.SaveChangesAsync();
            Assert.NotEqual(Guid.Empty, first.EventoId);
            Assert.Equal(first.EventoId, replay.EventoId);
            Assert.Equal(a.Id, replay.EmpresaId);

            var records = await db.MensajesOutbox.AsNoTracking().ToListAsync();
            var stored = Assert.Single(records);
            Assert.Equal(a.Id, stored.EmpresaId);
            Assert.Equal(first.EventoId, stored.EventoId);
            Assert.Equal(EstadoMensajeOutbox.Pendiente, stored.Estado);
            Assert.Equal("positive-replay-key", stored.ClaveIdempotencia);
            Assert.DoesNotContain(records, record => record.EmpresaId == b.Id);

            var changed = new RegistrarMensajeOutboxRequest(
                "ventas.factura.emitida", "{\"tenant\":\"changed\"}",
                "Factura", "42", "c1103-acceptance");
            await Assert.ThrowsAsync<ConflictException>(() =>
                service.RegistrarAsync(a.Id, changed, "positive-replay-key"));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
                service.RegistrarAsync(b.Id, request, "positive-replay-key"));
            Assert.Equal(1, await db.MensajesOutbox.AsNoTracking().CountAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
