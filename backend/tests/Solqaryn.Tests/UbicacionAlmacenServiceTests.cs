using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class UbicacionAlmacenServiceTests
{
    private readonly Mock<IUbicacionAlmacenRepository> _repoMock = new();
    private readonly Mock<IAlmacenRepository> _almacenRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly UbicacionAlmacenService _service;

    public UbicacionAlmacenServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(7);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("admin-ubicaciones");
        _service = new UbicacionAlmacenService(
            _repoMock.Object,
            _almacenRepoMock.Object,
            _currentUserMock.Object,
            _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_NormalizaDatos_Y_ConservaJerarquia()
    {
        var almacen = CrearAlmacen(10, activo: true, sucursalActiva: true);
        var padre = CrearUbicacion(20, 10, activa: true);
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(almacen);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(padre);
        _repoMock.Setup(r => r.ExisteCodigoAsync(10, "RACK-A1", null)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        UbicacionAlmacen? creada = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<UbicacionAlmacen>()))
            .Callback<UbicacionAlmacen>(u => creada = u)
            .Returns(Task.CompletedTask);

        var resultado = await _service.CreateAsync(new CreateUbicacionAlmacenDto
        {
            AlmacenId = 10,
            UbicacionPadreId = 20,
            Codigo = " rack-a1 ",
            Nombre = " Rack A1 ",
            Tipo = "rack"
        });

        Assert.NotNull(creada);
        Assert.Equal("RACK-A1", creada!.Codigo);
        Assert.Equal("Rack A1", creada.Nombre);
        Assert.Equal(TipoUbicacionAlmacen.Rack, creada.Tipo);
        Assert.Equal(almacen, creada.Almacen);
        Assert.Equal(padre, creada.UbicacionPadre);
        Assert.True(creada.Activa);
        Assert.Equal("RACK-A1", resultado.Codigo);
        Assert.Equal(20, resultado.UbicacionPadreId);
    }

    [Fact]
    public async Task CreateAsync_AlmacenO_SucursalInactivos_FallaCerrado()
    {
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(CrearAlmacen(10, activo: true, sucursalActiva: false));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateUbicacionAlmacenDto
            {
                AlmacenId = 10,
                Codigo = "P-A",
                Nombre = "Pasillo A",
                Tipo = "Pasillo"
            }));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<UbicacionAlmacen>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PadreDeOtroAlmacen_FallaCerrado()
    {
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(CrearAlmacen(10, true, true));
        _repoMock.Setup(r => r.GetByIdAsync(21))
            .ReturnsAsync(CrearUbicacion(21, 11, activa: true));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateUbicacionAlmacenDto
            {
                AlmacenId = 10,
                UbicacionPadreId = 21,
                Codigo = "B-01",
                Nombre = "Bin 01",
                Tipo = "Bin"
            }));
    }

    [Fact]
    public async Task CreateAsync_PadreInactivo_FallaCerrado()
    {
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(CrearAlmacen(10, true, true));
        _repoMock.Setup(r => r.GetByIdAsync(20))
            .ReturnsAsync(CrearUbicacion(20, 10, activa: false));

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CreateAsync(new CreateUbicacionAlmacenDto
            {
                AlmacenId = 10,
                UbicacionPadreId = 20,
                Codigo = "B-01",
                Nombre = "Bin 01",
                Tipo = "Bin"
            }));
    }

    [Fact]
    public async Task UpdateAsync_ReparentarACiclo_FallaCerrado()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        var padreCandidato = CrearUbicacion(21, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _repoMock.Setup(r => r.GetByIdAsync(21)).ReturnsAsync(padreCandidato);
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(actual.Almacen);
        _repoMock.Setup(r => r.CreariaCicloAsync(20, 10, 21)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.UpdateAsync(20, new UpdateUbicacionAlmacenDto
            {
                AlmacenId = 10,
                UbicacionPadreId = 21,
                Codigo = "P-A",
                Nombre = "Pasillo A",
                Tipo = "Pasillo"
            }));

        _repoMock.Verify(r => r.Update(It.IsAny<UbicacionAlmacen>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MoverAlmacenConHijas_FallaCerrado()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _almacenRepoMock.Setup(r => r.GetByIdAsync(11))
            .ReturnsAsync(CrearAlmacen(11, true, true));
        _repoMock.Setup(r => r.TieneHijasNoEliminadasAsync(20)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.UpdateAsync(20, new UpdateUbicacionAlmacenDto
            {
                AlmacenId = 11,
                Codigo = "P-A",
                Nombre = "Pasillo A",
                Tipo = "Pasillo"
            }));
    }

    [Fact]
    public async Task UpdateAsync_MetadataNoMutaEstado_NiExigeAlmacenActivo()
    {
        var actual = CrearUbicacion(20, 10, activa: false, almacenActivo: false, sucursalActiva: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _repoMock.Setup(r => r.ExisteCodigoAsync(10, "P-B", 20)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(20, new UpdateUbicacionAlmacenDto
        {
            AlmacenId = 10,
            UbicacionPadreId = null,
            Codigo = "p-b",
            Nombre = "Pasillo B",
            Tipo = "Otra"
        });

        Assert.NotNull(resultado);
        Assert.False(actual.Activa);
        Assert.Equal("P-B", actual.Codigo);
        Assert.Equal(TipoUbicacionAlmacen.Otra, actual.Tipo);
        _almacenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_DesactivarConHijaActiva_FallaCerrado()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _repoMock.Setup(r => r.TieneHijasActivasAsync(20)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CambiarEstadoAsync(20, false));

        _repoMock.Verify(r => r.Update(It.IsAny<UbicacionAlmacen>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_ActivarBajoAlmacenInactivo_FallaCerrado()
    {
        var actual = CrearUbicacion(20, 10, activa: false, almacenActivo: false, sucursalActiva: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _almacenRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(actual.Almacen);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.CambiarEstadoAsync(20, true));
    }

    [Fact]
    public async Task CambiarEstadoAsync_MismoEstado_EsIdempotente()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);

        var resultado = await _service.CambiarEstadoAsync(20, true);

        Assert.NotNull(resultado);
        Assert.True(resultado!.Activa);
        _repoMock.Verify(r => r.Update(It.IsAny<UbicacionAlmacen>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        _auditoriaMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ConHijasNoEliminadas_FallaCerrado()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _repoMock.Setup(r => r.TieneHijasNoEliminadasAsync(20)).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(20));
        _repoMock.Verify(r => r.Update(It.IsAny<UbicacionAlmacen>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SinHijas_AplicaSoftDeleteAuditado()
    {
        var actual = CrearUbicacion(20, 10, activa: true);
        _repoMock.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(actual);
        _repoMock.Setup(r => r.TieneHijasNoEliminadasAsync(20)).ReturnsAsync(false);
        _repoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.DeleteAsync(20);

        Assert.True(resultado);
        Assert.False(actual.Activa);
        Assert.True(actual.Eliminado);
        Assert.Equal(7, actual.EliminadoPorUsuarioId);
        Assert.NotNull(actual.FechaEliminacion);
    }

    [Fact]
    public async Task BuscarAsync_AplicaPaginacionDefensiva_Y_TipoTipado()
    {
        _repoMock.Setup(r => r.BuscarAsync(null, null, null, false, TipoUbicacionAlmacen.Seccion, null, 1, 100))
            .ReturnsAsync((new List<UbicacionAlmacen>(), 0));

        var resultado = await _service.BuscarAsync(new UbicacionAlmacenFiltroDto
        {
            Tipo = "seccion",
            Pagina = 0,
            TamanoPagina = 1000
        });

        Assert.Equal(1, resultado.Pagina);
        Assert.Equal(100, resultado.TamanoPagina);
        Assert.Equal(0, resultado.TotalPaginas);
    }

    [Fact]
    public void GetTipos_ExponeSeisTiposEnOrdenEstable()
    {
        var tipos = _service.GetTipos();

        Assert.Equal(
            new[] { "Pasillo", "Estante", "Rack", "Seccion", "Bin", "Otra" },
            tipos.Select(t => t.Codigo).ToArray());
    }

    private static Almacen CrearAlmacen(int id, bool activo, bool sucursalActiva) => new()
    {
        Id = id,
        SucursalId = 100 + id,
        Sucursal = new Sucursal
        {
            Id = 100 + id,
            Codigo = $"SUC-{id}",
            Nombre = $"Sucursal {id}",
            ZonaHoraria = "America/Tegucigalpa",
            Activa = sucursalActiva
        },
        Codigo = $"ALM-{id}",
        Nombre = $"Almacén {id}",
        Tipo = TipoAlmacen.Bodega,
        Activo = activo
    };

    private static UbicacionAlmacen CrearUbicacion(
        int id,
        int almacenId,
        bool activa,
        bool almacenActivo = true,
        bool sucursalActiva = true) => new()
    {
        Id = id,
        AlmacenId = almacenId,
        Almacen = CrearAlmacen(almacenId, almacenActivo, sucursalActiva),
        Codigo = $"U-{id}",
        Nombre = $"Ubicación {id}",
        Tipo = TipoUbicacionAlmacen.Pasillo,
        Activa = activa
    };
}
