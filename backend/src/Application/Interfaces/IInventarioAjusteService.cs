using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IInventarioAjusteService
{
    Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request);

    Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request);
}
