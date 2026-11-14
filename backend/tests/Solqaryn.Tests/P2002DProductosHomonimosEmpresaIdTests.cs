using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-002.D: products may share a visible name across tenants, but never a read scope.
/// The assertion covers the EF tenant filter, not HTTP JWT/RBAC or MySQL behavior.
/// </summary>
public sealed class P2002DProductosHomonimosEmpresaIdTests
{
    [Fact]
    public async Task Mismo_nombre_de_producto_no_permite_saltar_empresa_verificada_por_header()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002D empresa A");
        var empresaB = new Empresa("P2002D empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombreCompartido = "P2002D producto compartido";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = nombreCompartido };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = nombreCompartido };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        Assert.NotEqual(productoA.Id, productoB.Id);

        // The untrusted request header disagrees with the authenticated tenant.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Productos.AsNoTracking()
            .Where(p => p.Nombre == nombreCompartido)
            .Select(p => p.Id).ToArrayAsync();
        Assert.Equal(new[] { productoA.Id }, visiblesA);
        Assert.DoesNotContain(productoB.Id, visiblesA);

        // Reuse the same DbContext to detect a stale or cached tenant filter.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Productos.AsNoTracking()
            .Where(p => p.Nombre == nombreCompartido)
            .Select(p => p.Id).ToArrayAsync();
        Assert.Equal(new[] { productoB.Id }, visiblesB);
        Assert.DoesNotContain(productoA.Id, visiblesB);
    }
}
