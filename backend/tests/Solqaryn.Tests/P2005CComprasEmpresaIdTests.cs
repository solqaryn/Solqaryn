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
/// P2-005.C: verifies the EF read boundary for draft purchases owned by EmpresaId.
/// This does not certify HTTP/JWT/RBAC authorization or a MySQL transaction.
/// </summary>
public sealed class P2005CComprasEmpresaIdTests
{
    [Fact]
    public async Task Compras_del_tenant_verificado_no_exponen_compras_ajenas_ni_por_header()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005C Empresa A");
        var empresaB = new Empresa("P2005C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var compraA = new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = "P2005C-A",
            ProveedorNombre = "Proveedor A"
        };
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var compraB = new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005C-B",
            ProveedorNombre = "Proveedor B"
        };
        db.Compras.Add(compraB);
        await db.SaveChangesAsync();

        // A client-controlled header cannot select company B over server-verified A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Compras.AsNoTracking()
            .Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { compraA.Id }, visiblesA);
        Assert.DoesNotContain(compraB.Id, visiblesA);

        // A new server-verified scope must apply even on the same DbContext.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Compras.AsNoTracking()
            .Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { compraB.Id }, visiblesB);
        Assert.DoesNotContain(compraA.Id, visiblesB);
    }
}
