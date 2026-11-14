using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICuentaPorPagarService
{
    Task<PagedResult<CuentaPorPagarDto>> GetPagedAsync(CuentaPorPagarFiltroDto filtro);
    Task<CuentaPorPagarDto?> GetByIdAsync(int id);
    Task<CuentaPorPagarDto> GenerarAsync(GenerarCuentaPorPagarDto dto);
    Task<CuentaPorPagarDto> AplicarAsync(int id, AplicarCuentaPorPagarDto dto);
    Task<CuentaPorPagarDto> RevertirAplicacionAsync(int id, RevertirAplicacionCuentaPorPagarDto dto);
    Task<CuentaPorPagarDto> AnularAsync(int id, AnularCuentaPorPagarDto dto);
}
