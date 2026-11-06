using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-006.C: prueba dirigida del filtro EF de ventas por EmpresaId verificado.
/// No certifica JWT/RBAC HTTP, jobs, IgnoreQueryFilters ni persistencia MySQL.
/// </summary>
public sealed class P2006CVentasEmpresaIdTests
{
    [Fact]
    public async Task Ventas_borrador_se_aislan_por_tenant_verificado_y_no_por_header()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2006C Empresa A");
        var empresaB = new Empresa("P2006C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { EmpresaId = empresaA.Id, NumeroVenta = "P2006C-A" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { EmpresaId = empresaB.Id, NumeroVenta = "P2006C-B" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // El header del cliente no puede cambiar el tenant validado por servidor.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { ventaA.Id },
            await db.Ventas.AsNoTracking().Select(v => v.Id).ToArrayAsync());

        // La misma instancia de DbContext debe leer solo el nuevo tenant validado.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { ventaB.Id },
            await db.Ventas.AsNoTracking().Select(v => v.Id).ToArrayAsync());
    }
}
