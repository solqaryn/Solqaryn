using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICotizacionService
{
    Task<CotizacionDto> GetByIdAsync(int id);
    Task<PagedResult<CotizacionDto>> GetPagedAsync(CotizacionFiltroDto request);
    Task<CotizacionDto> CrearAsync(CreateCotizacionDto dto);
    Task<CotizacionDto> ActualizarAsync(UpdateCotizacionDto dto);
    Task EliminarAsync(int id);
    Task<CotizacionDto> EnviarAsync(int id);
    Task<CotizacionDto> AceptarAsync(int id);
    Task<CotizacionDto> RechazarAsync(int id, string motivo);
    Task<CotizacionDto> ConvertirAsync(int id);
    Task<CotizacionDto> DuplicarComoBorradorAsync(int id);
}
