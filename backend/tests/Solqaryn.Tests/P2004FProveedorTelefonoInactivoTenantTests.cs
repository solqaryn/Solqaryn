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
/// P2-004.F: fixture causal de proveedores A/B con telefonos equivalentes
/// despues de normalizacion y un proveedor B inactivo. La marca verificada
/// del servidor gobierna la consulta; X-Empresa-Id no concede autoridad.
/// EF InMemory no sustituye HTTP/JWT ni MySQL reales.
/// </summary>
public sealed class P2004FProveedorTelefonoInactivoTenantTests
{
    [Fact]
    public async Task Telefono_normalizado_no_cruza_empresas_y_no_resucita_proveedor_inactivo()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004F Empresa A");
        var empresaB = new Empresa("P2004F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor
        {
            Nombre = "P2004F activo A",
            EmpresaId = empresaA.Id,
            Telefono = "9711-2244",
            Activo = true
        };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor
        {
            Nombre = "P2004F activo B",
            EmpresaId = empresaB.Id,
            Telefono = "9711 2244",
            Activo = true
        };
        var proveedorInactivoB = new Proveedor
        {
            Nombre = "P2004F inactivo B",
            EmpresaId = empresaB.Id,
            Telefono = "7777-9900",
            Activo = false
        };
        db.Proveedores.AddRange(proveedorB, proveedorInactivoB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.NotEqual(proveedorA.Id, proveedorB.Id);
        var repository = new ProveedorRepository(db);

        // La cabecera intenta B; la marca del servidor solo permite A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(proveedorA.Id, (await repository.BuscarCoincidenciaActivaAsync(
            null, null, "9711 2244", null))?.Id);
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, null, "7777-9900", null));
        Assert.Equal(new[] { proveedorA.Id },
            (await repository.BuscarActivosAsync("P2004F", 10))
            .Select(p => p.Id).ToArray());

        // Reutilizar el contexto con B autorizado no filtra con el tenant A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(proveedorB.Id, (await repository.BuscarCoincidenciaActivaAsync(
            null, null, "9711-2244", null))?.Id);
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, null, "77779900", null));
        Assert.Equal(new[] { proveedorB.Id },
            (await repository.BuscarActivosAsync("P2004F", 10))
            .Select(p => p.Id).ToArray());
    }
}
