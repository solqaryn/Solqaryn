using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class CompraServiceTests
{
    private readonly Mock<ICompraRepository> _compraRepoMock = new();
    private readonly Mock<IProveedorRepository> _proveedorRepoMock = new();
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<IProductoVarianteRepository> _varianteRepoMock = new();
    private readonly Mock<IInventarioConcurrencyService> _inventarioConcurrencyMock = new();
    private readonly Mock<IMovimientoInventarioRepository> _movInvRepoMock = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movFinRepoMock = new();
    private readonly Mock<ICalculoService> _calculoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly CompraService _service;

    public CompraServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(3);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("comprador1");
        _compraRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _compraRepoMock
            .Setup(r => r.GetMetodoPagoPorCodigoONombreAsync(It.IsAny<string>()))
            .ReturnsAsync((string valor) => new CatalogoMetodoPago
            {
                Id = 1,
                Codigo = valor.Trim(),
                Nombre = valor.Trim(),
                Activo = true
            });

        _service = new CompraService(
            _compraRepoMock.Object,
            _proveedorRepoMock.Object,
            _productoRepoMock.Object,
            _varianteRepoMock.Object,
            _inventarioConcurrencyMock.Object,
            _movInvRepoMock.Object,
            _movFinRepoMock.Object,
            _calculoMock.Object,
            _currentUserMock.Object,
            new FakeUnitOfWork(),
            _auditoriaMock.Object);
    }

    private static Producto ProductoDePrueba(int id = 1, int cantidad = 10) =>
        new() { Id = id, Nombre = "Mouse", Marca = "Logitech", Modelo = "M185", Cantidad = cantidad, Costo = 5, Precio = 10 };

    private static Compra CompraDePrueba(int cantidad = 5, int? varianteId = null, EstadoDocumento estado = EstadoDocumento.Borrador)
    {
        var metodo = new CatalogoMetodoPago
        {
            Id = 1,
            Codigo = "Efectivo",
            Nombre = "Efectivo",
            Activo = true
        };
        var compra = new Compra
        {
            Id = 1,
            NumeroCompra = "COM-000001",
            ProveedorNombre = "Proveedor X",
            Estado = estado,
            Total = 20,
            MetodoPagoId = metodo.Id,
            MetodoPagoCatalogo = metodo,
            MetodoPago = MetodoPago.Efectivo
        };
        compra.Detalles.Add(new CompraDetalle
        {
            ProductoId = 1,
            ProductoVarianteId = varianteId,
            Cantidad = cantidad,
            CostoUnitario = 4,
            Subtotal = cantidad * 4,
            ProductoNombreSnapshot = "Mouse",
            ProductoMarcaSnapshot = "Logitech",
            ProductoModeloSnapshot = "M185",
            ProductoColorSnapshot = varianteId.HasValue ? "Negro" : null,
            ProductoSkuSnapshot = varianteId.HasValue ? "M185-BLK" : null
        });
        return compra;
    }

    private void PrepararBloqueos(Compra compra, Producto producto, ProductoVariante? variante = null)
    {
        _compraRepoMock.Setup(r => r.GetByIdForUpdateAsync(compra.Id)).ReturnsAsync(compra);
        _compraRepoMock.Setup(r => r.GetByIdAsync(compra.Id)).ReturnsAsync(compra);

        var productos = new Dictionary<int, Producto> { [producto.Id] = producto };
        var variantes = variante is null
            ? new Dictionary<int, ProductoVariante>()
            : new Dictionary<int, ProductoVariante> { [variante.Id] = variante };

        _inventarioConcurrencyMock
            .Setup(c => c.BloquearYValidarInventarioAsync(
                It.IsAny<IEnumerable<InventarioDemanda>>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new InventarioLockSet(
                productos,
                variantes,
                compra.Detalles
                    .Select(d => new InventarioDemanda(d.ProductoId, d.ProductoVarianteId, d.Cantidad))
                    .ToList()));
        _movInvRepoMock
            .Setup(r => r.GetUltimoMovimientoOriginalCompraIdAsync(compra.Id))
            .ReturnsAsync(1);
        _movInvRepoMock
            .Setup(r => r.ExisteMovimientoPosteriorAsync(
                It.IsAny<int>(),
                It.IsAny<IReadOnlyCollection<int>>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task ConfirmarAsync_Aumenta_Stock_Y_Guarda_Usuario_Responsable()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var compra = CompraDePrueba();
        PrepararBloqueos(compra, producto);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(15, producto.Cantidad);
        Assert.Equal("Confirmada", resultado!.Estado);
        Assert.Equal(3, compra.ConfirmadoPorUsuarioId);
        Assert.Equal("comprador1", compra.ConfirmadoPorNombreUsuario);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.Tipo == TipoMovimientoInventario.Entrada && m.Cantidad == 5),
            It.Is<OrigenMovimientoInventario>(o => o.CompraId == compra.Id && o.VentaId == null && o.ConsumoInsumoId == null)), Times.Once);
        _movInvRepoMock.Verify(r => r.AddAsync(It.IsAny<MovimientoInventario>()), Times.Never);
        _movFinRepoMock.Verify(r => r.AddAsync(It.Is<MovimientoFinanciero>(m =>
            m.CompraId == compra.Id &&
            m.MetodoPagoId == compra.MetodoPagoId &&
            m.MetodoPago == MetodoPago.Efectivo)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_Variante_Aumenta_Stock_Exacto_Y_Consolidado()
    {
        var producto = ProductoDePrueba(cantidad: 10);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 3, Costo = 4, Precio = 10, Activo = true };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8);
        PrepararBloqueos(compra, producto, variante);

        var resultado = await _service.ConfirmarAsync(1);

        Assert.Equal(8, variante.Cantidad);
        Assert.Equal(15, producto.Cantidad);
        Assert.Equal("M185-BLK", resultado!.Detalles.Single().ProductoSku);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.ProductoVarianteId == 8 && m.StockAnterior == 3 && m.StockNuevo == 8),
            It.Is<OrigenMovimientoInventario>(o => o.CompraId == compra.Id)), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_Compra_Ya_Confirmada_Lanza_Excepcion()
    {
        var compra = new Compra { Id = 1, Estado = EstadoDocumento.Confirmada };
        _compraRepoMock.Setup(r => r.GetByIdForUpdateAsync(1)).ReturnsAsync(compra);
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));
    }

    [Fact]
    public async Task ConfirmarAsync_Sin_MetodoPago_Relacional_Lanza_Excepcion()
    {
        var compra = CompraDePrueba();
        compra.MetodoPagoId = null;
        compra.MetodoPagoCatalogo = null;
        _compraRepoMock.Setup(r => r.GetByIdForUpdateAsync(1)).ReturnsAsync(compra);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ConfirmarAsync(1));
        Assert.Contains("método de pago relacional", ex.Message);
    }

    [Fact]
    public async Task AnularAsync_Revierte_Stock_Y_Guarda_Motivo()
    {
        var producto = ProductoDePrueba(cantidad: 15);
        var compra = CompraDePrueba(estado: EstadoDocumento.Confirmada);
        PrepararBloqueos(compra, producto);

        var resultado = await _service.AnularAsync(1, "Producto dañado devuelto al proveedor");

        Assert.Equal(10, producto.Cantidad);
        Assert.Equal("Anulada", resultado!.Estado);
        Assert.Equal("Producto dañado devuelto al proveedor", compra.MotivoAnulacion);
        Assert.Equal(3, compra.AnuladoPorUsuarioId);
        _movInvRepoMock.Verify(r => r.AddConOrigenTipadoAsync(
            It.Is<MovimientoInventario>(m => m.Causa == CausaMovimientoInventario.AnulacionCompra && m.Tipo == TipoMovimientoInventario.Salida),
            It.Is<OrigenMovimientoInventario>(o => o.CompraId == compra.Id)), Times.Once);
    }

    [Fact]
    public async Task AnularAsync_Variante_Revierte_Stock_Exacto()
    {
        var producto = ProductoDePrueba(cantidad: 15);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 8, Activo = false };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8, estado: EstadoDocumento.Confirmada);
        PrepararBloqueos(compra, producto, variante);

        await _service.AnularAsync(1, "Reversión controlada");

        Assert.Equal(3, variante.Cantidad);
        Assert.Equal(10, producto.Cantidad);
    }

    [Fact]
    public async Task AnularAsync_Variante_Sin_Stock_Suficiente_Lanza_Excepcion()
    {
        var producto = ProductoDePrueba(cantidad: 12);
        var variante = new ProductoVariante { Id = 8, ProductoId = 1, Sku = "M185-BLK", Cantidad = 2, Activo = true };
        var compra = CompraDePrueba(cantidad: 5, varianteId: 8, estado: EstadoDocumento.Confirmada);
        PrepararBloqueos(compra, producto, variante);
        _inventarioConcurrencyMock
            .Setup(c => c.BloquearYValidarInventarioAsync(It.IsAny<IEnumerable<InventarioDemanda>>(), true))
            .ThrowsAsync(new BusinessRuleException("Stock insuficiente."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, "motivo"));
        Assert.Equal(2, variante.Cantidad);
    }

    [Fact]
    public async Task AnularAsync_Sin_Motivo_Lanza_Excepcion()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.AnularAsync(1, ""));
    }

    [Fact]
    public async Task CreateAsync_Recalcula_Totales_Con_Motor_De_Calculo()
    {
        var producto = ProductoDePrueba();
        producto.Variantes.Add(new ProductoVariante { Id = 8, ProductoId = producto.Id, Sku = "TEC-0000000001", Cantidad = producto.Cantidad, Costo = producto.Costo, Precio = producto.Precio, UmbralStockBajo = producto.UmbralStockBajo, EsTecnica = true, Activo = true });
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _compraRepoMock.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);
        _calculoMock.Setup(c => c.CalcularCompraAsync(It.IsAny<List<DetalleCalculoInput>>(), It.IsAny<int?>()))
            .ReturnsAsync(new ResultadoCalculoDto { Subtotal = 20, TotalDescuento = 0, TotalImpuesto = 3, Total = 23 });
        Compra? creada = null;
        _compraRepoMock.Setup(r => r.AddAsync(It.IsAny<Compra>())).Callback<Compra>(c => creada = c).Returns(Task.CompletedTask);
        var dto = new CreateCompraDto
        {
            ProveedorNombre = "Proveedor X",
            Detalles = new List<CompraDetalleInputDto> { new() { ProductoId = 1, Cantidad = 2, CostoUnitario = 10 } }
        };

        await _service.CreateAsync(dto);

        Assert.NotNull(creada);
        Assert.Equal(20, creada!.Subtotal);
        Assert.Equal(0, creada.Descuento);
        Assert.Equal(3, creada.Impuesto);
        Assert.Equal(23, creada.Total);
        Assert.Equal(1, creada.MetodoPagoId);
    }

    [Fact]
    public async Task CreateAsync_MetodoAdministrable_Nuevo_Conserva_Id_Y_Proyecta_Legacy_Otro()
    {
        var metodo = new CatalogoMetodoPago
        {
            Id = 9,
            Codigo = "QR_EMPRESARIAL",
            Nombre = "QR empresarial",
            Activo = true
        };
        _compraRepoMock
            .Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("QR_EMPRESARIAL"))
            .ReturnsAsync(metodo);

        var producto = ProductoDePrueba();
        producto.Variantes.Add(new ProductoVariante { Id = 8, ProductoId = producto.Id, Sku = "TEC-0000000001", Cantidad = 10, Costo = 5, Precio = 10, EsTecnica = true, Activo = true });
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _compraRepoMock.Setup(r => r.ContarTodasAsync()).ReturnsAsync(0);
        _calculoMock.Setup(c => c.CalcularCompraAsync(It.IsAny<List<DetalleCalculoInput>>(), It.IsAny<int?>()))
            .ReturnsAsync(new ResultadoCalculoDto { Subtotal = 10, Total = 10 });
        Compra? creada = null;
        _compraRepoMock.Setup(r => r.AddAsync(It.IsAny<Compra>())).Callback<Compra>(c => creada = c).Returns(Task.CompletedTask);

        await _service.CreateAsync(new CreateCompraDto
        {
            ProveedorNombre = "Proveedor X",
            MetodoPago = "QR_EMPRESARIAL",
            Detalles = new List<CompraDetalleInputDto> { new() { ProductoId = 1, Cantidad = 1, CostoUnitario = 10 } }
        });

        Assert.NotNull(creada);
        Assert.Equal(9, creada!.MetodoPagoId);
        Assert.Same(metodo, creada.MetodoPagoCatalogo);
        Assert.Equal(MetodoPago.Otro, creada.MetodoPago);
    }

    [Fact]
    public async Task CreateAsync_MetodoPago_No_Existe_Falla_Cerrado()
    {
        _compraRepoMock
            .Setup(r => r.GetMetodoPagoPorCodigoONombreAsync("INEXISTENTE"))
            .ReturnsAsync((CatalogoMetodoPago?)null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(new CreateCompraDto
        {
            MetodoPago = "INEXISTENTE"
        }));

        Assert.Contains("no existe en el catálogo", ex.Message);
        _compraRepoMock.Verify(r => r.AddAsync(It.IsAny<Compra>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_Incluye_Imagen_Principal_Del_Producto()
    {
        var producto = ProductoDePrueba();
        producto.Imagenes.Add(new ProductoImagen { Id = 10, Url = "https://res.cloudinary.com/demo/image/upload/producto-principal.webp", EsPrincipal = true, Orden = 0 });
        var compra = new Compra { Id = 7, NumeroCompra = "COM-000007", ProveedorNombre = "Proveedor X" };
        compra.Detalles.Add(new CompraDetalle
        {
            Id = 4, ProductoId = producto.Id, Producto = producto, ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca, ProductoModeloSnapshot = producto.Modelo,
            Cantidad = 1, CostoUnitario = 5, Subtotal = 5
        });
        _compraRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(compra);

        var resultado = await _service.GetByIdAsync(7);

        Assert.Equal("https://res.cloudinary.com/demo/image/upload/producto-principal.webp", resultado!.Detalles.Single().ProductoImagenPrincipalUrl);
    }
}
