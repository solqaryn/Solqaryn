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
/// P2-006.D: venta con folio homonimo en A/B no salta el filtro EF de tenant.
/// EF InMemory no acredita autorizacion HTTP/JWT/RBAC ni unicidad MySQL.
/// </summary>
public sealed class P2006DVentasNumeroHomonimoTests
{
    [Fact]
    public async Task Folio_de_venta_repetido_no_permite_lectura_entre_empresas_por_header()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2006D Empresa A");
        var empresaB = new Empresa("P2006D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string folioCompartido = "P2006D-FOLIO-COMPARTIDO";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { EmpresaId = empresaA.Id, NumeroVenta = folioCompartido };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { EmpresaId = empresaB.Id, NumeroVenta = folioCompartido };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();

        Assert.NotEqual(ventaA.Id, ventaB.Id);
        Assert.Equal(2, await db.Ventas.IgnoreQueryFilters()
            .CountAsync(v => v.NumeroVenta == folioCompartido));

        // La cabecera falsificada apunta a B, pero el tenant confiable sigue siendo A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visibleA = await db.Ventas.AsNoTracking()
            .SingleAsync(v => v.NumeroVenta == folioCompartido);
        Assert.Equal(ventaA.Id, visibleA.Id);
        Assert.Equal(empresaA.Id, visibleA.EmpresaId);

        // La misma instancia debe aplicar B, nunca la cabecera A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visibleB = await db.Ventas.AsNoTracking()
            .SingleAsync(v => v.NumeroVenta == folioCompartido);
        Assert.Equal(ventaB.Id, visibleB.Id);
        Assert.Equal(empresaB.Id, visibleB.EmpresaId);
    }
}
