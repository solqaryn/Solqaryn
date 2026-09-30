using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// N5.5.D read-only purchase reporting over the existing ERP purchase authorities.
/// No aggregate, migration, backfill, FX conversion, percentage variation, ranking or
/// composite supplier score is introduced here.
/// </summary>
public sealed class ReporteComprasService : IReporteComprasService
{
    private readonly AppDbContext _context;

    public ReporteComprasService(AppDbContext context) => _context = context;

    public async Task<PagedResult<ReporteComprasDetalleDto>> ObtenerDetallePaginadoAsync(
        ReporteComprasFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        Validar(filtro);

        var query = _context.Set<OrdenCompraDetalle>().AsNoTracking();

        if (filtro.DesdeUtc.HasValue)
            query = query.Where(x => x.OrdenCompra.FechaCreacion >= filtro.DesdeUtc.Value);
        if (filtro.HastaUtc.HasValue)
            query = query.Where(x => x.OrdenCompra.FechaCreacion < filtro.HastaUtc.Value);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(x => x.OrdenCompra.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.ProductoId.HasValue)
            query = query.Where(x => x.ProductoId == filtro.ProductoId.Value);
        if (filtro.ProductoVarianteId.HasValue)
            query = query.Where(x => x.ProductoVarianteId == filtro.ProductoVarianteId.Value);
        if (filtro.EstadoOrden.HasValue)
            query = query.Where(x => x.OrdenCompra.Estado == filtro.EstadoOrden.Value);

        if (filtro.EstadoFactura.HasValue)
        {
            var lineasFactura = _context.Set<FacturaProveedorDetalle>()
                .AsNoTracking()
                .Where(x => x.FacturaProveedor.Estado == filtro.EstadoFactura.Value)
                .Select(x => x.OrdenCompraDetalleId);
            query = query.Where(x => lineasFactura.Contains(x.Id));
        }

        if (filtro.EstadoRecepcion.HasValue)
        {
            var lineasRecepcion = _context.Set<RecepcionCompraDetalle>()
                .AsNoTracking()
                .Where(x => x.RecepcionCompra.Estado == filtro.EstadoRecepcion.Value)
                .Select(x => x.OrdenCompraDetalleId);
            query = query.Where(x => lineasRecepcion.Contains(x.Id));
        }

        if (filtro.EstadoDevolucion.HasValue)
        {
            var lineasDevolucion =
                from detalle in _context.Set<DevolucionProveedorDetalle>().AsNoTracking()
                join documento in _context.Set<DevolucionProveedor>().AsNoTracking()
                    on detalle.DevolucionProveedorId equals documento.Id
                where documento.Estado == filtro.EstadoDevolucion.Value
                select detalle.OrdenCompraDetalleId;
            query = query.Where(x => lineasDevolucion.Contains(x.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var baseItems = await query
            .OrderByDescending(x => x.OrdenCompra.FechaCreacion)
            .ThenByDescending(x => x.Id)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(x => new LineaBase
            {
                OrdenCompraId = x.OrdenCompraId,
                OrdenCompraDetalleId = x.Id,
                NumeroOrden = x.OrdenCompra.NumeroOrden,
                FechaCreacionUtc = x.OrdenCompra.FechaCreacion,
                FechaEsperadaUtc = x.OrdenCompra.FechaEsperadaUtc,
                ProveedorId = x.OrdenCompra.ProveedorId,
                ProveedorNombre = x.OrdenCompra.ProveedorNombreSnapshot,
                Moneda = x.OrdenCompra.Moneda,
                ProductoId = x.ProductoId,
                ProductoVarianteId = x.ProductoVarianteId,
                ProductoSku = x.ProductoSkuSnapshot,
                ProductoNombre = x.ProductoNombreSnapshot,
                ProductoMarca = x.ProductoMarcaSnapshot,
                ProductoModelo = x.ProductoModeloSnapshot,
                ProductoColor = x.ProductoColorSnapshot,
                ProductoTalla = x.ProductoTallaSnapshot,
                CantidadOrdenada = x.CantidadOrdenada,
                PrecioUnitarioOrdenado = x.PrecioUnitario
            })
            .ToListAsync(cancellationToken);

        if (baseItems.Count == 0)
        {
            return new PagedResult<ReporteComprasDetalleDto>
            {
                Items = [],
                Page = filtro.Page,
                PageSize = filtro.PageSize,
                TotalCount = totalCount
            };
        }

        var detalleIds = baseItems.Select(x => x.OrdenCompraDetalleId).ToArray();
        var ordenIds = baseItems.Select(x => x.OrdenCompraId).Distinct().ToArray();

        var facturasQuery = _context.Set<FacturaProveedorDetalle>()
            .AsNoTracking()
            .Where(x => detalleIds.Contains(x.OrdenCompraDetalleId));
        facturasQuery = filtro.EstadoFactura.HasValue
            ? facturasQuery.Where(x => x.FacturaProveedor.Estado == filtro.EstadoFactura.Value)
            : facturasQuery.Where(x => x.FacturaProveedor.Estado == EstadoFacturaProveedor.Registrada);

        var facturas = await facturasQuery
            .Select(x => new FacturaLinea
            {
                OrdenCompraDetalleId = x.OrdenCompraDetalleId,
                FacturaProveedorId = x.FacturaProveedorId,
                FechaEmisionUtc = x.FacturaProveedor.FechaEmisionUtc,
                PrecioUnitarioSnapshot = x.PrecioUnitarioSnapshot,
                Moneda = x.FacturaProveedor.Moneda
            })
            .ToListAsync(cancellationToken);

        var recepcionesQuery = _context.Set<RecepcionCompraDetalle>()
            .AsNoTracking()
            .Where(x => detalleIds.Contains(x.OrdenCompraDetalleId));
        recepcionesQuery = filtro.EstadoRecepcion.HasValue
            ? recepcionesQuery.Where(x => x.RecepcionCompra.Estado == filtro.EstadoRecepcion.Value)
            : recepcionesQuery.Where(x => x.RecepcionCompra.Estado == EstadoRecepcionCompra.Recibida);

        var recepciones = await recepcionesQuery
            .GroupBy(x => x.OrdenCompraDetalleId)
            .Select(g => new RecepcionLinea
            {
                OrdenCompraDetalleId = g.Key,
                Recibida = g.Sum(x => x.CantidadRecibida),
                Aceptada = g.Sum(x => x.CantidadRecibida - x.CantidadDanada - x.CantidadSobrante),
                Danada = g.Sum(x => x.CantidadDanada),
                Faltante = g.Sum(x => x.CantidadFaltante),
                Sobrante = g.Sum(x => x.CantidadSobrante)
            })
            .ToListAsync(cancellationToken);

        var devolucionesBase =
            from detalle in _context.Set<DevolucionProveedorDetalle>().AsNoTracking()
            join documento in _context.Set<DevolucionProveedor>().AsNoTracking()
                on detalle.DevolucionProveedorId equals documento.Id
            where detalleIds.Contains(detalle.OrdenCompraDetalleId)
            select new { detalle, documento };

        devolucionesBase = filtro.EstadoDevolucion.HasValue
            ? devolucionesBase.Where(x => x.documento.Estado == filtro.EstadoDevolucion.Value)
            : devolucionesBase.Where(x => x.documento.Estado == EstadoDevolucionProveedor.Confirmada);

        var devoluciones = await devolucionesBase
            .GroupBy(x => x.detalle.OrdenCompraDetalleId)
            .Select(g => new DevolucionLinea
            {
                OrdenCompraDetalleId = g.Key,
                Cantidad = g.Sum(x => x.detalle.Cantidad)
            })
            .ToListAsync(cancellationToken);

        var evaluaciones = await _context.Set<EvaluacionProveedor>()
            .AsNoTracking()
            .Where(x => ordenIds.Contains(x.OrdenCompraId))
            .OrderByDescending(x => x.FechaRecepcionUtc)
            .ThenByDescending(x => x.Id)
            .Select(x => new EvaluacionLinea
            {
                Id = x.Id,
                OrdenCompraId = x.OrdenCompraId,
                FechaEsperadaUtc = x.FechaEsperadaUtc,
                FechaRecepcionUtc = x.FechaRecepcionUtc,
                CantidadOrdenada = x.CantidadOrdenada,
                CantidadAceptada = x.CantidadAceptada,
                CantidadDanada = x.CantidadDanada,
                CantidadSobrante = x.CantidadSobrante
            })
            .ToListAsync(cancellationToken);

        var facturasPorLinea = facturas.GroupBy(x => x.OrdenCompraDetalleId).ToDictionary(g => g.Key, ResolverFactura);
        var recepcionPorLinea = recepciones.ToDictionary(x => x.OrdenCompraDetalleId);
        var devolucionPorLinea = devoluciones.ToDictionary(x => x.OrdenCompraDetalleId);
        var evaluacionPorOrden = evaluaciones.GroupBy(x => x.OrdenCompraId).ToDictionary(g => g.Key, g => g.First());

        var items = baseItems.Select(x =>
        {
            facturasPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var factura);
            recepcionPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var recepcion);
            devolucionPorLinea.TryGetValue(x.OrdenCompraDetalleId, out var devolucion);
            evaluacionPorOrden.TryGetValue(x.OrdenCompraId, out var evaluacion);

            var comparable = factura is not null &&
                string.Equals(x.Moneda, factura.Moneda, StringComparison.OrdinalIgnoreCase);

            return new ReporteComprasDetalleDto
            {
                OrdenCompraId = x.OrdenCompraId,
                OrdenCompraDetalleId = x.OrdenCompraDetalleId,
                NumeroOrden = x.NumeroOrden,
                FechaCreacionUtc = x.FechaCreacionUtc,
                FechaEsperadaUtc = x.FechaEsperadaUtc,
                ProveedorId = x.ProveedorId,
                ProveedorNombre = x.ProveedorNombre,
                Moneda = x.Moneda,
                ProductoId = x.ProductoId,
                ProductoVarianteId = x.ProductoVarianteId,
                ProductoSku = x.ProductoSku,
                ProductoNombre = x.ProductoNombre,
                ProductoMarca = x.ProductoMarca,
                ProductoModelo = x.ProductoModelo,
                ProductoColor = x.ProductoColor,
                ProductoTalla = x.ProductoTalla,
                CantidadOrdenada = x.CantidadOrdenada,
                PrecioUnitarioOrdenado = x.PrecioUnitarioOrdenado,
                PrecioUnitarioFacturado = factura?.PrecioUnitarioSnapshot,
                MonedaFactura = factura?.Moneda,
                VariacionPrecioAbsoluta = comparable
                    ? factura!.PrecioUnitarioSnapshot - x.PrecioUnitarioOrdenado
                    : null,
                CantidadRecibida = recepcion?.Recibida ?? 0m,
                CantidadAceptada = recepcion?.Aceptada ?? 0m,
                CantidadDanada = recepcion?.Danada ?? 0m,
                CantidadFaltante = recepcion?.Faltante ?? 0m,
                CantidadSobrante = recepcion?.Sobrante ?? 0m,
                CantidadDevueltaEfectiva = devolucion?.Cantidad ?? 0m,
                FechaEsperadaEvaluadaUtc = evaluacion?.FechaEsperadaUtc,
                FechaRecepcionEvaluadaUtc = evaluacion?.FechaRecepcionUtc,
                DesviacionEntregaDias = evaluacion is null
                    ? null
                    : (evaluacion.FechaRecepcionUtc.Date - evaluacion.FechaEsperadaUtc.Date).Days,
                CantidadEvaluadaOrdenada = evaluacion?.CantidadOrdenada,
                CantidadEvaluadaAceptada = evaluacion?.CantidadAceptada,
                CantidadEvaluadaDanada = evaluacion?.CantidadDanada,
                CantidadEvaluadaSobrante = evaluacion?.CantidadSobrante
            };
        }).ToList();

        return new PagedResult<ReporteComprasDetalleDto>
        {
            Items = items,
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = totalCount
        };
    }

    internal static void Validar(ReporteComprasFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.DesdeUtc.HasValue && filtro.HastaUtc.HasValue && filtro.DesdeUtc.Value > filtro.HastaUtc.Value)
            throw new ArgumentException("DesdeUtc no puede ser posterior a HastaUtc.", nameof(filtro));
        if (filtro.ProveedorId is <= 0 || filtro.ProductoId is <= 0 || filtro.ProductoVarianteId is <= 0)
            throw new ArgumentException("Los identificadores de filtro deben ser positivos.", nameof(filtro));
        if (filtro.Page < 1)
            throw new ArgumentException("Page debe ser mayor o igual a 1.", nameof(filtro));
        if (filtro.PageSize < 1 || filtro.PageSize > 100)
            throw new ArgumentException("PageSize debe estar entre 1 y 100.", nameof(filtro));
        if (filtro.EstadoOrden.HasValue && !Enum.IsDefined(typeof(EstadoOrdenCompra), filtro.EstadoOrden.Value))
            throw new ArgumentException("EstadoOrden no es válido.", nameof(filtro));
        if (filtro.EstadoFactura.HasValue && !Enum.IsDefined(typeof(EstadoFacturaProveedor), filtro.EstadoFactura.Value))
            throw new ArgumentException("EstadoFactura no es válido.", nameof(filtro));
        if (filtro.EstadoRecepcion.HasValue && !Enum.IsDefined(typeof(EstadoRecepcionCompra), filtro.EstadoRecepcion.Value))
            throw new ArgumentException("EstadoRecepcion no es válido.", nameof(filtro));
        if (filtro.EstadoDevolucion.HasValue && !Enum.IsDefined(typeof(EstadoDevolucionProveedor), filtro.EstadoDevolucion.Value))
            throw new ArgumentException("EstadoDevolucion no es válido.", nameof(filtro));
    }

