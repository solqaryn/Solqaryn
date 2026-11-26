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

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(99)]
    public async Task Crear_rechaza_tipo_no_comercial_antes_de_persistir_documento_mixto(int tipo)
    {
        _clientes.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Cliente
        {
            Id = 1, Nombre = "Cliente", Activo = true
        });
        _productos.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new Producto
        {
            Id = 10, Nombre = "Mercadería", Precio = 50m, Activo = true
        });
        _productos.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(new Producto
        {
            Id = 11, Nombre = "No comercial", Precio = 25m, Activo = true,
            TipoInventario = (TipoInventario)tipo
        });

        var dto = new CreateCotizacionDto
        {
            ClienteId = 1,
            Detalles =
            [
                new CreateCotizacionDetalleDto { ProductoId = 10, Cantidad = 1, PrecioUnitario = 50m },
                new CreateCotizacionDetalleDto { ProductoId = 11, ProductoVarianteId = 8, Cantidad = 1, PrecioUnitario = 25m }
            ]
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CrearAsync(dto));
        Assert.Contains("no está habilitado para cotización comercial", error.Message);
        _variantes.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _repository.Verify(x => x.AddAsync(It.IsAny<Cotizacion>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Actualizar_rechaza_producto_reclasificado_sin_persistir_ni_auditar()
    {
        var cotizacion = CrearBorradorValido(20);
        var detalle = cotizacion.Detalles.Single();
        _repository.Setup(x => x.GetByIdForUpdateAsync(20)).ReturnsAsync(cotizacion);
        _productos.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new Producto
        {
            Id = 10, Nombre = "Insumo", Precio = 50m, Activo = true,
            TipoInventario = TipoInventario.InsumoAdministrativo
        });

        var dto = new UpdateCotizacionDto
        {
            Id = 20, ClienteId = 1,
            Detalles =
            [
                new UpdateCotizacionDetalleDto
                {
                    Id = detalle.Id, ProductoId = 10, Cantidad = 2, PrecioUnitario = 50m
                }
            ]
        };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ActualizarAsync(dto));
        Assert.Contains("no está habilitado para cotización comercial", error.Message);
        Assert.Equal(1m, detalle.Cantidad);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)] // Producto ausente del ámbito autorizado.
    [InlineData(1)] // Reclasificado después del documento original.
    [InlineData(2)] // Inactivo.
    [InlineData(3)] // Eliminado.
    [InlineData(4)] // Variante ajena al producto.
    [InlineData(5)] // Variante ausente.
    [InlineData(6)] // Variante inactiva.
    [InlineData(7)] // Variante eliminada.
    public async Task Duplicar_revalida_catalogo_actual_antes_de_persistir(int caso)
    {
        var original = CrearBorradorValido(20);
        var segundo = new CotizacionDetalle { ProductoId = 11, ProductoVarianteId = 81 };
        segundo.EstablecerValores(2m, 25m);
        original.Detalles.Add(segundo);
        _repository.Setup(x => x.GetByIdAsync(20, true)).ReturnsAsync(original);
        var productos = new List<Producto> { new Producto { Id = 10, Activo = true, TipoInventario = TipoInventario.MercaderiaVenta } };
        if (caso != 0)
            productos.Add(new Producto
            {
                Id = 11, Activo = caso != 2, Eliminado = caso == 3,
                TipoInventario = caso == 1 ? TipoInventario.InsumoAdministrativo : TipoInventario.MercaderiaVenta
            });
        _productos.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(productos);
        var variantes = new List<ProductoVariante>();
        if (caso != 5)
            variantes.Add(new ProductoVariante { Id = 81, ProductoId = caso == 4 ? 10 : 11, Activo = caso != 6, Eliminado = caso == 7 });
        _variantes.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(variantes);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DuplicarComoBorradorAsync(20));

        _productos.Verify(x => x.GetByIdsForUpdateAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 10, 11 }))), Times.Once);
        _variantes.Verify(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()), caso <= 3 ? Times.Never() : Times.Once());
        _repository.Verify(x => x.AddAsync(It.IsAny<Cotizacion>()), Times.Never);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Never);
        _auditoria.VerifyNoOtherCalls();
        Assert.Equal(2, original.Detalles.Count);
    }

    [Fact]
    public async Task Duplicar_valida_mercaderia_vigente_y_conserva_snapshot_del_original()
    {
        var original = CrearBorradorValido(20);
        _repository.Setup(x => x.GetByIdAsync(20, true)).ReturnsAsync(original);
        _productos.Setup(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(
            new List<Producto> { new Producto { Id = 10, Activo = true, TipoInventario = TipoInventario.MercaderiaVenta, Precio = 999m } });
        Cotizacion? copia = null;
        _repository.Setup(x => x.AddAsync(It.IsAny<Cotizacion>())).Callback<Cotizacion>(x => { x.Id = 77; copia = x; }).Returns(Task.CompletedTask);
        _repository.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(x => x.GetByIdAsync(77, true)).ReturnsAsync(() => copia);
        await _service.DuplicarComoBorradorAsync(20);
        Assert.NotNull(copia);
        Assert.NotSame(original, copia);
        Assert.Equal(50m, copia!.Detalles.Single().PrecioUnitario);
        Assert.Equal(50m, original.Detalles.Single().PrecioUnitario);
        _repository.Verify(x => x.SaveChangesAsync(), Times.Once);
        _variantes.Verify(x => x.GetByIdsForUpdateAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
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
