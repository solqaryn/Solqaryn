using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public sealed class ReservaInventarioRepository : IReservaInventarioRepository
{
    private readonly AppDbContext _context;
    public ReservaInventarioRepository(AppDbContext context) => _context = context;
    private DbSet<ReservaInventario> Reservas => _context.Set<ReservaInventario>();

    private IQueryable<ReservaInventario> ConDetalle(bool tracking)
    {
        var query = tracking ? Reservas.AsTracking() : Reservas.AsNoTracking();
        return query.Include(x => x.Detalles).ThenInclude(x => x.ProductoVariante).AsSplitQuery();
    }

    public async Task<(IReadOnlyList<ReservaInventario> Items, int Total)> GetPagedAsync(ReservaInventarioQueryDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        IQueryable<ReservaInventario> query = Reservas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var busqueda = filtro.Busqueda.Trim();
            query = query.Where(x => x.Numero.Contains(busqueda));
        }
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && Enum.TryParse<EstadoReservaInventario>(filtro.Estado, true, out var estado))
            query = query.Where(x => x.Estado == estado);
        if (filtro.VentaId.HasValue) query = query.Where(x => x.VentaId == filtro.VentaId.Value);
        if (filtro.AlmacenId.HasValue) query = query.Where(x => x.Detalles.Any(d => d.AlmacenId == filtro.AlmacenId.Value));
        if (filtro.ExpiraDesde.HasValue) query = query.Where(x => x.FechaExpiracion >= filtro.ExpiraDesde.Value);
        if (filtro.ExpiraHasta.HasValue) query = query.Where(x => x.FechaExpiracion <= filtro.ExpiraHasta.Value);

        var total = await query.CountAsync();
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var items = await query.OrderByDescending(x => x.FechaCreacion).ThenByDescending(x => x.Id)
            .Include(x => x.Detalles).ThenInclude(x => x.ProductoVariante).AsSplitQuery()
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<ReservaInventario?> GetByIdAsync(int id, bool tracking = false) =>
        ConDetalle(tracking).FirstOrDefaultAsync(x => x.Id == id);

    public Task<ReservaInventario?> GetByPedidoVentaIdAsync(int pedidoVentaId, bool tracking = false) =>
        ConDetalle(tracking).FirstOrDefaultAsync(x => x.PedidoVentaId == pedidoVentaId);

    public Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null)
    {
        var normalizado = numero.Trim();
        return Reservas.AsNoTracking().AnyAsync(x => x.Numero == normalizado && (!excluirId.HasValue || x.Id != excluirId.Value));
    }

    public Task AddAsync(ReservaInventario reserva) => Reservas.AddAsync(reserva).AsTask();
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
