using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;

namespace Solqaryn.Application.Interfaces;

public interface IPeriodoContableService
{
    Task<PagedResult<PeriodoContableDto>> GetPagedAsync(PeriodoContableQueryDto filter);
    Task<PeriodoContableDto?> GetByIdAsync(int id);
    Task<PeriodoContableDto> CreateAsync(CrearPeriodoContableDto dto);
    Task CerrarAsync(int id);
    Task ValidarOperacionAsync(DateTime fechaOperacion, bool autorizadoCambioRetroactivo = false);
}
