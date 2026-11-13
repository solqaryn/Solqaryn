using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// OrdenCompra legacy no posee EmpresaId. Estos casos solo prueban paginacion
/// en persistencia; el aislamiento multiempresa se valida en gates separados.
/// </summary>
public sealed class V5C0001M0304OrdenCompraPaginationPersistenceTests
{
    [Fact]
    public async Task Offset_extremo_no_devuelve_primera_pagina_ni_desborda()
    {
        await using var f = await CrearAsync();
        var pagina = await f.Repo.GetPagedAsync(new OrdenCompraFiltroDto
        {
            Page = int.MaxValue, PageSize = 100
        });
        Assert.Equal(1, pagina.Total);
        Assert.Empty(pagina.Items);
    }

    [Fact]
    public async Task Paginacion_valida_y_fuera_de_rango_conserva_total()
    {
        await using var f = await CrearAsync();
        var primera = await f.Repo.GetPagedAsync(new OrdenCompraFiltroDto { Page = 1, PageSize = 1 });
        var fuera = await f.Repo.GetPagedAsync(new OrdenCompraFiltroDto { Page = 2, PageSize = 1 });
        Assert.Equal(1, primera.Total);
        Assert.Single(primera.Items);
        Assert.Equal(f.OrdenId, primera.Items[0].Id);
        Assert.Equal(1, fuera.Total);
        Assert.Empty(fuera.Items);
    }

    [Fact]
    public async Task Pagina_y_tamano_invalidos_se_normalizan_sin_ampliar_consulta()
    {
        await using var f = await CrearAsync();
        var pagina = await f.Repo.GetPagedAsync(new OrdenCompraFiltroDto
        {
            Page = 0, PageSize = -500
        });
        Assert.Equal(1, pagina.Total);
        Assert.Single(pagina.Items);
        Assert.Equal(f.OrdenId, pagina.Items[0].Id);
    }

    private static async Task<Fixture> CrearAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("v5-m03-04-" + Guid.NewGuid().ToString("N")).Options;
        var db = new AppDbContext(options);
        db.Set<Empresa>().Add(new Empresa("V5 M03.04 legacy monoempresa"));
        await db.SaveChangesAsync();

        var orden = new OrdenCompra
        {
            NumeroOrden = "M0304-OC", ProveedorId = 1,
            ProveedorNombreSnapshot = "Proveedor local"
        };
        db.Set<OrdenCompra>().Add(orden);
        await db.SaveChangesAsync();
        return new Fixture(db, new OrdenCompraRepository(db), orden.Id);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly AppDbContext _db;
        public Fixture(AppDbContext db, OrdenCompraRepository repo, int ordenId)
        { _db = db; Repo = repo; OrdenId = ordenId; }
        public OrdenCompraRepository Repo { get; }
        public int OrdenId { get; }
        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }
}
