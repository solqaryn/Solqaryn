using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.E: direct lookup by Cliente.Id must not bypass the verified EmpresaId
/// filter. EF InMemory only; this does not certify HTTP/JWT or MySQL.
/// </summary>
public sealed class P2003EClientesIdorTenantTests
{
    [Fact]
    public async Task Id_ajeno_es_invisible_aun_con_X_Empresa_Id_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003E Empresa A");
        var empresaB = new Empresa("P2003E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clienteA = new Cliente
        {
            EmpresaId = empresaA.Id,
            Nombre = "P2003E Cliente A",
            IdentidadORTN = "P2003E-IDENT-A"
        };
        db.Clientes.Add(clienteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clienteB = new Cliente
        {
            EmpresaId = empresaB.Id,
            Nombre = "P2003E Cliente B",
            IdentidadORTN = "P2003E-IDENT-B"
        };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Clientes.IgnoreQueryFilters().CountAsync());

        // The client-controlled header claims B, but the server verified A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await db.Clientes.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clienteB.Id));
        Assert.Equal(clienteA.Id, (await db.Clientes.AsNoTracking()
            .SingleAsync(c => c.Id == clienteA.Id)).Id);

        // The same DbContext switches to server-verified B, not the forged A header.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Null(await db.Clientes.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clienteA.Id));
        Assert.Equal(clienteB.Id, (await db.Clientes.AsNoTracking()
            .SingleAsync(c => c.Id == clienteB.Id)).Id);

        // Returning to A does not leak a previously authorized B lookup.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Null(await db.Clientes.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == clienteB.Id));
    }
}
