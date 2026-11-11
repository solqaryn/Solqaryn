using System;
using System.Linq;
using System.Threading;
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
/// P2-004.G: negative purchase-to-supplier ownership regression.
/// The server-verified tenant, not X-Empresa-Id, controls writes; a purchase
/// for A must not reference B's supplier even through a persisted foreign key.
/// EF InMemory does not certify MySQL physical FKs, JWT or HTTP authorization.
/// </summary>
public sealed class P2004GProveedorCompraCrossTenantNegativeTests
{
    [Fact]
    public async Task Compra_con_proveedor_ajeno_se_rechaza_antes_de_persistir_y_no_contamina_el_tenant()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004G Empresa A");
        var empresaB = new Empresa("P2004G Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor
        {
            Nombre = "P2004G Proveedor A",
            EmpresaId = empresaA.Id,
            Activo = true
        };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor
        {
            Nombre = "P2004G Proveedor B",
            EmpresaId = empresaB.Id,
            Activo = true
        };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // A is authorized, while a forged browser header claims B.
        // The persisted FK points at a supplier owned by B.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaA.Id,
            ProveedorId = proveedorB.Id,
            NumeroCompra = "P2004G-A-CROSS",
            ProveedorNombre = proveedorB.Nombre
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CommercialCrossTenantWriteGuard.ValidateAsync(
                db, CancellationToken.None, empresaA.Id));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Compras.AsNoTracking().ToArrayAsync());

        // Genuine A-to-A purchase remains possible after the rejected write.
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaA.Id,
            ProveedorId = proveedorA.Id,
            NumeroCompra = "P2004G-A-VALID",
            ProveedorNombre = proveedorA.Nombre
        });
        await db.SaveChangesAsync();
        Assert.Equal("P2004G-A-VALID",
            (await db.Compras.AsNoTracking().SingleAsync()).NumeroCompra);

        // Reverse the verified scope: B cannot bind a purchase to A's FK.
        db.ChangeTracker.Clear();
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaB.Id,
            ProveedorId = proveedorA.Id,
            NumeroCompra = "P2004G-B-CROSS",
            ProveedorNombre = proveedorA.Nombre
        });
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CommercialCrossTenantWriteGuard.ValidateAsync(
                db, CancellationToken.None, empresaB.Id));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Compras.AsNoTracking().ToArrayAsync());
    }
}
