using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class SeedFiscalMySqlIntegrationTests
{
    private static DbContextOptions<AppDbContext> CrearOpciones(string baseDatos) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={baseDatos};User=root;Password=root;")
            .Options;

    [Fact]
    public async Task Reinicio_NoDuplicaNiReactivaImpuestoEliminadoLogicamente()
    {
        var nombreBase = $"test_fiscal_seed_{Guid.NewGuid():N}";
        var opciones = CrearOpciones(nombreBase);

        try
        {
            await using (var inicial = new AppDbContext(opciones))
            {
                await inicial.Database.MigrateAsync();
                await new SeedFiscalService(inicial).SeedDefaultsAsync();

                var isc = await inicial.Impuestos.SingleAsync(x => x.Codigo == "ISC5");
                isc.Eliminado = true;
                isc.Activo = false;
                isc.FechaEliminacion = DateTime.UtcNow;
                await inicial.SaveChangesAsync();
            }

            await using (var reinicio = new AppDbContext(opciones))
            {
                await new SeedFiscalService(reinicio).SeedDefaultsAsync();

                var filas = await reinicio.Impuestos
                    .IgnoreQueryFilters()
                    .Where(x => x.Codigo == "ISC5")
                    .ToListAsync();

                var persistido = Assert.Single(filas);
                Assert.True(persistido.Eliminado);
                Assert.False(persistido.Activo);
                Assert.NotNull(persistido.FechaEliminacion);
                Assert.Equal(2, await reinicio.Impuestos.IgnoreQueryFilters().CountAsync());
            }
        }
        finally
        {
            await using var limpieza = new AppDbContext(opciones);
            await limpieza.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task Reinicio_PreservaTasaEstadoEIncluidoEnPrecioAdministrados()
    {
        var nombreBase = $"test_fiscal_admin_{Guid.NewGuid():N}";
        var opciones = CrearOpciones(nombreBase);

        try
        {
            await using (var inicial = new AppDbContext(opciones))
            {
                await inicial.Database.MigrateAsync();
                await new SeedFiscalService(inicial).SeedDefaultsAsync();

                var isv = await inicial.Impuestos.SingleAsync(x => x.Codigo == "ISV15");
                isv.Nombre = "ISV administrado";
                isv.Tasa = 12.3456m;
                isv.Activo = false;
                isv.IncluidoEnPrecio = false;
                isv.FechaActualizacion = DateTime.UtcNow;
                await inicial.SaveChangesAsync();
            }

            await using (var reinicio = new AppDbContext(opciones))
            {
                await new SeedFiscalService(reinicio).SeedDefaultsAsync();

                var isv = await reinicio.Impuestos.SingleAsync(x => x.Codigo == "ISV15");
                Assert.Equal("ISV administrado", isv.Nombre);
                Assert.Equal(12.3456m, isv.Tasa);
                Assert.False(isv.Activo);
                Assert.False(isv.IncluidoEnPrecio);
                Assert.Equal(2, await reinicio.Impuestos.IgnoreQueryFilters().CountAsync());
            }
        }
        finally
        {
            await using var limpieza = new AppDbContext(opciones);
            await limpieza.Database.EnsureDeletedAsync();
        }
    }
}
