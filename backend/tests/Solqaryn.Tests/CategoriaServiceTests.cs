using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class CategoriaServiceTests
{
    private readonly Mock<ICategoriaRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly CategoriaService _service;

    public CategoriaServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new CategoriaService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Guarda_Usuario_Creador()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Electrónica", null)).ReturnsAsync(false);

        Categoria? categoriaCreada = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Categoria>()))
            .Callback<Categoria>(c => categoriaCreada = c)
            .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateCategoriaDto { Nombre = "Electrónica" });

        Assert.NotNull(categoriaCreada);
        Assert.Equal(1, categoriaCreada!.CreadoPorUsuarioId);
        Assert.Equal("admin", categoriaCreada.CreadoPorNombreUsuario);
        Assert.True(categoriaCreada.Activa);
        Assert.False(categoriaCreada.Eliminada);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Lanza_Excepcion()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Electrónica", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateCategoriaDto { Nombre = "Electrónica" }));
    }

    [Fact]
    public async Task DeleteAsync_Con_Productos_Asociados_Aplica_Eliminacion_Logica()
    {
        var categoria = new Categoria { Id = 1, Nombre = "Accesorios", Activa = true };
        categoria.Productos.Add(new Producto { Nombre = "Mouse", Marca = "X", Modelo = "Y" });

        _repoMock.Setup(r => r.GetByIdConProductosAsync(1)).ReturnsAsync(categoria);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(categoria.Activa);
        Assert.True(categoria.Eliminada);
        Assert.Equal(1, categoria.EliminadaPorUsuarioId);
        Assert.NotNull(categoria.FechaEliminacion);
        _repoMock.Verify(r => r.Update(categoria), Times.Once);
        _repoMock.Verify(r => r.Remove(It.IsAny<Categoria>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_NoConvierteDesactivacionEnEliminacion()
    {
        var categoria = new Categoria
        {
            Id = 2,
            Nombre = "Audio",
            Activa = true,
            Eliminada = false
        };
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(categoria);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.CambiarEstadoAsync(2, false);

        Assert.NotNull(resultado);
        Assert.False(categoria.Activa);
        Assert.False(categoria.Eliminada);
        Assert.Null(categoria.FechaEliminacion);
    }
}
