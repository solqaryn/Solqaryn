using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IAjusteInventarioService
{
    Task<List<AjusteInventarioDto>> GetAllAsync();
    Task<AjusteInventarioDto?> GetByIdAsync(int id);
    Task<AjusteInventarioDto> CreateAsync(CreateAjusteInventarioDto dto);
    Task<AjusteInventarioDto?> UpdateAsync(int id, UpdateAjusteInventarioDto dto);
    Task<AjusteInventarioDto?> ConfirmarAsync(int id);
    Task<AjusteStockResultadoDto> AjustarStockCompatibilidadAsync(
        int productoId,
        int? varianteId,
        AjusteStockRequest request);
    Task<AjusteInventarioDto?> AnularAsync(int id, string motivoAnulacion);
}
