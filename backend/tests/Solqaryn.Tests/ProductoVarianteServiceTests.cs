using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoVarianteServiceTests
{
    private readonly Mock<IProductoVarianteRepository> _repository = new();
    private readonly Mock<IProductoRepository> _productoRepository = new();
    private readonly Mock<ICatalogoProductoService> _catalogoService = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly ProductoVarianteService _service;
    private readonly Producto _producto;

    public ProductoVarianteServiceTests()
    {
        _producto = new Producto { Id = 1, Nombre = "Cobertor", Marca = "Samsung", Modelo = "S24 Ultra", Cantidad = 0, Costo = 0, Precio = 220, Activo = true };
        _currentUser.SetupGet(x => x.UsuarioId).Returns(2);
        _currentUser.SetupGet(x => x.NombreUsuario).Returns("admin");
        _productoRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(_producto);
        _productoRepository.Setup(r => r.GetByIdForUpdateAsync(1)).ReturnsAsync(_producto);
        _productoRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(new List<ProductoVariante>());
        _catalogoService.Setup(s => s.ValidarSeleccionProductoAsync(
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        _service = new ProductoVarianteService(
            _repository.Object,
            _productoRepository.Object,
            _catalogoService.Object,
            _currentUser.Object,
            new FakeUnitOfWork(),
            _auditoria.Object);
    }

    [Fact]
    public async Task CreateAsync_Normaliza_Sku_Y_Registra_Dimensiones()
    {
        ProductoVariante? capturada = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => { capturada = v; v.Id = 31; })
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetByIdAsync(31)).ReturnsAsync(() =>
        {
            capturada!.Producto = _producto;
            capturada.Marca = new Marca { Id = 2, Nombre = "Samsung" };
            capturada.Modelo = new Modelo { Id = 3, MarcaId = 2, Nombre = "S24 Ultra" };
            capturada.Color = new Color { Id = 9, Nombre = "Negro", CodigoVisual = "#111111" };
            capturada.Talla = new Talla { Id = 12, Nombre = "XL" };
            return capturada;
        });
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(() => capturada is null ? new() : new() { capturada });

        var resultado = await _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            MarcaId = 2,
            ModeloId = 3,
            ColorId = 9,
            TallaId = 12,
            Sku = " case-s24u-blk-xl ",
            CodigoBarras = " 123456 ",
            Cantidad = 4,
            UmbralStockBajo = 1,
            Costo = 100,
            Precio = 220
        });

        Assert.Equal("CASE-S24U-BLK-XL", resultado.Sku);
        Assert.Equal("123456", resultado.CodigoBarras);
        Assert.Equal(2, resultado.MarcaId);
        Assert.Equal(3, resultado.ModeloId);
        Assert.Equal(9, resultado.ColorId);
        Assert.Equal(12, resultado.TallaId);
        Assert.Contains("S24 Ultra", resultado.Etiqueta);
        Assert.Contains("XL", resultado.Etiqueta);
        Assert.Equal(4, resultado.Cantidad);
        Assert.Equal(4, _producto.Cantidad);
        _catalogoService.Verify(s => s.ValidarSeleccionProductoAsync(9, 12, 2, 3), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Productos,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            31,
            "ProductoVariante",
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Sku_Vacio_Genera_Sku_En_Backend()
    {
        ProductoVariante? capturada = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => { capturada = v; v.Id = 41; })
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetByIdAsync(41)).ReturnsAsync(() =>
        {
            capturada!.Producto = _producto;
            capturada.Color = new Color { Id = 9, Nombre = "Negro" };
            return capturada;
        });
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(() => capturada is null ? new() : new() { capturada });

        var resultado = await _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            ColorId = 9,
            Sku = "   ",
            Cantidad = 1,
            Costo = 100,
            Precio = 220
        });

        Assert.StartsWith("VAR-000001-", resultado.Sku);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Sku));
    }

    [Fact]
    public async Task CreateAsync_Misma_Color_Diferente_Modelo_Es_Valido()
    {
        _repository.Setup(r => r.GetByCombinacionAsync(1, 2, 3, 9, null))
            .ReturnsAsync((ProductoVariante?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => v.Id = 51)
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetByIdAsync(51)).ReturnsAsync(new ProductoVariante
        {
            Id = 51,
            ProductoId = 1,
            Producto = _producto,
            MarcaId = 2,
            Marca = new Marca { Id = 2, Nombre = "Samsung" },
            ModeloId = 3,
            Modelo = new Modelo { Id = 3, MarcaId = 2, Nombre = "S24 Ultra" },
            ColorId = 9,
            Color = new Color { Id = 9, Nombre = "Negro" },
            Sku = "S24-NEGRO",
            Precio = 220
        });

        var resultado = await _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            MarcaId = 2,
            ModeloId = 3,
            ColorId = 9,
            Sku = "S24-NEGRO",
            Precio = 220
        });

        Assert.Equal(3, resultado.ModeloId);
        _repository.Verify(r => r.GetByCombinacionAsync(1, 2, 3, 9, null), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Combinacion_Exacta_Duplicada_Lanza_Excepcion()
    {
        _repository.Setup(r => r.GetByCombinacionAsync(1, 2, 3, 9, 12))
            .ReturnsAsync(new ProductoVariante { Id = 70, ProductoId = 1 });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            MarcaId = 2,
            ModeloId = 3,
            ColorId = 9,
            TallaId = 12,
            Precio = 220
        }));

        _repository.Verify(r => r.AddAsync(It.IsAny<ProductoVariante>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Sku_Duplicado_Lanza_Excepcion()
    {
        _repository.Setup(r => r.GetBySkuAsync("CASE-S24U-BLK"))
            .ReturnsAsync(new ProductoVariante { Id = 77, ProductoId = 2, Sku = "CASE-S24U-BLK" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(1, new CreateProductoVarianteDto
        {
            ColorId = 9,
            Sku = "CASE-S24U-BLK",
            Cantidad = 0,
            Costo = 100,
            Precio = 220
        }));

        _repository.Verify(r => r.AddAsync(It.IsAny<ProductoVariante>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Con_Stock_No_Elimina()
    {
        var variante = new ProductoVariante
        {
            Id = 5,
            ProductoId = 1,
            Sku = "CASE-S24U-BLK",
            Cantidad = 2,
            Activo = true
        };
        _repository.Setup(r => r.GetByIdForUpdateAsync(5)).ReturnsAsync(variante);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.DeleteAsync(1, 5));
        _repository.Verify(r => r.Update(It.IsAny<ProductoVariante>()), Times.Never);
    }

    [Fact]
    public async Task CambiarEstadoAsync_Conserva_Stock_Fisico_Y_Usa_Precio_Activo()
    {
        var negra = new ProductoVariante { Id = 5, ProductoId = 1, Sku = "BLACK", Cantidad = 3, Costo = 90, Precio = 210, Activo = true };
        var azul = new ProductoVariante { Id = 6, ProductoId = 1, Sku = "BLUE", Cantidad = 2, Costo = 110, Precio = 230, Activo = true };
        _repository.Setup(r => r.GetByIdForUpdateAsync(5)).ReturnsAsync(negra);
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(new List<ProductoVariante> { negra, azul });
        _repository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(negra);

        await _service.CambiarEstadoAsync(1, 5, false);

        Assert.False(negra.Activo);
        Assert.Equal(5, _producto.Cantidad);
        Assert.Equal(98, _producto.Costo);
        Assert.Equal(230, _producto.Precio);
    }

    [Fact]
    public async Task AsegurarTecnicaAsync_Deja_Todas_Las_Dimensiones_Null()
    {
        _producto.Cantidad = 3;
        _producto.Costo = 10;
        _producto.Precio = 20;
        ProductoVariante? tecnica = null;
        _repository.Setup(r => r.GetTecnicaByProductoIdAsync(1, true)).ReturnsAsync((ProductoVariante?)null);
        _repository.Setup(r => r.AddAsync(It.IsAny<ProductoVariante>()))
            .Callback<ProductoVariante>(v => { tecnica = v; v.Id = 91; })
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.GetByProductoIdAsync(1, true)).ReturnsAsync(new List<ProductoVariante>());

        var resultado = await _service.AsegurarTecnicaAsync(1);

        Assert.True(resultado.EsTecnica);
        Assert.Null(resultado.MarcaId);
        Assert.Null(resultado.ModeloId);
        Assert.Null(resultado.ColorId);
        Assert.Null(resultado.TallaId);
        Assert.NotNull(tecnica);
    }
}
