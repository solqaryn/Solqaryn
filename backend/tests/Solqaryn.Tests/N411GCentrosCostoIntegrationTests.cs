using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N411GCentrosCostoIntegrationTests
{
    [Fact]
    public async Task Buscar_AplicaFiltroOrdenYPaginacionYExcluyeEliminados()
    {
        await using var context = CreateContext();
        context.Set<CentroCosto>().AddRange(
            Centro("ADM-002", "Administracion 2"),
            Centro("ADM-001", "Administracion 1"),
            Centro("OPS-001", "Operaciones"),
            new CentroCosto
            {
                Codigo = "ADM-000",
                Nombre = "Eliminado",
                Tipo = TipoCentroCosto.Departamento,
                Activo = false,
                Eliminado = true
            });
        await context.SaveChangesAsync();

        var repository = new CentroCostoRepository(context);

        var (items, total) = await repository.BuscarAsync("ADM", null, null, true, 1, 1);

        Assert.Equal(2, total);
        var item = Assert.Single(items);
        Assert.Equal("ADM-001", item.Codigo);

        var (secondPage, secondTotal) = await repository.BuscarAsync("ADM", null, null, true, 2, 1);
        Assert.Equal(2, secondTotal);
        Assert.Equal("ADM-002", Assert.Single(secondPage).Codigo);
    }

    [Fact]
    public async Task GetActivosYExisteCodigo_RespetanEstadoYNormalizacion()
    {
        await using var context = CreateContext();
        context.Set<CentroCosto>().AddRange(
            Centro("ADM-001", "Administracion"),
            new CentroCosto
            {
                Codigo = "OPS-001",
                Nombre = "Operaciones inactivo",
                Tipo = TipoCentroCosto.Proyecto,
                Activo = false
            });
        await context.SaveChangesAsync();

        var repository = new CentroCostoRepository(context);

        var activos = await repository.GetActivosAsync();

        Assert.Equal("ADM-001", Assert.Single(activos).Codigo);
        Assert.True(await repository.ExisteCodigoAsync(" adm-001 "));
        Assert.False(await repository.ExisteCodigoAsync("missing"));
    }

    private static CentroCosto Centro(string codigo, string nombre) => new()
    {
        Codigo = codigo,
        Nombre = nombre,
        Tipo = TipoCentroCosto.Departamento,
        Activo = true,
        Eliminado = false
    };

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n411-g-centrocosto-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
