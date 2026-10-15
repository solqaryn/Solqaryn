using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Una factura ya autorizada del tenant B jamás recibe logo/eslogan/textos
/// fiscales de EmpresaConfiguracion global de A. Aplica también al DTO
/// utilizado por PDFs y enlaces públicos con capability verificado.
/// </summary>
public sealed class FacturaTenantVisualIdentityTests
{
    [Fact]
    public async Task Factura_con_owner_usa_logo_del_tenant_y_nunca_consulta_config_global()
    {
        var doc = new Factura
        {
            Id = 44,
            EmpresaId = 9,
            VentaId = 12,
            NumeroFactura = "FAC-000044",
            EmpresaNombre = "Empresa B JWT",
            VendedorUsuarioId = 1
        };
        var own = new Empresa("Empresa B JWT");
        own.ActualizarIdentidadLegal("0801-TEST", "Dirección privada B",
            "https://cdn.example.invalid/tenants/b/logo.png", null);

        var facturas = new Mock<IFacturaRepository>(MockBehavior.Strict);
        facturas.Setup(x => x.GetByIdAsync(44)).ReturnsAsync(doc);
        var global = new Mock<IEmpresaConfiguracionService>(MockBehavior.Strict);
        var empresas = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        empresas.Setup(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(own);

        var service = new FacturaService(facturas.Object, global.Object, null, empresas.Object);
        var dto = await service.GetByIdAsync(44);

        Assert.NotNull(dto);
        Assert.Equal("Empresa B JWT", dto.EmpresaNombre);
        Assert.Equal("https://cdn.example.invalid/tenants/b/logo.png", dto.EmpresaLogoUrl);
        Assert.Null(dto.EmpresaEslogan);
        Assert.Null(dto.EmpresaTextoFactura);
        Assert.Null(dto.EmpresaTextoLegal);
        Assert.Null(dto.EmpresaCopyright);
        global.Verify(x => x.GetActivaAsync(), Times.Never());
        empresas.Verify(x => x.GetByIdAsync(9, It.IsAny<CancellationToken>()), Times.Once());
    }
}
