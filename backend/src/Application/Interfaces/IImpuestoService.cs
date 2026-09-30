using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IImpuestoService
{
    Task<List<ImpuestoDto>> GetAllAsync(bool incluirEliminados = false);
    Task<ImpuestoDto?> GetByIdAsync(int id);
    Task<ImpuestoDto> CreateAsync(GuardarImpuestoDto dto);
    Task<ImpuestoDto> UpdateAsync(int id, GuardarImpuestoDto dto);
    Task<ImpuestoDto> ActivarAsync(int id);
    Task<ImpuestoDto> DesactivarAsync(int id);
    Task EliminarLogicoAsync(int id);
    Task EliminarPermanenteAsync(int id);
    Task<ImpuestoDto> DuplicarAsync(int id, string nuevoNombre, string nuevoCodigo);
}
