using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IDescuentoService
{
    Task<List<DescuentoDto>> GetAllAsync(bool incluirEliminados = false);
    Task<DescuentoDto?> GetByIdAsync(int id);
    Task<DescuentoDto> CreateAsync(GuardarDescuentoDto dto);
    Task<DescuentoDto> UpdateAsync(int id, GuardarDescuentoDto dto);
    Task<DescuentoDto> ActivarAsync(int id);
    Task<DescuentoDto> DesactivarAsync(int id);
    Task EliminarLogicoAsync(int id);
    Task EliminarPermanenteAsync(int id);
    Task<DescuentoDto> DuplicarAsync(int id, string nuevoNombre);
}
