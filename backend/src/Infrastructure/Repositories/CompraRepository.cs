using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Infrastructure.Repositories;

public class CompraRepository : ICompraRepository
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IUsuarioScopeService _usuarioScope;

    public CompraRepository(
        AppDbContext context,
        ICurrentUserService currentUser,
        IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _currentUser = currentUser;
        _usuarioScope = usuarioScope;
    }

    private IQueryable<Compra> ConIncludes() =>
        _context.Compras
            .Include(c => c.MetodoPagoCatalogo)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
                    .ThenInclude(p => p!.Imagenes)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.ProductoVariante)
                    .ThenInclude(v => v!.Color)
            .Include(c => c.ImpuestosAplicados)
            .AsSplitQuery();

    private static IQueryable<Compra> AplicarAlcance(
        IQueryable<Compra> query,
        UsuarioScopeActual? alcance,
        int? usuarioSolicitadoPorAdministrador = null)
    {
        if (alcance is null)
            return query.Where(_ => false);

        if (alcance.EsAdministrador)
        {
            return usuarioSolicitadoPorAdministrador.HasValue
                ? query.Where(c => c.CreadoPorUsuarioId == usuarioSolicitadoPorAdministrador.Value)
                : query;
        }

        return query.Where(c => c.CreadoPorUsuarioId == alcance.UsuarioId);
    }

    public async Task<Compra?> GetByIdAsync(int id)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        return await AplicarAlcance(ConIncludes(), alcance)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Compra?> GetByIdForUpdateAsync(int id)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("GetByIdForUpdateAsync requiere una transacción activa.");

        var alcance = await _usuarioScope.ObtenerActualAsync();
        if (alcance is null)
            return null;

        Compra? cabecera;
        if (alcance.EsAdministrador)
        {
            cabecera = await _context.Compras
                .FromSqlInterpolated($"SELECT c.* FROM Compras c WHERE c.Id = {id} AND c.Eliminado = 0 FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }
        else
        {
            cabecera = await _context.Compras
                .FromSqlInterpolated($"SELECT c.* FROM Compras c WHERE c.Id = {id} AND c.Eliminado = 0 AND c.CreadoPorUsuarioId = {alcance.UsuarioId} FOR UPDATE")
                .AsTracking()
                .FirstOrDefaultAsync();
        }

        if (cabecera is null)
            return null;

        await _context.Entry(cabecera).Reference(c => c.MetodoPagoCatalogo).LoadAsync();

        // Bridge transitorio one-way ERP-N0.8.D. Una fila creada por un escritor
        // legacy después del backfill puede seguir trayendo únicamente el enum.
        // Antes de confirmar, bajo el mismo lock, se materializa la FK relacional
        // siempre que exista una equivalencia activa y no eliminada. Si no existe,
        // CompraService mantiene el rechazo fail-closed y la operación no muta stock.
        if (!cabecera.MetodoPagoId.HasValue)
        {
            var metodoPago = await GetMetodoPagoPorCodigoONombreAsync(cabecera.MetodoPago.ToString());
            if (metodoPago is not null)
            {
                cabecera.MetodoPagoId = metodoPago.Id;
                cabecera.MetodoPagoCatalogo = metodoPago;
            }
        }

        await _context.Entry(cabecera).Collection(c => c.Detalles).LoadAsync();
        await _context.Entry(cabecera).Collection(c => c.ImpuestosAplicados).LoadAsync();

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

    public async Task<(List<Compra> Items, int TotalCount)> GetPagedAsync(PagedRequest request)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? request.UsuarioIdScope : null;
        var query = AplicarAlcance(ConIncludes().AsNoTracking(), alcance, usuarioSolicitado);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.NumeroCompra.ToLower().Contains(search) ||
                c.ProveedorNombre.ToLower().Contains(search) ||
                (c.ProveedorDocumento != null && c.ProveedorDocumento.ToLower().Contains(search)) ||
                (c.ProveedorTelefono != null && c.ProveedorTelefono.ToLower().Contains(search)) ||
                (c.DocumentoReferencia != null && c.DocumentoReferencia.ToLower().Contains(search)) ||
                (c.Notas != null && c.Notas.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var sortDirDesc = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = (request.SortBy?.ToLower()) switch
        {
            "total" => sortDirDesc ? query.OrderByDescending(c => c.Total) : query.OrderBy(c => c.Total),
            "proveedornombre" => sortDirDesc ? query.OrderByDescending(c => c.ProveedorNombre) : query.OrderBy(c => c.ProveedorNombre),
            _ => sortDirDesc ? query.OrderByDescending(c => c.Fecha) : query.OrderBy(c => c.Fecha),
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
        var query = AplicarAlcance(_context.Compras.AsQueryable(), alcance, usuarioSolicitado);
        return await query.CountAsync(c => c.Fecha >= inicioMes && c.Estado == EstadoDocumento.Confirmada);
    }

    public async Task<decimal> GetCuentasPorPagarAsync(int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        var query = AplicarAlcance(_context.Compras.AsQueryable(), alcance, usuarioSolicitado);
        return await query
            .Where(c => c.Estado == EstadoDocumento.Confirmada && c.EstadoPago != EstadoPago.Pagado)
            .SumAsync(c => (decimal?)c.Total) ?? 0m;
    }

    public async Task<List<Compra>> GetUltimasAsync(int cantidad = 5, int? usuarioId = null)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var usuarioSolicitado = alcance?.EsAdministrador == true ? usuarioId : null;
        return await AplicarAlcance(ConIncludes(), alcance, usuarioSolicitado)
            .OrderByDescending(c => c.Fecha)
            .Take(cantidad)
            .ToListAsync();
    }

    public async Task<int> ContarTodasAsync() =>
        await _context.Compras.IgnoreQueryFilters().CountAsync();

    public async Task AddAsync(Compra compra) =>
        await _context.Compras.AddAsync(compra);

    public void Update(Compra compra) =>
        _context.Compras.Update(compra);

    public async Task<bool> SaveChangesAsync()
    {
        var borradoresEliminados = _context.ChangeTracker.Entries<Compra>()
            .Where(e => e.State == EntityState.Modified &&
                        e.Entity.Estado == EstadoDocumento.Borrador &&
                        e.Entity.Detalles.Count == 0 &&
                        !e.Entity.Eliminado)
            .ToList();

        foreach (var entry in borradoresEliminados)
        {
            foreach (var detalleEntry in _context.ChangeTracker.Entries<CompraDetalle>()
                         .Where(d => d.State == EntityState.Deleted && d.Entity.CompraId == entry.Entity.Id))
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
        var nuevos = _context.ChangeTracker.Entries<CompraImpuesto>()
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
