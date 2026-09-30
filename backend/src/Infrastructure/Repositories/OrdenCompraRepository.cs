using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class OrdenCompraRepository : IOrdenCompraRepository
{
    private const string IdempotencyConstraint = "UX_OrdenesCompra_IdempotencyKey";
    private const string NumeroConstraint = "UX_OrdenesCompra_NumeroOrden";
    private readonly AppDbContext _context;

    public OrdenCompraRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<OrdenCompra> ConDetalles(bool tracking = false)
    {
        var query = _context.Set<OrdenCompra>()
            .Include(x => x.Detalles)
            .AsSplitQuery();
        return tracking ? query.AsTracking() : query.AsNoTracking();
    }

    public async Task<(IReadOnlyList<OrdenCompra> Items, int Total)> GetPagedAsync(OrdenCompraFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<OrdenCompra>().AsNoTracking().AsQueryable();

        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.SolicitudCompraId.HasValue)
            query = query.Where(x => x.SolicitudCompraId == filtro.SolicitudCompraId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(x => x.NumeroOrden.Contains(numero));
        }
        if (filtro.Desde.HasValue)
            query = query.Where(x => x.FechaCreacion >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(x => x.FechaCreacion <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(x => x.NumeroOrden.Contains(search) || x.ProveedorNombreSnapshot.Contains(search));
        }

        var total = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numeroorden" or "numero" => desc ? query.OrderByDescending(x => x.NumeroOrden) : query.OrderBy(x => x.NumeroOrden),
            "estado" => desc ? query.OrderByDescending(x => x.Estado) : query.OrderBy(x => x.Estado),
            "proveedor" => desc ? query.OrderByDescending(x => x.ProveedorNombreSnapshot) : query.OrderBy(x => x.ProveedorNombreSnapshot),
            "fechaesperadautc" or "fechaesperada" => desc ? query.OrderByDescending(x => x.FechaEsperadaUtc) : query.OrderBy(x => x.FechaEsperadaUtc),
            _ => desc ? query.OrderByDescending(x => x.FechaCreacion) : query.OrderBy(x => x.FechaCreacion)
        };

        var ids = await query
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => x.Id)
            .ToListAsync();

        if (ids.Count == 0)
            return (Array.Empty<OrdenCompra>(), total);

        var byId = await ConDetalles()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var items = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        return (items, total);
    }

    public Task<OrdenCompra?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<OrdenCompra?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var orden = await _context.Set<OrdenCompra>()
            .FromSqlInterpolated($"SELECT o.* FROM OrdenesCompra o WHERE o.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (orden is not null)
            await _context.Entry(orden).Collection(x => x.Detalles).LoadAsync();
        return orden;
    }

    public Task<OrdenCompra?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) =>
        _context.Set<OrdenCompra>().AsNoTracking()
            .AnyAsync(x => x.NumeroOrden == numero && (!excluirId.HasValue || x.Id != excluirId.Value));

    public Task<string?> GetUltimoNumeroAsync(string prefijo) =>
        _context.Set<OrdenCompra>().AsNoTracking()
            .Where(x => x.NumeroOrden.StartsWith(prefijo))
            .OrderByDescending(x => x.NumeroOrden)
            .Select(x => x.NumeroOrden)
            .FirstOrDefaultAsync();

    public Task AddAsync(OrdenCompra orden) => _context.Set<OrdenCompra>().AddAsync(orden).AsTask();

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, IdempotencyConstraint))
        {
            throw new UniqueConstraintViolationException(
                IdempotencyConstraint,
                "La clave de idempotencia ya fue utilizada por otra solicitud concurrente.",
                ex);
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, NumeroConstraint))
        {
            throw new UniqueConstraintViolationException(
                NumeroConstraint,
                "El número de orden de compra ya existe.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detalle = ex.InnerException?.Message ?? ex.Message;
        return detalle.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
