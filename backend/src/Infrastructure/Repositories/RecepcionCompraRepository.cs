using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class RecepcionCompraRepository : IRecepcionCompraRepository
{
    private const string IdempotencyConstraint = "UX_RecepcionesCompra_IdempotencyKey";
    private const string NumeroConstraint = "UX_RecepcionesCompra_NumeroRecepcion";
    private readonly AppDbContext _context;

    public RecepcionCompraRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private IQueryable<RecepcionCompra> ConDetalles(bool tracking = false)
    {
        var query = _context.Set<RecepcionCompra>()
            .Include(x => x.OrdenCompra)
            .Include(x => x.Detalles)
            .AsSplitQuery();
        return tracking ? query.AsTracking() : query.AsNoTracking();
    }

    public async Task<(IReadOnlyList<RecepcionCompra> Items, int Total)> GetPagedAsync(RecepcionCompraQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = _context.Set<RecepcionCompra>().AsNoTracking().AsQueryable();

        if (filtro.OrdenCompraId.HasValue)
            query = query.Where(x => x.OrdenCompraId == filtro.OrdenCompraId.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.DesdeUtc.HasValue)
            query = query.Where(x => x.FechaCreacion >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue)
            query = query.Where(x => x.FechaCreacion <= filtro.HastaUtc.Value);

        var total = await query.CountAsync();
        var ids = await query
            .OrderByDescending(x => x.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => x.Id)
            .ToListAsync();

        if (ids.Count == 0)
            return (Array.Empty<RecepcionCompra>(), total);

        var byId = await ConDetalles()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        return (ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList(), total);
    }

    public Task<RecepcionCompra?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<RecepcionCompra?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var recepcion = await _context.Set<RecepcionCompra>()
            .FromSqlInterpolated($"SELECT r.* FROM RecepcionesCompra r WHERE r.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();
        if (recepcion is not null)
        {
            await _context.Entry(recepcion).Reference(x => x.OrdenCompra).LoadAsync();
            await _context.Entry(recepcion).Collection(x => x.Detalles).LoadAsync();
        }
        return recepcion;
    }

    public Task<RecepcionCompra?> GetByIdempotencyKeyAsync(string idempotencyKey, bool tracking = false) =>
        ConDetalles(tracking).FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);

    public async Task<decimal> GetCantidadAceptadaAcumuladaPorDetalleAsync(int ordenCompraDetalleId, int? excluirRecepcionId = null)
    {
        var query = _context.Set<RecepcionCompraDetalle>()
            .AsNoTracking()
            .Where(x => x.OrdenCompraDetalleId == ordenCompraDetalleId && x.RecepcionCompra.Estado == EstadoRecepcionCompra.Recibida);
        if (excluirRecepcionId.HasValue)
            query = query.Where(x => x.RecepcionCompraId != excluirRecepcionId.Value);

        return await query.SumAsync(x => x.CantidadRecibida - x.CantidadDanada - x.CantidadSobrante);
    }

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) =>
        _context.Set<RecepcionCompra>().AsNoTracking()
            .AnyAsync(x => x.NumeroRecepcion == numero && (!excluirId.HasValue || x.Id != excluirId.Value));

    public Task AddAsync(RecepcionCompra recepcion) =>
        _context.Set<RecepcionCompra>().AddAsync(recepcion).AsTask();

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
                "La clave de idempotencia ya fue utilizada por otra recepción concurrente.",
                ex);
        }
        catch (DbUpdateException ex) when (ContieneConstraint(ex, NumeroConstraint))
        {
            throw new UniqueConstraintViolationException(
                NumeroConstraint,
                "El número de recepción ya existe.",
                ex);
        }
    }

    private static bool ContieneConstraint(DbUpdateException ex, string constraintName)
    {
        var detalle = ex.InnerException?.Message ?? ex.Message;
        return detalle.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
    }
}
