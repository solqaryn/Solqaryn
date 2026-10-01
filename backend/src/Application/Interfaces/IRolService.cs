using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IRolService
{
    Task<List<RolDto>> GetAllAsync(bool incluirEliminados = false);
    Task<RolDto?> GetByIdAsync(int id);
    Task<RolDto> CreateAsync(CrearRolDto dto);
    Task<RolDto> UpdateAsync(int id, ActualizarRolDto dto);
    Task<RolDto> ActivarAsync(int id);
    Task<RolDto> DesactivarAsync(int id);
    Task EliminarLogicoAsync(int id);
    Task EliminarPermanenteAsync(int id);
    Task<RolDto> DuplicarAsync(int id, string nuevoNombre);
}
