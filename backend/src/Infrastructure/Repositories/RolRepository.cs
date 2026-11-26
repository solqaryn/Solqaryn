using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
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

    private IQueryable<Rol> RolesEmpresa(int empresaId) =>
        _context.Roles.Where(r => empresaId > 0 &&
            r.Ambito == AmbitoAutorizacion.Empresa &&
            (r.EmpresaId == empresaId || (r.EmpresaId == null && r.EsSistema)));

    public async Task<List<Rol>> GetAllEmpresaAsync(int empresaId, bool incluirEliminados = false) =>
        await RolesEmpresa(empresaId).Where(r => incluirEliminados || !r.Eliminado)
            .OrderBy(r => r.Nombre).ToListAsync();

    public async Task<Rol?> GetByIdEmpresaAsync(int id, int empresaId) =>
        await RolesEmpresa(empresaId).FirstOrDefaultAsync(r => r.Id == id && !r.Eliminado);

    public async Task<int> ContarMembresiasEmpresaAsync(int rolId, int empresaId) =>
        await _context.UsuarioEmpresas.CountAsync(m => m.RolId == rolId && m.EmpresaId == empresaId);

    public async Task<int> ContarMembresiasPorRolAsync(int rolId) =>
        await _context.UsuarioEmpresas.CountAsync(m => m.RolId == rolId);

    public async Task<int> ContarAdministradoresEmpresaActivosAsync(int empresaId, int excluirRolId) =>
        await (from m in _context.UsuarioEmpresas
               join u in _context.Usuarios on m.UsuarioId equals u.Id
               join r in RolesEmpresa(empresaId) on m.RolId equals r.Id
               where m.EmpresaId == empresaId && m.Activa && u.Activo && !u.Eliminado &&
                   !u.Bloqueado && r.Id != excluirRolId && r.Activo && !r.Eliminado && r.EsAdministrador
               select m.UsuarioId).Distinct().CountAsync();

    public async Task<int> ContarRolesAdministradorEmpresaAsync(int empresaId, int excluirRolId) =>
        await RolesEmpresa(empresaId).CountAsync(r => r.Id != excluirRolId &&
            r.Activo && !r.Eliminado && r.EsAdministrador);

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
