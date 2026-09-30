using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ProveedorServiceTests
{
    private readonly Mock<IProveedorRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly ProveedorService _service;

    public ProveedorServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(1);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin");
        _service = new ProveedorService(_repoMock.Object, _currentUserMock.Object, _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Nombre_Duplicado_Es_Permitido()
    {
        _repoMock.Setup(r => r.ExisteNombreAsync("Tech Import SA", null)).ReturnsAsync(true);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Proveedor>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateProveedorDto { Nombre = "Tech Import SA" });

        _repoMock.Verify(r => r.AddAsync(It.Is<Proveedor>(p => p.Nombre == "Tech Import SA")), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Con_Compras_Asociadas_Aplica_Eliminacion_Logica()
    {
        var proveedor = new Proveedor { Id = 1, Nombre = "Tech Import SA", Activo = true };
        proveedor.Compras.Add(new Compra { NumeroCompra = "COM-000001", Estado = EstadoDocumento.Confirmada, Total = 100 });

        _repoMock.Setup(r => r.GetByIdConComprasAsync(1)).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(proveedor.Activo);
        _repoMock.Verify(r => r.Remove(It.IsAny<Proveedor>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Sin_Compras_Aplica_Eliminacion_Logica()
    {
        var proveedor = new Proveedor { Id = 1, Nombre = "Nuevo Proveedor", Activo = true };
        _repoMock.Setup(r => r.GetByIdConComprasAsync(1)).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(1);

        Assert.True(resultado);
        Assert.False(proveedor.Activo);
        _repoMock.Verify(r => r.Remove(It.IsAny<Proveedor>()), Times.Never);
    }
}
