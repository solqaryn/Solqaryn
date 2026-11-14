using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110PoliticaCosteoInventarioRepositoryTests
{
    [Fact]
    public async Task Historial_respeta_empresa_orden_y_paginacion_normalizada()
    {
        await using var context = CrearContexto();
        var inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var anterior = PoliticaCosteoInventario.Crear(1, MetodoCosteoInventario.PromedioPonderado, inicio, "Inicial");
        anterior.Cerrar(inicio.AddDays(10));
        var vigente = PoliticaCosteoInventario.Crear(1, MetodoCosteoInventario.FIFO, inicio.AddDays(10), "Cambio FIFO");
        var otraEmpresa = PoliticaCosteoInventario.Crear(2, MetodoCosteoInventario.Estandar, inicio.AddDays(20), "Otra empresa");
        context.Set<PoliticaCosteoInventario>().AddRange(anterior, vigente, otraEmpresa);
        await context.SaveChangesAsync();

        var repository = new PoliticaCosteoInventarioRepository(context);
        var (items, total) = await repository.GetHistorialAsync(1, new PoliticaCosteoInventarioQueryDto
        {
            Page = 0,
            PageSize = 500
        });

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.Equal(MetodoCosteoInventario.FIFO, items[0].Metodo);
        Assert.Equal(MetodoCosteoInventario.PromedioPonderado, items[1].Metodo);
    }

    [Fact]
    public async Task Consulta_vigente_y_filtros_no_reinterpretan_historial_cerrado()
    {
        await using var context = CrearContexto();
        var inicio = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var anterior = PoliticaCosteoInventario.Crear(1, MetodoCosteoInventario.PromedioPonderado, inicio, "Inicial");
        anterior.Cerrar(inicio.AddDays(5));
        var vigente = PoliticaCosteoInventario.Crear(1, MetodoCosteoInventario.Estandar, inicio.AddDays(5), "Costo estándar");
        context.Set<PoliticaCosteoInventario>().AddRange(anterior, vigente);
        await context.SaveChangesAsync();

        var repository = new PoliticaCosteoInventarioRepository(context);
        var actual = await repository.GetVigenteAsync(1);
        var (cerradas, totalCerradas) = await repository.GetHistorialAsync(1, new PoliticaCosteoInventarioQueryDto
        {
            Vigente = false,
            Metodo = MetodoCosteoInventario.PromedioPonderado
        });

        Assert.NotNull(actual);
        Assert.Equal(MetodoCosteoInventario.Estandar, actual!.Metodo);
        Assert.Single(cerradas);
        Assert.Equal(1, totalCerradas);
        Assert.Equal(MetodoCosteoInventario.PromedioPonderado, cerradas[0].Metodo);
        Assert.False(cerradas[0].EstaVigente);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n110-costeo-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
