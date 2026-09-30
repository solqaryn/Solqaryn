using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N31CotizacionApplicationTests
{
    private readonly Mock<ICotizacionRepository> _repository = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IProductoRepository> _productos = new();
    private readonly Mock<IProductoVarianteRepository> _variantes = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CotizacionService _service;

    public N31CotizacionApplicationTests()
    {
        _currentUser.Setup(x => x.UsuarioId).Returns(41);
        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> operation) => operation());

        _service = new CotizacionService(
            _repository.Object,
            _clientes.Object,
            _productos.Object,
            _variantes.Object,
            _auditoria.Object,
            _currentUser.Object,
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Buscar_rango_invertido_falla_antes_de_consultar_repositorio()
    {
        var filtro = new CotizacionFiltroDto
        {
            FechaDesdeUtc = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            FechaHastaUtc = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.GetPagedAsync(filtro));
        _repository.Verify(x => x.GetPagedAsync(It.IsAny<CotizacionFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task Crear_sin_usuario_falla_antes_de_tocar_dependencias()
    {
        _currentUser.Setup(x => x.UsuarioId).Returns((int?)null);

        var dto = new CreateCotizacionDto
        {
            ClienteId = 1,
            Detalles =
            [
                new CreateCotizacionDetalleDto
                {
                    ProductoId = 10,
                    Cantidad = 1,
                    PrecioUnitario = 100
                }
            ]
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => _service.CrearAsync(dto));

        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
        _clientes.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _productos.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _repository.Verify(x => x.AddAsync(It.IsAny<Cotizacion>()), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Crear_persiste_y_audita_dentro_de_unidad_transaccional()
    {
        _clientes.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Cliente
        {
            Id = 1,
            Nombre = "Cliente",
            Activo = true
        });
        _productos.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new Producto
        {
            Id = 10,
            Nombre = "Producto",
            Precio = 50m,
            Activo = true
        });

        Cotizacion? creada = null;
        _repository
            .Setup(x => x.AddAsync(It.IsAny<Cotizacion>()))
            .Callback<Cotizacion>(x =>
            {
                x.Id = 77;
                creada = x;
            })
            .Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(x => x.GetByIdAsync(77, true)).ReturnsAsync(() => creada);

        var dto = new CreateCotizacionDto
        {
            ClienteId = 1,
            Detalles =
            [
                new CreateCotizacionDetalleDto
                {
                    ProductoId = 10,
                    Cantidad = 2,
                    PrecioUnitario = 999999
                }
            ]
        };

        var result = await _service.CrearAsync(dto);

        Assert.Equal(77, result.Id);
        Assert.Equal(100m, result.Total);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        _auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            77,
            nameof(Cotizacion),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Enviar_usa_lock_for_update_y_auditoria_estricta()
    {
        var cotizacion = CrearBorradorValido(12);
        _repository.Setup(x => x.GetByIdForUpdateAsync(12)).ReturnsAsync(cotizacion);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(x => x.GetByIdAsync(12, true)).ReturnsAsync(cotizacion);

        var result = await _service.EnviarAsync(12);

        Assert.Equal(EstadoCotizacion.Enviada, result.Estado);
        _repository.Verify(x => x.GetByIdForUpdateAsync(12), Times.Once);
        _repository.Verify(x => x.GetByIdAsync(12, false), Times.Never);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        _auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Ventas,
            AccionPermiso.CambiarEstado,
            It.IsAny<string>(),
            12,
            nameof(Cotizacion),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Enviar_si_auditoria_falla_propaga_error_dentro_del_uow()
    {
        var cotizacion = CrearBorradorValido(13);
        _repository.Setup(x => x.GetByIdForUpdateAsync(13)).ReturnsAsync(cotizacion);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _auditoria
            .Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("audit-store-down"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.EnviarAsync(13));

        Assert.Equal("audit-store-down", ex.Message);
        _unitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        _repository.Verify(x => x.GetByIdForUpdateAsync(13), Times.Once);
    }

    [Fact]
    public async Task Actualizar_rechaza_detalle_ajeno_fail_closed()
    {
        var cotizacion = CrearBorradorValido(20);
        cotizacion.Detalles.Single().Id = 301;
        _repository.Setup(x => x.GetByIdForUpdateAsync(20)).ReturnsAsync(cotizacion);

        var dto = new UpdateCotizacionDto
        {
            Id = 20,
            ClienteId = 1,
            Detalles =
            [
                new UpdateCotizacionDetalleDto
                {
                    Id = 999,
                    ProductoId = 10,
                    Cantidad = 1,
                    PrecioUnitario = 50
                }
            ]
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ActualizarAsync(dto));

        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    private static Cotizacion CrearBorradorValido(int id)
    {
        var cotizacion = new Cotizacion
        {
            Id = id,
            ClienteId = 1,
            ClienteNombreSnapshot = "Cliente"
        };
        var detalle = new CotizacionDetalle
        {
            Id = 100,
            ProductoId = 10,
            ProductoNombreSnapshot = "Producto"
        };
        detalle.EstablecerValores(1, 50);
        cotizacion.Detalles.Add(detalle);
        return cotizacion;
    }
}
