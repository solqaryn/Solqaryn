using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class TipoClienteServiceTests
{
    private readonly Mock<ITipoClienteRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly TipoClienteService _service;

    public TipoClienteServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new TipoClienteService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object, new FakeUnitOfWork());
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Lanza_Excepcion()
    {
        _repoMock.Setup(r => r.ExisteNombreNormalizadoAsync("VIP", null)).ReturnsAsync(true);

        var dto = new CreateTipoClienteDto { Nombre = "VIP", ColorHex = "#FF0000" };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_Genera_Codigo_Unico()
    {
        _repoMock.Setup(r => r.ExisteNombreNormalizadoAsync("MAYORISTA NUEVO", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.ExisteCodigoAsync("MAYORISTA_NUEVO", null)).ReturnsAsync(false);
        
        TipoCliente? creado = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<TipoCliente>()))
            .Callback<TipoCliente>(t => creado = t)
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var dto = new CreateTipoClienteDto { Nombre = "Mayorista Nuevo", ColorHex = "#00FF00" };
        await _service.CreateAsync(dto);

        Assert.NotNull(creado);
        Assert.Equal("MAYORISTA_NUEVO", creado!.Codigo);
        Assert.False(creado.EsSistema);
        Assert.Equal("Mayorista Nuevo", creado.Nombre);
    }

    [Fact]
    public async Task UpdateAsync_Sin_Clasificar_Bloquea_Desactivar_O_Desmarcar_Predeterminado()
    {
        var tipo = new TipoCliente { Id = 2, Codigo = "SIN_CLASIFICAR", Nombre = "Sin clasificar", EsSistema = true, EsPredeterminado = true, Activo = true };
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(tipo);

        var dtoDesactivar = new UpdateTipoClienteDto { Nombre = "Sin clasificar", ColorHex = "#9E9E9E", Activo = false, EsPredeterminado = true };
        var dtoNoPredet = new UpdateTipoClienteDto { Nombre = "Sin clasificar", ColorHex = "#9E9E9E", Activo = true, EsPredeterminado = false };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(2, dtoDesactivar));
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(2, dtoNoPredet));
    }

    [Fact]
    public async Task DeleteAsync_Bloquea_Tipos_Sistema()
    {
        var tipo = new TipoCliente { Id = 3, Codigo = "VIP", Nombre = "VIP", EsSistema = true };
        _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(tipo);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(3));
    }

    [Fact]
    public async Task DeleteAsync_Bloquea_Tipos_Con_Clientes_Asignados()
    {
        var tipo = new TipoCliente { Id = 4, Codigo = "MAYORISTA", Nombre = "Mayorista", EsSistema = false };
        _repoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(tipo);
        _repoMock.Setup(r => r.TieneClientesAsignadosAsync(4)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(4));
    }

    [Fact]
    public async Task DeleteAsync_Aplica_Eliminacion_Logica_Si_Es_Valido()
    {
        var tipo = new TipoCliente { Id = 5, Codigo = "MAYORISTA", Nombre = "Mayorista", EsSistema = false, Activo = true };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(tipo);
        _repoMock.Setup(r => r.TieneClientesAsignadosAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(5);

        Assert.True(resultado);
        Assert.True(tipo.Eliminado);
        Assert.False(tipo.Activo);
        Assert.NotNull(tipo.FechaEliminacion);
        Assert.Equal(1, tipo.EliminadoPorUsuarioId);
    }

    [Fact]
    public async Task CreateAsync_Predeterminado_Inactivo_Lanza_Excepcion()
    {
        var dto = new CreateTipoClienteDto
        {
            Nombre = "VIP",
            ColorHex = "#FF0000",
            Activo = false,
            EsPredeterminado = true
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(dto));
        Assert.Equal("El tipo de cliente predeterminado debe estar activo.", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_Predeterminado_Inactivo_Lanza_Excepcion()
    {
        var dto = new UpdateTipoClienteDto
        {
            Nombre = "VIP",
            ColorHex = "#FF0000",
            Activo = false,
            EsPredeterminado = true
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(1, dto));
        Assert.Equal("El tipo de cliente predeterminado debe estar activo.", ex.Message);
    }
}
