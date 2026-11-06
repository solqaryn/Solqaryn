using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-002.C: una regresion concreta del aislamiento de productos por EmpresaId.
/// Solo usa datos efimeros InMemory; no cambia reglas de autorizacion.
/// </summary>
public sealed class P2002CProductoEmpresaIdTests
{
    [Fact]
    public async Task Cabecera_de_B_sin_tenant_verificado_no_expone_productos_de_B()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002C TEST A");
        var empresaB = new Empresa("P2002C TEST B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = "P2002C Producto A" };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();
        Assert.Equal(empresaA.Id, productoA.EmpresaId);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = "P2002C Producto B" };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        Assert.Equal(empresaB.Id, productoB.EmpresaId);

        // Ni la cabecera manipulada ni el cambio de tenant deben contaminar
        // el filtro EF de una misma instancia de DbContext.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { productoA.Id },
            await db.Productos.AsNoTracking().Select(p => p.Id).ToArrayAsync());

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(db.AuthorizedReadEmpresaId);
        Assert.Empty(await db.Productos.AsNoTracking().ToArrayAsync());

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { productoB.Id },
            await db.Productos.AsNoTracking().Select(p => p.Id).ToArrayAsync());
    }
}
