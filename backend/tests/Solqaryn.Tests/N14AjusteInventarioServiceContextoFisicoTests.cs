using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioServiceContextoFisicoTests
{
    [Fact]
    public async Task CreateAsync_PersisteAlmacenYUbicacionEnDetalle()
    {
        var ajustes = new Mock<IAjusteInventarioRepository>();
        var productos = new Mock<IProductoRepository>();
        var variantes = new Mock<IProductoVarianteRepository>();
        var movimientos = new Mock<IMovimientoInventarioRepository>();
        var concurrency = new Mock<IInventarioConcurrencyService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var currentUser = new Mock<ICurrentUserService>();
        var auditoria = new Mock<IAuditoriaService>();

        var producto = new Producto
        {
            Id = 10,
            Nombre = "Producto físico",
            Cantidad = 5,
            Costo = 4m
        };
        var variante = new ProductoVariante
        {
            Id = 20,
            ProductoId = producto.Id,
            Sku = "SKU-20",
            Cantidad = 5
        };
        producto.Variantes.Add(variante);

        productos.Setup(x => x.GetByIdAsync(producto.Id)).ReturnsAsync(producto);
        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(operation => operation());
        currentUser.SetupGet(x => x.UsuarioId).Returns(99);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("tester");
        auditoria
            .Setup(x => x.RegistrarAsync(
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
            .Returns(Task.CompletedTask);

        AjusteInventario? creado = null;
        ajustes
            .Setup(x => x.AddAsync(It.IsAny<AjusteInventario>()))
            .Callback<AjusteInventario>(ajuste =>
            {
                ajuste.Id = 77;
                creado = ajuste;
            })
            .Returns(Task.CompletedTask);
        ajustes.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var service = new AjusteInventarioService(
            ajustes.Object,
            productos.Object,
            variantes.Object,
            movimientos.Object,
            concurrency.Object,
            unitOfWork.Object,
            currentUser.Object,
            auditoria.Object);

        await service.CreateAsync(new CreateAjusteInventarioDto
        {
            Motivo = "Conteo por ubicación",
            Detalles =
            {
                new AjusteInventarioDetalleInputDto
                {
                    ProductoId = producto.Id,
                    ProductoVarianteId = variante.Id,
                    AlmacenId = 30,
                    UbicacionAlmacenId = 40,
                    CantidadObjetivo = 8
                }
            }
        });

        var detalle = Assert.Single(Assert.IsType<AjusteInventario>(creado).Detalles);
        Assert.Equal(30, detalle.AlmacenId);
        Assert.Equal(40, detalle.UbicacionAlmacenId);
    }
}
