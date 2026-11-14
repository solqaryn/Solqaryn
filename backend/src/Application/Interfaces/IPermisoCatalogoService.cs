using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IPermisoCatalogoService
{
    Task<List<PermisoCatalogoDto>> GetAllAsync(bool incluirEliminados = false);
    Task<PermisoCatalogoDto?> GetByIdAsync(int id);
    Task<PermisoCatalogoDto> CreateAsync(CrearPermisoDto dto);
    Task<PermisoCatalogoDto> UpdateAsync(int id, ActualizarPermisoDto dto);
    Task<PermisoCatalogoDto> ActivarAsync(int id);
    Task<PermisoCatalogoDto> DesactivarAsync(int id);
    Task EliminarLogicoAsync(int id);
    Task EliminarPermanenteAsync(int id);
    Task<PermisoCatalogoDto> DuplicarAsync(int id, string nuevoNombre, string nuevaAccion);

    /// Siembra en el catálogo cualquier combinación Módulo/Acción definida en
    /// CatalogoPermisosBase que aún no exista. Idempotente: nunca modifica ni
    /// desactiva filas ya existentes (respeta cambios manuales del administrador).
    Task SembrarCatalogoAsync();
}
