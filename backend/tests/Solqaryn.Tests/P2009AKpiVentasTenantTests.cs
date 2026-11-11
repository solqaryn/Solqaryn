using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-009.A: agregado monetario EF de ventas en empresas A/B con folio repetido.
/// Complementa P2-009.C (conteo de clientes), .D (agrupacion de homonimos)
/// y .E (denegacion del servicio sin alcance autenticado).
/// EF InMemory NO acredita SQL MySQL, HTTP/JWT ni el endpoint de dashboard.
/// La guarda de SQL directo se valida ANTES de cualquier comando.
/// </summary>
public sealed class P2009AKpiVentasTenantTests
{
    [Fact]
    public async Task Kpi_ventas_no_mezcla_importes_A_B_ni_admite_SQL_sin_tenant()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2009a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2009A Empresa A");
        var empresaB = new Empresa("P2009A Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Ventas.AddRange(
            new Venta { NumeroVenta = "KPI-REPETIDO", EmpresaId = empresaA.Id, Total = 125m },
            new Venta { NumeroVenta = "KPI-A-2", EmpresaId = empresaA.Id, Total = 75m });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Ventas.Add(new Venta
        {
            NumeroVenta = "KPI-REPETIDO", EmpresaId = empresaB.Id, Total = 900m
        });
        await db.SaveChangesAsync();

        Assert.Equal(3, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // La cabecera B no puede sumar importe B al KPI de A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var resumenA = await db.Ventas.AsNoTracking()
            .GroupBy(v => v.EmpresaId)
            .Select(g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .SingleAsync();
        Assert.Equal(2, resumenA.Cantidad);
        Assert.Equal(200m, resumenA.Total);

        // Mismo DbContext, nuevo tenant verificado y cabecera A falsificada.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var resumenB = await db.Ventas.AsNoTracking()
            .GroupBy(v => v.EmpresaId)
            .Select(g => new { Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .SingleAsync();
        Assert.Equal(1, resumenB.Cantidad);
        Assert.Equal(900m, resumenB.Total);

        // En un entorno con varias empresas, SQL directo no se habilita
        // solo por X-Empresa-Id; exige la marca comercial autenticada.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => CommercialTenantSqlLockScope.RequireAsync(db));
    }
}
