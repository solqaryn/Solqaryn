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
/// P2-008.E: una capability PDF publica solo habilita la factura y empresa
/// expresamente verificadas. No eleva lecturas privadas de otros documentos.
/// No sustituye pruebas HTTP/JWT ni MySQL real.
/// </summary>
public sealed class P2008ECapabilityFacturaPrivadaTenantTests
{
    [Fact]
    public async Task Capability_publica_no_se_trasplanta_a_factura_B_ni_otorga_lectura_privada()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2008e-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);

        var empresaA = new Empresa("P2008E A");
        var empresaB = new Empresa("P2008E B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        var ventaA = new Venta { NumeroVenta = "P2008E-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();
        var facturaA = new Factura
        {
            VentaId = ventaA.Id,
            NumeroFactura = "P2008E-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 11
        };
        db.Facturas.Add(facturaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        var ventaB = new Venta { NumeroVenta = "P2008E-FOLIO-COMPARTIDO" };
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();
        var facturaB = new Factura
        {
            VentaId = ventaB.Id,
            NumeroFactura = "P2008E-FACTURA-COMPARTIDA",
            VendedorUsuarioId = 22
        };
        db.Facturas.Add(facturaB);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Global", true));
        var repository = new FacturaRepository(db, scope.Object, accessor.Object);

        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Path = "/facturas/publico/token-verificado/pdf";
        http.Request.Headers["X-Empresa-Id"] = empresaB.Id.ToString();

        // El ID, la ruta y un header sin marca validada no bastan.
        http.Items[PublicInvoiceAccessContext.FacturaIdKey] = facturaA.Id;
        Assert.Null(await repository.GetByIdAsync(facturaA.Id));
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));

        // La capability de A abre solo la factura A, nunca el listado privado.
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
        Assert.Equal(facturaA.Id, (await repository.GetByIdAsync(facturaA.Id))?.Id);
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        Assert.Null(await repository.GetByIdParaEnlacePublicoValidadoAsync(facturaB.Id));
        Assert.Empty(await repository.GetAllAsync());
        Assert.Null(await repository.GetByVentaIdAsync(ventaA.Id));

        // Trasplantar el identificador de B con la marca de A no lo autoriza.
        http.Items[PublicInvoiceAccessContext.FacturaIdKey] = facturaB.Id;
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        Assert.Null(await repository.GetByIdAsync(facturaA.Id));

        // El control positivo de B exige la marca de B, no la cabecera de A.
        http.Request.Headers["X-Empresa-Id"] = empresaA.Id.ToString();
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
        Assert.Equal(facturaB.Id, (await repository.GetByIdAsync(facturaB.Id))?.Id);
        Assert.Null(await repository.GetByIdAsync(facturaA.Id));

        // La capability solo vive en la ruta publica designada.
        http.Request.Path = "/facturas";
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        http.Request.Path = "/facturas/publico/token-verificado/pdf";
        http.Items.Remove(VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey);
        Assert.Null(await repository.GetByIdAsync(facturaB.Id));
        scope.Verify(x => x.ObtenerActualAsync(), Times.Never);
    }
}
