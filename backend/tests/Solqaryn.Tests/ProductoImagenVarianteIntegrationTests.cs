using System;
using System.Threading.Tasks;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class ProductoImagenVarianteIntegrationTests
{
    private static DbContextOptions<AppDbContext> CrearOpciones(string nombreBase) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                $"Server=localhost;Port=3306;Database={nombreBase};User=root;Password=root;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

    // Gate final M2: cubre migración MySQL 8.4, unicidad por ámbito de imagen
    // y forma parte de la certificación integral posterior al cierre de regresiones.
    [Fact]
    public async Task PrincipalPorAmbito_PermiteGeneralYUnaPorCadaVariante_PeroRechazaDuplicado()
    {
        var options = CrearOpciones($"test_imagen_ambito_{Guid.NewGuid():N}");
        try
        {
            await using var db = new AppDbContext(options);
            // Este MigrateAsync es parte del gate: prueba que MySQL 8.4 puede
            // crear e indexar PrincipalAmbitoKey como columna generada VIRTUAL
            // conservando la FK histórica de ProductoImagenes -> Productos.
            await db.Database.MigrateAsync();

            var colorNegro = new Color
            {
                Nombre = "Negro",
                CodigoVisual = "#111111",
                Activo = true,
                Eliminado = false,
                CreadoPorUsuarioId = 1,
                CreadoPorNombreUsuario = "integration-admin"
            };
            var colorAzul = new Color
            {
                Nombre = "Azul",
                CodigoVisual = "#0000FF",
                Activo = true,
                Eliminado = false,
                CreadoPorUsuarioId = 1,
                CreadoPorNombreUsuario = "integration-admin"
            };
            db.Colores.AddRange(colorNegro, colorAzul);
            await db.SaveChangesAsync();

            var producto = new Producto
            {
                Nombre = "Producto imágenes M2",
                Marca = "Marca histórica",
                Modelo = "Modelo histórico",
                TipoInventario = TipoInventario.MercaderiaVenta,
                Cantidad = 5,
                Costo = 100m,
                Precio = 150m,
                UmbralStockBajo = 1,
                Activo = true,
                Eliminado = false,
                CreadoPorUsuarioId = 1,
                CreadoPorNombreUsuario = "integration-admin"
            };
            db.Productos.Add(producto);
            await db.SaveChangesAsync();

            var varianteNegra = new ProductoVariante
            {
                ProductoId = producto.Id,
                ColorId = colorNegro.Id,
                Sku = $"M2-NEGRO-{Guid.NewGuid():N}",
                Cantidad = 3,
                Costo = 100m,
                Precio = 150m,
                Activo = true,
                Eliminado = false,
                CreadoPorUsuarioId = 1,
                CreadoPorNombreUsuario = "integration-admin"
            };
            var varianteAzul = new ProductoVariante
            {
                ProductoId = producto.Id,
                ColorId = colorAzul.Id,
                Sku = $"M2-AZUL-{Guid.NewGuid():N}",
                Cantidad = 2,
                Costo = 105m,
                Precio = 155m,
                Activo = true,
                Eliminado = false,
                CreadoPorUsuarioId = 1,
                CreadoPorNombreUsuario = "integration-admin"
            };
            db.ProductoVariantes.AddRange(varianteNegra, varianteAzul);
            await db.SaveChangesAsync();

            db.ProductoImagenes.AddRange(
                CrearImagen(producto.Id, null, "general-principal", true),
                CrearImagen(producto.Id, varianteNegra.Id, "negra-principal", true),
                CrearImagen(producto.Id, varianteAzul.Id, "azul-principal", true));
            await db.SaveChangesAsync();

            Assert.Equal(3, await db.ProductoImagenes.CountAsync(x => x.EsPrincipal));

            db.ProductoImagenes.Add(CrearImagen(producto.Id, varianteNegra.Id, "negra-duplicada", true));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static ProductoImagen CrearImagen(int productoId, int? varianteId, string nombre, bool principal) => new()
    {
        ProductoId = productoId,
        ProductoVarianteId = varianteId,
        Url = $"https://example.invalid/{nombre}.jpg",
        PublicId = nombre,
        Orden = 0,
        EsPrincipal = principal,
        CreadoPorUsuarioId = 1,
        CreadoPorNombreUsuario = "integration-admin"
    };
}
