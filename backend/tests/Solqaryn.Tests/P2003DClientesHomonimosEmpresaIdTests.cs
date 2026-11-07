using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.D: dos clientes homonimos con identificador duplicado entre
/// empresas no deben mezclarse bajo la marca tenant verificada.
/// El filtro EF sin marca es global y NO autoriza una ruta HTTP.
/// </summary>
public sealed class P2003DClientesHomonimosEmpresaIdTests
{
    [Fact]
    public async Task Identidad_repetida_entre_tenants_respeta_la_marca_RBAC_no_la_cabecera()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003D Empresa A");
        var empresaB = new Empresa("P2003D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        const string nombre = "P2003D Cliente homonimo";
        const string documento = "P2003D-IDENTIDAD-COMPARTIDA";

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clienteA = new Cliente
        {
            EmpresaId = empresaA.Id,
            Nombre = nombre,
            IdentidadORTN = documento
        };
        db.Clientes.Add(clienteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clienteB = new Cliente
        {
            EmpresaId = empresaB.Id,
            Nombre = nombre,
            IdentidadORTN = documento
        };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        // Una cabecera controlada por el solicitante no es autoridad RBAC.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var visiblesA = await db.Clientes.AsNoTracking()
            .Where(c => c.Nombre == nombre && c.IdentidadORTN == documento)
            .Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { clienteA.Id }, visiblesA);
        Assert.DoesNotContain(clienteB.Id, visiblesA);

        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var visiblesB = await db.Clientes.AsNoTracking()
            .Where(c => c.Nombre == nombre && c.IdentidadORTN == documento)
            .Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { clienteB.Id }, visiblesB);
        Assert.DoesNotContain(clienteA.Id, visiblesB);

        // EF sin marca conserva consulta global para flujos internos.
        // El acceso HTTP sin marca se rechaza mediante la cuarentena P0 aparte.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(db.AuthorizedReadEmpresaId);
        Assert.Equal(2, await db.Clientes.AsNoTracking()
            .CountAsync(c => c.Nombre == nombre && c.IdentidadORTN == documento));

        // La consulta de A posterior no arrastra el contexto global ni B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { clienteA.Id }, await db.Clientes.AsNoTracking()
            .Where(c => c.Nombre == nombre).Select(c => c.Id).ToArrayAsync());
    }
}
