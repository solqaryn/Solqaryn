using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

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
        Assert.Equal(f.OrdenA, primera.Items[0].Id);
        Assert.Equal(1, fuera.Total);
        Assert.Empty(fuera.Items);
    }

    [Fact]
    public async Task Header_de_B_no_reemplaza_scope_verificado_de_A()
    {
        await using var f = await CrearAsync();
        f.Http.Request.Headers["X-Empresa-Id"] = f.EmpresaB.ToString();
        var pagina = await f.Repo.GetPagedAsync(new OrdenCompraFiltroDto { Page = 1, PageSize = 100 });
        Assert.Equal(1, pagina.Total);
        Assert.Single(pagina.Items);
        Assert.Equal(f.OrdenA, pagina.Items[0].Id);
        Assert.DoesNotContain(pagina.Items, o => o.Id == f.OrdenB);
    }

    private static async Task<Fixture> CrearAsync()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("v5-m03-04-" + Guid.NewGuid().ToString("N")).Options;
        var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("V5 M03.04 A");
        var empresaB = new Empresa("V5 M03.04 B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ordenA = new OrdenCompra
        {
            EmpresaId = empresaA.Id, NumeroOrden = "M0304-A",
            ProveedorId = 1, ProveedorNombreSnapshot = "Proveedor A"
        };
        db.Set<OrdenCompra>().Add(ordenA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ordenB = new OrdenCompra
        {
            EmpresaId = empresaB.Id, NumeroOrden = "M0304-B",
            ProveedorId = 2, ProveedorNombreSnapshot = "Proveedor B"
        };
        db.Set<OrdenCompra>().Add(ordenB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        return new Fixture(db, http, new OrdenCompraRepository(db), empresaB.Id, ordenA.Id, ordenB.Id);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly AppDbContext _db;
        public Fixture(AppDbContext db, DefaultHttpContext http, OrdenCompraRepository repo,
            int empresaB, int ordenA, int ordenB)
        {
            _db = db; Http = http; Repo = repo;
            EmpresaB = empresaB; OrdenA = ordenA; OrdenB = ordenB;
        }
        public DefaultHttpContext Http { get; }
        public OrdenCompraRepository Repo { get; }
        public int EmpresaB { get; }
        public int OrdenA { get; }
        public int OrdenB { get; }
        public ValueTask DisposeAsync() => _db.DisposeAsync();
    }
}
