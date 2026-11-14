using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-006.A: regresion transversal de estados de venta por tenant.
/// Complementa P2-006.C (borradores), D (folio) y E (IDOR).
/// EF InMemory no acredita HTTP/JWT, cobros, anulaciones transaccionales ni MySQL.
/// </summary>
public sealed class P2006AVentasEstadosResumenTenantTests
{
    [Fact]
    public async Task Estados_y_totales_confirmados_no_se_mezclan_entre_empresas_con_header_falso()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2006a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2006A Empresa A");
        var empresaB = new Empresa("P2006A Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var borradorA = new Venta
        {
            EmpresaId = empresaA.Id, NumeroVenta = "P2006A-A-1",
            Estado = EstadoDocumento.Borrador, Total = 11m
        };
        var confirmadaA = new Venta
        {
            EmpresaId = empresaA.Id, NumeroVenta = "P2006A-A-2",
            Estado = EstadoDocumento.Confirmada, Total = 23m
        };
        var anuladaA = new Venta
        {
            EmpresaId = empresaA.Id, NumeroVenta = "P2006A-A-3",
            Estado = EstadoDocumento.Anulada, Total = 47m
        };
        db.Ventas.AddRange(borradorA, confirmadaA, anuladaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var borradorB = new Venta
        {
            EmpresaId = empresaB.Id, NumeroVenta = "P2006A-B-1",
            Estado = EstadoDocumento.Borrador, Total = 13m
        };
        var confirmadaB = new Venta
        {
            EmpresaId = empresaB.Id, NumeroVenta = "P2006A-B-2",
            Estado = EstadoDocumento.Confirmada, Total = 79m
        };
        var anuladaB = new Venta
        {
            EmpresaId = empresaB.Id, NumeroVenta = "P2006A-B-3",
            Estado = EstadoDocumento.Anulada, Total = 101m
        };
        db.Ventas.AddRange(borradorB, confirmadaB, anuladaB);
        await db.SaveChangesAsync();

        Assert.Equal(6, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // El header B no debe ampliar el scope A verificado por el backend.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Ventas.AsNoTracking()
            .OrderBy(v => v.Id).Select(v => v.Id).ToArrayAsync();
        Assert.Equal(new[] { borradorA.Id, confirmadaA.Id, anuladaA.Id }, visiblesA);
        Assert.Equal(23m, await db.Ventas.AsNoTracking()
            .Where(v => v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => v.Total));
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == confirmadaB.Id));

        // Cambiar la marca verificada a B invierte el aislamiento, no el header A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Ventas.AsNoTracking()
            .OrderBy(v => v.Id).Select(v => v.Id).ToArrayAsync();
        Assert.Equal(new[] { borradorB.Id, confirmadaB.Id, anuladaB.Id }, visiblesB);
        Assert.Equal(79m, await db.Ventas.AsNoTracking()
            .Where(v => v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => v.Total));
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == confirmadaA.Id));

        // Reutilizar el mismo DbContext no puede conservar ventas del tenant B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await db.Ventas.AsNoTracking()
            .SingleOrDefaultAsync(v => v.Id == confirmadaB.Id));
    }
}
