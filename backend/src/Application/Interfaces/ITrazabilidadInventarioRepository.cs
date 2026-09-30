using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface ITrazabilidadInventarioRepository
{
    Task<(IReadOnlyList<LoteInventario> Items, int Total)> GetLotesPagedAsync(LoteInventarioQueryDto query);
    Task<LoteInventario?> GetLoteByIdAsync(int id, bool tracking = false);
    Task<LoteInventario?> GetLoteByCodigoAsync(int productoVarianteId, string codigo, bool tracking = false);
    Task<bool> TryAddLoteAsync(LoteInventario lote);

    Task<(IReadOnlyList<SerieInventario> Items, int Total)> GetSeriesPagedAsync(SerieInventarioQueryDto query);
    Task<SerieInventario?> GetSerieByIdAsync(int id, bool tracking = false);
    Task<SerieInventario?> GetSerieByNumeroAsync(string numeroSerie, bool tracking = false);
    Task<bool> TryAddSerieAsync(SerieInventario serie);

    Task<bool> TieneStockFisicoAsync(int productoVarianteId);
    Task<bool> TieneLotesActivosAsync(int productoVarianteId);
    Task<bool> TieneLotesActivosSinVencimientoAsync(int productoVarianteId);
    Task<bool> TieneSeriesActivasAsync(int productoVarianteId);
    Task<bool> TieneSeriesActivasEnLoteAsync(int loteInventarioId);
    Task SaveChangesAsync();
}
