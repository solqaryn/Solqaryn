using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class ConteoInventarioRepository : IConteoInventarioRepository
{
    private readonly AppDbContext _context;

    public ConteoInventarioRepository(AppDbContext context) => _context = context;

    private DbSet<ConteoInventario> Conteos => _context.Set<ConteoInventario>();

    private IQueryable<ConteoInventario> ConDetalle(bool tracking = false)
    {
        var query = tracking ? Conteos.AsTracking() : Conteos.AsNoTracking();
        return query
            .Include(x => x.Almacen)
            .Include(x => x.UbicacionAlmacen)
            .Include(x => x.Categoria)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery();
    }

    public async Task<(List<ConteoInventario> Items, int TotalCount)> GetPagedAsync(ConteoInventarioQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);
        IQueryable<ConteoInventario> query = Conteos.AsNoTracking();

        if (filtro.Estado.HasValue) query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.Tipo.HasValue) query = query.Where(x => x.Tipo == filtro.Tipo.Value);
        if (filtro.AlmacenId.HasValue) query = query.Where(x => x.AlmacenId == filtro.AlmacenId.Value);
        if (filtro.UbicacionAlmacenId.HasValue) query = query.Where(x => x.UbicacionAlmacenId == filtro.UbicacionAlmacenId.Value);
        if (filtro.CategoriaId.HasValue) query = query.Where(x => x.CategoriaId == filtro.CategoriaId.Value);
        if (filtro.Desde.HasValue) query = query.Where(x => x.FechaCreacion >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue) query = query.Where(x => x.FechaCreacion <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x => x.Numero.Contains(search) || (x.Observaciones != null && x.Observaciones.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Include(x => x.Almacen)
            .Include(x => x.UbicacionAlmacen)
            .Include(x => x.Categoria)
            .Include(x => x.Detalles).ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public Task<ConteoInventario?> GetByIdAsync(int id) =>
        ConDetalle().FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ConteoInventario?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var conteo = await Conteos
            .FromSqlInterpolated($"SELECT c.* FROM ConteosInventario c WHERE c.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (conteo is null) return null;

        await _context.Entry(conteo).Reference(x => x.Almacen).LoadAsync();
        await _context.Entry(conteo).Reference(x => x.UbicacionAlmacen).LoadAsync();
        await _context.Entry(conteo).Reference(x => x.Categoria).LoadAsync();
        await _context.Entry(conteo).Collection(x => x.Detalles).Query()
            .Include(x => x.ProductoVariante)
            .LoadAsync();
        return conteo;
    }

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        var normalizado = numero.Trim();
        return Conteos.AsNoTracking().AnyAsync(x => x.Numero == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value));
    }

    public Task AddAsync(ConteoInventario conteo) => Conteos.AddAsync(conteo).AsTask();
    public void Update(ConteoInventario conteo) => Conteos.Update(conteo);
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
