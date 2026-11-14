using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.C: delimita la lectura EF de clientes entre dos empresas
/// con tenant comercial verificado. El control HTTP sin binding es otro gate.
/// </summary>
public sealed class P2003CClientesEmpresaIdTests
{
    [Fact]
    public async Task Lectura_de_clientes_respeta_empresa_verificada_y_no_una_cabecera_inyectada()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003C A");
        var empresaB = new Empresa("P2003C B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clienteA = new Cliente { Nombre = "Cliente A", EmpresaId = empresaA.Id };
        db.Clientes.Add(clienteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clienteB = new Cliente { Nombre = "Cliente B", EmpresaId = empresaB.Id };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        // El header controlado por el cliente no puede suplantar la marca RBAC.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clientesA = await db.Clientes.AsNoTracking().Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { clienteA.Id }, clientesA);
        Assert.DoesNotContain(clienteB.Id, clientesA);

        // El mismo DbContext no debe congelar ni contaminar el tenant previo.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clientesB = await db.Clientes.AsNoTracking().Select(c => c.Id).ToArrayAsync();
        Assert.Equal(new[] { clienteB.Id }, clientesB);
        Assert.DoesNotContain(clienteA.Id, clientesB);
    }
}
