using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-006.E: an untrusted Venta.Id or X-Empresa-Id must not bypass verified
/// tenant ownership. EF InMemory only; no HTTP/JWT/MySQL certification.
/// </summary>
public sealed class P2006EVentasIdorTenantTests
{
    [Fact]
    public async Task Venta_Id_ajena_es_invisible_aun_con_header_de_empresa_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2006E Empresa A");
        var empresaB = new Empresa("P2006E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { EmpresaId = empresaA.Id, NumeroVenta = "P2006E-A" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { EmpresaId = empresaB.Id, NumeroVenta = "P2006E-B" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();

        Assert.NotEqual(ventaA.Id, ventaB.Id);
        Assert.Equal(2, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // El cliente falsifica B; la marca autorizada por backend permanece A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == ventaB.Id));
        Assert.Equal(empresaA.Id, (await db.Ventas.AsNoTracking()
            .SingleAsync(v => v.Id == ventaA.Id)).EmpresaId);

        // El mismo DbContext cambia a B de forma verificada, sin aceptar cabecera A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == ventaA.Id));
        Assert.Equal(empresaB.Id, (await db.Ventas.AsNoTracking()
            .SingleAsync(v => v.Id == ventaB.Id)).EmpresaId);

        // Regresar a A no debe revelar la venta B leida anteriormente.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == ventaB.Id));
    }
}
