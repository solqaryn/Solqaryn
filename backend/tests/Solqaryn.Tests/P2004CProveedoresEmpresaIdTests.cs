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
/// P2-004.C: delimita el aislamiento de lecturas EF de proveedores por
/// EmpresaId verificado. No acredita por sí solo JWT, RBAC HTTP ni MySQL.
/// </summary>
public sealed class P2004CProveedoresEmpresaIdTests
{
    [Fact]
    public async Task Proveedores_respeta_tenant_verificado_aunque_header_intente_otro_y_cambie_contexto()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004C Empresa A");
        var empresaB = new Empresa("P2004C Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor { Nombre = "Proveedor A", EmpresaId = empresaA.Id };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor { Nombre = "Proveedor B", EmpresaId = empresaB.Id };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();

        // La cabecera manipulable NO reemplaza la marca RBAC del servidor.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Proveedores.AsNoTracking()
            .Select(p => p.Id).ToArrayAsync();
        Assert.Equal(new[] { proveedorA.Id }, visiblesA);
        Assert.DoesNotContain(proveedorB.Id, visiblesA);

        // Reusar el DbContext no debe congelar la selección anterior.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Proveedores.AsNoTracking()
            .Select(p => p.Id).ToArrayAsync();
        Assert.Equal(new[] { proveedorB.Id }, visiblesB);
        Assert.DoesNotContain(proveedorA.Id, visiblesB);
    }
}
