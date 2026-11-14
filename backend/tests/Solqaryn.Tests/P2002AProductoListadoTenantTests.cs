using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Common;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-002.A: cierre atomico de regresion de listado y busqueda de productos
/// por EmpresaId verificado. Complementa P2-002.C/D/E sin sustituir sus pruebas.
/// Solo EF InMemory; no afirma aislamiento HTTP/JWT ni consultas SQL MySQL reales.
/// </summary>
public sealed class P2002AProductoListadoTenantTests
{
    [Fact]
    public async Task Listado_paginado_y_recientes_no_mezclan_productos_homonimos_por_header()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002A empresa A");
        var empresaB = new Empresa("P2002A empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombreCompartido = "P2002A producto homonimo";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = nombreCompartido, EmpresaId = empresaA.Id, Activo = true };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = nombreCompartido, EmpresaId = empresaB.Id, Activo = true };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new ProductoRepository(db);
        var request = new ProductoPagedRequest { Search = "P2002A", Page = 1, PageSize = 10 };

        // La cabecera no confiable apunta a B, pero la marca verificada pertenece a A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var (itemsA, totalA) = await repo.GetPagedAsync(request);
        Assert.Equal(1, totalA);
        Assert.Equal(productoA.Id, Assert.Single(itemsA).Id);
        Assert.DoesNotContain(itemsA, p => p.Id == productoB.Id);
        Assert.Equal(productoA.Id, Assert.Single(await repo.GetUltimosAgregadosAsync(10)).Id);

        // Reutilizar exactamente el mismo DbContext detecta un filtro tenant cacheado.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var (itemsB, totalB) = await repo.GetPagedAsync(request);
        Assert.Equal(1, totalB);
        Assert.Equal(productoB.Id, Assert.Single(itemsB).Id);
        Assert.DoesNotContain(itemsB, p => p.Id == productoA.Id);
        Assert.Equal(productoB.Id, Assert.Single(await repo.GetUltimosAgregadosAsync(10)).Id);
    }
}
