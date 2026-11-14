using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class RentabilidadVentasService : IRentabilidadVentasService
{
    private const string SemanticaVenta = "SALE_HEADER_TOTAL_COST_GROSS_PROFIT__HEADER_DISCOUNT_ALREADY_REFLECTED_IN_VENTA_UTILIDAD_BRUTA";
    private const string SemanticaLinea = "LINE_SUBTOTAL_HISTORICAL_COST_GROSS_PROFIT__EXCLUDES_SALE_HEADER_DISCOUNT_ALLOCATION";

    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public RentabilidadVentasService(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    public async Task<IReadOnlyList<ReporteRentabilidadDto>> ObtenerAsync(
        ReporteVentasFiltroDto filtro,
        RentabilidadAgrupacion agrupacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var errores = ReporteVentasQueryRules.Validate(filtro);
        if (errores.Count > 0)
            throw new ArgumentException(string.Join(" ", errores), nameof(filtro));

        var alcance = await _usuarioScope.ObtenerActualAsync();
        var ventas = ConstruirVentasFiltradas(filtro, alcance);

        return agrupacion switch
        {
            RentabilidadAgrupacion.Vendedor => await PorVendedorAsync(ventas, cancellationToken),
            RentabilidadAgrupacion.Cliente => await PorClienteAsync(ventas, cancellationToken),
            RentabilidadAgrupacion.Producto => await PorProductoAsync(ventas, filtro, cancellationToken),
            RentabilidadAgrupacion.Categoria => await PorCategoriaAsync(ventas, filtro, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(agrupacion))
        };
    }

    private static async Task<IReadOnlyList<ReporteRentabilidadDto>> PorVendedorAsync(
        IQueryable<Venta> ventas,
        CancellationToken cancellationToken)
    {
        return await ventas
            .GroupBy(v => new { v.CreadoPorUsuarioId, v.CreadoPorNombreUsuario })
            .Select(g => new ReporteRentabilidadDto
            {
                AgrupacionId = g.Key.CreadoPorUsuarioId,
                Agrupacion = "vendedor",
                Nombre = g.Key.CreadoPorNombreUsuario ?? "Sin vendedor",
                Venta = g.Sum(v => v.Total),
                Costo = g.Sum(v => v.CostoTotal),
                UtilidadBruta = g.Sum(v => v.UtilidadBruta),
                IncluyeDescuentoEncabezadoEnUtilidad = true,
                Semantica = SemanticaVenta
            })
            .OrderByDescending(x => x.UtilidadBruta)
            .ThenBy(x => x.AgrupacionId)
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<ReporteRentabilidadDto>> PorClienteAsync(
        IQueryable<Venta> ventas,
        CancellationToken cancellationToken)
    {
        return await ventas
            .GroupBy(v => new { v.ClienteId, v.ClienteNombre })
            .Select(g => new ReporteRentabilidadDto
            {
                AgrupacionId = g.Key.ClienteId,
                Agrupacion = "cliente",
                Nombre = g.Key.ClienteNombre,
                Venta = g.Sum(v => v.Total),
                Costo = g.Sum(v => v.CostoTotal),
                UtilidadBruta = g.Sum(v => v.UtilidadBruta),
                IncluyeDescuentoEncabezadoEnUtilidad = true,
                Semantica = SemanticaVenta
            })
            .OrderByDescending(x => x.UtilidadBruta)
            .ThenBy(x => x.AgrupacionId)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ReporteRentabilidadDto>> PorProductoAsync(
        IQueryable<Venta> ventas,
        ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var ventaIds = ventas.Select(v => v.Id);
        var detalles = AplicarDimensionesDetalle(_context.VentaDetalles.AsNoTracking(), filtro)
            .Where(d => ventaIds.Contains(d.VentaId));

        return await detalles
            .GroupBy(d => new { d.ProductoId, Nombre = d.Producto != null ? d.Producto.Nombre : d.ProductoNombreSnapshot })
            .Select(g => new ReporteRentabilidadDto
            {
                AgrupacionId = g.Key.ProductoId,
                Agrupacion = "producto",
                Nombre = g.Key.Nombre,
                Venta = g.Sum(d => d.Subtotal),
                Costo = g.Sum(d => d.Cantidad * d.CostoUnitarioSnapshot),
                UtilidadBruta = g.Sum(d => d.UtilidadBruta),
                IncluyeDescuentoEncabezadoEnUtilidad = false,
                Semantica = SemanticaLinea
            })
            .OrderByDescending(x => x.UtilidadBruta)
            .ThenBy(x => x.AgrupacionId)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ReporteRentabilidadDto>> PorCategoriaAsync(
        IQueryable<Venta> ventas,
        ReporteVentasFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var ventaIds = ventas.Select(v => v.Id);
        var detalles = AplicarDimensionesDetalle(_context.VentaDetalles.AsNoTracking(), filtro)
            .Where(d => ventaIds.Contains(d.VentaId))
            .Where(d => d.Producto != null && d.Producto.CategoriaId.HasValue);

        return await detalles
            .GroupBy(d => new
            {
                Id = d.Producto!.CategoriaId,
                Nombre = d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin categoria"
            })
            .Select(g => new ReporteRentabilidadDto
            {
                AgrupacionId = g.Key.Id,
                Agrupacion = "categoria",
                Nombre = g.Key.Nombre,
                Venta = g.Sum(d => d.Subtotal),
                Costo = g.Sum(d => d.Cantidad * d.CostoUnitarioSnapshot),
                UtilidadBruta = g.Sum(d => d.UtilidadBruta),
                IncluyeDescuentoEncabezadoEnUtilidad = false,
                Semantica = SemanticaLinea
            })
            .OrderByDescending(x => x.UtilidadBruta)
            .ThenBy(x => x.AgrupacionId)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Venta> ConstruirVentasFiltradas(
        ReporteVentasFiltroDto filtro,
        UsuarioScopeActual? alcance)
    {
        var query = _context.Ventas
            .AsNoTracking()
            .Where(v => !v.Eliminado && v.Estado == EstadoDocumento.Confirmada);

        if (alcance is null)
            return query.Where(_ => false);

        query = alcance.EsAdministrador
            ? (filtro.VendedorId.HasValue
                ? query.Where(v => v.CreadoPorUsuarioId == filtro.VendedorId.Value)
                : query)
            : query.Where(v => v.CreadoPorUsuarioId == alcance.UsuarioId);

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
            var matchingSaleIds = AplicarDimensionesDetalle(_context.VentaDetalles.AsNoTracking(), filtro)
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
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.MarcaId == filtro.MarcaId.Value
                : d.Producto != null && d.Producto.MarcaId == filtro.MarcaId.Value);
        if (filtro.ModeloId.HasValue)
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.ModeloId == filtro.ModeloId.Value
                : d.Producto != null && d.Producto.ModeloId == filtro.ModeloId.Value);
        if (filtro.ColorId.HasValue)
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.ColorId == filtro.ColorId.Value
                : d.Producto != null && d.Producto.ColorId == filtro.ColorId.Value);
        if (filtro.TallaId.HasValue)
            query = query.Where(d => d.ProductoVarianteId.HasValue
                ? d.ProductoVariante != null && d.ProductoVariante.TallaId == filtro.TallaId.Value
                : d.Producto != null && d.Producto.TallaId == filtro.TallaId.Value);
        return query;
    }
}
