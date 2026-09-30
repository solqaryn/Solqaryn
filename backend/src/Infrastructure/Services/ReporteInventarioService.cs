using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class ReporteInventarioService : IReporteInventarioService
{
    private readonly AppDbContext _context;
    private readonly IUsuarioScopeService _usuarioScope;

    public ReporteInventarioService(AppDbContext context, IUsuarioScopeService usuarioScope)
    {
        _context = context;
        _usuarioScope = usuarioScope;
    }

    public async Task<PagedResult<ReporteInventarioStockHealthDto>> ObtenerStockHealthAsync(
        ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var tenant = await _usuarioScope.ObtenerUnicoActualAsync(cancellationToken);
        if (tenant is null)
            return Empty<ReporteInventarioStockHealthDto>(filtro.Page, filtro.PageSize);

        return await ObtenerStockHealthAsync(tenant.EmpresaId, filtro, cancellationToken);
    }

    public async Task<PagedResult<ReporteInventarioStockHealthDto>> ObtenerStockHealthAsync(
        int empresaId,
        ReporteInventarioStockHealthFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (empresaId <= 0)
            return Empty<ReporteInventarioStockHealthDto>(filtro.Page, filtro.PageSize);

        var alcance = await _usuarioScope.ObtenerActualAsync(empresaId, cancellationToken);
        if (alcance is null)
            return Empty<ReporteInventarioStockHealthDto>(filtro.Page, filtro.PageSize);

        var now = DateTime.UtcNow;
        var desde = filtro.Desde ?? now.AddDays(-filtro.Dias);
        var hasta = filtro.Hasta ?? now;
        var cutoff = now.AddDays(-filtro.Dias);

        // N6 tenant boundary: every inventory row must belong to the Empresa selected
        // by the already-validated tenant context. Nullable/legacy ownership fails closed.
        var existencias = _context.Set<ExistenciaVariante>()
            .AsNoTracking()
            .Where(e => e.Almacen.Sucursal.EmpresaId == empresaId);
        if (!alcance.EsAdministrador)
            existencias = existencias.Where(e => e.CreadoPorUsuarioId == alcance.UsuarioId);
        if (filtro.ProductoId.HasValue)
            existencias = existencias.Where(e => e.ProductoVariante.ProductoId == filtro.ProductoId.Value);
        if (filtro.ProductoVarianteId.HasValue)
            existencias = existencias.Where(e => e.ProductoVarianteId == filtro.ProductoVarianteId.Value);
        if (filtro.SucursalId.HasValue)
            existencias = existencias.Where(e => e.Almacen.SucursalId == filtro.SucursalId.Value);
        if (filtro.AlmacenId.HasValue)
            existencias = existencias.Where(e => e.AlmacenId == filtro.AlmacenId.Value);
        if (filtro.UbicacionAlmacenId.HasValue)
            existencias = existencias.Where(e => e.UbicacionAlmacenId == filtro.UbicacionAlmacenId.Value);

        var movimientos = _context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Where(m => m.Almacen != null && m.Almacen.Sucursal.EmpresaId == empresaId);
        if (!alcance.EsAdministrador)
            movimientos = movimientos.Where(m => m.CreadoPorUsuarioId == alcance.UsuarioId);

        var movimientoStats = movimientos
            .GroupBy(m => new { m.ProductoVarianteId, m.AlmacenId, m.UbicacionAlmacenId })
            .Select(g => new
            {
                g.Key.ProductoVarianteId,
                g.Key.AlmacenId,
                g.Key.UbicacionAlmacenId,
                UltimoMovimientoUtc = g.Max(m => (DateTime?)m.Fecha),
                UnidadesSalidaPeriodo = g
                    .Where(m => m.Tipo == TipoMovimientoInventario.Salida &&
                                m.Fecha >= desde && m.Fecha <= hasta)
                    .Sum(m => (int?)m.Cantidad) ?? 0
            });

        var query =
            from e in existencias
            join stats in movimientoStats
                on new
                {
                    ProductoVarianteId = (int?)e.ProductoVarianteId,
                    AlmacenId = (int?)e.AlmacenId,
                    e.UbicacionAlmacenId
                }
                equals new { stats.ProductoVarianteId, stats.AlmacenId, stats.UbicacionAlmacenId }
                into statsJoin
            from stats in statsJoin.DefaultIfEmpty()
            select new StockHealthRaw
            {
                ProductoVarianteId = e.ProductoVarianteId,
                ProductoId = e.ProductoVariante.ProductoId,
                ProductoNombre = e.ProductoVariante.Producto.Nombre,
                Sku = e.ProductoVariante.Sku,
                AlmacenId = e.AlmacenId,
                AlmacenNombre = e.Almacen.Nombre,
                UbicacionAlmacenId = e.UbicacionAlmacenId,
                UbicacionNombre = e.UbicacionAlmacen != null ? e.UbicacionAlmacen.Nombre : null,
                StockDisponible = e.StockDisponible,
                StockMinimo = e.StockMinimo,
                UltimoMovimientoUtc = stats == null ? null : stats.UltimoMovimientoUtc,
                UnidadesSalidaPeriodo = stats == null ? 0 : stats.UnidadesSalidaPeriodo
            };

        query = filtro.TipoReporte switch
        {
            TipoReporteStockHealth.StockBajo => query.Where(x => x.StockDisponible <= x.StockMinimo),
            TipoReporteStockHealth.Agotado => query.Where(x => x.StockDisponible <= 0),
            TipoReporteStockHealth.SinMovimiento => query.Where(x => x.UltimoMovimientoUtc == null || x.UltimoMovimientoUtc <= cutoff),
            TipoReporteStockHealth.Aging => query,
            TipoReporteStockHealth.Rotacion => query,
            _ => query.Where(_ => false)
        };

        var total = await query.CountAsync(cancellationToken);
        var descending = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = filtro.SortBy ?? "Fecha";
        IOrderedQueryable<StockHealthRaw> ordered = sortBy.ToLowerInvariant() switch
        {
            "producto" => descending ? query.OrderByDescending(x => x.ProductoNombre).ThenByDescending(x => x.ProductoVarianteId) : query.OrderBy(x => x.ProductoNombre).ThenBy(x => x.ProductoVarianteId),
            "sku" => descending ? query.OrderByDescending(x => x.Sku).ThenByDescending(x => x.ProductoVarianteId) : query.OrderBy(x => x.Sku).ThenBy(x => x.ProductoVarianteId),
            "stockdisponible" => descending ? query.OrderByDescending(x => x.StockDisponible).ThenByDescending(x => x.ProductoVarianteId) : query.OrderBy(x => x.StockDisponible).ThenBy(x => x.ProductoVarianteId),
            "rotacion" => descending ? query.OrderByDescending(x => (decimal)x.UnidadesSalidaPeriodo / (x.StockDisponible > 0 ? x.StockDisponible : 1)).ThenByDescending(x => x.ProductoVarianteId) : query.OrderBy(x => (decimal)x.UnidadesSalidaPeriodo / (x.StockDisponible > 0 ? x.StockDisponible : 1)).ThenBy(x => x.ProductoVarianteId),
            _ => descending ? query.OrderByDescending(x => x.UltimoMovimientoUtc).ThenByDescending(x => x.ProductoVarianteId) : query.OrderBy(x => x.UltimoMovimientoUtc).ThenBy(x => x.ProductoVarianteId)
        };

        var rows = await ordered
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReporteInventarioStockHealthDto>
        {
            Items = rows.Select(x => new ReporteInventarioStockHealthDto
            {
                ProductoVarianteId = x.ProductoVarianteId,
                ProductoId = x.ProductoId,
                ProductoNombre = x.ProductoNombre,
                Sku = x.Sku,
                AlmacenId = x.AlmacenId,
                AlmacenNombre = x.AlmacenNombre,
                UbicacionAlmacenId = x.UbicacionAlmacenId,
                UbicacionNombre = x.UbicacionNombre,
                StockDisponible = x.StockDisponible,
                StockMinimo = x.StockMinimo,
                StockBajo = x.StockDisponible <= x.StockMinimo,
                Agotado = x.StockDisponible <= 0,
                UltimoMovimientoUtc = x.UltimoMovimientoUtc,
                DiasSinMovimiento = x.UltimoMovimientoUtc.HasValue ? Math.Max(0, (int)(now - x.UltimoMovimientoUtc.Value).TotalDays) : filtro.Dias + 1,
                UnidadesSalidaPeriodo = x.UnidadesSalidaPeriodo,
                RotacionPeriodo = x.UnidadesSalidaPeriodo / (decimal)Math.Max(x.StockDisponible, 1)
            }).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ReporteInventarioReconciliacionDto>> ObtenerReporteReconciliacionAsync(
        ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var tenant = await _usuarioScope.ObtenerUnicoActualAsync(cancellationToken);
        if (tenant is null)
            return Empty<ReporteInventarioReconciliacionDto>(filtro.Page, filtro.PageSize);

        return await ObtenerReporteReconciliacionAsync(tenant.EmpresaId, filtro, cancellationToken);
    }

    public async Task<PagedResult<ReporteInventarioReconciliacionDto>> ObtenerReporteReconciliacionAsync(
        int empresaId,
        ReporteInventarioReconciliacionFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (empresaId <= 0)
            return Empty<ReporteInventarioReconciliacionDto>(filtro.Page, filtro.PageSize);

        var alcance = await _usuarioScope.ObtenerActualAsync(empresaId, cancellationToken);
        if (alcance is null)
            return Empty<ReporteInventarioReconciliacionDto>(filtro.Page, filtro.PageSize);

        var requested = filtro.Origen;
        var take = checked(filtro.Page * filtro.PageSize);
        var descending = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var rows = new List<ReporteInventarioReconciliacionDto>();
        var total = 0;

        if (!requested.HasValue || requested == OrigenReconciliacionInventario.Conteo)
        {
            var source = _context.Set<ConteoInventarioDetalle>().AsNoTracking()
                .Where(d => d.Almacen.Sucursal.EmpresaId == empresaId &&
                            d.Diferencia != null && d.CantidadContada != null &&
                            (d.ConteoInventario.Estado == EstadoConteoInventario.Cerrado || d.ConteoInventario.Estado == EstadoConteoInventario.Aprobado));
            if (!alcance.EsAdministrador) source = source.Where(d => d.ConteoInventario.CreadoPorUsuarioId == alcance.UsuarioId);
            source = ApplyConteoFilters(source, filtro);
            total += await source.CountAsync(cancellationToken);
            var page = descending
                ? await source.OrderByDescending(d => d.ConteoInventario.FechaCierre).ThenByDescending(d => d.Id).Take(take).ToListAsync(cancellationToken)
                : await source.OrderBy(d => d.ConteoInventario.FechaCierre).ThenBy(d => d.Id).Take(take).ToListAsync(cancellationToken);
            rows.AddRange(page.Select(d => new ReporteInventarioReconciliacionDto
            {
                Fecha = d.ConteoInventario.FechaCierre ?? d.FechaConteo ?? DateTime.MinValue,
                AlmacenId = d.AlmacenId,
                AlmacenNombre = d.Almacen.Nombre,
                UbicacionAlmacenId = d.UbicacionAlmacenId,
                UbicacionNombre = d.UbicacionAlmacen != null ? d.UbicacionAlmacen.Nombre : null,
                ProductoId = d.ProductoVariante.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                Sku = d.ProductoSkuSnapshot ?? d.ProductoVariante.Sku,
                ProductoNombre = d.ProductoVariante.Producto.Nombre,
                Marca = d.ProductoMarcaSnapshot,
                Modelo = d.ProductoModeloSnapshot,
                Color = d.ProductoColorSnapshot,
                Talla = d.ProductoTallaSnapshot,
                Origen = OrigenReconciliacionInventario.Conteo,
                DocumentoId = d.ConteoInventarioId,
                NumeroDocumento = d.ConteoInventario.Numero,
                CantidadEsperada = d.StockEsperadoSnapshot,
                CantidadObservada = d.CantidadContada!.Value,
                DiferenciaNormalizada = d.Diferencia!.Value,
                UsuarioId = d.ContadoPorUsuarioId,
                Observaciones = d.ConteoInventario.Observaciones
            }));
        }

        if (!requested.HasValue || requested == OrigenReconciliacionInventario.Transferencia)
        {
            var source = _context.Set<TransferenciaInventarioDetalle>().AsNoTracking()
                .Where(d => d.TransferenciaInventario.AlmacenOrigen.Sucursal.EmpresaId == empresaId &&
                            d.TransferenciaInventario.AlmacenDestino.Sucursal.EmpresaId == empresaId &&
                            d.TransferenciaInventario.Estado == EstadoTransferenciaInventario.Recibida &&
                            d.CantidadDespachada > 0 &&
                            d.CantidadRecibida + d.CantidadFaltante + d.CantidadDanada == d.CantidadDespachada);
            if (!alcance.EsAdministrador) source = source.Where(d => d.TransferenciaInventario.CreadoPorUsuarioId == alcance.UsuarioId);
            source = ApplyTransferFilters(source, filtro);
            total += await source.CountAsync(cancellationToken);
            var page = descending
                ? await source.OrderByDescending(d => d.TransferenciaInventario.FechaRecepcion).ThenByDescending(d => d.Id).Take(take).ToListAsync(cancellationToken)
                : await source.OrderBy(d => d.TransferenciaInventario.FechaRecepcion).ThenBy(d => d.Id).Take(take).ToListAsync(cancellationToken);
            rows.AddRange(page.Select(d => new ReporteInventarioReconciliacionDto
            {
                Fecha = d.TransferenciaInventario.FechaRecepcion ?? DateTime.MinValue,
                AlmacenId = d.TransferenciaInventario.AlmacenDestinoId,
                AlmacenNombre = d.TransferenciaInventario.AlmacenDestino.Nombre,
                UbicacionAlmacenId = d.UbicacionDestinoId,
                UbicacionNombre = d.UbicacionDestino != null ? d.UbicacionDestino.Nombre : null,
                ProductoId = d.ProductoVariante.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                Sku = d.ProductoSkuSnapshot ?? d.ProductoVariante.Sku,
                ProductoNombre = d.ProductoVariante.Producto.Nombre,
                Marca = d.ProductoMarcaSnapshot,
                Modelo = d.ProductoModeloSnapshot,
                Color = d.ProductoColorSnapshot,
                Talla = d.ProductoTallaSnapshot,
                Origen = OrigenReconciliacionInventario.Transferencia,
                DocumentoId = d.TransferenciaInventarioId,
                NumeroDocumento = d.TransferenciaInventario.Numero,
                CantidadEsperada = d.CantidadDespachada,
                CantidadObservada = d.CantidadRecibida + d.CantidadDanada + d.CantidadSobrante,
                DiferenciaNormalizada = d.CantidadSobrante - d.CantidadFaltante,
                CantidadFaltante = d.CantidadFaltante,
                CantidadSobrante = d.CantidadSobrante,
                CantidadDanada = d.CantidadDanada,
                UsuarioId = d.TransferenciaInventario.RecibidaPorUsuarioId,
                Observaciones = d.TransferenciaInventario.Observaciones
            }));
        }

        if (!requested.HasValue || requested == OrigenReconciliacionInventario.Ajuste)
        {
            var source = _context.Set<AjusteInventarioDetalle>().AsNoTracking()
                .Where(d => d.Almacen != null && d.Almacen.Sucursal.EmpresaId == empresaId &&
                            d.AjusteInventario.Estado == EstadoAjusteInventario.Confirmado &&
                            d.CantidadAnteriorSnapshot.HasValue &&
                            d.CantidadNuevaSnapshot.HasValue &&
                            d.CostoUnitarioSnapshot.HasValue);
            if (!alcance.EsAdministrador) source = source.Where(d => d.AjusteInventario.CreadoPorUsuarioId == alcance.UsuarioId);
            source = ApplyAjusteFilters(source, filtro);
            total += await source.CountAsync(cancellationToken);
            var page = descending
                ? await source.OrderByDescending(d => d.AjusteInventario.FechaConfirmacion).ThenByDescending(d => d.Id).Take(take).ToListAsync(cancellationToken)
                : await source.OrderBy(d => d.AjusteInventario.FechaConfirmacion).ThenBy(d => d.Id).Take(take).ToListAsync(cancellationToken);
            rows.AddRange(page.Select(d => new ReporteInventarioReconciliacionDto
            {
                Fecha = d.AjusteInventario.FechaConfirmacion ?? d.AjusteInventario.FechaAjuste,
                AlmacenId = d.AlmacenId,
                AlmacenNombre = d.Almacen?.Nombre,
                UbicacionAlmacenId = d.UbicacionAlmacenId,
                UbicacionNombre = d.UbicacionAlmacen?.Nombre,
                ProductoId = d.ProductoId,
                ProductoVarianteId = d.ProductoVarianteId,
                Sku = d.SkuSnapshot ?? d.ProductoVariante?.Sku,
                ProductoNombre = d.NombreSnapshot ?? d.Producto.Nombre,
                Marca = d.MarcaSnapshot,
                Modelo = d.ModeloSnapshot,
                Color = d.ColorSnapshot,
                Talla = d.TallaSnapshot,
                Origen = OrigenReconciliacionInventario.Ajuste,
                DocumentoId = d.AjusteInventarioId,
                NumeroDocumento = d.AjusteInventario.NumeroAjuste,
                CantidadEsperada = d.CantidadAnteriorSnapshot!.Value,
                CantidadObservada = d.CantidadNuevaSnapshot!.Value,
                DiferenciaNormalizada = d.DiferenciaSnapshot!.Value,
                CostoUnitario = d.CostoUnitarioSnapshot,
                ImpactoCosto = d.ImpactoCostoSnapshot,
                UsuarioId = d.AjusteInventario.ConfirmadoPorUsuarioId,
                UsuarioNombre = d.AjusteInventario.ConfirmadoPorNombreUsuario,
                Observaciones = d.AjusteInventario.Observaciones
            }));
        }

        var ordered = descending
            ? rows.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.DocumentoId)
            : rows.OrderBy(x => x.Fecha).ThenBy(x => x.DocumentoId);

        return new PagedResult<ReporteInventarioReconciliacionDto>
        {
            Items = ordered.Skip((filtro.Page - 1) * filtro.PageSize).Take(filtro.PageSize).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    private static IQueryable<ConteoInventarioDetalle> ApplyConteoFilters(IQueryable<ConteoInventarioDetalle> query, ReporteInventarioReconciliacionFiltroDto f)
    {
        if (f.Desde.HasValue) query = query.Where(d => d.ConteoInventario.FechaCierre >= f.Desde.Value);
        if (f.Hasta.HasValue) query = query.Where(d => d.ConteoInventario.FechaCierre <= f.Hasta.Value);
        if (f.SucursalId.HasValue) query = query.Where(d => d.Almacen.SucursalId == f.SucursalId.Value);
        if (f.AlmacenId.HasValue) query = query.Where(d => d.AlmacenId == f.AlmacenId.Value);
        if (f.UbicacionAlmacenId.HasValue) query = query.Where(d => d.UbicacionAlmacenId == f.UbicacionAlmacenId.Value);
        if (f.ProductoId.HasValue) query = query.Where(d => d.ProductoVariante.ProductoId == f.ProductoId.Value);
        if (f.ProductoVarianteId.HasValue) query = query.Where(d => d.ProductoVarianteId == f.ProductoVarianteId.Value);
        if (!string.IsNullOrWhiteSpace(f.Sku)) { var sku = f.Sku.Trim(); query = query.Where(d => d.ProductoSkuSnapshot == sku || d.ProductoVariante.Sku == sku); }
        return query;
    }

    private static IQueryable<TransferenciaInventarioDetalle> ApplyTransferFilters(IQueryable<TransferenciaInventarioDetalle> query, ReporteInventarioReconciliacionFiltroDto f)
    {
        if (f.Desde.HasValue) query = query.Where(d => d.TransferenciaInventario.FechaRecepcion >= f.Desde.Value);
        if (f.Hasta.HasValue) query = query.Where(d => d.TransferenciaInventario.FechaRecepcion <= f.Hasta.Value);
        if (f.SucursalId.HasValue) query = query.Where(d => d.TransferenciaInventario.AlmacenDestino.SucursalId == f.SucursalId.Value);
        if (f.AlmacenId.HasValue) query = query.Where(d => d.TransferenciaInventario.AlmacenDestinoId == f.AlmacenId.Value);
        if (f.UbicacionAlmacenId.HasValue) query = query.Where(d => d.UbicacionDestinoId == f.UbicacionAlmacenId.Value);
        if (f.ProductoId.HasValue) query = query.Where(d => d.ProductoVariante.ProductoId == f.ProductoId.Value);
        if (f.ProductoVarianteId.HasValue) query = query.Where(d => d.ProductoVarianteId == f.ProductoVarianteId.Value);
        if (!string.IsNullOrWhiteSpace(f.Sku)) { var sku = f.Sku.Trim(); query = query.Where(d => d.ProductoSkuSnapshot == sku || d.ProductoVariante.Sku == sku); }
        return query;
    }

    private static IQueryable<AjusteInventarioDetalle> ApplyAjusteFilters(IQueryable<AjusteInventarioDetalle> query, ReporteInventarioReconciliacionFiltroDto f)
    {
        if (f.Desde.HasValue) query = query.Where(d => d.AjusteInventario.FechaConfirmacion >= f.Desde.Value);
        if (f.Hasta.HasValue) query = query.Where(d => d.AjusteInventario.FechaConfirmacion <= f.Hasta.Value);
        if (f.SucursalId.HasValue) query = query.Where(d => d.Almacen != null && d.Almacen.SucursalId == f.SucursalId.Value);
        if (f.AlmacenId.HasValue) query = query.Where(d => d.AlmacenId == f.AlmacenId.Value);
        if (f.UbicacionAlmacenId.HasValue) query = query.Where(d => d.UbicacionAlmacenId == f.UbicacionAlmacenId.Value);
        if (f.ProductoId.HasValue) query = query.Where(d => d.ProductoId == f.ProductoId.Value);
        if (f.ProductoVarianteId.HasValue) query = query.Where(d => d.ProductoVarianteId == f.ProductoVarianteId.Value);
        if (!string.IsNullOrWhiteSpace(f.Sku)) { var sku = f.Sku.Trim(); query = query.Where(d => d.SkuSnapshot == sku || (d.ProductoVariante != null && d.ProductoVariante.Sku == sku)); }
        return query;
    }

    private static PagedResult<T> Empty<T>(int page, int pageSize) => new()
    {
        Page = page,
        PageSize = pageSize,
        TotalCount = 0
    };

    private sealed class StockHealthRaw
    {
        public int ProductoVarianteId { get; init; }
        public int ProductoId { get; init; }
        public string ProductoNombre { get; init; } = string.Empty;
        public string? Sku { get; init; }
        public int AlmacenId { get; init; }
        public string AlmacenNombre { get; init; } = string.Empty;
        public int? UbicacionAlmacenId { get; init; }
        public string? UbicacionNombre { get; init; }
        public int StockDisponible { get; init; }
        public int StockMinimo { get; init; }
        public DateTime? UltimoMovimientoUtc { get; init; }
        public int UnidadesSalidaPeriodo { get; init; }
    }
}
