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
/// P2-004.A: cierra la matriz de proveedores por EmpresaId para la deteccion
/// de coincidencias documentales/correo usada por Compras. Una cabecera
/// manipulable no sustituye el tenant autorizado por el backend.
/// EF InMemory no acredita JWT, middleware HTTP ni MySQL real.
/// </summary>
public sealed class P2004AProveedorCoincidenciaTenantTests
{
    [Fact]
    public async Task Coincidencia_de_proveedor_no_reusa_documento_ni_correo_de_empresa_ajena()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2004a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2004A Empresa A");
        var empresaB = new Empresa("P2004A Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombreCompartido = "P2004A Proveedor Homonimo";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var proveedorA = new Proveedor
        {
            Nombre = nombreCompartido,
            EmpresaId = empresaA.Id,
            Documento = "RTN-A-100",
            Correo = "a-p2004a@example.test",
            Activo = true
        };
        db.Proveedores.Add(proveedorA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var proveedorB = new Proveedor
        {
            Nombre = nombreCompartido,
            EmpresaId = empresaB.Id,
            Documento = "RTN-B-200",
            Correo = "b-p2004a@example.test",
            Activo = true
        };
        db.Proveedores.Add(proveedorB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new ProveedorRepository(db);

        // El navegador afirma B, pero RBAC autentico solo autoriza A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            "RTN-B-200", null, null, null));
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, "b-p2004a@example.test", null, null));
        Assert.False(await repository.ExisteDocumentoAsync("RTN-B-200"));
        Assert.Equal(proveedorA.Id, (await repository.BuscarCoincidenciaActivaAsync(
            "RTN-A-100", null, null, null))?.Id);
        Assert.Equal(new[] { proveedorA.Id },
            (await repository.BuscarActivosAsync("P2004A", 10)).Select(p => p.Id).ToArray());

        // Invertir la marca verificada recalcula las coincidencias sin
        // conceder acceso al documento ni al correo de A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            "RTN-A-100", null, null, null));
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, "a-p2004a@example.test", null, null));
        Assert.False(await repository.ExisteDocumentoAsync("RTN-A-100"));
        Assert.Equal(proveedorB.Id, (await repository.BuscarCoincidenciaActivaAsync(
            "RTN-B-200", null, null, null))?.Id);
        Assert.Equal(new[] { proveedorB.Id },
            (await repository.BuscarActivosAsync("P2004A", 10)).Select(p => p.Id).ToArray());
    }
}
