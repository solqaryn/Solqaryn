using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-005.E: una cabecera controlada por el cliente nunca reemplaza el tenant
/// comercial verificado para lecturas de compras. EF InMemory, no HTTP/MySQL.
/// </summary>
public sealed class P2005EComprasSinMarcaTenantTests
{
    [Fact]
    public async Task Sin_tenant_verificado_no_se_ven_compras_ni_con_header_falsificado()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2005e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        var empresaA = new Empresa("P2005E Empresa A");
        var empresaB = new Empresa("P2005E Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var compraA = new Compra
        {
            EmpresaId = empresaA.Id,
            NumeroCompra = "P2005E-A",
            ProveedorNombre = "Proveedor A"
        };
        db.Compras.Add(compraA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var compraB = new Compra
        {
            EmpresaId = empresaB.Id,
            NumeroCompra = "P2005E-B",
            ProveedorNombre = "Proveedor B"
        };
        db.Compras.Add(compraB);
        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Compras.IgnoreQueryFilters().CountAsync());
        db.ChangeTracker.Clear();

        // La consulta EF directa no es el limite de confianza: el filtro global
        // permite lectores internos sin HTTP. Probar el repositorio PRIVADO real
        // con un administrador legacy deliberadamente habilitado.
        var usuario = new Mock<ICurrentUserService>();
        var scopes = new Mock<IUsuarioScopeService>();
        scopes.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(91, 1, "Admin legacy", true));
        scopes.Setup(s => s.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(91, empresaA.Id, 1, "Admin A", true));
        scopes.Setup(s => s.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(91, empresaB.Id, 1, "Admin B", true));
        var compras = new CompraRepository(db, usuario.Object, scopes.Object);
        var pagina = new PagedRequest { Page = 1, PageSize = 10 };

        // Aunque el rol legacy sea administrador, 2+ empresas sin marca
        // implican denegacion para listado, lookup por ID y lecturas recientes.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Null(db.AuthorizedCommercialEmpresaId);
        var (sinMarca, totalSinMarca) = await compras.GetPagedAsync(pagina);
        Assert.Empty(sinMarca);
        Assert.Equal(0, totalSinMarca);
        Assert.Null(await compras.GetByIdAsync(compraB.Id));
        Assert.Empty(await compras.GetUltimasAsync());
        Assert.Equal(0, await compras.ContarTodasAsync());
        scopes.Verify(s => s.ObtenerActualAsync(), Times.Never);

        // La marca RBAC A prevalece sobre el encabezado falso de B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var (visiblesA, totalA) = await compras.GetPagedAsync(pagina);
        Assert.Equal(1, totalA);
        Assert.Equal(new[] { compraA.Id }, visiblesA.Select(c => c.Id));
        Assert.NotNull(await compras.GetByIdAsync(compraA.Id));
        Assert.Null(await compras.GetByIdAsync(compraB.Id));

        // Revocada la marca en la misma instancia, el repositorio falla cerrado.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.Empty((await compras.GetPagedAsync(pagina)).Items);
        Assert.Null(await compras.GetByIdAsync(compraA.Id));
    }
}
