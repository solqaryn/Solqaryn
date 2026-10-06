using MySql.EntityFrameworkCore.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class CargaMasivaConcurrencyTests
{
    private sealed record Escenario(DbContextOptions<AppDbContext> Options, int CargaId, int ProductoId, int VarianteId);

    private static DbContextOptions<AppDbContext> CrearOpciones(string nombreBase) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={nombreBase};User=root;Password=root;", mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

    private static Mock<ICurrentUserService> CrearUsuarioActual()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(1);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("integration-admin");
        currentUser.SetupGet(x => x.NombreCompleto).Returns("Integration Admin");
        currentUser.SetupGet(x => x.EsAdministrador).Returns(true);
        return currentUser;
    }

    private static CargaMasivaService CrearServicio(AppDbContext context) =>
        new(context, new UnitOfWork(context), CrearUsuarioActual().Object, Mock.Of<IAuditoriaService>(), Mock.Of<ILogger<CargaMasivaService>>(), Mock.Of<ITipoClientePredeterminadoResolver>());

    private static async Task<Escenario> PrepararAsync(int cantidadActual, int cantidadNueva)
    {
        var options = CrearOpciones($"test_carga_snapshot_{Guid.NewGuid():N}");
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();

        var marca = new Marca { Nombre = "Marca Test", Activo = true, Eliminado = false, CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin" };
        context.Marcas.Add(marca);
        await context.SaveChangesAsync();

        var modelo = new Modelo { MarcaId = marca.Id, Nombre = "Modelo Test", Activo = true, Eliminado = false, CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin" };
        var color = new Color { Nombre = "Negro", CodigoVisual = "#111111", Activo = true, Eliminado = false, CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin" };
        var talla = new Talla { Nombre = "M", Activo = true, Eliminado = false, CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin" };
        context.Modelos.Add(modelo);
        context.Colores.Add(color);
        context.Tallas.Add(talla);
        await context.SaveChangesAsync();

        var producto = new Producto
        {
            Nombre = "Producto carga concurrente", Marca = "", Modelo = "", TipoInventario = TipoInventario.MercaderiaVenta,
            Cantidad = cantidadActual, Costo = 10m, Precio = 20m, UmbralStockBajo = 1, Activo = true, Eliminado = false,
            CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin"
        };
        context.Productos.Add(producto);
        await context.SaveChangesAsync();

        var variante = new ProductoVariante
        {
            ProductoId = producto.Id, MarcaId = marca.Id, ModeloId = modelo.Id, ColorId = color.Id, TallaId = talla.Id,
            Sku = $"SKU-{Guid.NewGuid():N}".ToUpperInvariant(), CodigoBarras = Guid.NewGuid().ToString("N"),
            Cantidad = cantidadActual, UmbralStockBajo = 1, Costo = 10m, Precio = 20m, Activo = true, Eliminado = false,
            CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin"
        };
        context.ProductoVariantes.Add(variante);
        await context.SaveChangesAsync();

        var fila = new CargaMasivaFilaDto
        {
            NumeroFila = 2, Accion = "Actualizar", EsValida = true, ProductoIdSnapshot = producto.Id,
            ProductoVarianteIdSnapshot = variante.Id, CantidadActualSnapshot = cantidadActual, FechaValidacionSnapshot = DateTime.UtcNow,
            Datos = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Producto"] = producto.Nombre, ["Marca"] = marca.Nombre, ["Modelo"] = modelo.Nombre,
                ["Color"] = color.Nombre, ["Talla"] = talla.Nombre, ["SKU"] = variante.Sku,
                ["CodigoBarras"] = variante.CodigoBarras, ["Cantidad"] = cantidadNueva.ToString(), ["UmbralStockBajo"] = "1",
                ["Costo"] = "12.50", ["Precio"] = "25.00", ["Activo"] = "true"
            }
        };

        var carga = new CargaMasiva
        {
            Tipo = TipoCargaMasiva.VariantesInventario, Estado = EstadoCargaMasiva.Validada, NombreArchivo = "variantes.csv",
            Extension = ".csv", ContentType = "text/csv", TamanoBytes = 128, HashArchivo = Guid.NewGuid().ToString("N").PadRight(64, '0'),
            DatosNormalizadosJson = JsonSerializer.Serialize(new[] { fila }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            TotalFilas = 1, FilasValidas = 1, FilasConError = 0, FechaValidacion = DateTime.UtcNow,
            CreadoPorUsuarioId = 1, CreadoPorNombreUsuario = "integration-admin"
        };
        context.CargasMasivas.Add(carga);
        await context.SaveChangesAsync();
        return new Escenario(options, carga.Id, producto.Id, variante.Id);
    }

    private static async Task LimpiarAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task ConfirmarAsync_SnapshotVigente_AplicaAjusteExactoYConservaSnapshotsMultidimensionales()
    {
        var escenario = await PrepararAsync(5, 9);
        try
        {
            await using (var context = new AppDbContext(escenario.Options))
            {
                var resultado = await CrearServicio(context).ConfirmarAsync(escenario.CargaId);
                Assert.Equal(nameof(EstadoCargaMasiva.Confirmada), resultado.Estado);
            }

            await using var verify = new AppDbContext(escenario.Options);
            var variante = await verify.ProductoVariantes.AsNoTracking().SingleAsync(x => x.Id == escenario.VarianteId);
            var producto = await verify.Productos.AsNoTracking().SingleAsync(x => x.Id == escenario.ProductoId);
            var carga = await verify.CargasMasivas.AsNoTracking().SingleAsync(x => x.Id == escenario.CargaId);
            var movimiento = Assert.Single(await verify.MovimientosInventario.AsNoTracking().Where(x => x.ReferenciaTipo == "CargaMasiva" && x.ReferenciaId == escenario.CargaId).ToListAsync());

            Assert.Equal(9, variante.Cantidad);
            Assert.Equal(9, producto.Cantidad);
            Assert.Equal(EstadoCargaMasiva.Confirmada, carga.Estado);
            Assert.Equal(TipoMovimientoInventario.Ajuste, movimiento.Tipo);
            Assert.Equal(5, movimiento.StockAnterior);
            Assert.Equal(9, movimiento.StockNuevo);
            Assert.Equal(4, movimiento.Cantidad);
            Assert.Equal("Marca Test", movimiento.ProductoMarcaSnapshot);
            Assert.Equal("Modelo Test", movimiento.ProductoModeloSnapshot);
            Assert.Equal("Negro", movimiento.ProductoColorSnapshot);
            Assert.Equal("M", movimiento.ProductoTallaSnapshot);
            Assert.Equal(variante.Sku, movimiento.ProductoSkuSnapshot);
        }
        finally { await LimpiarAsync(escenario.Options); }
    }

    [Fact]
    public async Task ConfirmarAsync_SnapshotVencido_RevierteLoteYConservaStockConcurrente()
    {
        var escenario = await PrepararAsync(5, 9);
        try
        {
            await using (var concurrente = new AppDbContext(escenario.Options))
            {
                var variante = await concurrente.ProductoVariantes.SingleAsync(x => x.Id == escenario.VarianteId);
                var producto = await concurrente.Productos.SingleAsync(x => x.Id == escenario.ProductoId);
                variante.Cantidad = 4;
                producto.Cantidad = 4;
                await concurrente.SaveChangesAsync();
            }

            await using (var context = new AppDbContext(escenario.Options))
            {
                var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CrearServicio(context).ConfirmarAsync(escenario.CargaId));
                Assert.Contains("cambió", ex.Message, StringComparison.OrdinalIgnoreCase);
            }

            await using var verify = new AppDbContext(escenario.Options);
            Assert.Equal(4, (await verify.ProductoVariantes.AsNoTracking().SingleAsync(x => x.Id == escenario.VarianteId)).Cantidad);
            Assert.Equal(4, (await verify.Productos.AsNoTracking().SingleAsync(x => x.Id == escenario.ProductoId)).Cantidad);
            var cargaFinal = await verify.CargasMasivas.AsNoTracking().SingleAsync(x => x.Id == escenario.CargaId);
            Assert.Equal(EstadoCargaMasiva.Fallida, cargaFinal.Estado);
            Assert.Contains("Revalida", cargaFinal.ErrorGeneral ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await verify.MovimientosInventario.AsNoTracking().CountAsync(x => x.ReferenciaTipo == "CargaMasiva" && x.ReferenciaId == escenario.CargaId));
        }
        finally { await LimpiarAsync(escenario.Options); }
    }
}