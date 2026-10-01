using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Services;

public sealed class RentabilidadVentasServiceTests
{
    [Fact]
    public async Task VendedorYCliente_UsanTotalesCostoYUtilidadPersistidosDeVenta()
    {
        await using var context = CreateContext();
        context.Ventas.AddRange(
            Confirmada("V1", 7, 11, "Cliente A", 120m, 70m, 42m),
            Confirmada("V2", 7, 11, "Cliente A", 80m, 40m, 35m));
        await context.SaveChangesAsync();

        var service = CreateService(context, new UsuarioScopeActual(1, 1, "Admin", EsAdministrador: true));

        var vendedores = await service.ObtenerAsync(new ReporteVentasFiltroDto(), RentabilidadAgrupacion.Vendedor);
        var vendedor = Assert.Single(vendedores);
        Assert.Equal(200m, vendedor.Venta);
        Assert.Equal(110m, vendedor.Costo);
        Assert.Equal(77m, vendedor.UtilidadBruta);
        Assert.True(vendedor.IncluyeDescuentoEncabezadoEnUtilidad);

        var clientes = await service.ObtenerAsync(new ReporteVentasFiltroDto(), RentabilidadAgrupacion.Cliente);
        var cliente = Assert.Single(clientes);
        Assert.Equal(200m, cliente.Venta);
        Assert.Equal(110m, cliente.Costo);
        Assert.Equal(77m, cliente.UtilidadBruta);
        Assert.True(cliente.IncluyeDescuentoEncabezadoEnUtilidad);
    }

    [Fact]
    public async Task ProductoYCategoria_UsanSoloSubtotalCostoHistoricoYUtilidadDeLinea_SinAsignarDescuentoEncabezado()
    {
        await using var context = CreateContext();
        var categoria = new Categoria { Id = 5, Nombre = "Categoria A" };
        var producto = new Producto { Id = 9, Nombre = "Producto A", CategoriaId = 5, Categoria = categoria };
        var venta = Confirmada("V1", 7, 11, "Cliente A", 100m, 45m, 40m);
        context.AddRange(categoria, producto, venta);
        await context.SaveChangesAsync();
        context.VentaDetalles.AddRange(
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = producto.Id,
                Producto = producto,
                Cantidad = 2,
                CostoUnitarioSnapshot = 15m,
                PrecioUnitario = 25m,
                Subtotal = 50m,
                UtilidadBruta = 20m,
                ProductoNombreSnapshot = "Producto A",
                ProductoMarcaSnapshot = "M",
                ProductoModeloSnapshot = "X"
            },
            new VentaDetalle
            {
                VentaId = venta.Id,
                Venta = venta,
                ProductoId = producto.Id,
                Producto = producto,
                Cantidad = 1,
                CostoUnitarioSnapshot = 12m,
                PrecioUnitario = 30m,
                Subtotal = 30m,
                UtilidadBruta = 18m,
                ProductoNombreSnapshot = "Producto A",
                ProductoMarcaSnapshot = "M",
                ProductoModeloSnapshot = "X"
            });
        await context.SaveChangesAsync();

        var service = CreateService(context, new UsuarioScopeActual(1, 1, "Admin", EsAdministrador: true));

        var productos = await service.ObtenerAsync(new ReporteVentasFiltroDto(), RentabilidadAgrupacion.Producto);
        var productoResultado = Assert.Single(productos);
        Assert.Equal(80m, productoResultado.Venta);
        Assert.Equal(42m, productoResultado.Costo);
        Assert.Equal(38m, productoResultado.UtilidadBruta);
        Assert.False(productoResultado.IncluyeDescuentoEncabezadoEnUtilidad);
        Assert.Contains("EXCLUDES_SALE_HEADER_DISCOUNT_ALLOCATION", productoResultado.Semantica);

        var categorias = await service.ObtenerAsync(new ReporteVentasFiltroDto(), RentabilidadAgrupacion.Categoria);
        var categoriaResultado = Assert.Single(categorias);
        Assert.Equal(80m, categoriaResultado.Venta);
        Assert.Equal(42m, categoriaResultado.Costo);
        Assert.Equal(38m, categoriaResultado.UtilidadBruta);
        Assert.False(categoriaResultado.IncluyeDescuentoEncabezadoEnUtilidad);
    }

    [Fact]
    public async Task NoAdministrador_NoPuedeExpandirScopeConVendedorForjado()
    {
        await using var context = CreateContext();
        context.Ventas.AddRange(
            Confirmada("OWN", 7, 11, "Cliente A", 100m, 50m, 45m),
            Confirmada("OTHER", 99, 12, "Cliente B", 900m, 100m, 790m));
        await context.SaveChangesAsync();

        var service = CreateService(context, new UsuarioScopeActual(7, 2, "Operador", EsAdministrador: false));
        var resultado = await service.ObtenerAsync(
            new ReporteVentasFiltroDto { VendedorId = 99 },
            RentabilidadAgrupacion.Vendedor);

        var item = Assert.Single(resultado);
        Assert.Equal(7, item.AgrupacionId);
        Assert.Equal(100m, item.Venta);
    }

    [Fact]
    public async Task ScopeNoResuelto_FallaCerrado()
    {
        await using var context = CreateContext();
        context.Ventas.Add(Confirmada("V1", 7, 11, "Cliente A", 100m, 50m, 45m));
        await context.SaveChangesAsync();

        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync()).ReturnsAsync((UsuarioScopeActual?)null);
        var service = new RentabilidadVentasService(context, scope.Object);

        var resultado = await service.ObtenerAsync(new ReporteVentasFiltroDto(), RentabilidadAgrupacion.Vendedor);

        Assert.Empty(resultado);
    }

    [Fact]
    public void Dto_NoExponeMargenPorcentajeNiAsignacionSintetica()
    {
        var propiedades = typeof(ReporteRentabilidadDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain(propiedades, p => p.Contains("Porcentaje", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propiedades, p => p.Contains("Margen", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propiedades, p => p.Contains("DescuentoAsignado", StringComparison.OrdinalIgnoreCase));
    }

    private static RentabilidadVentasService CreateService(AppDbContext context, UsuarioScopeActual scopeActual)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync()).ReturnsAsync(scopeActual);
        return new RentabilidadVentasService(context, scope.Object);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static Venta Confirmada(
        string numero,
        int vendedorId,
        int clienteId,
        string clienteNombre,
        decimal total,
        decimal costo,
        decimal utilidad) => new()
    {
        NumeroVenta = numero,
        Fecha = new DateTime(2026, 9, 10, 5, 0, 0, DateTimeKind.Utc),
        Estado = EstadoDocumento.Confirmada,
        CreadoPorUsuarioId = vendedorId,
        CreadoPorNombreUsuario = $"U{vendedorId}",
        ClienteId = clienteId,
        ClienteNombre = clienteNombre,
        ImporteBruto = total,
        ImporteProductos = total,
        Subtotal = total,
        Total = total,
        CostoTotal = costo,
        UtilidadBruta = utilidad
    };
}
