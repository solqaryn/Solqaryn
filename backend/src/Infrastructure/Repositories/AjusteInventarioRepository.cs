using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class AjusteInventarioRepository : IAjusteInventarioRepository
{
    private readonly AppDbContext _context;

    public AjusteInventarioRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<AjusteInventario> ConDetalles() =>
        _context.AjustesInventario
            .Include(a => a.Detalles)
            .AsSplitQuery();

    public async Task<List<AjusteInventario>> GetAllAsync() =>
        await ConDetalles()
            .OrderByDescending(a => a.FechaAjuste)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

    public async Task<(List<AjusteInventario> Items, int TotalCount)> GetPagedAsync(
        AjusteInventarioFiltroDto filtro)
    {
        IQueryable<AjusteInventario> query = _context.AjustesInventario.AsNoTracking();

        if (filtro.Estado.HasValue)
            query = query.Where(a => a.Estado == filtro.Estado.Value);
        if (filtro.Desde.HasValue)
            query = query.Where(a => a.FechaAjuste >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(a => a.FechaAjuste <= filtro.Hasta.Value);
        if (filtro.ProductoId.HasValue)
            query = query.Where(a => a.Detalles.Any(d => d.ProductoId == filtro.ProductoId.Value));
        if (filtro.ProductoVarianteId.HasValue)
            query = query.Where(a => a.Detalles.Any(d => d.ProductoVarianteId == filtro.ProductoVarianteId.Value));

        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(a =>
                a.NumeroAjuste.Contains(search) ||
                a.Motivo.Contains(search) ||
                (a.Observaciones != null && a.Observaciones.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numeroajuste" or "numero" => desc
                ? query.OrderByDescending(a => a.NumeroAjuste)
                : query.OrderBy(a => a.NumeroAjuste),
            "estado" => desc
                ? query.OrderByDescending(a => a.Estado).ThenByDescending(a => a.Id)
                : query.OrderBy(a => a.Estado).ThenBy(a => a.Id),
            "motivo" => desc
                ? query.OrderByDescending(a => a.Motivo).ThenByDescending(a => a.Id)
                : query.OrderBy(a => a.Motivo).ThenBy(a => a.Id),
            _ => desc
                ? query.OrderByDescending(a => a.FechaAjuste).ThenByDescending(a => a.Id)
                : query.OrderBy(a => a.FechaAjuste).ThenBy(a => a.Id)
        };

        var items = await query
            .Include(a => a.Detalles)
            .AsSplitQuery()
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<AjusteInventario?> GetByIdAsync(int id) =>
        await ConDetalles().FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AjusteInventario?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var ajuste = await _context.AjustesInventario
            .FromSqlInterpolated($"SELECT a.* FROM AjustesInventario a WHERE a.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (ajuste is not null)
        {
            await _context.Entry(ajuste)
                .Collection(a => a.Detalles)
                .LoadAsync();
        }

        return ajuste;
    }

    public async Task AddAsync(AjusteInventario ajuste) =>
        await _context.AjustesInventario.AddAsync(ajuste);

    public void Update(AjusteInventario ajuste) =>
        _context.AjustesInventario.Update(ajuste);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
