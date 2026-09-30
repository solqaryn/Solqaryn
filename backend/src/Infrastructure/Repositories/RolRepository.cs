using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class RolRepository : IRolRepository
{
    private readonly AppDbContext _context;

    public RolRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rol?> GetByIdAsync(int id) =>
        await _context.Roles.FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);

    public async Task<List<Rol>> GetAllAsync(bool incluirEliminados = false) =>
        await _context.Roles
            .Where(r => incluirEliminados || !r.Eliminado)
            .OrderBy(r => r.Nombre)
            .ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombreNormalizado, int? excluirId = null) =>
        await _context.Roles.AnyAsync(r =>
            !r.Eliminado &&
            r.NombreNormalizado == nombreNormalizado &&
            (excluirId == null || r.Id != excluirId));

    public async Task<int> ContarUsuariosAsync(int rolId) =>
        await _context.Usuarios.CountAsync(u => u.RolId == rolId);

    public async Task<int> ContarPermisosAsync(int rolId) =>
        await _context.RolPermisos.CountAsync(p => p.RolId == rolId);

    public async Task<int> ContarAdministradoresActivosAsync(int? excluirRolId = null) =>
        await _context.Usuarios
            .Include(u => u.RolEntidad)
            .CountAsync(u => u.Activo &&
                !u.Eliminado &&
                !u.Bloqueado &&
                u.RolEntidad.EsAdministrador &&
                u.RolEntidad.Activo &&
                !u.RolEntidad.Eliminado &&
                (excluirRolId == null || u.RolId != excluirRolId));

    public async Task<int> ContarRolesAdministradorAsync(int? excluirRolId = null) =>
        await _context.Roles.CountAsync(r =>
            r.Activo && !r.Eliminado && r.EsAdministrador &&
            (excluirRolId == null || r.Id != excluirRolId));

    public async Task AddAsync(Rol rol) => await _context.Roles.AddAsync(rol);
    public void Update(Rol rol) => _context.Roles.Update(rol);
    public void Remove(Rol rol) => _context.Roles.Remove(rol);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
