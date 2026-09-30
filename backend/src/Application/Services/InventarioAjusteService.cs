using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.Application.Services;

/// <summary>
/// Adaptador de compatibilidad para los endpoints legacy de ajuste directo.
/// La única autoridad que crea, confirma y materializa cambios de stock es IAjusteInventarioService.
/// </summary>
public sealed class InventarioAjusteService : IInventarioAjusteService
{
    private readonly IAjusteInventarioService _ajustes;

    public InventarioAjusteService(IAjusteInventarioService ajustes)
    {
        _ajustes = ajustes;
    }

    public Task<AjusteStockResultadoDto> AjustarProductoAsync(
        int productoId,
        AjusteStockRequest request) =>
        _ajustes.AjustarStockCompatibilidadAsync(productoId, null, request);

    public Task<AjusteStockResultadoDto> AjustarVarianteAsync(
        int productoId,
        int varianteId,
        AjusteStockRequest request) =>
        _ajustes.AjustarStockCompatibilidadAsync(productoId, varianteId, request);
}
