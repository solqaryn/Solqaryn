using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class DevolucionClienteRepository : IDevolucionClienteRepository
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public DevolucionClienteRepository(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _usuarioScope = usuarioScope ?? throw new ArgumentNullException(nameof(usuarioScope));
    }

    private DbSet<DevolucionCliente> Devoluciones => _context.Set<DevolucionCliente>();

    private IQueryable<DevolucionCliente> ConDetalles(bool tracking)
    {
        var query = tracking ? Devoluciones.AsTracking() : Devoluciones.AsNoTracking();
        return query.Include(x => x.Venta).Include(x => x.Factura).Include(x => x.Detalles).AsSplitQuery();
    }

    private async Task<IQueryable<DevolucionCliente>> AplicarAlcanceAsync(IQueryable<DevolucionCliente> query)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
            return query.Where(_ => false);
        return alcance.EsAdministrador ? query : query.Where(x => x.Venta.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task<DevolucionCliente?> GetByIdAsync(int id, bool asNoTracking = false)
    {
        var query = await AplicarAlcanceAsync(ConDetalles(!asNoTracking));
        return await query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<DevolucionCliente?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
            return null;

        DevolucionCliente? devolucion;
        if (alcance.EsAdministrador)
        {
            devolucion = await Devoluciones
                .FromSqlInterpolated($"SELECT d.* FROM DevolucionesCliente d WHERE d.Id = {id} FOR UPDATE")
                .AsTracking().FirstOrDefaultAsync();
        }
        else
        {
            devolucion = await Devoluciones
                .FromSqlInterpolated($"SELECT d.* FROM DevolucionesCliente d INNER JOIN Ventas v ON v.Id = d.VentaId WHERE d.Id = {id} AND v.CreadoPorUsuarioId = {alcance.UsuarioId} FOR UPDATE")
                .AsTracking().FirstOrDefaultAsync();
        }

        if (devolucion is not null)
        {
            await _context.Entry(devolucion).Reference(x => x.Venta).LoadAsync();
            await _context.Entry(devolucion).Reference(x => x.Factura).LoadAsync();
            await _context.Entry(devolucion).Collection(x => x.Detalles).LoadAsync();
        }
        return devolucion;
    }

    public async Task<DevolucionCliente?> GetByIdempotencyKeyAsync(string key, bool tracking = false)
    {
        var query = await AplicarAlcanceAsync(ConDetalles(tracking));
        return await query.FirstOrDefaultAsync(x => x.IdempotencyKey == key);
    }

    public async Task<DevolucionCliente?> GetByIdempotencyKeyForUpdateAsync(string key)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdempotencyKeyForUpdateAsync requiere una transacción activa.");

        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
            return null;

        DevolucionCliente? devolucion;
        if (alcance.EsAdministrador)
        {
            devolucion = await Devoluciones
                .FromSqlInterpolated($"SELECT d.* FROM DevolucionesCliente d WHERE d.IdempotencyKey = {key} FOR UPDATE")
                .AsTracking().FirstOrDefaultAsync();
        }
        else
        {
            devolucion = await Devoluciones
                .FromSqlInterpolated($"SELECT d.* FROM DevolucionesCliente d INNER JOIN Ventas v ON v.Id = d.VentaId WHERE d.IdempotencyKey = {key} AND v.CreadoPorUsuarioId = {alcance.UsuarioId} FOR UPDATE")
                .AsTracking().FirstOrDefaultAsync();
        }
        if (devolucion is not null)
            await _context.Entry(devolucion).Collection(x => x.Detalles).LoadAsync();
        return devolucion;
    }

    public async Task<(List<DevolucionCliente> Items, int Total)> GetPagedAsync(DevolucionClienteFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var query = await AplicarAlcanceAsync(Devoluciones.AsNoTracking());
        if (filtro.VentaId.HasValue)
            query = query.Where(x => x.VentaId == filtro.VentaId.Value);
        if (filtro.Estado.HasValue)
            query = query.Where(x => x.Estado == filtro.Estado.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Include(x => x.Detalles).AsSplitQuery().ToListAsync();
        return (items, total);
    }

    public async Task<int> GetCantidadConfirmadaPorVentaDetalleAsync(int ventaDetalleId)
    {
        var query = await AplicarAlcanceAsync(Devoluciones.AsNoTracking());
        return await query.Where(x => x.Estado == EstadoDevolucionCliente.Confirmada)
            .SelectMany(x => x.Detalles)
            .Where(x => x.VentaDetalleId == ventaDetalleId)
            .SumAsync(x => (int?)x.Cantidad) ?? 0;
    }

    public Task AddAsync(DevolucionCliente devolucion) => Devoluciones.AddAsync(devolucion).AsTask();
    public void Update(DevolucionCliente devolucion) => Devoluciones.Update(devolucion);
    public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
}
