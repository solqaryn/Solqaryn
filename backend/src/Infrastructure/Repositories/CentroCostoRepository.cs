using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class CentroCostoRepository : ICentroCostoRepository
{
    private readonly AppDbContext _context;

    public CentroCostoRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<CentroCosto> CentrosCosto => _context.Set<CentroCosto>();

    public async Task<CentroCosto?> GetByIdAsync(int id) =>
        await CentrosCosto
            .Include(c => c.Sucursal)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CentroCosto?> GetByCodigoAsync(string codigo)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await CentrosCosto
            .Include(c => c.Sucursal)
            .FirstOrDefaultAsync(c => c.Codigo.ToUpper() == normalizado);
    }

    public async Task<(List<CentroCosto> Items, int Total)> BuscarAsync(
        string? termino,
        TipoCentroCosto? tipo,
        int? sucursalId,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        var query = CentrosCosto
            .AsNoTracking()
            .Include(c => c.Sucursal)
            .AsQueryable();

        if (tipo.HasValue)
            query = query.Where(c => c.Tipo == tipo.Value);

        if (sucursalId.HasValue)
            query = query.Where(c => c.SucursalId == sucursalId.Value);

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var valor = termino.Trim();
            query = query.Where(c =>
                c.Codigo.Contains(valor) ||
                c.Nombre.Contains(valor) ||
                (c.Descripcion != null && c.Descripcion.Contains(valor)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Codigo)
            .ThenBy(c => c.Nombre)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<CentroCosto>> GetActivosAsync(
        TipoCentroCosto? tipo = null,
        int? sucursalId = null)
    {
        var query = CentrosCosto
            .AsNoTracking()
            .Include(c => c.Sucursal)
            .Where(c => c.Activo);

        if (tipo.HasValue)
            query = query.Where(c => c.Tipo == tipo.Value);

        if (sucursalId.HasValue)
            query = query.Where(c => c.SucursalId == sucursalId.Value);

        return await query
            .OrderBy(c => c.Codigo)
            .ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null)
    {
        var normalizado = codigo.Trim().ToUpper();
        return await CentrosCosto.AnyAsync(c =>
            c.Codigo.ToUpper() == normalizado &&
            (!excluirId.HasValue || c.Id != excluirId.Value));
    }

    public async Task AddAsync(CentroCosto centroCosto) =>
        await CentrosCosto.AddAsync(centroCosto);

    public void Update(CentroCosto centroCosto) =>
        CentrosCosto.Update(centroCosto);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
