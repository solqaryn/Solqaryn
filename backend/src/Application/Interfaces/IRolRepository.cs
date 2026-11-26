using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IRolRepository
{
    Task<Rol?> GetByIdAsync(int id);
    Task<List<Rol>> GetAllAsync(bool incluirEliminados = false);
    Task<List<Rol>> GetAllEmpresaAsync(int empresaId, bool incluirEliminados = false);
    Task<Rol?> GetByIdEmpresaAsync(int id, int empresaId);
    Task<int> ContarMembresiasEmpresaAsync(int rolId, int empresaId);
    Task<int> ContarMembresiasPorRolAsync(int rolId);
    Task<int> ContarAdministradoresEmpresaActivosAsync(int empresaId, int excluirRolId);
    Task<int> ContarRolesAdministradorEmpresaAsync(int empresaId, int excluirRolId);
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
