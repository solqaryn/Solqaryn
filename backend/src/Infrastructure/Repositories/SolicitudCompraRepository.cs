using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class SolicitudCompraRepository : ISolicitudCompraRepository
{
    private readonly AppDbContext _context;
    public SolicitudCompraRepository(AppDbContext context) => _context = context;
    private DbSet<SolicitudCompra> Solicitudes => _context.Set<SolicitudCompra>();

    private IQueryable<SolicitudCompra> ConDetalle(bool tracking)
    {
        var query = tracking ? Solicitudes.AsTracking() : Solicitudes.AsNoTracking();
        return query
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.Producto)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery();
    }

    public async Task<(IReadOnlyList<SolicitudCompra> Items, int Total)> GetPagedAsync(SolicitudCompraFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        IQueryable<SolicitudCompra> query = Solicitudes.AsNoTracking();

        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.Desde.HasValue)
            query = query.Where(x => x.FechaCreacion >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(x => x.FechaCreacion <= filtro.Hasta.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Numero))
        {
            var numero = filtro.Numero.Trim();
            query = query.Where(x => x.NumeroSolicitud.Contains(numero));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var items = await query
            .OrderByDescending(x => x.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.Producto)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.ProductoVariante)
            .AsSplitQuery()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<SolicitudCompra?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalle(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<SolicitudCompra?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var solicitud = await Solicitudes
            .FromSqlInterpolated($"SELECT sc.* FROM SolicitudesCompra sc WHERE sc.Id = {id} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync();

        if (solicitud is not null)
        {
            await _context.Entry(solicitud).Reference(x => x.Proveedor).LoadAsync();
            await _context.Entry(solicitud)
                .Collection(x => x.Detalles)
                .Query()
                .Include(x => x.Producto)
                .Include(x => x.ProductoVariante)
                .LoadAsync();
        }

        return solicitud;
    }

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        var normalizado = numero.Trim();
        return Solicitudes.AsNoTracking()
            .AnyAsync(x => x.NumeroSolicitud == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value));
    }

    public Task AddAsync(SolicitudCompra solicitud) => Solicitudes.AddAsync(solicitud).AsTask();
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
