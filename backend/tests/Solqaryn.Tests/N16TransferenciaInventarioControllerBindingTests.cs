using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaInventarioControllerBindingTests
{
    [Theory]
    [InlineData(nameof(TransferenciasInventarioController.Create), typeof(CreateTransferenciaInventarioDto))]
    [InlineData(nameof(TransferenciasInventarioController.Update), typeof(UpdateTransferenciaInventarioDto))]
    [InlineData(nameof(TransferenciasInventarioController.Aprobar), typeof(AprobarTransferenciaInventarioDto))]
    [InlineData(nameof(TransferenciasInventarioController.Despachar), typeof(DespacharTransferenciaInventarioDto))]
    [InlineData(nameof(TransferenciasInventarioController.Recibir), typeof(RecibirTransferenciaInventarioDto))]
    [InlineData(nameof(TransferenciasInventarioController.Cancelar), typeof(CancelarTransferenciaInventarioDto))]
    public void Mutaciones_UsanDtoTipadoDesdeBody(string nombreMetodo, Type tipoDto)
    {
        var metodo = typeof(TransferenciasInventarioController).GetMethod(nombreMetodo)
            ?? throw new InvalidOperationException($"No se encontró {nombreMetodo}.");
        var parametro = Assert.Single(
            metodo.GetParameters(),
            p => p.ParameterType == tipoDto);

        Assert.NotNull(parametro.GetCustomAttributes(typeof(FromBodyAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public void Buscar_UsaFiltroTipadoDesdeQuery()
    {
        var metodo = typeof(TransferenciasInventarioController).GetMethod(nameof(TransferenciasInventarioController.Buscar))
            ?? throw new InvalidOperationException("No se encontró Buscar.");
        var parametro = Assert.Single(metodo.GetParameters());

        Assert.Equal(typeof(TransferenciaInventarioFiltroDto), parametro.ParameterType);
        Assert.NotNull(parametro.GetCustomAttributes(typeof(FromQueryAttribute), inherit: true).SingleOrDefault());
    }

    [Theory]
    [InlineData(nameof(TransferenciasInventarioController.GetById))]
    [InlineData(nameof(TransferenciasInventarioController.Update))]
    [InlineData(nameof(TransferenciasInventarioController.Solicitar))]
    [InlineData(nameof(TransferenciasInventarioController.Aprobar))]
    [InlineData(nameof(TransferenciasInventarioController.Despachar))]
    [InlineData(nameof(TransferenciasInventarioController.Recibir))]
    [InlineData(nameof(TransferenciasInventarioController.Cancelar))]
    public void EndpointsPorId_ConservanIdentificadorEntero(string nombreMetodo)
    {
        var metodo = typeof(TransferenciasInventarioController).GetMethod(nombreMetodo)
            ?? throw new InvalidOperationException($"No se encontró {nombreMetodo}.");

        Assert.Contains(metodo.GetParameters(), p => p.Name == "id" && p.ParameterType == typeof(int));
    }
}