    private static FacturaLinea? ResolverFactura(IGrouping<int, FacturaLinea> grupo)
    {
        var candidatas = grupo
            .OrderByDescending(x => x.FechaEmisionUtc)
            .ThenByDescending(x => x.FacturaProveedorId)
            .ToList();
        if (candidatas.Count == 0)
            return null;
        if (candidatas.Count == 1)
            return candidatas[0];

        var primera = candidatas[0];
        return candidatas.All(x =>
                x.PrecioUnitarioSnapshot == primera.PrecioUnitarioSnapshot &&
                string.Equals(x.Moneda, primera.Moneda, StringComparison.OrdinalIgnoreCase))
            ? primera
            : null;
    }

    private sealed class LineaBase
    {
        public int OrdenCompraId { get; init; }
        public int OrdenCompraDetalleId { get; init; }
        public string NumeroOrden { get; init; } = string.Empty;
        public DateTime FechaCreacionUtc { get; init; }
        public DateTime? FechaEsperadaUtc { get; init; }
        public int ProveedorId { get; init; }
        public string ProveedorNombre { get; init; } = string.Empty;
        public string Moneda { get; init; } = string.Empty;
        public int ProductoId { get; init; }
        public int? ProductoVarianteId { get; init; }
        public string? ProductoSku { get; init; }
        public string? ProductoNombre { get; init; }
        public string? ProductoMarca { get; init; }
        public string? ProductoModelo { get; init; }
        public string? ProductoColor { get; init; }
        public string? ProductoTalla { get; init; }
        public decimal CantidadOrdenada { get; init; }
        public decimal PrecioUnitarioOrdenado { get; init; }
    }

    private sealed class FacturaLinea
    {
        public int OrdenCompraDetalleId { get; init; }
        public int FacturaProveedorId { get; init; }
        public DateTime FechaEmisionUtc { get; init; }
        public decimal PrecioUnitarioSnapshot { get; init; }
        public string Moneda { get; init; } = string.Empty;
    }

    private sealed class RecepcionLinea
    {
        public int OrdenCompraDetalleId { get; init; }
        public decimal Recibida { get; init; }
        public decimal Aceptada { get; init; }
        public decimal Danada { get; init; }
        public decimal Faltante { get; init; }
        public decimal Sobrante { get; init; }
    }

    private sealed class DevolucionLinea
    {
        public int OrdenCompraDetalleId { get; init; }
        public decimal Cantidad { get; init; }
    }

    private sealed class EvaluacionLinea
    {
        public int Id { get; init; }
        public int OrdenCompraId { get; init; }
        public DateTime FechaEsperadaUtc { get; init; }
        public DateTime FechaRecepcionUtc { get; init; }
        public decimal CantidadOrdenada { get; init; }
        public decimal CantidadAceptada { get; init; }
        public decimal CantidadDanada { get; init; }
        public decimal CantidadSobrante { get; init; }
    }
}
