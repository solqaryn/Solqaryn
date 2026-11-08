using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.A: matrix of existing customer list, active search and detail reads.
/// The verified server-side EmpresaId, not X-Empresa-Id, is the authority.
/// EF InMemory coverage only; HTTP/JWT, MySQL, mutations and imports are separate gates.
/// </summary>
public sealed class P2003AClientesMatrizEndpointTests
{
    [Fact]
    public async Task Listado_busqueda_y_detalle_son_aislados_por_tenant_verificado_A_B_A()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003A Empresa A");
        var empresaB = new Empresa("P2003A Empresa B");
        var tipo = new TipoCliente
        {
            Codigo = "P2003A",
            Nombre = "P2003A Tipo",
            NombreNormalizado = "P2003A TIPO",
            EsSistema = true
        };
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        db.Set<TipoCliente>().Add(tipo);
        await db.SaveChangesAsync();

        const string nombreCompartido = "P2003A Cliente gemelo";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clienteA = new Cliente
        {
            EmpresaId = empresaA.Id,
            TipoClienteId = tipo.Id,
            Nombre = nombreCompartido,
            IdentidadORTN = "P2003A-DOC-A",
            Activo = true
        };
        db.Clientes.Add(clienteA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clienteB = new Cliente
        {
            EmpresaId = empresaB.Id,
            TipoClienteId = tipo.Id,
            Nombre = nombreCompartido,
            IdentidadORTN = "P2003A-DOC-B",
            Activo = true
        };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Clientes.IgnoreQueryFilters().CountAsync());
        // Match the tenant read tests: remove tracked seed entities before switching
        // the verified A/B/A read scope; keep the malicious header and assertions.
        db.ChangeTracker.Clear();
        var repository = new ClienteRepository(db);

        // The hostile header asks for B while the trusted server binding grants A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { clienteA.Id }, (await repository.GetAllAsync()).Select(c => c.Id));
        Assert.Equal(new[] { clienteA.Id }, (await repository.GetActivosAsync()).Select(c => c.Id));
        Assert.Equal(new[] { clienteA.Id }, (await repository.BuscarActivosAsync("P2003A Cliente")).Select(c => c.Id));
        Assert.Null(await repository.GetByIdAsync(clienteB.Id));
        Assert.Equal(clienteA.Id, (await repository.GetByIdAsync(clienteA.Id))?.Id);
        Assert.False(await repository.ExisteIdentidadAsync("P2003A-DOC-B"));

        // Reversing the verified binding must not reuse A's visibility.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(new[] { clienteB.Id }, (await repository.GetAllAsync()).Select(c => c.Id));
        Assert.Equal(new[] { clienteB.Id }, (await repository.BuscarActivosAsync("P2003A Cliente")).Select(c => c.Id));
        Assert.Null(await repository.GetByIdAsync(clienteA.Id));
        Assert.False(await repository.ExisteIdentidadAsync("P2003A-DOC-A"));

        // Returning to A does not leak the intervening B query.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(new[] { clienteA.Id }, (await repository.GetActivosAsync()).Select(c => c.Id));
        Assert.Null(await repository.GetByIdConVentasAsync(clienteB.Id));
    }
}
