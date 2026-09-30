using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class PermisoRepository : IPermisoRepository
{
    private readonly AppDbContext _context;

    public PermisoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Permiso?> GetByIdAsync(int id) =>
        await _context.Permisos.FirstOrDefaultAsync(p => p.Id == id && !p.Eliminado);

    public async Task<Permiso?> GetByModuloAccionAsync(ModuloSistema modulo, AccionPermiso accion) =>
        await _context.Permisos.FirstOrDefaultAsync(p =>
            p.Modulo == modulo && p.Accion == accion && p.Activo && !p.Eliminado);

    public async Task<List<Permiso>> GetAllAsync(bool incluirEliminados = false) =>
        await _context.Permisos
            .Where(p => incluirEliminados || !p.Eliminado)
            .OrderBy(p => p.Modulo).ThenBy(p => p.Accion)
            .ToListAsync();

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null) =>
        await _context.Permisos.AnyAsync(p =>
            !p.Eliminado && p.Codigo == codigo && (excluirId == null || p.Id != excluirId));

    public async Task<bool> ExisteModuloAccionAsync(ModuloSistema modulo, AccionPermiso accion, int? excluirId = null) =>
        await _context.Permisos.AnyAsync(p =>
            !p.Eliminado && p.Modulo == modulo && p.Accion == accion && (excluirId == null || p.Id != excluirId));

    public async Task<int> ContarAsignacionesAsync(int permisoId) =>
        await _context.RolPermisos.CountAsync(rp => rp.PermisoId == permisoId);

    public async Task AddAsync(Permiso permiso) => await _context.Permisos.AddAsync(permiso);
    public void Update(Permiso permiso) => _context.Permisos.Update(permiso);
    public void Remove(Permiso permiso) => _context.Permisos.Remove(permiso);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}
