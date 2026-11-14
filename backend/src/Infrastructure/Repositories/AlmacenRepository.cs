using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class AlmacenRepository : IAlmacenRepository
{
    private readonly AppDbContext _context;

    public AlmacenRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<Almacen> Almacenes => _context.Set<Almacen>();

    public async Task<Almacen?> GetByIdAsync(int id) =>
        await Almacenes
            .Include(a => a.Sucursal)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<(List<Almacen> Items, int Total)> BuscarAsync(
        string? termino,
        bool? activo,
        int? sucursalId,
        TipoAlmacen? tipo,
        int pagina,
        int tamanoPagina)
    {
        var query = Almacenes
            .AsNoTracking()
            .Include(a => a.Sucursal)
            .AsQueryable();

        if (activo.HasValue)
            query = query.Where(a => a.Activo == activo.Value);

        if (sucursalId.HasValue)
            query = query.Where(a => a.SucursalId == sucursalId.Value);

        if (tipo.HasValue)
            query = query.Where(a => a.Tipo == tipo.Value);

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var valor = termino.Trim();
            query = query.Where(a =>
                a.Codigo.Contains(valor) ||
                a.Nombre.Contains(valor) ||
                a.Sucursal.Codigo.Contains(valor) ||
                a.Sucursal.Nombre.Contains(valor));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Sucursal.Codigo)
            .ThenBy(a => a.Codigo)
            .ThenBy(a => a.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Almacen>> GetActivosAsync(int? sucursalId = null)
    {
        var query = Almacenes
            .AsNoTracking()
            .Include(a => a.Sucursal)
            .Where(a => a.Activo && a.Sucursal.Activa);

        if (sucursalId.HasValue)
            query = query.Where(a => a.SucursalId == sucursalId.Value);

        return await query
            .OrderBy(a => a.Sucursal.Codigo)
            .ThenBy(a => a.Codigo)
            .ThenBy(a => a.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await Almacenes.AnyAsync(a =>
            a.Codigo.ToUpper() == normalizado &&
            (!excluirId.HasValue || a.Id != excluirId.Value));
    }

    public async Task AddAsync(Almacen almacen) =>
        await Almacenes.AddAsync(almacen);

    public void Update(Almacen almacen) =>
        Almacenes.Update(almacen);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
