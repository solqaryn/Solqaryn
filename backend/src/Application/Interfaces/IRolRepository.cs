using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IRolRepository
{
    Task<Rol?> GetByIdAsync(int id);
    Task<List<Rol>> GetAllAsync(bool incluirEliminados = false);
    Task<bool> ExisteNombreAsync(string nombreNormalizado, int? excluirId = null);
    Task<int> ContarUsuariosAsync(int rolId);
    Task<int> ContarPermisosAsync(int rolId);
    Task<int> ContarAdministradoresActivosAsync(int? excluirRolId = null);

    /// Cuenta cuántos ROLES (no usuarios) activos y no eliminados tienen
    /// EsAdministrador=true, excluyendo el indicado. Distinto de
    /// ContarAdministradoresActivosAsync (que cuenta usuarios con ese rol).
    Task<int> ContarRolesAdministradorAsync(int? excluirRolId = null);
    Task AddAsync(Rol rol);
    void Update(Rol rol);
    void Remove(Rol rol);
    Task SaveChangesAsync();
}
