using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N33ReservaAutomaticaApplicationContractTests
{
    [Fact]
    public void ConfirmarPedidoVenta_ContratoExigeAsignacionesFisicasExplicitas()
    {
        var request = new ConfirmarPedidoVentaDto();

        Assert.NotNull(request.Asignaciones);
        Assert.Empty(request.Asignaciones);

        var asignacion = new AsignacionReservaPedidoDto
        {
            ProductoVarianteId = 11,
            AlmacenId = 22,
            UbicacionAlmacenId = 33,
            Cantidad = 4
        };

        request.Asignaciones.Add(asignacion);

        var registrada = Assert.Single(request.Asignaciones);
        Assert.Equal(11, registrada.ProductoVarianteId);
        Assert.Equal(22, registrada.AlmacenId);
        Assert.Equal(33, registrada.UbicacionAlmacenId);
        Assert.Equal(4, registrada.Cantidad);
    }

    [Fact]
    public void IPedidoVentaService_ConfirmarAsync_RecibeElContratoDeReserva()
    {
        var method = typeof(IPedidoVentaService).GetMethod(nameof(IPedidoVentaService.ConfirmarAsync));

        Assert.NotNull(method);
        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal(typeof(ConfirmarPedidoVentaDto), parameters[1].ParameterType);
        Assert.Equal(typeof(Task<PedidoVentaDto>), method.ReturnType);
    }

    [Fact]
    public void PedidoVentaService_ImplementaLaMismaFirmaDeConfirmacion()
    {
        var interfaceMethod = typeof(IPedidoVentaService).GetMethod(nameof(IPedidoVentaService.ConfirmarAsync));
        var implementationMethod = typeof(PedidoVentaService).GetMethod(nameof(PedidoVentaService.ConfirmarAsync));

        Assert.NotNull(interfaceMethod);
        Assert.NotNull(implementationMethod);
        Assert.Equal(interfaceMethod!.ReturnType, implementationMethod!.ReturnType);
        Assert.Equal(
            interfaceMethod.GetParameters().Select(x => x.ParameterType),
            implementationMethod.GetParameters().Select(x => x.ParameterType));
    }
}
