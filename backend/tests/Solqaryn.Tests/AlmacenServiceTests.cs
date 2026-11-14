using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class AlmacenServiceTests
{
    private readonly Mock<IAlmacenRepository> _repoMock = new();
    private readonly Mock<ISucursalRepository> _sucursalRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly AlmacenService _service;

    public AlmacenServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(7);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin-almacenes");
        _service = new AlmacenService(
            _repoMock.Object,
            _sucursalRepoMock.Object,
            _currentUserMock.Object,
            _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_NormalizaDatos_Y_AsignaSucursalActiva()
    {
        var sucursal = CrearSucursal(activa: true);
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(sucursal);
        _repoMock.Setup(r => r.ExisteCodigoAsync("BOD-01", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        Almacen? creado = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Almacen>()))
            .Callback<Almacen>(a => creado = a)
            .Returns(Task.CompletedTask);

        var resultado = await _service.CreateAsync(new CreateAlmacenDto
        {
            SucursalId = 10,
            Codigo = " bod-01 ",
            Nombre = " Bodega Central ",
            Tipo = "bodega"
        });

        Assert.NotNull(creado);
        Assert.Equal("BOD-01", creado!.Codigo);
        Assert.Equal("Bodega Central", creado.Nombre);
        Assert.Equal(TipoAlmacen.Bodega, creado.Tipo);
        Assert.Equal(sucursal, creado.Sucursal);
        Assert.True(creado.Activo);
        Assert.Equal("BOD-01", resultado.Codigo);
        Assert.Equal("TGU-01", resultado.SucursalCodigo);
    }

    [Fact]
    public async Task CreateAsync_SucursalInactiva_FallaCerrado()
    {
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CrearSucursal(activa: false));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateAlmacenDto
            {
                SucursalId = 10,
                Codigo = "BOD-01",
                Nombre = "Bodega Central",
                Tipo = "Bodega"
            }));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Almacen>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CodigoDuplicado_FallaCerrado()
    {
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(CrearSucursal(activa: true));
        _repoMock.Setup(r => r.ExisteCodigoAsync("BOD-01", null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateAlmacenDto
            {
                SucursalId = 10,
                Codigo = "BOD-01",
                Nombre = "Bodega Central",
                Tipo = "Bodega"
            }));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Almacen>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_ActivarConSucursalInactiva_FallaCerrado()
    {
        var almacen = CrearAlmacen(activo: false, sucursalActiva: false);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(almacen);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CambiarEstadoAsync(20, true));

        _repoMock.Verify(r => r.Update(It.IsAny<Almacen>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_MismoEstado_EsIdempotente()
    {
        var almacen = CrearAlmacen(activo: true, sucursalActiva: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(almacen);

        var resultado = await _service.CambiarEstadoAsync(20, true);

        Assert.NotNull(resultado);
        Assert.True(resultado!.Activo);
        _repoMock.Verify(r => r.Update(It.IsAny<Almacen>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        _auditoriaMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_NoModificaEstadoOperativo()
    {
        var almacen = CrearAlmacen(activo: false, sucursalActiva: false);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(almacen);
        _repoMock.Setup(r => r.ExisteCodigoAsync("BOD-02", 20)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(20, new UpdateAlmacenDto
        {
            SucursalId = 10,
            Codigo = "bod-02",
            Nombre = "Bodega Editada",
            Tipo = "Cuarentena"
        });

        Assert.NotNull(resultado);
        Assert.False(almacen.Activo);
        Assert.Equal("BOD-02", almacen.Codigo);
        Assert.Equal(TipoAlmacen.Cuarentena, almacen.Tipo);
        _sucursalRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MoverASucursalInactiva_FallaCerrado()
    {
        var almacen = CrearAlmacen(activo: true, sucursalActiva: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(almacen);
        _repoMock.Setup(r => r.ExisteCodigoAsync("BOD-01", 20)).ReturnsAsync(false);
        _sucursalRepoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(CrearSucursal(11, false));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.UpdateAsync(20, new UpdateAlmacenDto
            {
                SucursalId = 11,
                Codigo = "BOD-01",
                Nombre = "Bodega Central",
                Tipo = "Bodega"
            }));

        _repoMock.Verify(r => r.Update(It.IsAny<Almacen>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AplicaSoftDeleteAuditado()
    {
        var almacen = CrearAlmacen(activo: true, sucursalActiva: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(almacen);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(20);

        Assert.True(resultado);
        Assert.False(almacen.Activo);
        Assert.True(almacen.Eliminado);
        Assert.Equal(7, almacen.EliminadoPorUsuarioId);
        Assert.NotNull(almacen.FechaEliminacion);
        _repoMock.Verify(r => r.Update(almacen), Times.Once);
    }

    [Fact]
    public async Task BuscarAsync_AplicaPaginacionDefensiva_Y_TipoTipado()
    {
        _repoMock.Setup(r => r.BuscarAsync(null, null, null, TipoAlmacen.Transito, 1, 100))
            .ReturnsAsync((new List<Almacen>(), 0));

        var resultado = await _service.BuscarAsync(new AlmacenFiltroDto
        {
            Tipo = "transito",
            Pagina = 0,
            TamanoPagina = 1000
        });

        Assert.Equal(1, resultado.Pagina);
        Assert.Equal(100, resultado.TamanoPagina);
        Assert.Equal(0, resultado.TotalPaginas);
    }

    [Fact]
    public void GetTipos_ExponeCincoTiposEnOrdenEstable()
    {
        var tipos = _service.GetTipos();

        Assert.Equal(new[] { "Tienda", "Bodega", "Transito", "Devolucion", "Cuarentena" },
            tipos.Select(t => t.Codigo).ToArray());
    }

    private static Sucursal CrearSucursal(int id = 10, bool activa = true) => new()
    {
        Id = id,
        Codigo = id == 10 ? "TGU-01" : $"SUC-{id}",
        Nombre = "Sucursal Centro",
        ZonaHoraria = "America/Tegucigalpa",
        Activa = activa
    };

    private static Almacen CrearAlmacen(bool activo, bool sucursalActiva) => new()
    {
        Id = 20,
        SucursalId = 10,
        Sucursal = CrearSucursal(activa: sucursalActiva),
        Codigo = "BOD-01",
        Nombre = "Bodega Central",
        Tipo = TipoAlmacen.Bodega,
        Activo = activo
    };
}
