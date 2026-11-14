using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Clave autoritativa de stock vivo en ERP-N1.4. El valor null de ubicación
/// representa la existencia raíz del almacén y nunca se sustituye por una
/// ubicación arbitraria.
/// </summary>
public readonly record struct InventarioExistenciaClave(
    int ProductoVarianteId,
    int AlmacenId,
    int? UbicacionAlmacenId);

public sealed record InventarioDemandaExistencia(
    int ProductoId,
    int ProductoVarianteId,
    int AlmacenId,
    int? UbicacionAlmacenId,
    int Cantidad)
{
    public InventarioExistenciaClave Clave =>
        new(ProductoVarianteId, AlmacenId, UbicacionAlmacenId);
}

public sealed class InventarioExistenciaLockSet
{
    public InventarioExistenciaLockSet(
        IReadOnlyDictionary<InventarioExistenciaClave, ExistenciaVariante> existencias,
        IReadOnlyList<InventarioDemandaExistencia>? demandas = null)
    {
        Existencias = existencias;
        Demandas = demandas ?? Array.Empty<InventarioDemandaExistencia>();
    }

    public IReadOnlyDictionary<InventarioExistenciaClave, ExistenciaVariante> Existencias { get; }
    public IReadOnlyList<InventarioDemandaExistencia> Demandas { get; }
}

public interface IExistenciaVarianteConcurrencyService
{
    /// <summary>
    /// Consolida, ordena y bloquea FOR UPDATE las existencias indicadas. Cuando
    /// esDeduccion=true valida contra StockDisponible, nunca contra cantidades legacy.
    /// </summary>
    Task<InventarioExistenciaLockSet> BloquearYValidarExistenciasAsync(
        IEnumerable<InventarioDemandaExistencia> demandas,
        bool esDeduccion = true);

    /// <summary>
    /// Variante de reversión histórica que puede recuperar una existencia
    /// inactiva/eliminada sin reactivarla ni alterar su soft-delete.
    /// </summary>
    Task<InventarioExistenciaLockSet> BloquearExistenciasParaReversionAsync(
        IEnumerable<InventarioDemandaExistencia> demandas);

    /// <summary>
    /// Ajuste con precondición optimista sobre StockFisico y lock pesimista de la
    /// misma fila autoritativa. Preserva reservado, tránsito y umbrales actuales.
    /// </summary>
    Task AjustarStockFisicoPesimistaAsync(
        InventarioExistenciaClave clave,
        int cantidadActualEsperada,
        int cantidadNueva);

    /// <summary>
    /// Ajusta StockReservado sobre la fila autoritativa ya protegida por la misma
    /// transacción. N1.8 lo usa para activar/liberar/consumir/expirar reservas sin
    /// crear una segunda autoridad de disponibilidad.
    /// </summary>
    Task AjustarStockReservadoPesimistaAsync(
        InventarioExistenciaClave clave,
        int stockReservadoActualEsperado,
        int stockReservadoNuevo) =>
        throw new NotSupportedException("El adapter de existencias no soporta actualización autoritativa de StockReservado.");

    /// <summary>
    /// Ajusta conjuntamente StockFisico y StockTransito sobre la misma existencia
    /// ya bloqueada. N1.6 lo usa para transferencias sin crear una segunda autoridad.
    /// La implementación por defecto mantiene compatibilidad binaria con adapters de
    /// pruebas antiguos y falla cerrado cuando no soportan tránsito.
    /// </summary>
    Task AjustarStocksPesimistaAsync(
        InventarioExistenciaClave clave,
        int stockFisicoActualEsperado,
        int stockFisicoNuevo,
        int stockTransitoActualEsperado,
        int stockTransitoNuevo) =>
        throw new NotSupportedException("El adapter de existencias no soporta actualización autoritativa de StockTransito.");
}
