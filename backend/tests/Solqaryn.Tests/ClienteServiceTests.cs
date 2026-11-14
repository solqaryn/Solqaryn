using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _repoMock = new();
    private readonly Mock<ITipoClienteRepository> _tipoClienteRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly Mock<ITipoClientePredeterminadoResolver> _predeterminadoResolverMock = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");

        var fallback = new TipoCliente { Id = 1, Codigo = "SIN_CLASIFICAR", Nombre = "Sin clasificar", ColorHex = "#9E9E9E", Activo = true };
        _tipoClienteRepoMock.Setup(r => r.GetActivosAsync()).ReturnsAsync(new List<TipoCliente> { fallback });
        _tipoClienteRepoMock.Setup(r => r.GetByCodigoAsync("SIN_CLASIFICAR")).ReturnsAsync(fallback);
        _tipoClienteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(fallback);
        _predeterminadoResolverMock.Setup(r => r.ResolverIdPredeterminadoAsync()).ReturnsAsync(1);

        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((int id) => new Cliente { Id = id, Nombre = "Test", TipoClienteId = 1, TipoCliente = fallback });

        _service = new ClienteService(_repoMock.Object, _tipoClienteRepoMock.Object, _currentUserMock.Object, _auditoriaMock.Object, _predeterminadoResolverMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Es_Permitido()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Juan Pérez", null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateClienteDto { Nombre = "Juan Pérez" });

        _repoMock.Verify(r => r.AddAsync(It.Is<Cliente>(c => c.Nombre == "Juan Pérez")), Times.Once);
        _repoMock.Verify(r => r.ExisteIdentidadAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Con_Ventas_Asociadas_Aplica_Eliminacion_Logica()
    {
        var cliente = new Cliente { Id = 1, Nombre = "Juan Pérez", Activo = true };
        cliente.Ventas.Add(new Venta { NumeroVenta = "VEN-000001", Estado = EstadoDocumento.Confirmada, Total = 100 });

        _repoMock.Setup(r => r.GetByIdConVentasAsync(1)).ReturnsAsync(cliente);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(cliente.Activo);
        _repoMock.Verify(r => r.Remove(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Guarda_Usuario_Creador()
    {
        Cliente? creado = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => creado = c)
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateClienteDto { Nombre = "María López" });

        Assert.NotNull(creado);
        Assert.Equal(1, creado!.CreadoPorUsuarioId);
        Assert.True(creado.Activo);
    }
}
