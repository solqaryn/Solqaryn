using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IAjusteInventarioConsultaService
{
    Task<PagedResult<AjusteInventarioDto>> GetPagedAsync(AjusteInventarioFiltroDto filtro);
}
