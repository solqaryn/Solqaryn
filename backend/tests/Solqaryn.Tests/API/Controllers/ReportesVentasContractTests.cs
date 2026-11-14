using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Solqaryn.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesVentasContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionRutaYPermisoVentasVer()
    {
        var type = typeof(ReportesVentasController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("ventas/reportes", type.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var methodName in new[] { nameof(ReportesVentasController.GetResumen), nameof(ReportesVentasController.GetDetalle) })
        {
            var method = type.GetMethod(methodName);
            Assert.NotNull(method);
            var permiso = Assert.Single(method!.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
            Assert.Equal((int)ModuloSistema.Ventas, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
            Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
        }
    }

    [Fact]
    public async Task Resumen_ConsultaValidaRegistraAuditoriaVentasVerConCorrelationId()
    {
        var service = new Mock<IReporteVentasService>(MockBehavior.Strict);
        service.Setup(s => s.ObtenerResumenAsync(It.IsAny<ReporteVentasFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReporteVentasResumenDto());
        var auditoria = CreateAuditoriaStrictMock();
        var controller = new ReportesVentasController(service.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "corr-n53-f2" }
            }
        };

        var result = await controller.GetResumen(new ReporteVentasFiltroDto());

        Assert.IsType<OkObjectResult>(result);
        VerifyAudit(auditoria, "resumen", "corr-n53-f2");
        service.VerifyAll();
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Detalle_ConsultaValidaRegistraAuditoriaVentasVerConCorrelationId()
    {
        var service = new Mock<IReporteVentasService>(MockBehavior.Strict);
        service.Setup(s => s.ObtenerDetallePaginadoAsync(It.IsAny<ReporteVentasFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ReporteVentasDetalleDto>
            {
                Items = new List<ReporteVentasDetalleDto>(),
                TotalCount = 0,
                Page = 1,
                PageSize = 10
            });
        var auditoria = CreateAuditoriaStrictMock();
        var controller = new ReportesVentasController(service.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "corr-n53-f2-detalle" }
            }
        };

        var result = await controller.GetDetalle(new ReporteVentasFiltroDto());

        Assert.IsType<OkObjectResult>(result);
        VerifyAudit(auditoria, "detalle", "corr-n53-f2-detalle");
        service.VerifyAll();
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resumen_RechazaRangoInvertidoAntesDeConsultarServicio()
    {
        var service = new Mock<IReporteVentasService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var controller = new ReportesVentasController(service.Object, auditoria.Object);
        var filtro = new ReporteVentasFiltroDto
        {
            Desde = new DateTime(2026, 9, 10),
            Hasta = new DateTime(2026, 9, 1)
        };

        var result = await controller.GetResumen(filtro);

        Assert.IsType<BadRequestObjectResult>(result);
        service.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resumen_NoAdministradorNoPuedeForjarVendedorIdParaExpandirAlcance()
    {
        await using var context = CreateContext();
        context.Ventas.AddRange(
            Confirmada("OWN", 7, 100m),
            Confirmada("FORGED", 99, 900m));
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(7, 1, 2, "Operador", EsAdministrador: false));
        var service = new ReporteVentasService(context, scope.Object);

        var result = await service.ObtenerResumenAsync(new ReporteVentasFiltroDto { VendedorId = 99 });

        Assert.Equal(100m, result.Total);
        Assert.Equal(100m, result.Subtotal);
    }

    [Fact]
    public async Task Detalle_FiltraSucursalPorAlmacenYPreservaSnapshotsHistoricosNulos()
    {
        await using var context = CreateContext();
        var venta = Confirmada("V-001", 7, 30m);
        var empresa1 = new Empresa("Empresa 1") { Id = 1 };
        var empresa2 = new Empresa("Empresa 2") { Id = 2 };
        var sucursal1 = new Sucursal { Id = 1, EmpresaId = empresa1.Id, Codigo = "S-1", Nombre = "Sucursal 1" };
        // Son dos sucursales de la misma empresa: la dimensión física no puede cruzar tenants.
        var sucursal2 = new Sucursal { Id = 2, EmpresaId = empresa1.Id, Codigo = "S-2", Nombre = "Sucursal 2" };
        var almacen1 = new Almacen
        {
            Id = 11,
            SucursalId = sucursal1.Id,
            Sucursal = sucursal1,
            Codigo = "A-1",
            Nombre = "Almacen 1"
        };
        var almacen2 = new Almacen
        {
            Id = 22,
            SucursalId = sucursal2.Id,
            Sucursal = sucursal2,
            Codigo = "A-2",
            Nombre = "Almacen 2"
        };
        var producto1 = new Producto { Id = 101, Nombre = "Producto 1", Marca = "Marca 1", Modelo = "Modelo 1" };
        var producto2 = new Producto { Id = 202, Nombre = "Producto 2", Marca = "Marca 2", Modelo = "Modelo 2" };
        context.AddRange(venta, empresa1, empresa2, sucursal1, sucursal2, almacen1, almacen2, producto1, producto2);
        await context.SaveChangesAsync();

        context.VentaDetalles.AddRange(
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = producto1.Id,
                Producto = producto1,
                AlmacenId = almacen1.Id,
                Almacen = almacen1,
                Cantidad = 1,
                PrecioUnitario = 10m,
                CostoUnitarioSnapshot = 5m,
                Subtotal = 10m,
                UtilidadBruta = 5m,
                ProductoNombreSnapshot = "Producto historico sucursal 1",
                ProductoMarcaSnapshot = "Marca historica 1",
                ProductoModeloSnapshot = "Modelo historico 1"
            },
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = producto2.Id,
                Producto = producto2,
                AlmacenId = almacen2.Id,
                Almacen = almacen2,
                Cantidad = 1,
                PrecioUnitario = 20m,
                CostoUnitarioSnapshot = 8m,
                Subtotal = 20m,
                UtilidadBruta = 12m,
                ProductoNombreSnapshot = "Producto historico sucursal 2",
                ProductoMarcaSnapshot = "Marca historica 2",
                ProductoModeloSnapshot = "Modelo historico 2",
                ProductoColorSnapshot = null,
                ProductoTallaSnapshot = null,
                ProductoSkuSnapshot = null
            });
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 1, 1, "Administrador", EsAdministrador: true));
        var service = new ReporteVentasService(context, scope.Object);

        var result = await service.ObtenerDetallePaginadoAsync(new ReporteVentasFiltroDto
        {
            SucursalId = 2,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal(2, item.SucursalId);
        Assert.Equal("Producto historico sucursal 2", item.ProductoNombre);
        Assert.Equal("Marca historica 2", item.ProductoMarca);
        Assert.Equal("Modelo historico 2", item.ProductoModelo);
        Assert.Null(item.ProductoColor);
        Assert.Null(item.ProductoTalla);
        Assert.Null(item.ProductoSku);
    }

    [Fact]
    public async Task A_B_ResumenRechazaVentasDeOtraEmpresaSinScopeYPreservaSeleccion()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-sales-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        var ventaA = Confirmada("SALE-A", 7, 100m);
        db.Ventas.Add(ventaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 2;
        var ventaB = Confirmada("SALE-B", 99, 900m);
        db.Ventas.Add(ventaB);
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(7, 1, 1, "A", EsAdministrador: true));
        scope.Setup(s => s.ObtenerActualAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(99, 2, 1, "B", EsAdministrador: true));
        var service = new ReporteVentasService(db, scope.Object);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        Assert.Equal(100m, (await service.ObtenerResumenAsync(new ReporteVentasFiltroDto())).Total);
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 2;
        Assert.Equal(900m, (await service.ObtenerResumenAsync(new ReporteVentasFiltroDto())).Total);
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        Assert.Equal(0m, (await service.ObtenerResumenAsync(new ReporteVentasFiltroDto())).Total);
    }

    [Fact]
    public async Task CapabilityPublica_SinMarcaRbac_NoAutorizaReportePrivadoDeVentas()
    {
        var http = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-sales-public-denied-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        db.Ventas.Add(Confirmada("PRIVATE-SALE", 7, 100m));
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(7, 1, 1, "A", EsAdministrador: true));
        var service = new ReporteVentasService(db, scope.Object);
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);

        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 1;
        Assert.Equal(0m, (await service.ObtenerResumenAsync(new ReporteVentasFiltroDto())).Total);
        http.Items.Remove(VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey);

        http.Request.Path = "/facturas/publico/token/pdf";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 1;
        Assert.Equal(0m, (await service.ObtenerResumenAsync(new ReporteVentasFiltroDto())).Total);
        scope.Verify(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IAuditoriaService> CreateAuditoriaStrictMock()
    {
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        auditoria.Setup(a => a.RegistrarAsync(
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
        return auditoria;
    }

    private static void VerifyAudit(Mock<IAuditoriaService> auditoria, string report, string correlationId)
    {
        auditoria.Verify(a => a.RegistrarAsync(
            ModuloSistema.Ventas,
            AccionPermiso.Ver,
            It.Is<string>(descripcion => descripcion.Contains($"'{report}'", StringComparison.Ordinal)),
            null,
            "ReportesVentas",
            null,
            It.Is<object>(valores =>
                ReadProperty(valores, "CorrelationId") == correlationId &&
                ReadProperty(valores, "Reporte") == report),
            null,
            "Exito",
            null), Times.Once);
    }

    private static string? ReadProperty(object value, string propertyName) =>
        value.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        return new AppDbContext(options, tenantHttpContextAccessor: new HttpContextAccessor
        {
            HttpContext = http
        });
    }

    private static Venta Confirmada(string numero, int usuarioId, decimal total) => new()
    {
        NumeroVenta = numero,
        Fecha = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc),
        Estado = EstadoDocumento.Confirmada,
        CreadoPorUsuarioId = usuarioId,
        CreadoPorNombreUsuario = $"U{usuarioId}",
        ImporteBruto = total,
        ImporteProductos = total,
        Subtotal = total,
        Total = total,
        CostoTotal = total / 2m,
        UtilidadBruta = total / 2m
    };
}
