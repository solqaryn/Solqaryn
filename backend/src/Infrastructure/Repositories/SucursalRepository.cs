using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class SucursalRepository : ISucursalRepository
{
    private readonly AppDbContext _context;

    public SucursalRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<Sucursal> Sucursales => _context.Set<Sucursal>();

    public async Task<Sucursal?> GetByIdAsync(int id) =>
        await Sucursales.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Sucursal?> GetByCodigoAsync(string codigo)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await Sucursales.FirstOrDefaultAsync(s => s.Codigo.ToUpper() == normalizado);
    }

    public async Task<(List<Sucursal> Items, int Total)> BuscarAsync(
        string? termino,
        bool? activa,
        int? empresaId,
        int pagina,
        int tamanoPagina)
    {
        var query = Sucursales.AsNoTracking().AsQueryable();

        if (activa.HasValue)
            query = query.Where(s => s.Activa == activa.Value);

        if (empresaId.HasValue)
            query = query.Where(s => s.EmpresaId == empresaId.Value);

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var valor = termino.Trim();
            query = query.Where(s =>
                s.Codigo.Contains(valor) ||
                s.Nombre.Contains(valor) ||
                (s.Direccion != null && s.Direccion.Contains(valor)) ||
                (s.Telefono != null && s.Telefono.Contains(valor)) ||
                (s.Correo != null && s.Correo.Contains(valor)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Codigo)
            .ThenBy(s => s.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Sucursal>> GetActivasAsync(int? empresaId = null)
    {
        var query = Sucursales.AsNoTracking().Where(s => s.Activa);
        if (empresaId.HasValue)
            query = query.Where(s => s.EmpresaId == empresaId.Value);

        return await query
            .OrderBy(s => s.Codigo)
            .ThenBy(s => s.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await Sucursales.AnyAsync(s =>
            s.Codigo.ToUpper() == normalizado &&
            (!excluirId.HasValue || s.Id != excluirId.Value));
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int empresaId, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await Sucursales.AnyAsync(s =>
            s.EmpresaId == empresaId &&
            s.Codigo.ToUpper() == normalizado &&
            (!excluirId.HasValue || s.Id != excluirId.Value));
    }

    public async Task AddAsync(Sucursal sucursal) =>
        await Sucursales.AddAsync(sucursal);

    public void Update(Sucursal sucursal) =>
        Sucursales.Update(sucursal);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
