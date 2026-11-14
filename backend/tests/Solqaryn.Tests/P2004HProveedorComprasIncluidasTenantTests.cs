using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-004.H: positive supplier/purchase navigation regression for verified
/// company A/B. GetByIdConComprasAsync must include the authorized supplier's
/// own purchase without mixing the other company's navigation.
/// Covers EF InMemory only, not HTTP/JWT, production MySQL or full RBAC.
/// </summary>
public sealed class P2004HProveedorComprasIncluidasTenantTests
{
    [Fact]
    public async Task Proveedor_y_compra_propia_se_recuperan_en_A_y_B_sin_contaminar_navegacion()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004h-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004H Empresa A");
        var empresaB = new Empresa("P2004H Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor
        {
            Nombre = "P2004H Proveedor A",
            EmpresaId = empresaA.Id,
            Activo = true
        };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaA.Id,
            ProveedorId = proveedorA.Id,
            NumeroCompra = "P2004H-A-POS",
            ProveedorNombre = proveedorA.Nombre
        });
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor
        {
            Nombre = "P2004H Proveedor B",
            EmpresaId = empresaB.Id,
            Activo = true
        };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaB.Id,
            ProveedorId = proveedorB.Id,
            NumeroCompra = "P2004H-B-POS",
            ProveedorNombre = proveedorB.Nombre
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new ProveedorRepository(db);

        // Header untrusted B must not change the server-verified A identity.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var resultA = await repository.GetByIdConComprasAsync(proveedorA.Id);
        Assert.NotNull(resultA);
        Assert.Equal(proveedorA.Id, resultA.Id);
        Assert.Equal("P2004H-A-POS", Assert.Single(resultA.Compras).NumeroCompra);
        Assert.Null(await repository.GetByIdConComprasAsync(proveedorB.Id));

        // Re-evaluating the verified scope with B must return B's linked order.
        db.ChangeTracker.Clear();
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var resultB = await repository.GetByIdConComprasAsync(proveedorB.Id);
        Assert.NotNull(resultB);
        Assert.Equal(proveedorB.Id, resultB.Id);
        Assert.Equal("P2004H-B-POS", Assert.Single(resultB.Compras).NumeroCompra);
        Assert.Null(await repository.GetByIdConComprasAsync(proveedorA.Id));

        // No query is permitted to silently combine both purchase sets.
        Assert.NotEqual(resultA.Compras.Single().NumeroCompra,
                        resultB.Compras.Single().NumeroCompra);
    }
}
