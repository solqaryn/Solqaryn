using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class N72DOutboxMySqlConcurrencyTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Claims_Concurrentes_MySql_Entregan_Un_Solo_Owner()
    {
        var database = $"test_n72d_outbox_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={database};User=root;Password=root;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int empresaId;
            int mensajeId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var empresa = new Empresa("N7.2.D MySQL Tenant");
                setup.Add(empresa);
                await setup.SaveChangesAsync();
                empresaId = empresa.Id;

                var mensaje = MensajeOutbox.Crear(
                    empresaId,
                    "inventario.cambiado",
                    "{\"safe\":true}",
                    $"mysql-concurrency-{Guid.NewGuid():N}",
                    creadoEnUtc: AhoraUtc.AddMinutes(-1));
                setup.Add(mensaje);
                await setup.SaveChangesAsync();
                mensajeId = mensaje.Id;
            }

            await using var db1 = new AppDbContext(options);
            await using var db2 = new AppDbContext(options);
            var repo1 = new MensajeOutboxRepository(db1);
            var repo2 = new MensajeOutboxRepository(db2);

            var claims = await Task.WhenAll(
                repo1.ClaimDisponiblesAsync(empresaId, AhoraUtc, 1),
                repo2.ClaimDisponiblesAsync(empresaId, AhoraUtc, 1));

            var obtenidos = claims.SelectMany(x => x).ToList();
            var unico = Assert.Single(obtenidos);
            Assert.Equal(mensajeId, unico.Id);
            Assert.Equal(1, unico.Intentos);

            await using var verify = new AppDbContext(options);
            var persisted = await verify.Set<MensajeOutbox>()
                .AsNoTracking()
                .SingleAsync(x => x.Id == mensajeId);
            Assert.Equal(1, persisted.Intentos);
            Assert.NotNull(persisted.ProcesandoDesdeUtc);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
