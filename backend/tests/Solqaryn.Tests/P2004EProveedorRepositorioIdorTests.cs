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
/// P2-004.E: regresión contra IDOR y búsquedas de proveedores homónimos
/// entre empresas. Prueba EF InMemory, no certifica rutas HTTP/JWT ni MySQL.
/// </summary>
public sealed class P2004EProveedorRepositorioIdorTests
{
    [Fact]
    public async Task Repo_proveedores_no_expone_id_ajeno_ni_busqueda_aunque_la_cabecera_se_falsifique()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004E Empresa A");
        var empresaB = new Empresa("P2004E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombreCompartido = "P2004E Proveedor Homonimo";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor { Nombre = nombreCompartido, EmpresaId = empresaA.Id, Activo = true };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor { Nombre = nombreCompartido, EmpresaId = empresaB.Id, Activo = true };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();
        Assert.NotEqual(proveedorA.Id, proveedorB.Id);
        db.ChangeTracker.Clear();

        var repo = new ProveedorRepository(db);

        // Cabecera de B no puede sustituir la empresa A autorizada por servidor.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(proveedorA.Id, (await repo.GetByIdAsync(proveedorA.Id))?.Id);
        Assert.Null(await repo.GetByIdAsync(proveedorB.Id));
        Assert.Null(await repo.GetByIdConComprasAsync(proveedorB.Id));
        Assert.Equal(new[] { proveedorA.Id },
            (await repo.BuscarActivosAsync("P2004E", 10)).Select(p => p.Id).ToArray());

        // Cambiar al tenant B en el mismo contexto debe recalcular el filtro.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(proveedorB.Id, (await repo.GetByIdConComprasAsync(proveedorB.Id))?.Id);
        Assert.Null(await repo.GetByIdAsync(proveedorA.Id));
        Assert.Equal(new[] { proveedorB.Id },
            (await repo.BuscarActivosAsync("P2004E", 10)).Select(p => p.Id).ToArray());
    }
}
