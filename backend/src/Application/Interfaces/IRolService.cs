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

    // Contrato de administración empresarial: nunca usar los métodos legacy
    // desde rutas tenant autorizadas.
    Task<List<RolDto>> GetAllEmpresaAsync(int empresaId, bool incluirEliminados = false);
    Task<RolDto?> GetByIdEmpresaAsync(int id, int empresaId);
    Task<RolDto> CreateEmpresaAsync(CrearRolDto dto, int empresaId);
    Task<RolDto> UpdateEmpresaAsync(int id, ActualizarRolDto dto, int empresaId);
    Task<RolDto> ActivarEmpresaAsync(int id, int empresaId);
    Task<RolDto> DesactivarEmpresaAsync(int id, int empresaId);
    Task EliminarLogicoEmpresaAsync(int id, int empresaId);
    Task EliminarPermanenteEmpresaAsync(int id, int empresaId);
    Task<RolDto> DuplicarEmpresaAsync(int id, string nuevoNombre, int empresaId);
}
