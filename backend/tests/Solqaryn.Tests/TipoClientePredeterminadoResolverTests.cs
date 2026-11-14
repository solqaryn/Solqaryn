using System.Collections.Generic;
using System.Threading.Tasks;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class TipoClientePredeterminadoResolverTests
{
    private readonly Mock<ITipoClienteRepository> _repoMock = new();
    private readonly Mock<ILogger<TipoClientePredeterminadoResolver>> _loggerMock = new();
    private readonly TipoClientePredeterminadoResolver _resolver;

    public TipoClientePredeterminadoResolverTests()
    {
        _resolver = new TipoClientePredeterminadoResolver(_repoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ResolverIdPredeterminadoAsync_Con_Un_Predeterminado_Retorna_Id()
    {
        // Arrange
        var tipo = new TipoCliente { Id = 10, Codigo = "VIP", Nombre = "VIP", EsPredeterminado = true, Activo = true };
        _repoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente> { tipo });

        // Act
        var result = await _resolver.ResolverIdPredeterminadoAsync();

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public async Task ResolverIdPredeterminadoAsync_Con_Multiples_Predeterminados_Lanza_Excepcion()
    {
        // Arrange
        var tipo1 = new TipoCliente { Id = 10, Codigo = "VIP", Nombre = "VIP", EsPredeterminado = true, Activo = true };
        var tipo2 = new TipoCliente { Id = 20, Codigo = "FRECUENTE", Nombre = "Frecuente", EsPredeterminado = true, Activo = true };
        _repoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente> { tipo1, tipo2 });

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _resolver.ResolverIdPredeterminadoAsync());
    }

    [Fact]
    public async Task ResolverIdPredeterminadoAsync_Sin_Predeterminado_Retorna_SinClasificar()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente>());
        var sinClasificar = new TipoCliente { Id = 1, Codigo = "SIN_CLASIFICAR", Nombre = "Sin clasificar", EsPredeterminado = false, Activo = true };
        _repoMock.Setup(r => r.GetByCodigoAsync("SIN_CLASIFICAR")).ReturnsAsync(sinClasificar);

        // Act
        var result = await _resolver.ResolverIdPredeterminadoAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ResolverIdPredeterminadoAsync_Sin_Predeterminado_Ni_SinClasificar_Lanza_Excepcion()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente>());
        _repoMock.Setup(r => r.GetByCodigoAsync("SIN_CLASIFICAR")).ReturnsAsync((TipoCliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _resolver.ResolverIdPredeterminadoAsync());
    }

    [Fact]
    public async Task ResolverIdPredeterminadoAsync_Sin_Predeterminado_Con_SinClasificar_Inactivo_Lanza_Excepcion()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente>());
        var sinClasificarInactivo = new TipoCliente { Id = 1, Codigo = "SIN_CLASIFICAR", Nombre = "Sin clasificar", EsPredeterminado = false, Activo = false };
        _repoMock.Setup(r => r.GetByCodigoAsync("SIN_CLASIFICAR")).ReturnsAsync(sinClasificarInactivo);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => _resolver.ResolverIdPredeterminadoAsync());
    }
}
