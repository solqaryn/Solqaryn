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
/// P2-002.G: rechazo dirigido de lectura por ID de producto entre empresas.
/// La marca VerifiedCommercialTenant, y no X-Empresa-Id, define el tenant de
/// lectura. Regresion EF InMemory; no certifica HTTP/JWT ni MySQL real.
/// </summary>
public sealed class P2002GProductoIdAjenoTenantTests
{
    [Fact]
    public async Task GetById_no_devuelve_producto_de_empresa_ajena_con_header_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2002g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2002G Empresa A");
        var empresaB = new Empresa("P2002G Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombre = "Producto de referencia compartida";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var productoA = new Producto { Nombre = nombre, EmpresaId = empresaA.Id, Activo = true };
        db.Productos.Add(productoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var productoB = new Producto { Nombre = nombre, EmpresaId = empresaB.Id, Activo = true };
        db.Productos.Add(productoB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new ProductoRepository(db);

        // La cabecera B falsificada no concede lectura del ID privado de B
        // cuando la autoridad comercial verificada corresponde a A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await repository.GetByIdAsync(productoB.Id));
        Assert.Equal(productoA.Id, (await repository.GetByIdAsync(productoA.Id))?.Id);

        // En la misma instancia EF, el filtro debe recalcularse para B.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Null(await repository.GetByIdAsync(productoA.Id));
        Assert.Equal(productoB.Id, (await repository.GetByIdAsync(productoB.Id))?.Id);
    }
}
