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
/// P2-014.C: dos solicitudes de empresas distintas consultan al mismo tiempo
/// la misma base EF InMemory, con HttpContext y DbContext independientes.
/// Verifica aislamiento del filtro, no certifica JWT/HTTP real ni MySQL.
/// </summary>
public sealed class P2014CConcurrenciaTenantTests
{
    [Fact]
    public async Task Dos_contextos_concurrentes_no_comparten_tenant_de_otra_solicitud()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2014c-" + Guid.NewGuid().ToString("N"))
            .Options;
        var setupHttp = new DefaultHttpContext();
        Empresa empresaA;
        Empresa empresaB;
        Venta ventaA;
        Venta ventaB;

        await using (var setup = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = setupHttp }))
        {
            empresaA = new Empresa("P2014C Concurrente A");
            empresaB = new Empresa("P2014C Concurrente B");
            setup.Set<Empresa>().AddRange(empresaA, empresaB);
            await setup.SaveChangesAsync();

            setupHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
            ventaA = new Venta { EmpresaId = empresaA.Id, NumeroVenta = "P2014C-A" };
            setup.Ventas.Add(ventaA);
            await setup.SaveChangesAsync();

            setupHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
            ventaB = new Venta { EmpresaId = empresaB.Id, NumeroVenta = "P2014C-B" };
            setup.Ventas.Add(ventaB);
            await setup.SaveChangesAsync();
        }

        var requestA = new DefaultHttpContext();
        var requestB = new DefaultHttpContext();
        // Cada cabecera intenta suplantar al tenant de la otra solicitud.
        requestA.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        requestB.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        requestA.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        requestB.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;

        await using var dbA = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = requestA });
        await using var dbB = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = requestB });

        var consultaA = dbA.Ventas.AsNoTracking().Select(v => v.Id).ToArrayAsync();
        var consultaB = dbB.Ventas.AsNoTracking().Select(v => v.Id).ToArrayAsync();
        await Task.WhenAll(consultaA, consultaB);

        Assert.Equal(new[] { ventaA.Id }, consultaA.Result);
        Assert.Equal(new[] { ventaB.Id }, consultaB.Result);
    }
}
