using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ProductoStockInicialTests
{
    [Theory]
    [InlineData(7, true, TipoAlmacen.Tienda, true)]
    [InlineData(7, true, TipoAlmacen.Bodega, true)]
    [InlineData(8, true, TipoAlmacen.Tienda, false)]
    [InlineData(7, false, TipoAlmacen.Tienda, false)]
    [InlineData(7, true, TipoAlmacen.Cuarentena, false)]
    public async Task CrearVariante_StockInicialRequiereAlmacenOperativoDelTenant(
        int empresaAlmacen, bool activo, TipoAlmacen tipo, bool permitido)
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(7, ModuloSistema.Inventario, AccionPermiso.Crear)).Returns(Task.CompletedTask);
        permisos.Setup(p => p.VerificarPermisoAsync(7, ModuloSistema.Almacenes, AccionPermiso.Ver)).Returns(Task.CompletedTask);
        var almacenes = new Mock<IAlmacenRepository>(MockBehavior.Strict);
        almacenes.Setup(a => a.GetByIdAsync(3)).ReturnsAsync(new Almacen
        {
            Id = 3, Activo = activo, Tipo = tipo, Sucursal = new Sucursal { EmpresaId = empresaAlmacen, Activa = true }
        });
        var variantes = new Mock<IProductoVarianteService>(MockBehavior.Strict);
        var solicitud = new CreateProductoVarianteDto { AlmacenInicialId = 3, Cantidad = 5, UmbralStockBajo = 2, MarcaId = 1, Precio = 10 };
        variantes.Setup(v => v.CreateAsync(9, solicitud)).ReturnsAsync(new ProductoVarianteDto
        {
            Id = 12, ProductoId = 9, Cantidad = 5, UmbralStockBajo = 2, Activo = true
        });
        var existencias = new Mock<IExistenciaVarianteService>(MockBehavior.Strict);
        existencias.Setup(e => e.CreateAsync(It.Is<CreateExistenciaVarianteDto>(d =>
            d.ProductoVarianteId == 12 && d.AlmacenId == 3 && d.StockFisico == 5 &&
            d.StockReservado == 0 && d.StockTransito == 0 && d.StockMinimo == 2 &&
            d.UbicacionAlmacenId == null))).ReturnsAsync(new ExistenciaVarianteDto { StockDisponible = 5 });

        using var services = new ServiceCollection()
            .AddSingleton(permisos.Object).AddSingleton(almacenes.Object)
            .AddSingleton(existencias.Object).AddSingleton<IUnitOfWork>(new FakeUnitOfWork()).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 7;
        var controller = new ProductoVariantesController(variantes.Object, Mock.Of<IProductoVarianteImagenService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        if (permitido)
        {
            Assert.IsType<CreatedAtActionResult>(await controller.Create(9, solicitud));
            variantes.Verify(v => v.CreateAsync(9, solicitud), Times.Once);
            existencias.Verify(e => e.CreateAsync(It.IsAny<CreateExistenciaVarianteDto>()), Times.Once);
        }
        else
        {
            await Assert.ThrowsAsync<BusinessRuleException>(() => controller.Create(9, solicitud));
            variantes.Verify(v => v.CreateAsync(It.IsAny<int>(), It.IsAny<CreateProductoVarianteDto>()), Times.Never);
            existencias.Verify(e => e.CreateAsync(It.IsAny<CreateExistenciaVarianteDto>()), Times.Never);
        }
    }
}
