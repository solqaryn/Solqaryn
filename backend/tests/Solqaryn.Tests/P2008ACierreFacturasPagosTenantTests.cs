using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-008.A: el cierre de cobertura de facturas A/B verifica tambien el
/// rechazo de un intento de abono cruzado antes de cualquier escritura.
/// Usa EF InMemory; no sustituye HTTP/JWT, MySQL ni integracion bancaria.
/// </summary>
public sealed class P2008ACierreFacturasPagosTenantTests
{
    [Fact]
    public async Task Abono_a_factura_B_con_tenant_A_o_sin_marca_no_modifica_factura_ajena()
    {
        var http = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = http };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008a-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);

        var empresaA = new Empresa("P2008A Empresa A");
        var empresaB = new Empresa("P2008A Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008A-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id,
            NumeroFactura = "P2008A-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 11,
            Total = 250m,
            SaldoPendiente = 250m
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008A-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id,
            NumeroFactura = "P2008A-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 22,
            Total = 250m,
            SaldoPendiente = 250m
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(empresaA.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(11, empresaA.Id, 1, "A", true));

        var repo = new FacturaRepository(db, scope.Object, accessor);
        var service = new FacturaService(repo, new Mock<IEmpresaConfiguracionService>().Object);
        var abono = new RegistrarFacturaPagoDto { Monto = 50m, MetodoPago = "Efectivo" };

        // El header B no eleva el tenant A verificado por el backend.
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.NotNull(await repo.GetByIdAsync(facturaA.Id));
        Assert.Null(await repo.GetByIdAsync(facturaB.Id));

        var rechazado = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RegistrarPagoAsync(facturaB.Id, abono, 11, "usuario-A"));
        Assert.Contains("no encontrada", rechazado.Message, StringComparison.OrdinalIgnoreCase);

        // Sin marca autenticada, la base multiempresa tampoco permite el abono.
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        var sinTenant = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RegistrarPagoAsync(facturaB.Id, abono, 11, "usuario-A"));
        Assert.Contains("no encontrada", sinTenant.Message, StringComparison.OrdinalIgnoreCase);

        var facturaPersistidaB = await db.Facturas.IgnoreQueryFilters()
            .AsNoTracking().Include(f => f.Pagos)
            .SingleAsync(f => f.Id == facturaB.Id);
        Assert.Equal(250m, facturaPersistidaB.SaldoPendiente);
        Assert.Equal(0m, facturaPersistidaB.TotalPagado);
        Assert.Empty(facturaPersistidaB.Pagos);
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }
}
