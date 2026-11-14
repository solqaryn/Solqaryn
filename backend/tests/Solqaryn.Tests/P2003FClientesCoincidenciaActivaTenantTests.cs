using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-003.F: caso causal para busqueda de clientes con documento compartido
/// entre empresas, inactivos y cabecera X-Empresa-Id falsificada.
/// EF InMemory: no certifica HTTP, JWT, MySQL ni controles de middleware.
/// </summary>
public sealed class P2003FClientesCoincidenciaActivaTenantTests
{
    [Fact]
    public async Task Documento_compartido_y_cliente_inactivo_no_escapan_del_tenant_verificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2003f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2003F Empresa A");
        var empresaB = new Empresa("P2003F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var tipoA = new TipoCliente
        {
            Codigo = "P2003F-A",
            Nombre = "P2003F Tipo A",
            NombreNormalizado = "P2003F TIPO A",
            EsSistema = true
        };
        db.Set<TipoCliente>().Add(tipoA);
        db.Entry(tipoA).Property<int?>("EmpresaId").CurrentValue = empresaA.Id;
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var tipoB = new TipoCliente
        {
            Codigo = "P2003F-B",
            Nombre = "P2003F Tipo B",
            NombreNormalizado = "P2003F TIPO B",
            EsSistema = true
        };
        db.Set<TipoCliente>().Add(tipoB);
        db.Entry(tipoB).Property<int?>("EmpresaId").CurrentValue = empresaB.Id;
        await db.SaveChangesAsync();

        const string documentoCompartido = "P2003F-DOC-COMPARTIDO";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var clienteA = new Cliente
        {
            EmpresaId = empresaA.Id,
            TipoClienteId = tipoA.Id,
            Nombre = "P2003F Cliente gemelo",
            IdentidadORTN = documentoCompartido,
            Activo = true
        };
        var inactivoA = new Cliente
        {
            EmpresaId = empresaA.Id,
            TipoClienteId = tipoA.Id,
            Nombre = "P2003F Cliente inactivo",
            Correo = "p2003f-inactivo@a.test",
            Activo = false
        };
        db.Clientes.AddRange(clienteA, inactivoA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var clienteB = new Cliente
        {
            EmpresaId = empresaB.Id,
            TipoClienteId = tipoB.Id,
            Nombre = "P2003F Cliente gemelo",
            IdentidadORTN = documentoCompartido,
            Activo = true
        };
        db.Clientes.Add(clienteB);
        await db.SaveChangesAsync();
        Assert.Equal(3, await db.Clientes.IgnoreQueryFilters().CountAsync());

        db.ChangeTracker.Clear();
        var repository = new ClienteRepository(db);

        // El mismo documento pertenece a dos tenants; la cabecera no autoriza B.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(clienteA.Id,
            (await repository.BuscarCoincidenciaActivaAsync(documentoCompartido, null, null, null))?.Id);
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, "p2003f-inactivo@a.test", null, null));
        Assert.Equal(new[] { clienteA.Id },
            (await repository.BuscarActivosAsync("P2003F Cliente")).Select(c => c.Id));

        // Cambiar solo la marca de servidor permite B, nunca reutilizar A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(clienteB.Id,
            (await repository.BuscarCoincidenciaActivaAsync(documentoCompartido, null, null, null))?.Id);
        Assert.Null(await repository.BuscarCoincidenciaActivaAsync(
            null, "p2003f-inactivo@a.test", null, null));
        Assert.Equal(new[] { clienteB.Id },
            (await repository.BuscarActivosAsync("P2003F Cliente")).Select(c => c.Id));

        // Regresar a A no arrastra la lectura del tenant B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(clienteA.Id,
            (await repository.BuscarCoincidenciaActivaAsync(documentoCompartido, null, null, null))?.Id);
    }
}
