using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class SeedFiscalServiceTests
{
    private static DbContextOptions<AppDbContext> CrearOpciones(string nombre) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nombre)
            .Options;

    [Fact]
    public async Task SeedDefaultsAsync_EsIdempotente_Y_CreaUnaSolaVezLosImpuestosBase()
    {
        var opciones = CrearOpciones($"fiscal-seed-{Guid.NewGuid():N}");

        await using var db = new AppDbContext(opciones);
        var seed = new SeedFiscalService(db);

        await seed.SeedDefaultsAsync();
        await seed.SeedDefaultsAsync();

        var impuestos = await db.Impuestos
            .Include(i => i.Operaciones)
            .OrderBy(i => i.Codigo)
            .ToListAsync();

        Assert.Equal(2, impuestos.Count);

        var isc = Assert.Single(impuestos.Where(i => i.Codigo == "ISC5"));
        Assert.Equal(5m, isc.Tasa);
        Assert.False(isc.Activo);
        Assert.True(isc.IncluidoEnPrecio);
        Assert.Equal(OperacionImpuesto.Compra, Assert.Single(isc.Operaciones).Operacion);

        var isv = Assert.Single(impuestos.Where(i => i.Codigo == "ISV15"));
        Assert.Equal(15m, isv.Tasa);
        Assert.True(isv.Activo);
        Assert.True(isv.IncluidoEnPrecio);
        Assert.Equal(OperacionImpuesto.Venta, Assert.Single(isv.Operaciones).Operacion);
    }

    [Fact]
    public async Task SeedDefaultsAsync_EnNuevoContexto_NoSobrescribeDecisionAdministrativa()
    {
        var opciones = CrearOpciones($"fiscal-restart-{Guid.NewGuid():N}");

        await using (var primerContexto = new AppDbContext(opciones))
        {
            await new SeedFiscalService(primerContexto).SeedDefaultsAsync();

            var isv = await primerContexto.Impuestos.SingleAsync(i => i.Codigo == "ISV15");
            isv.Nombre = "ISV administrado";
            isv.Tasa = 12.3456m;
            isv.Activo = false;
            isv.IncluidoEnPrecio = false;
            isv.FechaActualizacion = DateTime.UtcNow;
            await primerContexto.SaveChangesAsync();
        }

        await using (var segundoContexto = new AppDbContext(opciones))
        {
            await new SeedFiscalService(segundoContexto).SeedDefaultsAsync();

            var isv = await segundoContexto.Impuestos.SingleAsync(i => i.Codigo == "ISV15");
            Assert.Equal("ISV administrado", isv.Nombre);
            Assert.Equal(12.3456m, isv.Tasa);
            Assert.False(isv.Activo);
            Assert.False(isv.IncluidoEnPrecio);
            Assert.Equal(2, await segundoContexto.Impuestos.CountAsync());
        }
    }

    [Fact]
    public async Task SeedDefaultsAsync_NoReactivaNiRecreaRegistroEliminadoLogicamente()
    {
        var opciones = CrearOpciones($"fiscal-delete-{Guid.NewGuid():N}");

        await using (var primerContexto = new AppDbContext(opciones))
        {
            await new SeedFiscalService(primerContexto).SeedDefaultsAsync();
            var isc = await primerContexto.Impuestos.SingleAsync(i => i.Codigo == "ISC5");
            isc.Eliminado = true;
            isc.Activo = false;
            isc.FechaEliminacion = DateTime.UtcNow;
            await primerContexto.SaveChangesAsync();
        }

        await using var segundoContexto = new AppDbContext(opciones);
        await new SeedFiscalService(segundoContexto).SeedDefaultsAsync();

        var filas = await segundoContexto.Impuestos
            .IgnoreQueryFilters()
            .Where(i => i.Codigo == "ISC5")
            .ToListAsync();
        var persistido = Assert.Single(filas);
        Assert.True(persistido.Eliminado);
        Assert.False(persistido.Activo);
        Assert.NotNull(persistido.FechaEliminacion);
    }
}
