using Solqaryn.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solqaryn.Application.Interfaces;

public sealed record InventarioDemanda(
    int ProductoId,
    int? ProductoVarianteId,
    int Cantidad);

public sealed class InventarioLockSet
{
    public InventarioLockSet(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, ProductoVariante> variantes,
        IReadOnlyList<InventarioDemanda>? demandas = null)
    {
        Productos = productos;
        Variantes = variantes;
        Demandas = demandas ?? Array.Empty<InventarioDemanda>();
    }

    public IReadOnlyDictionary<int, Producto> Productos { get; }
    public IReadOnlyDictionary<int, ProductoVariante> Variantes { get; }
    public IReadOnlyList<InventarioDemanda> Demandas { get; }
}

public interface IInventarioConcurrencyService
{
    Task<InventarioLockSet> BloquearYValidarInventarioAsync(
        IEnumerable<InventarioDemanda> demandMap,
        bool esDeduccion = true);

    /// Bloquea las mismas filas en el orden global del protocolo 2B, pero permite
    /// recuperar entidades inactivas o eliminadas lógicamente para restauraciones
    /// históricas. Nunca reactiva ni elimina el soft-delete de las entidades.
    Task<InventarioLockSet> BloquearInventarioParaReversionAsync(
        IEnumerable<InventarioDemanda> demandMap);

    Task AjustarStockPesimistaAsync(
        int productoId,
        int? productoVarianteId,
        int cantidadActualEsperada,
        int cantidadNueva);
}
