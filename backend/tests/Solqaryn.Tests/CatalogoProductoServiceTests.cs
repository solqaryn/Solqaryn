using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Models;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class CatalogoProductoServiceTests
{
    private readonly Mock<ICatalogoProductoRepository> _repository = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly CatalogoProductoService _service;

    public CatalogoProductoServiceTests()
    {
        _currentUser.Setup(x => x.UsuarioId).Returns(7);
        _currentUser.Setup(x => x.NombreUsuario).Returns("admin_pruebas");
        _service = new CatalogoProductoService(
            _repository.Object,
            _currentUser.Object,
            _auditoria.Object);
    }

    [Fact]
    public async Task GetAllAsync_Modelos_SinMarca_ListaTodos()
    {
        _repository
            .Setup(r => r.GetAllAsync(TipoCatalogoProducto.Modelo, null, null))
            .ReturnsAsync(new List<MaestroProductoRegistro>
            {
                new() { Id = 1, Tipo = TipoCatalogoProducto.Modelo, Nombre = "S24", Activo = true }
            });

        var resultado = await _service.GetAllAsync(TipoCatalogoProducto.Modelo);

        Assert.Single(resultado);
        Assert.Equal("S24", resultado[0].Nombre);
    }

    [Fact]
    public async Task CreateAsync_ModeloSinMarca_LanzaReglaNegocio()
    {
        var dto = new CreateCatalogoProductoDto { Nombre = "S24" };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(TipoCatalogoProducto.Modelo, dto));

        Assert.Contains("marca", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_Color_NormalizaHexadecimalYAuditoria()
    {
        MaestroProductoRegistro? guardado = null;
        _repository
            .Setup(r => r.ExisteNombreAsync(TipoCatalogoProducto.Color, "Azul", null, null))
            .ReturnsAsync(false);
        _repository
            .Setup(r => r.AddAsync(It.IsAny<MaestroProductoRegistro>()))
            .Callback<MaestroProductoRegistro>(c => guardado = c)
            .ReturnsAsync(41);

        var resultado = await _service.CreateAsync(TipoCatalogoProducto.Color, new CreateCatalogoProductoDto
        {
            Nombre = " Azul ",
            CodigoVisual = "#1d4ed8"
        });

        Assert.NotNull(guardado);
        Assert.Equal(41, resultado.Id);
        Assert.Equal("Azul", guardado!.Nombre);
        Assert.Equal("#1D4ED8", guardado.CodigoVisual);
        Assert.Equal(7, guardado.CreadoPorUsuarioId);
        Assert.Equal("admin_pruebas", resultado.CreadoPorNombreUsuario);
    }

    [Fact]
    public async Task ValidarSeleccionProducto_ModeloDeOtraMarca_LanzaReglaNegocio()
    {
        _repository.Setup(r => r.GetByIdAsync(TipoCatalogoProducto.Marca, 10)).ReturnsAsync(new MaestroProductoRegistro
        {
            Id = 10,
            Tipo = TipoCatalogoProducto.Marca,
            Nombre = "Samsung",
            Activo = true
        });
        _repository.Setup(r => r.GetByIdAsync(TipoCatalogoProducto.Modelo, 20)).ReturnsAsync(new MaestroProductoRegistro
        {
            Id = 20,
            Tipo = TipoCatalogoProducto.Modelo,
            Nombre = "iPhone 15",
            CatalogoPadreId = 11,
            Activo = true
        });

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.ValidarSeleccionProductoAsync(null, null, 10, 20));

        Assert.Contains("no pertenece", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidarSeleccionProducto_IdsSolapados_ConsultaCadaMaestroPorTipo()
    {
        _repository.Setup(r => r.GetByIdAsync(TipoCatalogoProducto.Color, 10)).ReturnsAsync(new MaestroProductoRegistro
        {
            Id = 10,
            Tipo = TipoCatalogoProducto.Color,
            Nombre = "Negro",
            Activo = true
        });
        _repository.Setup(r => r.GetByIdAsync(TipoCatalogoProducto.Marca, 10)).ReturnsAsync(new MaestroProductoRegistro
        {
            Id = 10,
            Tipo = TipoCatalogoProducto.Marca,
            Nombre = "Samsung",
            Activo = true
        });

        await _service.ValidarSeleccionProductoAsync(10, null, 10, null);

        _repository.Verify(r => r.GetByIdAsync(TipoCatalogoProducto.Color, 10), Times.Once);
        _repository.Verify(r => r.GetByIdAsync(TipoCatalogoProducto.Marca, 10), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_MarcaConModelos_BloqueaEliminacion()
    {
        var marca = new MaestroProductoRegistro
        {
            Id = 1,
            Tipo = TipoCatalogoProducto.Marca,
            Nombre = "Samsung",
            Activo = true,
            TotalModelos = 1,
            TotalModelosActivos = 1
        };
        _repository
            .Setup(r => r.GetByIdConRelacionesAsync(TipoCatalogoProducto.Marca, 1))
            .ReturnsAsync(marca);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.DeleteAsync(TipoCatalogoProducto.Marca, 1));

        _repository.Verify(r => r.UpdateAsync(It.IsAny<MaestroProductoRegistro>()), Times.Never);
    }
}
