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
/// P2-004.D: cubre la lectura privada del repositorio de proveedores con
/// dos empresas. No acredita autenticacion HTTP/JWT, SQL MySQL ni RBAC integral.
/// </summary>
public sealed class P2004DProveedorRepositorioTenantTests
{
    [Fact]
    public async Task Repositorio_no_expone_proveedor_ajeno_con_header_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004D Empresa A");
        var empresaB = new Empresa("P2004D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor { Nombre = "P2004D proveedor A", EmpresaId = empresaA.Id };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor { Nombre = "P2004D proveedor B", EmpresaId = empresaB.Id };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new ProveedorRepository(db);

        // El header de B no permite acceder a un registro de B desde A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(proveedorA.Id, Assert.Single(await repository.GetAllAsync()).Id);
        Assert.Null(await repository.GetByIdAsync(proveedorB.Id));

        // Al cambiar el tenant verificado, solo las filas de B son visibles.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(proveedorB.Id, Assert.Single(await repository.GetAllAsync()).Id);
        Assert.Null(await repository.GetByIdAsync(proveedorA.Id));
    }
}
