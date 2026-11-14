using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IInventarioPublicoService
{
    Task<IReadOnlyDictionary<int, InventarioPublicoVarianteDto>> ObtenerPorVariantesAsync(
        IEnumerable<int> productoVarianteIds);
}
