using System;
using System.Threading;
using System.Threading.Tasks;
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
/// P2-008.D: verifica acceso privado GetByVentaIdAsync ante ventas y facturas
/// con folios homónimos en dos empresas. No sustituye HTTP/JWT ni MySQL real.
/// </summary>
public sealed class P2008DFacturaPorVentaTenantTests
{
    [Fact]
    public async Task Factura_por_venta_solo_resuelve_tenant_verificado_no_header()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008d-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var empresaA = new Empresa("P2008D Empresa A");
        var empresaB = new Empresa("P2008D Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008D-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id,
            NumeroFactura = "P2008D-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008D-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id,
            NumeroFactura = "P2008D-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 22
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "A", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaB.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(22, empresaB.Id, 1, "B", true));
        var repository = new FacturaRepository(db, scope.Object, accessor.Object);

        // La cabecera falsificada nombra B, pero la marca verificada exige A.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var propiaA = await repository.GetByVentaIdAsync(ventaA.Id);
        Assert.NotNull(propiaA);
        Assert.Equal(facturaA.Id, propiaA.Id);
        Assert.Null(await repository.GetByVentaIdAsync(ventaB.Id));

        // Incluso con folios idénticos, B solo puede leer su venta.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var propiaB = await repository.GetByVentaIdAsync(ventaB.Id);
        Assert.NotNull(propiaB);
        Assert.Equal(facturaB.Id, propiaB.Id);
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));

        // Sin marca tenant, dos empresas deben bloquear acceso privado legacy.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));
        Assert.Null(await repository.GetByVentaIdAsync(ventaB.Id));
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }
}
