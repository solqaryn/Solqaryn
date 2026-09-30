using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IEvaluacionProveedorService
{
    Task<PagedResult<EvaluacionProveedorDto>> GetPagedAsync(EvaluacionProveedorFiltroDto filtro);
    Task<EvaluacionProveedorDto?> GetByIdAsync(int id);
    Task<EvaluacionProveedorDto> GenerarPorRecepcionAsync(int recepcionCompraId);
}
