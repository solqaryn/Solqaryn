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
/// P2-009.D: KPI agrupado por nombre homonimo entre empresas aisladas.
/// El fixture EF InMemory NO certifica el endpoint de dashboard, JWT ni SQL MySQL.
/// </summary>
public sealed class P2009DKpiClientesHomonimosTenantTests
{
    [Fact]
    public async Task Agrupacion_kpi_por_nombre_usa_tenant_verificado_y_no_cabecera_falsificada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2009d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2009D Empresa A");
        var empresaB = new Empresa("P2009D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombreHomonimo = "Cliente KPI Compartido";

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Clientes.AddRange(
            new Cliente { Nombre = nombreHomonimo, EmpresaId = empresaA.Id },
            new Cliente { Nombre = nombreHomonimo, EmpresaId = empresaA.Id },
            new Cliente { Nombre = "Solo A", EmpresaId = empresaA.Id });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Clientes.AddRange(
            new Cliente { Nombre = nombreHomonimo, EmpresaId = empresaB.Id },
            new Cliente { Nombre = "Solo B", EmpresaId = empresaB.Id });
        await db.SaveChangesAsync();

        Assert.Equal(5, await db.Clientes.IgnoreQueryFilters().CountAsync());

        // Una cabecera B controlada por cliente no contamina los KPI del tenant A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var gruposA = await db.Clientes.AsNoTracking()
            .GroupBy(c => c.Nombre)
            .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
            .ToListAsync();
        Assert.Equal(2, gruposA.Count);
        Assert.Equal(3, gruposA.Sum(g => g.Cantidad));
        Assert.Equal(2, Assert.Single(gruposA.Where(g => g.Nombre == nombreHomonimo)).Cantidad);
        Assert.Equal(1, Assert.Single(gruposA.Where(g => g.Nombre == "Solo A")).Cantidad);
        Assert.DoesNotContain(gruposA, g => g.Nombre == "Solo B");

        // En el mismo DbContext, cambiar la marca verificada da el KPI del tenant B.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var gruposB = await db.Clientes.AsNoTracking()
            .GroupBy(c => c.Nombre)
            .Select(g => new { Nombre = g.Key, Cantidad = g.Count() })
            .ToListAsync();
        Assert.Equal(2, gruposB.Count);
        Assert.Equal(2, gruposB.Sum(g => g.Cantidad));
        Assert.Equal(1, Assert.Single(gruposB.Where(g => g.Nombre == nombreHomonimo)).Cantidad);
        Assert.Equal(1, Assert.Single(gruposB.Where(g => g.Nombre == "Solo B")).Cantidad);
        Assert.DoesNotContain(gruposB, g => g.Nombre == "Solo A");
    }
}
