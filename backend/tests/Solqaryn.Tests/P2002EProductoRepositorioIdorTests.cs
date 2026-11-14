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
/// P2-002.E: caso negativo IDOR sobre el repositorio de productos. Cubre
/// lectura por ID y conteo en EF InMemory, no JWT/HTTP ni MySQL productivo.
/// </summary>
public sealed class P2002EProductoRepositorioIdorTests
{
    [Fact]
    public async Task Repo_producto_por_id_rechaza_tenant_ajeno_aunque_el_header_apunte_a_el()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002E Empresa A");
        var empresaB = new Empresa("P2002E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombre = "P2002E producto de nombre compartido";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = nombre, EmpresaId = empresaA.Id };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = nombre, EmpresaId = empresaB.Id };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        Assert.NotEqual(productoA.Id, productoB.Id);
        db.ChangeTracker.Clear();

        var repo = new ProductoRepository(db);

        // El ID conocido del otro tenant no es permiso de lectura (IDOR).
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(productoA.Id, (await repo.GetByIdAsync(productoA.Id))?.Id);
        Assert.Null(await repo.GetByIdAsync(productoB.Id));
        Assert.Equal(1, await repo.GetTotalProductosAsync());

        // Un mismo DbContext debe revaluar el filtro con el tenant verificado.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(productoB.Id, (await repo.GetByIdAsync(productoB.Id))?.Id);
        Assert.Null(await repo.GetByIdAsync(productoA.Id));
        Assert.Equal(1, await repo.GetTotalProductosAsync());

        // No interpretar EF interno sin marca como autorización de una ruta HTTP.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(db.AuthorizedReadEmpresaId);
        Assert.Equal(2, await repo.GetTotalProductosAsync());
    }
}
