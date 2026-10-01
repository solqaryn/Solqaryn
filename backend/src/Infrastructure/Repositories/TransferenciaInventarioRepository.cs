using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class TransferenciaInventarioRepository : ITransferenciaInventarioRepository
{
    private readonly AppDbContext _context;

    public TransferenciaInventarioRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<TransferenciaInventario> Transferencias => _context.Set<TransferenciaInventario>();

    private IQueryable<TransferenciaInventario> ConDetalle() =>
        Transferencias
            .Include(t => t.AlmacenOrigen)
            .Include(t => t.AlmacenDestino)
            .Include(t => t.Detalles)
                .ThenInclude(d => d.ProductoVariante)
            .AsSplitQuery();

    public async Task<(List<TransferenciaInventario> Items, int TotalCount)> GetPagedAsync(
        TransferenciaInventarioFiltroDto filtro)
    {
        IQueryable<TransferenciaInventario> query = Transferencias.AsNoTracking();

        if (filtro.UsuarioIdScope.HasValue)
            query = query.Where(t => t.CreadoPorUsuarioId == filtro.UsuarioIdScope.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(t => t.Estado == filtro.Estado.Value);
        if (filtro.AlmacenOrigenId.HasValue)
            query = query.Where(t => t.AlmacenOrigenId == filtro.AlmacenOrigenId.Value);
        if (filtro.AlmacenDestinoId.HasValue)
            query = query.Where(t => t.AlmacenDestinoId == filtro.AlmacenDestinoId.Value);
        if (filtro.Desde.HasValue)
            query = query.Where(t => t.FechaCreacion >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(t => t.FechaCreacion <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(t => t.Numero.Contains(numero));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim();
            query = query.Where(t =>
                t.Numero.Contains(search) ||
                (t.Observaciones != null && t.Observaciones.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var desc = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = filtro.SortBy?.Trim().ToLowerInvariant() switch
        {
            "numero" => desc
                ? query.OrderByDescending(t => t.Numero).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.Numero).ThenBy(t => t.Id),
            "estado" => desc
                ? query.OrderByDescending(t => t.Estado).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.Estado).ThenBy(t => t.Id),
            "almacenorigen" or "almacenorigenid" => desc
                ? query.OrderByDescending(t => t.AlmacenOrigenId).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.AlmacenOrigenId).ThenBy(t => t.Id),
            "almacendestino" or "almacendestinoid" => desc
                ? query.OrderByDescending(t => t.AlmacenDestinoId).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.AlmacenDestinoId).ThenBy(t => t.Id),
            _ => desc
                ? query.OrderByDescending(t => t.FechaCreacion).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.FechaCreacion).ThenBy(t => t.Id)
        };

        var items = await query
            .Include(t => t.AlmacenOrigen)
            .Include(t => t.AlmacenDestino)
            .Include(t => t.Detalles)
                .ThenInclude(d => d.ProductoVariante)
            .AsSplitQuery()
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<TransferenciaInventario?> GetByIdAsync(int id) =>
        await ConDetalle().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<TransferenciaInventario?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var transferencia = await Transferencias
            .FromSqlInterpolated($"SELECT t.* FROM TransferenciasInventario t WHERE t.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (transferencia is not null)
        {
            await _context.Entry(transferencia).Reference(t => t.AlmacenOrigen).LoadAsync();
            await _context.Entry(transferencia).Reference(t => t.AlmacenDestino).LoadAsync();
            await _context.Entry(transferencia)
                .Collection(t => t.Detalles)
                .Query()
                .Include(d => d.ProductoVariante)
                .LoadAsync();
        }

        return transferencia;
    }

    public async Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        var normalizado = numero.Trim();
        return await Transferencias.AsNoTracking().AnyAsync(t =>
            t.Numero == normalizado && (!excluirId.HasValue || t.Id != excluirId.Value));
    }

    public async Task AddAsync(TransferenciaInventario transferencia) =>
        await Transferencias.AddAsync(transferencia);

    public void Update(TransferenciaInventario transferencia) =>
        Transferencias.Update(transferencia);

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
