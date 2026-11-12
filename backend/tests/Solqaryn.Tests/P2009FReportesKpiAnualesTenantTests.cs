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
/// P2-009.F: caso medible para reportes/KPI de ventas agrupados por anio y mes.
/// Los mismos meses y folios entre empresas A/B no deben sumar importes ajenos.
/// Usa EF InMemory y un tenant comercial verificado server-side; NO certifica
/// DashboardService, HTTP/JWT/RBAC, SQL MySQL ni la ruta sin tenant verificado.
/// </summary>
public sealed class P2009FReportesKpiAnualesTenantTests
{
    [Fact]
    public async Task Kpi_mensual_distingue_anio_y_empresa_aun_con_cabecera_suplantada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2009f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2009F Empresa A");
        var empresaB = new Empresa("P2009F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        // El mismo mes (enero) en distintos anios no debe agruparse junto.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Ventas.AddRange(
            new Venta { EmpresaId = empresaA.Id, NumeroVenta = "KPI-HIST", Fecha = new DateTime(2025, 1, 5), Total = 50m },
            new Venta { EmpresaId = empresaA.Id, NumeroVenta = "KPI-A26", Fecha = new DateTime(2026, 1, 5), Total = 150m },
            new Venta { EmpresaId = empresaA.Id, NumeroVenta = "KPI-A-FEB", Fecha = new DateTime(2026, 2, 5), Total = 200m });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Ventas.AddRange(
            new Venta { EmpresaId = empresaB.Id, NumeroVenta = "KPI-HIST", Fecha = new DateTime(2026, 1, 5), Total = 900m },
            new Venta { EmpresaId = empresaB.Id, NumeroVenta = "KPI-B-FEB", Fecha = new DateTime(2026, 2, 5), Total = 700m });
        await db.SaveChangesAsync();

        Assert.Equal(5, await db.Ventas.IgnoreQueryFilters().CountAsync());

        // La cabecera B es controlable por el cliente y NO define el filtro.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var kpiA = await db.Ventas.AsNoTracking()
            .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .ToListAsync();
        Assert.Equal(3, kpiA.Count);
        Assert.Equal(50m, Assert.Single(kpiA.Where(x => x.Year == 2025 && x.Month == 1)).Total);
        Assert.Equal(150m, Assert.Single(kpiA.Where(x => x.Year == 2026 && x.Month == 1)).Total);
        Assert.Equal(200m, Assert.Single(kpiA.Where(x => x.Year == 2026 && x.Month == 2)).Total);
        Assert.Equal(3, kpiA.Sum(x => x.Cantidad));

        // Con la marca verificada B prevalece B aunque la cabecera diga A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var kpiB = await db.Ventas.AsNoTracking()
            .GroupBy(v => new { v.Fecha.Year, v.Fecha.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count(), Total = g.Sum(v => v.Total) })
            .ToListAsync();
        Assert.Equal(2, kpiB.Count);
        Assert.Equal(900m, Assert.Single(kpiB.Where(x => x.Year == 2026 && x.Month == 1)).Total);
        Assert.Equal(700m, Assert.Single(kpiB.Where(x => x.Year == 2026 && x.Month == 2)).Total);
        Assert.Equal(2, kpiB.Sum(x => x.Cantidad));
        Assert.DoesNotContain(kpiB, x => x.Year == 2025);
    }
}
