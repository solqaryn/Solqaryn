using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Microsoft.AspNetCore.Http;
using Moq;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Services;

public sealed class ReporteComprasServiceTests
{
    [Fact]
    public async Task Variacion_precio_positiva_usa_precio_facturado_snapshot_en_misma_moneda()
    {
        await using var db = await CrearEscenarioAsync(10m, 12m, "HNL", "HNL", DateTime.UtcNow.Date, DateTime.UtcNow.Date);
        var item = Assert.Single((await CrearServicio(db).ObtenerDetallePaginadoAsync(new())).Items);

        Assert.Equal(10m, item.PrecioUnitarioOrdenado);
        Assert.Equal(12m, item.PrecioUnitarioFacturado);
        Assert.Equal("HNL", item.MonedaFactura);
        Assert.Equal(2m, item.VariacionPrecioAbsoluta);
    }

    [Fact]
    public async Task Variacion_precio_negativa_conserva_signo_y_no_calcula_porcentaje()
    {
        await using var db = await CrearEscenarioAsync(10m, 8m, "HNL", "HNL", DateTime.UtcNow.Date, DateTime.UtcNow.Date);
        var item = Assert.Single((await CrearServicio(db).ObtenerDetallePaginadoAsync(new())).Items);

        Assert.Equal(8m, item.PrecioUnitarioFacturado);
        Assert.Equal(-2m, item.VariacionPrecioAbsoluta);
    }

    [Fact]
    public async Task Moneda_incompatible_expone_precio_factual_pero_variacion_es_null_sin_fx()
    {
        await using var db = await CrearEscenarioAsync(10m, 12m, "HNL", "USD", DateTime.UtcNow.Date, DateTime.UtcNow.Date);
        var item = Assert.Single((await CrearServicio(db).ObtenerDetallePaginadoAsync(new())).Items);

        Assert.Equal(12m, item.PrecioUnitarioFacturado);
        Assert.Equal("USD", item.MonedaFactura);
        Assert.Null(item.VariacionPrecioAbsoluta);
    }

    [Fact]
    public async Task Recepcion_parcial_y_devolucion_confirmada_se_reportan_desde_hechos_persistidos()
    {
        await using var db = await CrearEscenarioAsync(10m, 10m, "HNL", "HNL", DateTime.UtcNow.Date, DateTime.UtcNow.Date);
        var item = Assert.Single((await CrearServicio(db).ObtenerDetallePaginadoAsync(new())).Items);

        Assert.Equal(8m, item.CantidadRecibida);
        Assert.Equal(6m, item.CantidadAceptada);
        Assert.Equal(1m, item.CantidadDanada);
        Assert.Equal(2m, item.CantidadFaltante);
        Assert.Equal(1m, item.CantidadSobrante);
        Assert.Equal(2m, item.CantidadDevueltaEfectiva);
    }

