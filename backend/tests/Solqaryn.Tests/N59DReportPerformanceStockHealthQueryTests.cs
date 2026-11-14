using Xunit;

namespace Solqaryn.Tests;

public sealed class N59DReportPerformanceStockHealthQueryTests
{
    [Fact]
    public void StockHealth_agrega_movimientos_una_vez_y_elimina_subconsultas_correlacionadas_por_existencia()
    {
        var source = Leer("backend/src/Infrastructure/Services/ReporteInventarioService.cs");

        Assert.Contains("var movimientoStats = movimientos", source, StringComparison.Ordinal);
        Assert.Contains(".GroupBy(m => new { m.ProductoVarianteId, m.AlmacenId, m.UbicacionAlmacenId })", source, StringComparison.Ordinal);
        Assert.Contains("join stats in movimientoStats", source, StringComparison.Ordinal);
        Assert.Contains("from stats in statsJoin.DefaultIfEmpty()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UltimoMovimientoUtc = movimientos", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UnidadesSalidaPeriodo = movimientos", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StockHealth_conserva_contrato_de_periodo_y_salida_en_el_agregado()
    {
        var source = Leer("backend/src/Infrastructure/Services/ReporteInventarioService.cs");

        Assert.Contains("m.Tipo == TipoMovimientoInventario.Salida", source, StringComparison.Ordinal);
        Assert.Contains("m.Fecha >= desde && m.Fecha <= hasta", source, StringComparison.Ordinal);
        Assert.Contains("UltimoMovimientoUtc = g.Max(m => (DateTime?)m.Fecha)", source, StringComparison.Ordinal);
        Assert.Contains("UnidadesSalidaPeriodo = stats == null ? 0 : stats.UnidadesSalidaPeriodo", source, StringComparison.Ordinal);
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
