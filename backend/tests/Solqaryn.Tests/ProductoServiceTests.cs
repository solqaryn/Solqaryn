using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoServiceTests
{
    private readonly Mock<IProductoRepository> _productoRepoMock = new();
    private readonly Mock<ICategoriaRepository> _categoriaRepoMock = new();
    private readonly Mock<IImageStorageService> _imageStorageMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAuditoriaService> _auditoriaMock = new();
    private readonly ProductoService _service;

    public ProductoServiceTests()
    {
        _currentUserMock.Setup(c => c.UsuarioId).Returns(2);
        _currentUserMock.Setup(c => c.NombreUsuario).Returns("vendedor1");

        _service = new ProductoService(
            _productoRepoMock.Object,
            _categoriaRepoMock.Object,
            _imageStorageMock.Object,
            _currentUserMock.Object,
            _auditoriaMock.Object);
    }

    [Fact]
    public async Task CreateAsync_Registra_Usuario_Creador()
    {
        Producto? productoCreado = null;
        _productoRepoMock.Setup(r => r.AddAsync(It.IsAny<Producto>()))
            .Callback<Producto>(p => productoCreado = p)
            .Returns(Task.CompletedTask);
        _productoRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        await _service.CreateAsync(new CreateProductoDto
        {
            Nombre = "Teclado",
            Marca = "HP",
            Modelo = "K100",
            Cantidad = 5,
            Costo = 10,
            Precio = 20
        });

        Assert.NotNull(productoCreado);
        Assert.Equal(2, productoCreado!.CreadoPorUsuarioId);
        Assert.Equal("vendedor1", productoCreado.CreadoPorNombreUsuario);
    }

    [Fact]
    public async Task CreateAsync_Con_Categoria_Inactiva_Lanza_Excepcion()
    {
        _categoriaRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Categoria { Id = 1, Nombre = "X", Activa = false });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.CreateAsync(new CreateProductoDto
        {
            Nombre = "Teclado",
            Marca = "HP",
            Modelo = "K100",
            Cantidad = 5,
            Costo = 10,
            Precio = 20,
            CategoriaId = 1
        }));
    }

    [Fact]
    public async Task UpdateAsync_Excede_Maximo_De_Imagenes_Lanza_Excepcion()
    {
        var producto = new Producto { Id = 1, Nombre = "Mouse", Marca = "X", Modelo = "Y" };
        for (int i = 0; i < 4; i++)
            producto.Imagenes.Add(new ProductoImagen { Id = i + 1, Url = $"url{i}", PublicId = $"pid{i}", Orden = i, EsPrincipal = i == 0 });

        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);

        var dto = new UpdateProductoDto
        {
            Nombre = "Mouse",
            Marca = "X",
            Modelo = "Y",
            Cantidad = 1,
            Costo = 1,
            Precio = 2,
            ImagenesNuevas = new List<IFormFile>
            {
                Mock.Of<IFormFile>(f => f.Length == 100 && f.ContentType == "image/jpeg"),
                Mock.Of<IFormFile>(f => f.Length == 100 && f.ContentType == "image/jpeg")
            }
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(1, dto));
    }

    [Theory]
    [InlineData("maximo", "máximo")]
    [InlineData("archivo", "Solo se permiten")]
    [InlineData("principal-eliminada", "galería general")]
    [InlineData("principal-variante", "galería general")]
    [InlineData("principal-cero", "galería general")]
    [InlineData("eliminar-ajena", "galería general")]
    [InlineData("eliminar-variante", "galería general")]
    [InlineData("eliminar-cero", "galería general")]
    public async Task UpdateAsync_GaleriaInvalida_NoModificaNiInvocaStorage(
        string caso, string mensaje)
    {
        var producto = new Producto { Id = 1, Nombre = "Original", EmpresaId = 23 };
        for (var i = 1; i <= 5; i++)
            producto.Imagenes.Add(new ProductoImagen
            {
                Id = i, Url = $"url{i}", PublicId = $"pid{i}", Orden = i, EsPrincipal = i == 1
            });
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 6, ProductoVarianteId = 77, Url = "variante", PublicId = "variante"
        });
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        var dto = new UpdateProductoDto
        {
            Nombre = "No persistir",
            ImagenesAEliminarIds = new List<int> { 1 }
        };
        if (caso == "maximo")
            dto.ImagenesNuevas = new List<IFormFile>
            {
                Mock.Of<IFormFile>(f => f.Length == 100 && f.ContentType == "image/jpeg" && f.FileName == "a.jpg"),
                Mock.Of<IFormFile>(f => f.Length == 100 && f.ContentType == "image/jpeg" && f.FileName == "b.jpg")
            };
        if (caso == "archivo")
            dto.ImagenesNuevas = new List<IFormFile>
            {
                Mock.Of<IFormFile>(f => f.Length == 100 && f.ContentType == "application/pdf" && f.FileName == "a.pdf")
            };
        if (caso == "principal-eliminada") dto.ImagenPrincipalId = 1;
        if (caso == "principal-variante") dto.ImagenPrincipalId = 6;
        if (caso == "principal-cero") dto.ImagenPrincipalId = 0;
        if (caso == "eliminar-ajena") dto.ImagenesAEliminarIds = new List<int> { 1, 999 };
        if (caso == "eliminar-variante") dto.ImagenesAEliminarIds = new List<int> { 1, 6 };
        if (caso == "eliminar-cero") dto.ImagenesAEliminarIds = new List<int> { 1, 0 };

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.UpdateAsync(1, dto));
        Assert.Contains(mensaje, error.Message);
        Assert.Equal("Original", producto.Nombre);
        Assert.Equal(6, producto.Imagenes.Count);
        Assert.True(producto.Imagenes.Single(i => i.Id == 1).EsPrincipal);
        Assert.Equal(77, producto.Imagenes.Single(i => i.Id == 6).ProductoVarianteId);
        Assert.Empty(_imageStorageMock.Invocations);
        Assert.Empty(_auditoriaMock.Invocations);
        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_GaleriaValida_SinCambioDeArchivos_PersistePrincipal()
    {
        var producto = new Producto { Id = 1, Nombre = "Original" };
        producto.Imagenes.Add(new ProductoImagen { Id = 1, EsPrincipal = true });
        producto.Imagenes.Add(new ProductoImagen { Id = 2 });
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _productoRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var resultado = await _service.UpdateAsync(1,
            new UpdateProductoDto { Nombre = "Actualizado", ImagenPrincipalId = 2 });

        Assert.NotNull(resultado);
        Assert.Equal("Actualizado", producto.Nombre);
        Assert.False(producto.Imagenes.Single(i => i.Id == 1).EsPrincipal);
        Assert.True(producto.Imagenes.Single(i => i.Id == 2).EsPrincipal);
        Assert.Empty(_imageStorageMock.Invocations);
        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Marca_Eliminacion_Logica_Y_Conserva_Imagenes()
    {
        var producto = new Producto
        {
            Id = 1,
            Nombre = "Mouse",
            Marca = "X",
            Modelo = "Y",
            Activo = true
        };
        producto.Imagenes.Add(new ProductoImagen { Id = 1, Url = "u1", PublicId = "pid1" });
        producto.Imagenes.Add(new ProductoImagen { Id = 2, Url = "u2", PublicId = "pid2" });

        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(producto);
        _productoRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var eliminado = await _service.DeleteAsync(1);

        Assert.True(eliminado);
        Assert.True(producto.Eliminado);
        Assert.False(producto.Activo);
        Assert.Equal(2, producto.EliminadoPorUsuarioId);
        Assert.NotNull(producto.FechaEliminacion);
        Assert.Equal(2, producto.Imagenes.Count);
        _productoRepoMock.Verify(r => r.Update(producto), Times.Never);
        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _imageStorageMock.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_HttpConHeaderEmpresaSinMarcaRbac_RechazaAntesDeLeerDatos()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "23";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var service = new ProductoService(
            _productoRepoMock.Object, _categoriaRepoMock.Object, _imageStorageMock.Object,
            _currentUserMock.Object, _auditoriaMock.Object,
            httpContextAccessor: accessor.Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DeleteAsync(1));

        _productoRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_HttpConMarcaRbacServerSide_PermiteBajaLogica()
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 23;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var product = new Producto
        {
            Id = 1, EmpresaId = 23, Nombre = "Mouse", Marca = "X", Modelo = "Y", Activo = true
        };
        _productoRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);
        _productoRepoMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);
        var service = new ProductoService(
            _productoRepoMock.Object, _categoriaRepoMock.Object, _imageStorageMock.Object,
            _currentUserMock.Object, _auditoriaMock.Object,
            httpContextAccessor: accessor.Object);

        Assert.True(await service.DeleteAsync(1));
        Assert.True(product.Eliminado);
        Assert.False(product.Activo);
        _productoRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
