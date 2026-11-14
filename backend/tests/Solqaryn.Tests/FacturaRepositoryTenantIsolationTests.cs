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

/// <summary>Una capability pública de PDF nunca concede acceso privado ni cruzado.</summary>
public sealed class FacturaRepositoryTenantIsolationTests
{
    [Fact]
    public async Task FacturasPrivadasYEnlacePublico_NoCruzanEmpresasA_B()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-facturas-" + Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);
        var a = new Empresa("A FACTURAS");
        var b = new Empresa("B FACTURAS");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var ventaA = new Venta { NumeroVenta = "P4-VENTA-A" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id, NumeroFactura = "P4-FACTURA-A", VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var ventaB = new Venta { NumeroVenta = "P4-VENTA-B" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id, NumeroFactura = "P4-FACTURA-B", VendedorUsuarioId = 22
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Global", true));
        scope.Setup(x => x.ObtenerActualAsync(a.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, a.Id, 1, "A", true));
        scope.Setup(x => x.ObtenerActualAsync(b.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(22, b.Id, 1, "B", true));
        var repo = new FacturaRepository(db, scope.Object, accessor.Object);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        Assert.Equal(facturaA.Id, (await repo.GetByIdAsync(facturaA.Id))?.Id);
        Assert.Null(await repo.GetByIdAsync(facturaB.Id));
        Assert.Null(await repo.GetByVentaIdAsync(ventaB.Id));
        Assert.Equal(facturaA.Id, Assert.Single(await repo.GetAllAsync()).Id);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        Assert.Equal(facturaB.Id, Assert.Single(await repo.GetAllAsync()).Id);

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        // Un administrador global legacy, sin marca RBAC, no tiene acceso con 2+.
        Assert.Empty(await repo.GetAllAsync());

        http.Request.Path = "/facturas/publico/token-verificado/pdf";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        http.Items[PublicInvoiceAccessContext.FacturaIdKey] = facturaA.Id;
        Assert.Equal(facturaA.Id,
            (await repo.GetByIdParaEnlacePublicoValidadoAsync(facturaA.Id))?.Id);
        Assert.Null(await repo.GetByIdParaEnlacePublicoValidadoAsync(facturaB.Id));
        Assert.Null(await repo.GetByIdAsync(facturaB.Id));
        Assert.Empty(await repo.GetAllAsync());

        // Solo poner FacturaId no basta: la capability necesita empresa y ruta.
        http.Items.Remove(VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(await repo.GetByIdParaEnlacePublicoValidadoAsync(facturaA.Id));
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        http.Request.Path = "/facturas";
        Assert.Null(await repo.GetByIdParaEnlacePublicoValidadoAsync(facturaA.Id));
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }
}
