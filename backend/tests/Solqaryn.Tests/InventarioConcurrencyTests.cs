using System;
using System.Collections.Generic;
using System.Diagnostics;
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
public class InventarioConcurrencyTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                GetConnectionString(dbName),
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

    private static Mock<IUsuarioScopeService> CrearScopeAdministrador()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Admin", true));
        return scope;
    }

    private static Mock<ICurrentUserService> CrearUsuarioActual()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(1);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("integration-admin");
        currentUser.SetupGet(x => x.NombreCompleto).Returns("Integration Admin");
        return currentUser;
    }

    private static async Task EsperarHastaAsync(
        Func<bool> condicion,
        TimeSpan timeout)
    {
        var inicio = Stopwatch.StartNew();
        while (!condicion())
        {
            if (inicio.Elapsed >= timeout)
                throw new TimeoutException("La condición concurrente no se alcanzó dentro del tiempo esperado.");

            await Task.Delay(25);
        }
    }

    [Fact]
    public async Task Concurrency_10VentasConcurrentesSobre5Unidades_Solo5ExitosasYStockFinalCero()
    {
        var dbName = $"test_inv_concurrency_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);
        var productoId = 0;
        var varianteId = 0;
        var ventaIds = new List<int>();

        try
        {
            await using (var setupContext = new AppDbContext(options))
            {
                await setupContext.Database.MigrateAsync();

                var producto = new Producto
                {
                    Nombre = "Producto Concurrente CI",
                    Marca = "Test",
                    Modelo = "Model",
                    Cantidad = 5,
                    Costo = 10m,
                    Precio = 20m,
                    UmbralStockBajo = 1,
                    Activo = true,
                    Eliminado = false
                };
                setupContext.Productos.Add(producto);
                await setupContext.SaveChangesAsync();
                productoId = producto.Id;

                var variante = new ProductoVariante
                {
                    ProductoId = producto.Id,
                    Sku = $"SKU-{Guid.NewGuid():N}",
                    Cantidad = 5,
                    Costo = 10m,
                    Precio = 20m,
                    Activo = true,
                    Eliminado = false
                };
                setupContext.ProductoVariantes.Add(variante);
                await setupContext.SaveChangesAsync();
                varianteId = variante.Id;

                for (var i = 1; i <= 10; i++)
                {
                    var venta = new Venta
                    {
                        NumeroVenta = $"VEN-TEST-{i:D4}",
                        ClienteNombre = "Cliente Test",
                        Estado = EstadoDocumento.Borrador,
                        EstadoPago = EstadoPago.Pagado,
                        MetodoPago = MetodoPago.Efectivo,
                        ImporteBruto = 20m,
                        ImporteProductos = 20m,
                        Total = 20m,
                        Subtotal = 20m,
                        CostoTotal = 10m,
                        UtilidadBruta = 10m,
                        CreadoPorUsuarioId = 1,
                        CreadoPorNombreUsuario = "integration-admin",
                        Detalles = new List<VentaDetalle>
                        {
                            new()
                            {
                                ProductoId = productoId,
                                ProductoVarianteId = varianteId,
                                ProductoNombreSnapshot = producto.Nombre,
                                ProductoMarcaSnapshot = producto.Marca,
                                ProductoModeloSnapshot = producto.Modelo,
                                ProductoSkuSnapshot = variante.Sku,
                                Cantidad = 1,
                                PrecioUnitario = 20m,
                                CostoUnitarioSnapshot = 10m,
                                Subtotal = 20m,
                                UtilidadBruta = 10m
                            }
                        }
                    };
                    setupContext.Ventas.Add(venta);
                    await setupContext.SaveChangesAsync();
                    ventaIds.Add(venta.Id);
                }
            }

            var gate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var preparadas = 0;

            var tasks = ventaIds.Select(async id =>
            {
                Interlocked.Increment(ref preparadas);
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(15));

                await using var context = new AppDbContext(options);
                var uow = new UnitOfWork(context);
                var scope = CrearScopeAdministrador();
                var currentUser = CrearUsuarioActual();

                var ventaRepo = new VentaRepository(context, currentUser.Object, scope.Object);
                var prodRepo = new ProductoRepository(context);
                var varRepo = new ProductoVarianteRepository(context);
                var inventarioConcurrency = new InventarioConcurrencyService(context, prodRepo, varRepo);
                var facturaRepo = new FacturaRepository(context, scope.Object, null);
                var movInvRepo = new MovimientoInventarioRepository(context, scope.Object);
                var movFinRepo = new MovimientoFinancieroRepository(context, scope.Object);

                var empresa = new Mock<IEmpresaConfiguracionService>();
                empresa.Setup(e => e.GetActivaEntidadAsync())
                    .ReturnsAsync(new EmpresaConfiguracion
                    {
                        NombreComercial = "CI",
                        RTN = "0000"
                    });

                var service = new VentaService(
                    ventaRepo,
                    Mock.Of<IClienteRepository>(),
                    prodRepo,
                    varRepo,
                    inventarioConcurrency,
                    facturaRepo,
                    movInvRepo,
                    movFinRepo,
                    empresa.Object,
                    Mock.Of<ICalculoService>(),
                    currentUser.Object,
                    uow,
                    Mock.Of<IAuditoriaService>(),
                    Mock.Of<ITipoClientePredeterminadoResolver>());

                try
                {
                    await service.ConfirmarAsync(id);
                    return true;
                }
                catch (BusinessRuleException ex)
                    when (ex.Message.Contains("Stock insuficiente", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }).ToList();

            await EsperarHastaAsync(
                () => Volatile.Read(ref preparadas) == ventaIds.Count,
                TimeSpan.FromSeconds(15));
            gate.TrySetResult(true);

            var results = await Task.WhenAll(tasks)
                .WaitAsync(TimeSpan.FromSeconds(90));

            Assert.Equal(5, results.Count(x => x));
            Assert.Equal(5, results.Count(x => !x));

            await using var verifyContext = new AppDbContext(options);
            var productoFinal = await verifyContext.Productos
                .AsNoTracking()
                .SingleAsync(x => x.Id == productoId);
            var varianteFinal = await verifyContext.ProductoVariantes
                .AsNoTracking()
                .SingleAsync(x => x.Id == varianteId);

            Assert.Equal(0, productoFinal.Cantidad);
            Assert.Equal(0, varianteFinal.Cantidad);

            var ventasConfirmadas = await verifyContext.Ventas
                .AsNoTracking()
                .CountAsync(x => ventaIds.Contains(x.Id) && x.Estado == EstadoDocumento.Confirmada);
            var ventasBorrador = await verifyContext.Ventas
                .AsNoTracking()
                .CountAsync(x => ventaIds.Contains(x.Id) && x.Estado == EstadoDocumento.Borrador);
            var facturas = await verifyContext.Facturas
                .AsNoTracking()
                .CountAsync(x => ventaIds.Contains(x.VentaId));
            var movimientosFinancieros = await verifyContext.MovimientosFinancieros
                .AsNoTracking()
                .CountAsync(x => x.ModuloOrigen == "Venta" && x.ReferenciaId.HasValue && ventaIds.Contains(x.ReferenciaId.Value));
            var movimientosInventario = await verifyContext.MovimientosInventario
                .AsNoTracking()
                .Where(x => x.ReferenciaTipo == "Venta" && ventaIds.Contains(x.ReferenciaId))
                .ToListAsync();

            Assert.Equal(5, ventasConfirmadas);
            Assert.Equal(5, ventasBorrador);
            Assert.Equal(5, facturas);
            Assert.Equal(5, movimientosFinancieros);
            Assert.Equal(5, movimientosInventario.Count);
            Assert.All(movimientosInventario, movimiento =>
            {
                Assert.Equal(1, movimiento.Cantidad);
                Assert.True(movimiento.StockNuevo >= 0);
            });
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ForUpdate_SegundaConexionEsperaHastaLiberacionDelLock()
    {
        var dbName = $"test_inv_lock_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);
        var productoId = 0;

        try
        {
            await using (var setupContext = new AppDbContext(options))
            {
                await setupContext.Database.MigrateAsync();
                var producto = new Producto
                {
                    Nombre = "Producto Lock CI",
                    Marca = "Test",
                    Modelo = "Lock",
                    Cantidad = 10,
                    Costo = 10m,
                    Precio = 20m,
                    UmbralStockBajo = 1,
                    Activo = true,
                    Eliminado = false
                };
                setupContext.Productos.Add(producto);
                await setupContext.SaveChangesAsync();
                productoId = producto.Id;
            }

            await using var contextA = new AppDbContext(options);
            await using var transactionA = await contextA.Database.BeginTransactionAsync();
            var repositoryA = new ProductoRepository(contextA);
            var bloqueadoA = await repositoryA.GetByIdForUpdateAsync(productoId);
            Assert.NotNull(bloqueadoA);

            var intentoIniciado = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var esperaB = Task.Run(async () =>
            {
                await using var contextB = new AppDbContext(options);
                await using var transactionB = await contextB.Database.BeginTransactionAsync();
                var repositoryB = new ProductoRepository(contextB);
                intentoIniciado.TrySetResult(true);
                var cronometro = Stopwatch.StartNew();
                var bloqueadoB = await repositoryB.GetByIdForUpdateAsync(productoId);
                cronometro.Stop();
                Assert.NotNull(bloqueadoB);
                await transactionB.RollbackAsync();
                return cronometro.Elapsed;
            });

            await intentoIniciado.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Task.Delay(500);
            Assert.False(esperaB.IsCompleted);

            await transactionA.CommitAsync();
            var tiempoEspera = await esperaB.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.True(
                tiempoEspera >= TimeSpan.FromMilliseconds(400),
                $"La segunda conexión solo esperó {tiempoEspera.TotalMilliseconds:N0} ms.");
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }
}
