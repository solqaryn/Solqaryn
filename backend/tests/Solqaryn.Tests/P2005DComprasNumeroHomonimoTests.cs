using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-005.D: regression for purchase numbers shared across company boundaries.
/// EF InMemory only; this does not certify JWT/HTTP authorization or MySQL uniqueness.
/// </summary>
public sealed class P2005DComprasNumeroHomonimoTests
{
    [Fact]
    public async Task Numero_compra_homonimo_no_selecciona_empresa_por_cabecera_cliente()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005D Empresa A");
        var empresaB = new Empresa("P2005D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string numeroCompartido = "P2005D-COMPRA-REPETIDA";

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var compraA = new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = numeroCompartido,
            ProveedorNombre = "P2005D Proveedor A"
        };
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var compraB = new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = numeroCompartido,
            ProveedorNombre = "P2005D Proveedor B"
        };
        db.Compras.Add(compraB);
        await db.SaveChangesAsync();

        // The synthetic backing store contains both rows; the query boundary must scope them.
        Assert.Equal(2, await db.Compras.IgnoreQueryFilters()
            .CountAsync(c => c.NumeroCompra == numeroCompartido));

        // A forged B header must not redirect A's verified purchase lookup.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visibleA = await db.Compras.AsNoTracking()
            .SingleAsync(c => c.NumeroCompra == numeroCompartido);
        Assert.Equal(compraA.Id, visibleA.Id);
        Assert.Equal(empresaA.Id, visibleA.EmpresaId);
        Assert.Equal("P2005D Proveedor A", visibleA.ProveedorNombre);

        // Reuse the DbContext with a new server-verified tenant and the opposite forged header.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visibleB = await db.Compras.AsNoTracking()
            .SingleAsync(c => c.NumeroCompra == numeroCompartido);
        Assert.Equal(compraB.Id, visibleB.Id);
        Assert.Equal(empresaB.Id, visibleB.EmpresaId);
        Assert.Equal("P2005D Proveedor B", visibleB.ProveedorNombre);
    }
}
