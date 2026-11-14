using Xunit;

namespace Solqaryn.Tests;

public sealed class N07AjusteInventarioSeguridadRegressionTests
{
    [Fact]
    public void Ajustes_stock_legacy_deben_exigir_confirmar_inventario()
    {
        var source = Leer("backend/src/API/Controllers/InventarioAjustesController.cs");

        Assert.DoesNotContain(
            "RequierePermiso(ModuloSistema.Productos, AccionPermiso.Editar)",
            source);

        const string permisoEsperado =
            "RequierePermiso(ModuloSistema.Inventario, AccionPermiso.Confirmar)";

        Assert.Equal(2, ContarOcurrencias(source, permisoEsperado));
    }

    [Fact]
    public void Ajustes_stock_legacy_deben_permanecer_autenticados()
    {
        var source = Leer("backend/src/API/Controllers/InventarioAjustesController.cs");

        Assert.Contains("[Authorize]", source);
        Assert.Contains("[HttpPost(\"{productoId:int}/ajustes-stock\")]", source);
        Assert.Contains(
            "[HttpPost(\"{productoId:int}/variantes/{varianteId:int}/ajustes-stock\")]",
            source);
    }

    [Fact]
    public void Confirmar_y_anular_deben_exigir_auditoria_critica_transaccional()
    {
        var source = LeerServicioAjusteFormal();

        Assert.Equal(2, ContarOcurrencias(source, "_auditoria.RegistrarEstrictoAsync("));
        Assert.Contains("AccionPermiso.Confirmar", source);
        Assert.Contains("AccionPermiso.Anular", source);

        // Crear/editar conservan auditoría tolerante; las dos operaciones que mutan stock
        // deben quedar cubiertas por la variante estricta dentro de la transacción.
        Assert.Equal(2, ContarOcurrencias(source, "_auditoria.RegistrarAsync("));
    }

    [Fact]
    public void Ajustes_stock_legacy_deben_delegar_en_la_autoridad_formal_atomica()
    {
        var adapter = Leer("backend/src/Application/Services/InventarioAjusteService.cs");
        var formal = LeerServicioAjusteFormal();

        Assert.Contains("IAjusteInventarioService _ajustes", adapter);
        Assert.Equal(2, ContarOcurrencias(adapter, "_ajustes.AjustarStockCompatibilidadAsync("));

        Assert.DoesNotContain("IInventarioConcurrencyService", adapter);
        Assert.DoesNotContain("IMovimientoInventarioRepository", adapter);
        Assert.DoesNotContain("IProductoRepository", adapter);
        Assert.DoesNotContain("IUnitOfWork", adapter);
        Assert.DoesNotContain("IAuditoriaService", adapter);
        Assert.DoesNotContain("new MovimientoInventario", adapter);
        Assert.DoesNotContain("AjusteProductoVariante", adapter);
        Assert.DoesNotContain("AjusteProducto\"", adapter);

        Assert.Contains("AjustarStockCompatibilidadAsync", formal);
        Assert.Contains("cantidadesEsperadas.TryGetValue", formal);
        Assert.Contains("cantidadEsperada != cantidadAnterior", formal);
        Assert.Contains("StockFisico", formal);
        Assert.Contains("SincronizarProyeccionLegacy", formal);
    }

    private static string LeerServicioAjusteFormal() => string.Join(
        Environment.NewLine,
        Leer("backend/src/Application/Services/AjusteInventarioService.N14.Core.cs"),
        Leer("backend/src/Application/Services/AjusteInventarioService.N14.Anular.cs"),
        Leer("backend/src/Application/Services/AjusteInventarioService.N14.Internal.cs"),
        Leer("backend/src/Application/Services/AjusteInventarioService.N14.Helpers.cs"));

    private static int ContarOcurrencias(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string Leer(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "src")))
            directory = directory.Parent;

        Assert.NotNull(directory);
        var path = Path.Combine(
            directory!.FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"No se encontró el archivo de fuente requerido: {path}");
        return File.ReadAllText(path);
    }
}