    [Fact]
    public async Task Devolucion_anulada_no_contamina_total_efectivo_por_defecto_y_es_consultable_explicitamente()
    {
        await using var db = await CrearEscenarioAsync(10m, 10m, "HNL", "HNL", DateTime.UtcNow.Date, DateTime.UtcNow.Date, incluirDevolucionAnulada: true);
        var service = CrearServicio(db);

        var efectivo = Assert.Single((await service.ObtenerDetallePaginadoAsync(new())).Items);
        Assert.Equal(2m, efectivo.CantidadDevueltaEfectiva);

        var anulada = Assert.Single((await service.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto
        {
            EstadoDevolucion = EstadoDevolucionProveedor.Anulada
        })).Items);
        Assert.Equal(3m, anulada.CantidadDevueltaEfectiva);
    }

    [Theory]
    [InlineData(-2, -2)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    public async Task Desviacion_entrega_es_diferencia_factual_de_dias(int offsetDias, int esperado)
    {
        var fechaEsperada = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
        var fechaRecepcion = fechaEsperada.AddDays(offsetDias);
        await using var db = await CrearEscenarioAsync(10m, 10m, "HNL", "HNL", fechaEsperada, fechaRecepcion);

        var item = Assert.Single((await CrearServicio(db).ObtenerDetallePaginadoAsync(new())).Items);
        Assert.Equal(fechaEsperada, item.FechaEsperadaEvaluadaUtc);
        Assert.Equal(fechaRecepcion, item.FechaRecepcionEvaluadaUtc);
        Assert.Equal(esperado, item.DesviacionEntregaDias);
    }

    [Fact]
    public async Task Filtros_invalidos_fallan_cerrado_antes_de_consultar()
    {
        await using var db = CrearContexto();
        var service = CrearServicio(db);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto
        {
            DesdeUtc = new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc),
            HastaUtc = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc)
        }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto { ProveedorId = 0 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto { Page = 0 }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto { PageSize = 101 }));
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n55-reporte-compras-{Guid.NewGuid():N}")
            .Options;
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        // IHttpContextAccessor real utiliza AsyncLocal; un fixture creado dentro
        // de una tarea async hija pierde el valor al regresar a la prueba.
        // Mantener el HttpContext verificado en un accesor determinista del fixture.
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        return new AppDbContext(options, tenantHttpContextAccessor: accessor.Object);
    }

    private static ReporteComprasService CrearServicio(AppDbContext db)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 1, 1, "Admin", EsAdministrador: true));
        return new ReporteComprasService(db, scope.Object);
    }

    [Fact]
    public async Task DosEmpresas_ReporteFiltraOrdenes_A_B_YRechazaAusenciaDeScope()
    {
        var http = new DefaultHttpContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-compras-ab-" + Guid.NewGuid().ToString("N"))
            .Options;
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(http);
        await using var db = new AppDbContext(
            options, tenantHttpContextAccessor: accessor.Object);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        var ordenA = new OrdenCompra
        {
            NumeroOrden = "ORDEN-A", ProveedorId = 101,
            ProveedorNombreSnapshot = "Proveedor-A", Moneda = "HNL"
        };
        var lineaA = new OrdenCompraDetalle
        {
            OrdenCompra = ordenA, ProductoId = 1001,
            ProductoNombreSnapshot = "Solo A", ProductoSkuSnapshot = "A"
        };
        lineaA.EstablecerValores(1m, 10m);
        ordenA.Detalles.Add(lineaA);
        db.Add(ordenA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 2;
        var ordenB = new OrdenCompra
        {
            NumeroOrden = "ORDEN-B", ProveedorId = 202,
            ProveedorNombreSnapshot = "Proveedor-B", Moneda = "HNL"
        };
        var lineaB = new OrdenCompraDetalle
        {
            OrdenCompra = ordenB, ProductoId = 2002,
            ProductoNombreSnapshot = "Solo B", ProductoSkuSnapshot = "B"
        };
        lineaB.EstablecerValores(1m, 90m);
        ordenB.Detalles.Add(lineaB);
        db.Add(ordenB);
        await db.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(7, 1, 1, "A", EsAdministrador: true));
        scope.Setup(x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(8, 2, 1, "B", EsAdministrador: true));
        var reportes = new ReporteComprasService(db, scope.Object);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 1;
        var resultadoA = await reportes.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto());
        Assert.Equal("Solo A", Assert.Single(resultadoA.Items).ProductoNombre);
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 2;
        var resultadoB = await reportes.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto());
        Assert.Equal("Solo B", Assert.Single(resultadoB.Items).ProductoNombre);
        http.Items.Remove(VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            reportes.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto()));

        // Un enlace publico con empresa valida no es permiso RBAC de Compras.
        http.Request.Path = "/tienda/productos";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 2;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            reportes.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto()));
        http.Items.Remove(VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey);
        http.Request.Path = "/facturas/publico/token/pdf";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 2;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            reportes.ObtenerDetallePaginadoAsync(new ReporteComprasFiltroDto()));
        scope.Verify(x => x.ObtenerActualAsync(2, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<AppDbContext> CrearEscenarioAsync(
        decimal precioOrdenado,
        decimal precioFacturado,
        string monedaOrden,
        string monedaFactura,
        DateTime fechaEsperada,
        DateTime fechaRecepcion,
        bool incluirDevolucionAnulada = false)
    {
        var db = CrearContexto();
        var orden = new OrdenCompra
        {
            Id = 1,
            NumeroOrden = "OC-N55-1",
            ProveedorId = 50,
            ProveedorNombreSnapshot = "Proveedor histórico",
            Moneda = monedaOrden,
            FechaEsperadaUtc = fechaEsperada,
            FechaCreacion = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc)
        };
        var linea = new OrdenCompraDetalle
        {
            Id = 10,
            OrdenCompraId = orden.Id,
            OrdenCompra = orden,
            ProductoId = 100,
            ProductoNombreSnapshot = "Producto histórico",
            ProductoSkuSnapshot = "SKU-N55"
        };
        linea.EstablecerValores(10m, precioOrdenado);
        orden.Detalles.Add(linea);

        var factura = new FacturaProveedor
        {
            Id = 20,
            NumeroFactura = "FP-N55-1",
            ProveedorId = orden.ProveedorId,
            OrdenCompraId = orden.Id,
            OrdenCompra = orden,
            ProveedorNombreSnapshot = orden.ProveedorNombreSnapshot,
            Moneda = monedaFactura,
            FechaEmisionUtc = fechaRecepcion
        };
        var lineaFactura = new FacturaProveedorDetalle
        {
            Id = 21,
            FacturaProveedorId = factura.Id,
            FacturaProveedor = factura,
            OrdenCompraDetalleId = linea.Id,
            OrdenCompraDetalle = linea,
            ProductoId = linea.ProductoId,
            ProductoNombreSnapshot = linea.ProductoNombreSnapshot ?? "Producto histórico"
        };
        lineaFactura.EstablecerValores(8m, precioFacturado);
        factura.Detalles.Add(lineaFactura);
        factura.Registrar(1, "qa", fechaRecepcion);

        var recepcion = new RecepcionCompra
        {
            Id = 30,
            NumeroRecepcion = "RC-N55-1",
            OrdenCompraId = orden.Id,
            OrdenCompra = orden
        };
        var lineaRecepcion = new RecepcionCompraDetalle
        {
            Id = 31,
            RecepcionCompraId = recepcion.Id,
            RecepcionCompra = recepcion,
            OrdenCompraDetalleId = linea.Id,
            OrdenCompraDetalle = linea,
            ProductoId = linea.ProductoId,
            AlmacenId = 1,
            CostoUnitarioSnapshot = precioOrdenado
        };
        lineaRecepcion.EstablecerCantidades(8m, cantidadDanada: 1m, cantidadFaltante: 2m, cantidadSobrante: 1m);
        recepcion.Detalles.Add(lineaRecepcion);
        recepcion.Confirmar(1, "qa", fechaRecepcion);

        var evaluacion = new EvaluacionProveedor(
            orden.ProveedorId,
            orden.Id,
            recepcion.Id,
            fechaEsperada,
            fechaRecepcion)
        {
            Id = 40
        };
        evaluacion.EstablecerCantidades(10m, 8m, 6m, 1m, 1m);

        var devolucion = CrearDevolucion(50, linea, lineaRecepcion, orden, factura, 2m, fechaRecepcion, anular: false);

        db.Add(orden);
        db.Add(factura);
        db.Add(recepcion);
        db.Add(evaluacion);
        db.Add(devolucion);
        if (incluirDevolucionAnulada)
            db.Add(CrearDevolucion(60, linea, lineaRecepcion, orden, factura, 3m, fechaRecepcion, anular: true));
        await db.SaveChangesAsync();
        return db;
    }

    private static DevolucionProveedor CrearDevolucion(
        int id,
        OrdenCompraDetalle linea,
        RecepcionCompraDetalle recepcion,
        OrdenCompra orden,
        FacturaProveedor factura,
        decimal cantidad,
        DateTime fecha,
        bool anular)
    {
        var devolucion = new DevolucionProveedor
        {
            Id = id,
            NumeroDevolucion = $"DP-N55-{id}",
            ProveedorId = orden.ProveedorId,
            OrdenCompraId = orden.Id,
            RecepcionCompraId = recepcion.RecepcionCompraId,
            FacturaProveedorId = factura.Id,
            ProveedorNombreSnapshot = orden.ProveedorNombreSnapshot,
            Moneda = factura.Moneda,
            Motivo = "Prueba factual"
        };
        devolucion.Detalles.Add(new DevolucionProveedorDetalle
        {
            Id = id + 1,
            DevolucionProveedorId = devolucion.Id,
            RecepcionCompraDetalleId = recepcion.Id,
            OrdenCompraDetalleId = linea.Id,
            ProductoId = linea.ProductoId,
            AlmacenId = 1,
            Cantidad = cantidad,
            CostoUnitarioSnapshot = 10m,
            ImpuestoUnitarioSnapshot = 0m,
            ProductoNombreSnapshot = linea.ProductoNombreSnapshot ?? "Producto histórico"
        });
        devolucion.Confirmar(1, "qa", fecha);
        if (anular)
            devolucion.Anular(1, "Anulada para prueba", fecha.AddMinutes(1));
        return devolucion;
    }
}
