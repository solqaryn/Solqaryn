using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ITrazabilidadInventarioService
{
    Task<ConfiguracionTrazabilidadVarianteDto?> GetConfiguracionAsync(int productoVarianteId);
    Task<ConfiguracionTrazabilidadVarianteDto> ConfigurarAsync(int productoVarianteId, ConfigurarTrazabilidadVarianteRequest request);

    Task<PagedResult<LoteInventarioDto>> GetLotesAsync(LoteInventarioQueryDto query);
    Task<LoteInventarioDto?> GetLoteByIdAsync(int id);
    Task<LoteInventarioDto> CrearLoteAsync(CrearLoteInventarioRequest request);
    Task<LoteInventarioDto> ActualizarLoteAsync(int id, ActualizarLoteInventarioRequest request);
    Task<LoteInventarioDto> DesactivarLoteAsync(int id);

    Task<PagedResult<SerieInventarioDto>> GetSeriesAsync(SerieInventarioQueryDto query);
    Task<SerieInventarioDto?> GetSerieByIdAsync(int id);
    Task<SerieInventarioDto> CrearSerieAsync(CrearSerieInventarioRequest request);
    Task<SerieInventarioDto> DarDeBajaSerieAsync(int id);
}
