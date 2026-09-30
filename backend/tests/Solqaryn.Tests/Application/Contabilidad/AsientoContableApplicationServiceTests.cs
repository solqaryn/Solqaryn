using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests.Application.Contabilidad;

public sealed class AsientoContableApplicationServiceTests
{
    [Fact]
    public void CrearAggregate_CreaAsientoCuadrado()
    {
        var dto = new CrearAsientoContableDto
        {
            Concepto = "Apertura",
            Numero = "ASI-001",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 1, Debe = 100m },
                new CrearAsientoDetalleDto { CuentaContableId = 2, Haber = 100m }
            }
        };

        var asiento = AsientoContableApplicationService.CrearAggregate(dto);

        Assert.True(asiento.EstaCuadrado());
        Assert.Equal("Apertura", asiento.Concepto);
        Assert.Equal(2, asiento.Detalles.Count);
    }

    [Fact]
    public void CrearAggregate_RechazaAsientoDescuadrado()
    {
        var dto = new CrearAsientoContableDto
        {
            Concepto = "Descuadre",
            Detalles =
            {
                new CrearAsientoDetalleDto { CuentaContableId = 1, Debe = 100m },
                new CrearAsientoDetalleDto { CuentaContableId = 2, Haber = 90m }
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            AsientoContableApplicationService.CrearAggregate(dto));
    }

    [Fact]
    public void CrearAggregate_RechazaConceptoVacioYOtroDetalleInsuficiente()
    {
        Assert.Throws<BusinessRuleException>(() =>
            AsientoContableApplicationService.CrearAggregate(new CrearAsientoContableDto()));

        var dto = new CrearAsientoContableDto
        {
            Concepto = "Uno",
            Detalles = { new CrearAsientoDetalleDto { CuentaContableId = 1, Debe = 1m } }
        };

        Assert.Throws<BusinessRuleException>(() =>
            AsientoContableApplicationService.CrearAggregate(dto));
    }
}
