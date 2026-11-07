using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-005.E: una cabecera controlada por el cliente nunca reemplaza el tenant
/// comercial verificado para lecturas de compras. EF InMemory, no HTTP/MySQL.
/// </summary>
public sealed class P2005EComprasSinMarcaTenantTests
{
    [Fact]
    public async Task Sin_tenant_verificado_no_se_ven_compras_ni_con_header_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005E Empresa A");
        var empresaB = new Empresa("P2005E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var compraA = new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = "P2005E-A",
            ProveedorNombre = "Proveedor A"
        };
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var compraB = new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005E-B",
            ProveedorNombre = "Proveedor B"
        };
        db.Compras.Add(compraB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Compras.IgnoreQueryFilters().CountAsync());

        // La cabecera B no establece una autorizacion cuando falta la marca servidor.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Null(db.AuthorizedCommercialEmpresaId);
        Assert.Empty(await db.Compras.AsNoTracking().ToArrayAsync());
        Assert.Null(await db.Compras.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == compraB.Id));

        // Control positivo: solo el tenant confirmado A puede leer su compra.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Compras.AsNoTracking()
            .Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { compraA.Id }, visiblesA);
        Assert.DoesNotContain(compraB.Id, visiblesA);

        // La misma instancia vuelve a denegar lecturas cuando se revoca la marca.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.Empty(await db.Compras.AsNoTracking().ToArrayAsync());
    }
}
