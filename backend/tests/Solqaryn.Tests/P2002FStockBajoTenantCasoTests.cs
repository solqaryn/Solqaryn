using System;
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
/// P2-002.F: escenario medible de productos por EmpresaId para stock bajo.
/// Fixture A/B sin variantes: ambos productos califican como stock bajo,
/// pero la consulta debe devolver solo el tenant verificado, nunca el header.
/// EF InMemory no acredita HTTP/JWT, MySQL ni un resultado CI por anticipado.
/// </summary>
public sealed class P2002FStockBajoTenantCasoTests
{
    [Fact]
    public async Task Stock_bajo_con_nombre_compartido_no_lee_productos_de_header_ajeno()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002F Empresa A");
        var empresaB = new Empresa("P2002F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombre = "P2002F producto sin variantes";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = nombre, EmpresaId = empresaA.Id, Activo = true };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = nombre, EmpresaId = empresaB.Id, Activo = true };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repo = new ProductoRepository(db);

        // Fixture: ambos productos carecen de variantes y califican como stock bajo.
        // Cabecera B no puede ampliar el tenant verificado A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var stockA = await repo.GetStockBajoAsync();
        Assert.Equal(productoA.Id, Assert.Single(stockA).Id);

        // Reutilizar DbContext verifica que el filtro se recalcula para B.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var stockB = await repo.GetStockBajoAsync();
        Assert.Equal(productoB.Id, Assert.Single(stockB).Id);
    }
}
