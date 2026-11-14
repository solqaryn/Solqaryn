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
/// P2-008.C: comprueba que una cabecera manipulada no eleva permisos
/// sobre facturas privadas de otra empresa. No sustituye pruebas HTTP/JWT.
/// </summary>
public sealed class P2008CFacturasPrivadasEmpresaIdTests
{
    [Fact]
    public async Task Facturas_privadas_ignoran_empresa_en_header_y_respetan_marca_verificada()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008c-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var empresaA = new Empresa("P2008C A");
        var empresaB = new Empresa("P2008C B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008C-VENTA-A" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id, NumeroFactura = "P2008C-FACTURA-A", VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008C-VENTA-B" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id, NumeroFactura = "P2008C-FACTURA-B", VendedorUsuarioId = 22
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Global", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "A", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(22, empresaB.Id, 1, "B", true));
        var repository = new FacturaRepository(db, scope.Object, accessor.Object);

        // B se declara en un header no confiable, pero solo A esta autorizada.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(facturaA.Id, Assert.Single(await repository.GetAllAsync()).Id);
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));

        // El mismo contexto cambia de tenant SOLO por la marca verificada.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(facturaB.Id, Assert.Single(await repository.GetAllAsync()).Id);
        Assert.Null(await repository.GetByIdAsync(facturaA.Id));

        // Con dos empresas y sin marca RBAC, la lectura privada no se abre.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Empty(await repository.GetAllAsync());
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }
}
