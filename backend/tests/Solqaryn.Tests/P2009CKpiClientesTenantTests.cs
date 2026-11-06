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
/// P2-009.C: un KPI simple (numero de clientes) debe contar solo filas
/// de la empresa verificada en el servidor. Este test de EF InMemory
/// NO certifica la API de dashboards, JWT/RBAC HTTP ni MySQL real.
/// </summary>
public sealed class P2009CKpiClientesTenantTests
{
    [Fact]
    public async Task Conteo_kpi_clientes_no_cruza_empresas_ni_obedece_header_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2009c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2009C A");
        var empresaB = new Empresa("P2009C B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Clientes.AddRange(
            new Cliente { Nombre = "KPI cliente A1", EmpresaId = empresaA.Id },
            new Cliente { Nombre = "KPI cliente A2", EmpresaId = empresaA.Id });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Clientes.Add(new Cliente { Nombre = "KPI cliente B1", EmpresaId = empresaB.Id });
        await db.SaveChangesAsync();

        // El header manipulable no debe decidir el alcance del KPI.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var conteoA = await db.Clientes.AsNoTracking().CountAsync();
        Assert.Equal(2, conteoA);

        // Mismo DbContext: el contexto verificado nuevo debe prevalecer.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var conteoB = await db.Clientes.AsNoTracking().CountAsync();
        Assert.Equal(1, conteoB);
    }
}
