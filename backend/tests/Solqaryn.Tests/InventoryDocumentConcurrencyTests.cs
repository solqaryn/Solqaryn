using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class InventoryDocumentConcurrencyTests
{
    private static DbContextOptions<AppDbContext> CrearOpciones(string nombreBase) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={nombreBase};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

    private static Mock<IUsuarioScopeService> CrearScopeAdministrador()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        return scope;
    }

    private static Mock<ICurrentUserService> CrearUsuarioActual()
    {
        var usuario = new Mock<ICurrentUserService>();
        usuario.SetupGet(x => x.UsuarioId).Returns(1);
        usuario.SetupGet(x => x.NombreUsuario).Returns("integration-admin");
        usuario.SetupGet(x => x.NombreCompleto).Returns("Integration Admin");
        usuario.SetupGet(x => x.EsAdministrador).Returns(true);
        return usuario;
    }

    private static VentaService CrearVentaService(AppDbContext context)
    {
        var scope = CrearScopeAdministrador();
        var usuario = CrearUsuarioActual();
        var productos = new ProductoRepository(context);
        var variantes = new ProductoVarianteRepository(context);
        var concurrencia = new InventarioConcurrencyService(
            context,
            productos,
            variantes);
        var empresa = new Mock<IEmpresaConfiguracionService>();
        empresa.Setup(x => x.GetActivaEntidadAsync())
            .ReturnsAsync(new EmpresaConfiguracion
            {
                NombreComercial = "Solqaryn CI",
                RTN = "00000000000000",
                Telefono = "0000-0000",
                Correo = "ci@example.invalid",
                Direccion = "CI"
            });

        return new VentaService(
            new VentaRepository(context, usuario.Object, scope.Object),
            Mock.Of<IClienteRepository>(),
            productos,
            variantes,
            concurrencia,
            new FacturaRepository(context, scope.Object),
            new MovimientoInventarioRepository(context, scope.Object),
            new MovimientoFinancieroRepository(context, scope.Object),
            empresa.Object,
            Mock.Of<ICalculoService>(),
            usuario.Object,
            new UnitOfWork(context),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ITipoClientePredeterminadoResolver>());
    }

    private static CompraService CrearCompraService(AppDbContext context)
    {
        var scope = CrearScopeAdministrador();
        var usuario = CrearUsuarioActual();
        var productos = new ProductoRepository(context);
        var variantes = new ProductoVarianteRepository(context);
        var concurrencia = new InventarioConcurrencyService(
            context,
            productos,
            variantes);

        return new CompraService(
            new CompraRepository(context, usuario.Object, scope.Object),
            Mock.Of<IProveedorRepository>(),
            productos,
            variantes,
            concurrencia,
            new MovimientoInventarioRepository(context, scope.Object),
            new MovimientoFinancieroRepository(context, scope.Object),
            Mock.Of<ICalculoService>(),
            usuario.Object,
            new UnitOfWork(context),
            Mock.Of<IAuditoriaService>());
    }

    private static Producto CrearProducto(string nombre, int cantidad) => new()
    {
        Nombre = nombre,
        Marca = "Test",
        Modelo = "CI",
        Cantidad = cantidad,
        Costo = 10m,
        Precio = 20m,
        UmbralStockBajo = 1,
        Activo = true,
        Eliminado = false,
        CreadoPorUsuarioId = 1,
        CreadoPorNombreUsuario = "integration-admin"
    };

    private static Venta CrearVenta(
        string numero,
        params (Producto Producto, int Cantidad, decimal Precio)[] items)
    {
        var venta = new Venta
        {
            NumeroVenta = numero,
            ClienteNombre = "Cliente CI",
            Estado = EstadoDocumento.Borrador,
            EstadoPago = EstadoPago.Pagado,
            MetodoPago = MetodoPago.Efectivo,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };

        foreach (var item in items)
        {
            venta.Detalles.Add(new VentaDetalle
            {
                ProductoId = item.Producto.Id,
                ProductoNombreSnapshot = item.Producto.Nombre,
                ProductoMarcaSnapshot = item.Producto.Marca,
                ProductoModeloSnapshot = item.Producto.Modelo,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.Precio,
                CostoUnitarioSnapshot = item.Producto.Costo,
                Subtotal = item.Cantidad * item.Precio,
                UtilidadBruta = item.Cantidad * (item.Precio - item.Producto.Costo)
            });
        }

        venta.ImporteBruto = venta.Detalles.Sum(x => x.Subtotal);
        venta.ImporteProductos = venta.ImporteBruto;
        venta.Subtotal = venta.ImporteBruto;
        venta.Total = venta.ImporteBruto;
        venta.CostoTotal = venta.Detalles.Sum(x => x.CostoUnitarioSnapshot * x.Cantidad);
        venta.UtilidadBruta = venta.Detalles.Sum(x => x.UtilidadBruta);
        return venta;
    }

    private static Compra CrearCompra(
        string numero,
        Producto producto,
        int cantidad,
        decimal costo)
    {
        var compra = new Compra
        {
            NumeroCompra = numero,
            ProveedorNombre = "Proveedor CI",
            Estado = EstadoDocumento.Borrador,
            EstadoPago = EstadoPago.Pagado,
            MetodoPago = MetodoPago.Efectivo,
            Subtotal = cantidad * costo,
            Total = cantidad * costo,
            CreadoPorUsuarioId = 1,
            CreadoPorNombreUsuario = "integration-admin"
        };
        compra.Detalles.Add(new CompraDetalle
        {
            ProductoId = producto.Id,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = producto.Marca,
            ProductoModeloSnapshot = producto.Modelo,
            Cantidad = cantidad,
            CostoUnitario = costo,
            Subtotal = cantidad * costo
        });
        return compra;
    }

    private static async Task LimpiarAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    private static async Task EsperarPreparadasAsync(
        Func<int> obtenerPreparadas,
        int esperadas)
    {
        var limite = DateTime.UtcNow.AddSeconds(15);
        while (obtenerPreparadas() != esperadas)
        {
            if (DateTime.UtcNow >= limite)
                throw new TimeoutException("Las operaciones concurrentes no llegaron a la barrera.");
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task DobleConfirmacionMismaVenta_ConfirmaUnaSolaVez()
    {
        var options = CrearOpciones($"test_double_confirm_{Guid.NewGuid():N}");
        var ventaId = 0;
        var productoId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var producto = CrearProducto("Producto doble confirmación", 2);
                setup.Productos.Add(producto);
                await setup.SaveChangesAsync();
                productoId = producto.Id;

                var venta = CrearVenta("VEN-DOUBLE-001", (producto, 1, 20m));
                setup.Ventas.Add(venta);
                await setup.SaveChangesAsync();
                ventaId = venta.Id;
            }

            var gate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var preparadas = 0;

            var tareas = Enumerable.Range(0, 2).Select(async _ =>
            {
                Interlocked.Increment(ref preparadas);
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using var context = new AppDbContext(options);
                try
                {
                    await CrearVentaService(context).ConfirmarAsync(ventaId);
                    return true;
                }
                catch (BusinessRuleException ex)
                    when (ex.Message.Contains("Borrador", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }).ToArray();

            await EsperarPreparadasAsync(() => Volatile.Read(ref preparadas), 2);
            gate.TrySetResult(true);
            var resultados = await Task.WhenAll(tareas)
                .WaitAsync(TimeSpan.FromSeconds(60));

            Assert.Single(resultados.Where(x => x));
            Assert.Single(resultados.Where(x => !x));

            await using var verify = new AppDbContext(options);
            Assert.Equal(1, await verify.Productos
                .Where(x => x.Id == productoId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(1, await verify.Facturas.CountAsync(x => x.VentaId == ventaId));
            Assert.Equal(1, await verify.MovimientosInventario.CountAsync(
                x => x.ReferenciaTipo == "Venta" && x.ReferenciaId == ventaId));
            Assert.Equal(1, await verify.MovimientosFinancieros.CountAsync(
                x => x.ModuloOrigen == "Venta" && x.ReferenciaId == ventaId));
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }

    [Fact]
    public async Task DetallesDuplicados_ConservaLineasYGeneraUnMovimientoConsolidado()
    {
        var options = CrearOpciones($"test_duplicate_details_{Guid.NewGuid():N}");
        var ventaId = 0;
        var productoId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var producto = CrearProducto("Producto detalles duplicados", 3);
                setup.Productos.Add(producto);
                await setup.SaveChangesAsync();
                productoId = producto.Id;

                var venta = CrearVenta(
                    "VEN-DUP-001",
                    (producto, 1, 20m),
                    (producto, 2, 25m));
                setup.Ventas.Add(venta);
                await setup.SaveChangesAsync();
                ventaId = venta.Id;
            }

            await using (var context = new AppDbContext(options))
                await CrearVentaService(context).ConfirmarAsync(ventaId);

            await using var verify = new AppDbContext(options);
            Assert.Equal(0, await verify.Productos
                .Where(x => x.Id == productoId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(2, await verify.FacturaDetalles.CountAsync(
                x => x.Factura!.VentaId == ventaId));
            var movimiento = await verify.MovimientosInventario
                .SingleAsync(x => x.ReferenciaTipo == "Venta" && x.ReferenciaId == ventaId);
            Assert.Equal(3, movimiento.Cantidad);
            Assert.Equal(3, movimiento.StockAnterior);
            Assert.Equal(0, movimiento.StockNuevo);
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }

    [Fact]
    public async Task VentaMultirrenglonConStockInsuficiente_RevierteTodo()
    {
        var options = CrearOpciones($"test_multiline_rollback_{Guid.NewGuid():N}");
        var ventaId = 0;
        var productoAId = 0;
        var productoBId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var productoA = CrearProducto("Producto A", 5);
                var productoB = CrearProducto("Producto B", 0);
                setup.Productos.AddRange(productoA, productoB);
                await setup.SaveChangesAsync();
                productoAId = productoA.Id;
                productoBId = productoB.Id;

                var venta = CrearVenta(
                    "VEN-ROLLBACK-001",
                    (productoA, 2, 20m),
                    (productoB, 1, 20m));
                setup.Ventas.Add(venta);
                await setup.SaveChangesAsync();
                ventaId = venta.Id;
            }

            await using (var context = new AppDbContext(options))
            {
                await Assert.ThrowsAsync<BusinessRuleException>(() =>
                    CrearVentaService(context).ConfirmarAsync(ventaId));
            }

            await using var verify = new AppDbContext(options);
            Assert.Equal(5, await verify.Productos
                .Where(x => x.Id == productoAId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(0, await verify.Productos
                .Where(x => x.Id == productoBId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(EstadoDocumento.Borrador, await verify.Ventas
                .Where(x => x.Id == ventaId)
                .Select(x => x.Estado)
                .SingleAsync());
            Assert.Equal(0, await verify.Facturas.CountAsync(x => x.VentaId == ventaId));
            Assert.Equal(0, await verify.MovimientosInventario.CountAsync(
                x => x.ReferenciaTipo == "Venta" && x.ReferenciaId == ventaId));
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }

    [Fact]
    public async Task DocumentosEnOrdenInverso_UsanMismoOrdenGlobalDeLocks()
    {
        var options = CrearOpciones($"test_reverse_order_{Guid.NewGuid():N}");
        var ventaIds = new List<int>();
        var productoIds = new List<int>();

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var productoA = CrearProducto("Producto orden A", 2);
                var productoB = CrearProducto("Producto orden B", 2);
                setup.Productos.AddRange(productoA, productoB);
                await setup.SaveChangesAsync();
                productoIds.AddRange(new[] { productoA.Id, productoB.Id });

                var venta1 = CrearVenta(
                    "VEN-ORDER-001",
                    (productoA, 1, 20m),
                    (productoB, 1, 20m));
                var venta2 = CrearVenta(
                    "VEN-ORDER-002",
                    (productoB, 1, 20m),
                    (productoA, 1, 20m));
                setup.Ventas.AddRange(venta1, venta2);
                await setup.SaveChangesAsync();
                ventaIds.AddRange(new[] { venta1.Id, venta2.Id });
            }

            var gate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var preparadas = 0;
            var tareas = ventaIds.Select(async ventaId =>
            {
                Interlocked.Increment(ref preparadas);
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using var context = new AppDbContext(options);
                await CrearVentaService(context).ConfirmarAsync(ventaId);
            }).ToArray();

            await EsperarPreparadasAsync(() => Volatile.Read(ref preparadas), 2);
            gate.TrySetResult(true);
            await Task.WhenAll(tareas).WaitAsync(TimeSpan.FromSeconds(60));

            await using var verify = new AppDbContext(options);
            var stocks = await verify.Productos
                .Where(x => productoIds.Contains(x.Id))
                .Select(x => x.Cantidad)
                .ToListAsync();
            Assert.Equal(new[] { 0, 0 }, stocks.OrderBy(x => x));
            Assert.Equal(2, await verify.Ventas.CountAsync(
                x => ventaIds.Contains(x.Id) && x.Estado == EstadoDocumento.Confirmada));
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }

    [Fact]
    public async Task DobleAnulacionMismaVenta_RestauraUnaSolaVez()
    {
        var options = CrearOpciones($"test_double_cancel_{Guid.NewGuid():N}");
        var ventaId = 0;
        var productoId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var producto = CrearProducto("Producto doble anulación", 2);
                setup.Productos.Add(producto);
                await setup.SaveChangesAsync();
                productoId = producto.Id;

                var venta = CrearVenta("VEN-CANCEL-001", (producto, 1, 20m));
                setup.Ventas.Add(venta);
                await setup.SaveChangesAsync();
                ventaId = venta.Id;
            }

            await using (var confirmarContext = new AppDbContext(options))
                await CrearVentaService(confirmarContext).ConfirmarAsync(ventaId);

            var gate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var preparadas = 0;
            var tareas = Enumerable.Range(0, 2).Select(async _ =>
            {
                Interlocked.Increment(ref preparadas);
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(15));
                await using var context = new AppDbContext(options);
                try
                {
                    await CrearVentaService(context).AnularAsync(
                        ventaId,
                        "Anulación concurrente CI");
                    return true;
                }
                catch (BusinessRuleException ex)
                    when (ex.Message.Contains("confirmadas", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }).ToArray();

            await EsperarPreparadasAsync(() => Volatile.Read(ref preparadas), 2);
            gate.TrySetResult(true);
            var resultados = await Task.WhenAll(tareas)
                .WaitAsync(TimeSpan.FromSeconds(60));

            Assert.Single(resultados.Where(x => x));
            Assert.Single(resultados.Where(x => !x));

            await using var verify = new AppDbContext(options);
            Assert.Equal(2, await verify.Productos
                .Where(x => x.Id == productoId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(EstadoDocumento.Anulada, await verify.Ventas
                .Where(x => x.Id == ventaId)
                .Select(x => x.Estado)
                .SingleAsync());
            Assert.Equal(EstadoFactura.Anulada, await verify.Facturas
                .Where(x => x.VentaId == ventaId)
                .Select(x => x.Estado)
                .SingleAsync());
            Assert.Equal(1, await verify.MovimientosInventario.CountAsync(
                x => x.ReferenciaTipo == "VentaAnulada" && x.ReferenciaId == ventaId));
            Assert.Equal(1, await verify.MovimientosFinancieros.CountAsync(
                x => x.ModuloOrigen == "Reversion" && x.VentaId == ventaId));
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }

    [Fact]
    public async Task CompraConMovimientoPosterior_NoPuedeAnularse()
    {
        var options = CrearOpciones($"test_purchase_later_move_{Guid.NewGuid():N}");
        var compraId = 0;
        var productoId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var producto = CrearProducto("Producto compra posterior", 5);
                setup.Productos.Add(producto);
                await setup.SaveChangesAsync();
                productoId = producto.Id;

                var compra = CrearCompra("COM-LATER-001", producto, 3, 12m);
                setup.Compras.Add(compra);
                await setup.SaveChangesAsync();
                compraId = compra.Id;
            }

            await using (var confirmarContext = new AppDbContext(options))
                await CrearCompraService(confirmarContext).ConfirmarAsync(compraId);

            await using (var movimientoContext = new AppDbContext(options))
            {
                movimientoContext.MovimientosInventario.Add(new MovimientoInventario
                {
                    ProductoId = productoId,
                    Tipo = TipoMovimientoInventario.Ajuste,
                    Cantidad = 1,
                    StockAnterior = 8,
                    StockNuevo = 8,
                    ReferenciaTipo = "AjustePosteriorCI",
                    ReferenciaId = productoId,
                    Descripcion = "Movimiento posterior de prueba",
                    CreadoPorUsuarioId = 1,
                    CreadoPorNombreUsuario = "integration-admin"
                });
                await movimientoContext.SaveChangesAsync();
            }

            await using (var cancelarContext = new AppDbContext(options))
            {
                var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
                    CrearCompraService(cancelarContext).AnularAsync(
                        compraId,
                        "Intento posterior CI"));
                Assert.Contains("movimientos posteriores", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            await using var verify = new AppDbContext(options);
            Assert.Equal(8, await verify.Productos
                .Where(x => x.Id == productoId)
                .Select(x => x.Cantidad)
                .SingleAsync());
            Assert.Equal(EstadoDocumento.Confirmada, await verify.Compras
                .Where(x => x.Id == compraId)
                .Select(x => x.Estado)
                .SingleAsync());
            Assert.Equal(0, await verify.MovimientosInventario.CountAsync(
                x => x.ReferenciaTipo == "CompraAnulada" && x.ReferenciaId == compraId));
        }
        finally
        {
            await LimpiarAsync(options);
        }
    }
}
