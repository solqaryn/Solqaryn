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
/// P2-008.G: una empresa inactiva sigue siendo un tenant distinto.
/// El acceso global legacy sin empresa verificada nunca debe abrir facturas
/// privadas aunque solo quede una empresa activa. EF InMemory no reemplaza
/// MySQL, HTTP, JWT ni controles de enlace PDF.
/// </summary>
public sealed class P2008GFacturaTenantInactivoNegativoTests
{
    [Fact]
    public async Task Tenant_inactivo_impide_lectura_global_legacy_y_header_suplantado()
    {
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008g-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);

        var empresaA = new Empresa("P2008G empresa A");
        var empresaB = new Empresa("P2008G empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008G-FOLIO" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id,
            NumeroFactura = "P2008G-FACTURA",
            VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008G-FOLIO" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id,
            NumeroFactura = "P2008G-FACTURA",
            VendedorUsuarioId = 22
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();

        empresaB.Desactivar();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(2, await db.Set<Empresa>().IgnoreQueryFilters().CountAsync());
        Assert.False((await db.Set<Empresa>().IgnoreQueryFilters()
            .SingleAsync(e => e.Id == empresaB.Id)).Activa);

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(99, 1, "Global", true));
        scope.Setup(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "A", true));
        var repository = new FacturaRepository(db, scope.Object, accessor);

        // Ni rol global legacy ni cabecera declarada A abren datos de A o B.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.Null(await repository.GetByIdAsync(facturaA.Id));
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));
        Assert.Empty(await repository.GetAllAsync());
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);

        // La marca verificada A permite solo A, incluso con cabecera falsa B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Equal(facturaA.Id, (await repository.GetByIdAsync(facturaA.Id))?.Id);
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        Assert.Equal(facturaA.Id, Assert.Single(await repository.GetAllAsync()).Id);
        scope.Verify(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
