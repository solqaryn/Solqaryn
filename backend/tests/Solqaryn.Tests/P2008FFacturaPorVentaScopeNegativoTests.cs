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
/// P2-008.F: caso medible de documento privado enlazado a venta.
/// Una cabecera de empresa ajena no debe atravesar el tenant verificado.
/// EF InMemory no equivale a MySQL, JWT, middleware ni PDF de produccion.
/// </summary>
public sealed class P2008FFacturaPorVentaScopeNegativoTests
{
    [Fact]
    public async Task Factura_por_venta_no_revela_empresa_B_con_cabecera_falsificada()
    {
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008f-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);

        var empresaA = new Empresa("P2008F Empresa A");
        var empresaB = new Empresa("P2008F Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008F-COMPARTIDO" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id,
            NumeroFactura = "P2008F-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008F-COMPARTIDO" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id,
            NumeroFactura = "P2008F-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 22
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
        var repository = new FacturaRepository(db, scope.Object, accessor);

        // La identidad A esta verificada por el servidor, aunque el header diga B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Equal(facturaA.Id, (await repository.GetByVentaIdAsync(ventaA.Id))?.Id);
        Assert.Null(await repository.GetByVentaIdAsync(ventaB.Id));

        // Solo la marca server-side B permite consultar B.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        Assert.Equal(facturaB.Id, (await repository.GetByVentaIdAsync(ventaB.Id))?.Id);
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));

        // Sin ninguna marca autorizada, tampoco basta la cabecera.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));
        Assert.Null(await repository.GetByVentaIdAsync(ventaB.Id));
    }
}
