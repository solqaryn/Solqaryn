using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C09-04.M03.04: persistencia EF/Oracle MySQL 8.4 efimera, por tenant.
/// Nunca usa Aiven, produccion ni proveedores de mensajeria.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0904M0304MySqlOutboxPersistenceTests
{
    [Fact]
    public async Task RepositorioMySqlPreservaTenantIdempotenciaYClaimAtomico()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se requiere MySQL efimero de CI.");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("C09-04.M03.04 prohibe conexiones MySQL remotas.");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v19_outbox_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var empresaA = new Empresa("V19 Outbox A");
            var empresaB = new Empresa("V19 Outbox B");
            db.Set<Empresa>().AddRange(empresaA, empresaB);
            await db.SaveChangesAsync();

            var repo = new MensajeOutboxRepository(db);
            await repo.AddAsync(MensajeOutbox.Crear(
                empresaA.Id, "crm.mensaje.preparado", "{\"canal\":\"email\"}", "clave-replay"));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var a = await repo.GetByClaveIdempotenciaAsync(empresaA.Id, " clave-replay ");
            Assert.NotNull(a);
            Assert.Equal(empresaA.Id, a.EmpresaId);
            Assert.Null(await repo.GetByClaveIdempotenciaAsync(empresaB.Id, "clave-replay"));
            Assert.False(await repo.ExisteClaveIdempotenciaAsync(empresaB.Id, "clave-replay"));

            var fecha = DateTime.UtcNow.AddMinutes(2);
            Assert.Single(await repo.ClaimDisponiblesAsync(empresaA.Id, fecha, 10));
            Assert.Empty(await repo.ClaimDisponiblesAsync(empresaA.Id, fecha, 10));

            await repo.AddAsync(MensajeOutbox.Crear(
                empresaB.Id, "crm.mensaje.preparado", "{\"canal\":\"email\"}", "clave-replay"));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var b = await repo.GetByClaveIdempotenciaAsync(empresaB.Id, "clave-replay");
            Assert.NotNull(b);
            Assert.Equal(empresaB.Id, b.EmpresaId);
            Assert.NotEqual(a.Id, b.Id);
            Assert.Single(await repo.ClaimDisponiblesAsync(empresaB.Id, fecha, 10));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                repo.GetByClaveIdempotenciaAsync(empresaA.Id, "clave-replay\n"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                repo.ExisteClaveIdempotenciaAsync(empresaA.Id, "clave-replay\r"));

            db.ChangeTracker.Clear();
            await repo.AddAsync(MensajeOutbox.Crear(
                empresaA.Id, "crm.mensaje.preparado", "{\"canal\":\"email\"}", "clave-replay"));
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
