using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Infrastructure.Repositories;

public class VentaRepository : IVentaRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUsuarioScopeService _usuarioScope;

    public VentaRepository(
        AppDbContext context,
        ICurrentUserService currentUser,
        IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _currentUser = currentUser;
        _usuarioScope = usuarioScope;
    }

    private IQueryable<Venta> ConIncludes() =>
        _context.Ventas
            .Include(v => v.MetodoPagoCatalogo)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
                    .ThenInclude(p => p!.Imagenes)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.ProductoVariante)
                    .ThenInclude(v => v!.Color)
            .Include(v => v.Factura)
            .Include(v => v.DescuentosAplicados)
            .Include(v => v.ImpuestosAplicados)
            .AsSplitQuery();

    private static IQueryable<Venta> AplicarAlcance(
        IQueryable<Venta> query,
        UsuarioScopeActual? alcance,
        int? usuarioSolicitadoPorAdministrador = null)
    {
        if (alcance is null)
            return query.Where(_ => false);

        if (alcance.EsAdministrador)
        {
            return usuarioSolicitadoPorAdministrador.HasValue
                ? query.Where(v => v.CreadoPorUsuarioId == usuarioSolicitadoPorAdministrador.Value)
                : query;
        }

        // Para cualquier rol no administrador, el único alcance válido es el
        // Usuario.Id autenticado. Se ignora cualquier parámetro enviado por HTTP.
        return query.Where(v => v.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task<Venta?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConIncludes(), alcance)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Venta?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
            return null;

        Venta? cabecera;
        if (alcance.EsAdministrador)
        {
            cabecera = await _context.Ventas
                .FromSqlInterpolated($"SELECT v.* FROM Ventas v WHERE v.Id = {id} AND v.Eliminado = 0 FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }
        else
        {
            cabecera = await _context.Ventas
                .FromSqlInterpolated($"SELECT v.* FROM Ventas v WHERE v.Id = {id} AND v.Eliminado = 0 AND v.CreadoPorUsuarioId = {alcance.UsuarioId} FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        if (cabecera is null)
            return null;

        await _context.Entry(cabecera).Reference(v => v.MetodoPagoCatalogo).LoadAsync();
        await _context.Entry(cabecera).Collection(v => v.Detalles).LoadAsync();
        await _context.Entry(cabecera).Collection(v => v.DescuentosAplicados).LoadAsync();
        await _context.Entry(cabecera).Collection(v => v.ImpuestosAplicados).LoadAsync();

        return cabecera;
    }

    public async Task<CatalogoMetodoPago?> GetMetodoPagoPorCodigoONombreAsync(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var normalizado = valor.Trim();
        return await _context.Set<CatalogoMetodoPago>()
            .AsTracking()
            .FirstOrDefaultAsync(m =>
                m.Activo && !m.Eliminado &&
                (m.Codigo == normalizado || m.Nombre == normalizado));
    }

    public async Task<(List<Venta> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? request.UsuarioIdScope : null;
        var query = AplicarAlcance(ConIncludes().AsNoTracking(), alcance, usuarioSolicitado);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(v =>
                v.NumeroVenta.ToLower().Contains(search) ||
                v.ClienteNombre.ToLower().Contains(search) ||
                (v.ClienteIdentidadORTN != null && v.ClienteIdentidadORTN.ToLower().Contains(search)) ||
                (v.ClienteTelefono != null && v.ClienteTelefono.ToLower().Contains(search)) ||
                (v.ClienteCorreo != null && v.ClienteCorreo.ToLower().Contains(search)) ||
                (v.Notas != null && v.Notas.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(v => v.Total) : query.OrderBy(v => v.Total),
            "clientenombre" => sortDirDesc ? query.OrderByDescending(v => v.ClienteNombre) : query.OrderBy(v => v.ClienteNombre),
            _ => sortDirDesc ? query.OrderByDescending(v => v.Fecha) : query.OrderBy(v => v.Fecha),
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> GetTotalDelMesAsync(int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), alcance, usuarioSolicitado);
        return await query.CountAsync(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetIngresosDelMesAsync(int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), alcance, usuarioSolicitado);
        return await query
            .Where(v => v.Fecha >= inicioMes && v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    public async Task<decimal> GetCuentasPorCobrarAsync(int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), alcance, usuarioSolicitado);
        return await query
            .Where(v => v.Estado == EstadoDocumento.Confirmada && v.EstadoPago != EstadoPago.Pagado)
            .SumAsync(v => (decimal?)v.Total) ?? 0m;
    }

    public async Task<decimal> GetUtilidadBrutaTotalAsync(int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        var query = AplicarAlcance(_context.Ventas.AsQueryable(), alcance, usuarioSolicitado);
        return await query
            .Where(v => v.Estado == EstadoDocumento.Confirmada)
            .SumAsync(v => (decimal?)v.UtilidadBruta) ?? 0m;
    }

    public async Task<List<Venta>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        return await AplicarAlcance(ConIncludes(), alcance, usuarioSolicitado)
            .OrderByDescending(v => v.Fecha)
            .Take(cantidad)
            .ToListAsync();
    }

    public async Task AddAsync(Venta venta) =>
        await _context.Ventas.AddAsync(venta);

    public void Update(Venta venta) =>
        _context.Ventas.Update(venta);

    public async Task<bool> SaveChangesAsync()
    {
        var borradoresEliminados = _context.ChangeTracker.Entries<Venta>()
            .Where(e => e.State == EntityState.Modified &&
                        e.Entity.Estado == EstadoDocumento.Borrador &&
                        e.Entity.Detalles.Count == 0 &&
                        !e.Entity.Eliminado)
            .ToList();

        foreach (var entry in borradoresEliminados)
        {
            foreach (var detalleEntry in _context.ChangeTracker.Entries<VentaDetalle>()
                         .Where(d => d.State == EntityState.Deleted && d.Entity.VentaId == entry.Entity.Id))
            {
                detalleEntry.State = EntityState.Unchanged;
            }

            entry.Entity.Eliminado = true;
            entry.Entity.FechaEliminacion = DateTime.UtcNow;
            entry.Entity.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            entry.Entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            entry.Entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            entry.Entity.FechaActualizacion = DateTime.UtcNow;
        }

        await CompletarSnapshotImpuestosAsync();
        return await _context.SaveChangesAsync() > 0;
    }

    private async Task CompletarSnapshotImpuestosAsync()
    {
        var nuevos = _context.ChangeTracker.Entries<VentaImpuesto>()
            .Where(e => e.State == EntityState.Added)
            .ToList();
        if (nuevos.Count == 0) return;

        var ids = nuevos.Select(e => e.Entity.ImpuestoId).Distinct().ToList();
        var configuracion = await _context.Impuestos.AsNoTracking()
            .Where(i => ids.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.IncluidoEnPrecio);

        foreach (var entry in nuevos)
        {
            entry.Entity.IncluidoEnPrecioSnapshot =
                configuracion.TryGetValue(entry.Entity.ImpuestoId, out var incluido) && incluido;
        }
    }
}
