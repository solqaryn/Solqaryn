using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Read-only sales reporting query service. The implementation deliberately uses
/// server-side projections instead of VentaRepository.ConIncludes() so report
/// queries do not materialize the operational sale aggregate graph.
/// </summary>
public sealed class ReporteVentasService : IReporteVentasService
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public ReporteVentasService(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    public async Task<ReporteVentasResumenDto> ObtenerResumenAsync(
        ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        Validar(filtro);
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var ventas = ConstruirVentasFiltradas(filtro, alcance);

        return await ventas
            .GroupBy(_ => 1)
            .Select(g => new ReporteVentasResumenDto
            {
                ImporteBruto = g.Sum(v => v.ImporteBruto),
                Subtotal = g.Sum(v => v.Subtotal),
                Descuento = g.Sum(v => v.Descuento),
                Impuesto = g.Sum(v => v.Impuesto),
                Total = g.Sum(v => v.Total),
                CostoTotal = g.Sum(v => v.CostoTotal),
                UtilidadBruta = g.Sum(v => v.UtilidadBruta)
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? new ReporteVentasResumenDto();
    }

    public async Task<PagedResult<ReporteVentasDetalleDto>> ObtenerDetallePaginadoAsync(
        ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        Validar(filtro);
        var alcance = await _usuarioScope.ObtenerActualAsync();
        var ventaIds = ConstruirVentasFiltradas(filtro, alcance).Select(v => v.Id);

        var detalles = AplicarDimensionesDetalle(
                _context.VentaDetalles.AsNoTracking(),
                filtro)
            .Where(d => ventaIds.Contains(d.VentaId));

        var totalCount = await detalles.CountAsync(cancellationToken);
        var descendente = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        detalles = (filtro.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "subtotal" => descendente
                ? detalles.OrderByDescending(d => d.Subtotal).ThenByDescending(d => d.Id)
                : detalles.OrderBy(d => d.Subtotal).ThenBy(d => d.Id),
            "cantidad" => descendente
                ? detalles.OrderByDescending(d => d.Cantidad).ThenByDescending(d => d.Id)
                : detalles.OrderBy(d => d.Cantidad).ThenBy(d => d.Id),
            "productonombre" or "producto" => descendente
                ? detalles.OrderByDescending(d => d.ProductoNombreSnapshot).ThenByDescending(d => d.Id)
                : detalles.OrderBy(d => d.ProductoNombreSnapshot).ThenBy(d => d.Id),
            _ => descendente
                ? detalles.OrderByDescending(d => d.Venta!.Fecha).ThenByDescending(d => d.Id)
                : detalles.OrderBy(d => d.Venta!.Fecha).ThenBy(d => d.Id)
        };

        var items = await detalles
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(d => new ReporteVentasDetalleDto
            {
                VentaId = d.VentaId,
                NumeroVenta = d.Venta!.NumeroVenta,
                Fecha = d.Venta.Fecha,
                ClienteId = d.Venta.ClienteId,
                ClienteNombre = d.Venta.ClienteNombre,
                VendedorId = d.Venta.CreadoPorUsuarioId,
                VendedorNombre = d.Venta.CreadoPorNombreUsuario,
                SucursalId = d.Almacen != null ? d.Almacen.SucursalId : null,
                CategoriaId = d.Producto != null ? d.Producto.CategoriaId : null,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                ProductoNombre = d.ProductoNombreSnapshot,
                ProductoMarca = d.ProductoMarcaSnapshot,
                ProductoModelo = d.ProductoModeloSnapshot,
                ProductoColor = d.ProductoColorSnapshot,
                ProductoTalla = d.ProductoTallaSnapshot,
                ProductoSku = d.ProductoSkuSnapshot,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                CostoUnitario = d.CostoUnitarioSnapshot,
                Subtotal = d.Subtotal,
                UtilidadBruta = d.UtilidadBruta
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ReporteVentasDetalleDto>
        {
            Items = items,
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = totalCount
        };
    }

    private IQueryable<Venta> ConstruirVentasFiltradas(
        ReporteVentasFiltroDto filtro,
        UsuarioScopeActual? alcance)
    {
        var query = _context.Ventas
            .AsNoTracking()
            .Where(v => !v.Eliminado && v.Estado == EstadoDocumento.Confirmada);

        // Fail closed when the authenticated database-backed scope cannot be resolved.
        if (alcance is null)
            return query.Where(_ => false);

        // A non-admin can never expand row scope with VendedorId supplied by HTTP.
        query = alcance.EsAdministrador
            ? (filtro.VendedorId.HasValue
                ? query.Where(v => v.CreadoPorUsuarioId == filtro.VendedorId.Value)
                : query)
            : query.Where(v => v.CreadoPorUsuarioId == alcance.UsuarioId);

        // Time range is lower-inclusive / upper-exclusive to avoid end-of-day ambiguity.
        if (filtro.Desde.HasValue)
            query = query.Where(v => v.Fecha >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(v => v.Fecha < filtro.Hasta.Value);
        if (filtro.ClienteId.HasValue)
            query = query.Where(v => v.ClienteId == filtro.ClienteId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Search))
        {
            var search = filtro.Search.Trim().ToLower();
            query = query.Where(v =>
                v.NumeroVenta.ToLower().Contains(search) ||
                v.ClienteNombre.ToLower().Contains(search) ||
                (v.ClienteIdentidadORTN != null && v.ClienteIdentidadORTN.ToLower().Contains(search)));
        }

        if (ReporteVentasQueryRules.RequiresDetailDimensionJoin(filtro))
        {
            var matchingSaleIds = AplicarDimensionesDetalle(
                    _context.VentaDetalles.AsNoTracking(),
                    filtro)
                .Select(d => d.VentaId)
                .Distinct();
            query = query.Where(v => matchingSaleIds.Contains(v.Id));
        }

        return query;
    }

    private static IQueryable<VentaDetalle> AplicarDimensionesDetalle(
        IQueryable<VentaDetalle> query,
        ReporteVentasFiltroDto filtro)
    {
        if (filtro.SucursalId.HasValue)
            query = query.Where(d => d.Almacen != null && d.Almacen.SucursalId == filtro.SucursalId.Value);
        if (filtro.CategoriaId.HasValue)
            query = query.Where(d => d.Producto != null && d.Producto.CategoriaId == filtro.CategoriaId.Value);
        if (filtro.ProductoId.HasValue)
            query = query.Where(d => d.ProductoId == filtro.ProductoId.Value);
        if (filtro.VarianteId.HasValue)
            query = query.Where(d => d.ProductoVarianteId == filtro.VarianteId.Value);

        if (filtro.MarcaId.HasValue)
        {
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.MarcaId == filtro.MarcaId.Value
                : d.Producto != null && d.Producto.MarcaId == filtro.MarcaId.Value);
        }

        if (filtro.ModeloId.HasValue)
        {
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.ModeloId == filtro.ModeloId.Value
                : d.Producto != null && d.Producto.ModeloId == filtro.ModeloId.Value);
        }

        if (filtro.ColorId.HasValue)
        {
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.ColorId == filtro.ColorId.Value
                : d.Producto != null && d.Producto.ColorId == filtro.ColorId.Value);
        }

        if (filtro.TallaId.HasValue)
        {
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.TallaId == filtro.TallaId.Value
                : d.Producto != null && d.Producto.TallaId == filtro.TallaId.Value);
        }

        return query;
    }

    private static void Validar(ReporteVentasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            throw new ArgumentException(string.Join(" ", errores), nameof(filtro));
    }
}
