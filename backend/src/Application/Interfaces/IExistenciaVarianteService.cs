using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IExistenciaVarianteService
{
    Task<PagedResult<ExistenciaVarianteDto>> BuscarAsync(ExistenciaVarianteFiltroDto filtro);
    Task<ExistenciaVarianteDto?> GetByIdAsync(int id);
    Task<ExistenciaVarianteDto> CreateAsync(CreateExistenciaVarianteDto dto);
    Task<ExistenciaVarianteDto?> UpdateConfiguracionAsync(int id, UpdateExistenciaVarianteConfiguracionDto dto);
}
