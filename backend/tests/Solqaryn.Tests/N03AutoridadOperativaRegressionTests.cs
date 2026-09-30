using Xunit;

namespace Solqaryn.Tests;

public sealed class N03AutoridadOperativaRegressionTests
{
    [Fact]
    public void Venta_no_debe_usar_stock_ni_costo_legacy_de_producto()
    {
        var source = Leer("backend/src/Application/Services/VentaService.cs");
        Assert.DoesNotContain("producto.Cantidad < input.Cantidad", source);
        Assert.DoesNotContain("variante?.Costo ?? producto.Costo", source);
        Assert.DoesNotContain("ProductoVarianteId = variante?.Id", source);
        Assert.Contains("variante.Cantidad < input.Cantidad", source);
        Assert.Contains("ProductoVarianteId = variante.Id", source);
    }

    [Fact]
    public void Compra_nueva_debe_quedar_vinculada_a_variante_operativa()
    {
        var source = Leer("backend/src/Application/Services/CompraService.cs");
        Assert.DoesNotContain("ProductoVarianteId = variante?.Id", source);
        Assert.Contains("ProductoVarianteId = variante.Id", source);
        Assert.Contains("no tiene una variante operativa activa", source);
    }

    [Fact]
    public void Carga_productos_debe_escribir_operacion_en_variante_tecnica()
    {
        var source = Leer("backend/src/Infrastructure/Services/CargaMasivaService.cs");
        Assert.DoesNotContain("producto.Costo = Decimal(fila, \"Costo\")", source);
        Assert.DoesNotContain("producto.Precio = Decimal(fila, \"Precio\")", source);
        Assert.DoesNotContain("producto.UmbralStockBajo = Entero(fila, \"UmbralStockBajo\")", source);
        Assert.Contains("variante.Costo = Decimal(fila, \"Costo\")", source);
        Assert.Contains("variante.Precio = Decimal(fila, \"Precio\")", source);
        Assert.Contains("producto.Costo = variante.Costo ?? 0m", source);
        Assert.Contains("PRODUCTO_REQUIERE_VARIANTES", source);
    }

    [Fact]
    public void Carga_variantes_debe_convertir_tecnica_sin_mezclar_autoridades()
    {
        var source = Leer("backend/src/Infrastructure/Services/CargaMasivaService.cs");
        Assert.Contains("conserva stock en su variante técnica", source);
        Assert.Contains("tecnica.Eliminado = true", source);
        Assert.Contains("!x.Eliminado && !x.EsTecnica", source);
    }

    private static string Leer(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "src")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var path = Path.Combine(directory!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"No se encontró el archivo de fuente requerido: {path}");
        return File.ReadAllText(path);
    }
}
