using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-005.G: las compras con EmpresaId de un tercero no pueden escribirse
/// bajo una marca comercial verificada de otro tenant, aunque se falsifique
/// X-Empresa-Id. Regresion aislada EF InMemory; no acredita HTTP/JWT/MySQL.
/// </summary>
public sealed class P2005GComprasOwnerFalsificadoTests
{
    [Fact]
    public async Task Compra_con_empresa_ajena_es_rechazada_en_A_y_B_sin_persistencia_cruzada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005G Empresa A");
        var empresaB = new Empresa("P2005G Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        // La marca autenticada A prevalece sobre la cabecera B. No se puede
        // crear una compra cuya propiedad explicita corresponde a B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005G-FALSA-B",
            ProveedorNombre = "Proveedor B"
        });
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.False(await db.Compras.IgnoreQueryFilters()
            .AnyAsync(c => c.NumeroCompra == "P2005G-FALSA-B"));

        // Control positivo: A puede persistir su propia compra incluso con
        // la cabecera de B, sin reasignar el propietario.
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = "P2005G-REAL-A",
            ProveedorNombre = "Proveedor A"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // La misma prueba en sentido inverso evita sesgo hacia un tenant.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        db.Compras.Add(new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = "P2005G-FALSA-A",
            ProveedorNombre = "Proveedor A"
        });
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        Assert.False(await db.Compras.IgnoreQueryFilters()
            .AnyAsync(c => c.NumeroCompra == "P2005G-FALSA-A"));

        db.Compras.Add(new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005G-REAL-B",
            ProveedorNombre = "Proveedor B"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var persistidas = await db.Compras.IgnoreQueryFilters().AsNoTracking()
            .OrderBy(c => c.NumeroCompra).ToArrayAsync();
        Assert.Equal(2, persistidas.Length);
        Assert.Equal(empresaA.Id,
            Assert.Single(persistidas, c => c.NumeroCompra == "P2005G-REAL-A").EmpresaId);
        Assert.Equal(empresaB.Id,
            Assert.Single(persistidas, c => c.NumeroCompra == "P2005G-REAL-B").EmpresaId);
    }
}
