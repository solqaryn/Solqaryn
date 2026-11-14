using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Mappings;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly IRevisionFinancieraRepository _revisionRepository;
    private readonly IMovimientoFinancieroRepository _movimientoFinancieroRepository;
    private readonly IUsuarioScopeService _usuarioScope;

    public DashboardService(
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        ICompraRepository compraRepository,
        IVentaRepository ventaRepository,
        IRevisionFinancieraRepository revisionRepository,
        IMovimientoFinancieroRepository movimientoFinancieroRepository,
        IUsuarioScopeService usuarioScope)
    {
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _compraRepository = compraRepository;
        _ventaRepository = ventaRepository;
        _revisionRepository = revisionRepository;
        _movimientoFinancieroRepository = movimientoFinancieroRepository;
        _usuarioScope = usuarioScope;
    }

    public async Task<DashboardResumenDto> GetResumenAsync()
    {
        var alcance = await _usuarioScope.ObtenerActualAsync()
            ?? throw new ForbiddenAccessException("No fue posible resolver el usuario autenticado y su rol vigente.");
        var esAdministrador = alcance.EsAdministrador;

        // El inventario físico y las alertas de stock son información operativa
        // compartida. Los costos, utilidades, compras y auditoría son corporativos.
        var stockBajo = await _productoRepository.GetStockBajoAsync();
        var ultimasVentas = await _ventaRepository.GetUltimasAsync();
        var ultimosProductos = esAdministrador
            ? await _productoRepository.GetUltimosAgregadosAsync()
            : new List<Producto>();
        var ultimasCompras = esAdministrador
            ? await _compraRepository.GetUltimasAsync()
            : new List<Compra>();

        var totalProductosMercaderia = await _productoRepository.GetTotalProductosPorTipoAsync(TipoInventario.MercaderiaVenta);
        var totalProductosInsumos = await _productoRepository.GetTotalProductosPorTipoAsync(TipoInventario.InsumoAdministrativo);
        var totalUnidadesMercaderia = await _productoRepository.GetTotalUnidadesPorTipoAsync(TipoInventario.MercaderiaVenta);
        var totalUnidadesInsumos = await _productoRepository.GetTotalUnidadesPorTipoAsync(TipoInventario.InsumoAdministrativo);

        var valorCostoMercaderia = esAdministrador
            ? await _productoRepository.GetValorTotalCostoPorTipoAsync(TipoInventario.MercaderiaVenta)
            : 0m;
        var valorCostoInsumos = esAdministrador
            ? await _productoRepository.GetValorTotalCostoPorTipoAsync(TipoInventario.InsumoAdministrativo)
            : 0m;
        var valorVentaMercaderia = esAdministrador
            ? await _productoRepository.GetValorTotalPrecioPorTipoAsync(TipoInventario.MercaderiaVenta)
            : 0m;

        decimal balanceOperativo = 0;
        var ultimaRevision = esAdministrador ? await _revisionRepository.GetUltimaAsync() : null;
        if (esAdministrador)
        {
            var movimientos = await _movimientoFinancieroRepository.GetFilteredAsync(null, null);
            var noAnulados = movimientos.Where(m => m.Estado != EstadoMovimientoFinanciero.Anulado).ToList();
            balanceOperativo =
                noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto) -
                noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);
        }

        return new DashboardResumenDto
        {
            TotalProductos = totalProductosMercaderia + totalProductosInsumos,
            TotalUnidades = totalUnidadesMercaderia + totalUnidadesInsumos,
            ValorTotalInventario = valorCostoMercaderia + valorCostoInsumos,
            // Solo la mercadería vendible forma parte del potencial de venta.
            ValorPotencialVenta = valorVentaMercaderia,
            TotalProductosMercaderia = totalProductosMercaderia,
            TotalProductosInsumosAdministrativos = totalProductosInsumos,
            TotalUnidadesMercaderia = totalUnidadesMercaderia,
            TotalUnidadesInsumosAdministrativos = totalUnidadesInsumos,
            ValorInventarioCostoMercaderia = valorCostoMercaderia,
            ValorInventarioCostoInsumosAdministrativos = valorCostoInsumos,
            ValorPotencialVentaMercaderia = valorVentaMercaderia,
            ProductosStockBajo = stockBajo.Select(ProductoMapper.ToDto).ToList(),
            UltimosAgregados = ultimosProductos.Select(ProductoMapper.ToDto).ToList(),

            ComprasDelMes = esAdministrador ? await _compraRepository.GetTotalDelMesAsync() : 0,
            VentasDelMes = await _ventaRepository.GetTotalDelMesAsync(),
            UltimasCompras = ultimasCompras.Select(c => new CompraResumenDto
            {
                NumeroCompra = c.NumeroCompra,
                ProveedorNombre = c.ProveedorNombre,
                Total = c.Total,
                Estado = c.Estado.ToString(),
                Fecha = c.Fecha
            }).ToList(),
            UltimasVentas = ultimasVentas.Select(v => new VentaResumenDto
            {
                NumeroVenta = v.NumeroVenta,
                ClienteNombre = v.ClienteNombre,
                Total = v.Total,
                Estado = v.Estado.ToString(),
                Fecha = v.Fecha
            }).ToList(),

            IngresosDelMes = await _ventaRepository.GetIngresosDelMesAsync(),
            UtilidadBruta = esAdministrador ? await _ventaRepository.GetUtilidadBrutaTotalAsync() : 0,
            CuentasPorCobrar = await _ventaRepository.GetCuentasPorCobrarAsync(),
            CuentasPorPagar = esAdministrador ? await _compraRepository.GetCuentasPorPagarAsync() : 0,
            BalanceOperativo = balanceOperativo,

            UltimaVentaPor = esAdministrador
                ? ultimasVentas.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasVentas.FirstOrDefault()?.CreadoPorNombreUsuario
                : null,
            UltimaCompraPor = esAdministrador
                ? ultimasCompras.FirstOrDefault()?.ConfirmadoPorNombreUsuario ?? ultimasCompras.FirstOrDefault()?.CreadoPorNombreUsuario
                : null,
            UltimaRevisionFinancieraPor = esAdministrador ? ultimaRevision?.RevisadoPorNombreUsuario : null,
            UltimoProductoRegistradoPor = esAdministrador ? ultimosProductos.FirstOrDefault()?.CreadoPorNombreUsuario : null
        };
    }

    public async Task<InventarioVariantesReporteDto> GetInventarioVariantesAsync(
        int? productoId = null,
        int? marcaId = null,
        int? modeloId = null,
        int? colorId = null,
        int? tallaId = null,
        bool incluirInactivas = true,
        CancellationToken cancellationToken = default)
    {
        var alcance = await _usuarioScope.ObtenerActualAsync()
            ?? throw new ForbiddenAccessException("No fue posible resolver el usuario autenticado y su rol vigente.");
        var incluirCostos = alcance.EsAdministrador;
        var variantes = await _productoVarianteRepository.GetForReporteAsync(
            productoId, marcaId, modeloId, colorId, tallaId, incluirInactivas, cancellationToken);

        // Única fuente de valoración: ProductoVariante. Nunca se suma Producto.Cantidad
        // encima de las variantes, evitando doble conteo de inventario físico.
        var filas = variantes.Select(v =>
        {
            var costoReal = v.Costo ?? v.Producto.Costo;
            var precioReal = v.Precio ?? v.Producto.Precio;
            var costoVisible = incluirCostos ? costoReal : 0m;
            var valorCosto = incluirCostos ? Math.Round(costoReal * v.Cantidad, 2, MidpointRounding.AwayFromZero) : 0m;
            var valorVenta = v.Producto.TipoInventario == TipoInventario.MercaderiaVenta
                ? Math.Round(precioReal * v.Cantidad, 2, MidpointRounding.AwayFromZero)
                : 0m;
            return new InventarioVarianteFilaDto
            {
                ProductoVarianteId = v.Id,
                ProductoId = v.ProductoId,
                Producto = v.Producto.Nombre,
                MarcaId = v.MarcaId,
                Marca = v.Marca?.Nombre,
                ModeloId = v.ModeloId,
                Modelo = v.Modelo?.Nombre,
                ColorId = v.ColorId,
                Color = v.Color?.Nombre,
                TallaId = v.TallaId,
                Talla = v.Talla?.Nombre,
                Sku = v.Sku ?? string.Empty,
                CodigoBarras = v.CodigoBarras,
                Etiqueta = ConstruirEtiqueta(v),
                Cantidad = v.Cantidad,
                Costo = costoVisible,
                Precio = precioReal,
                ValorCosto = valorCosto,
                ValorVenta = valorVenta,
                Activo = v.Activo,
                EsTecnica = v.EsTecnica
            };
        }).ToList();

        return new InventarioVariantesReporteDto
        {
            TotalVariantes = filas.Count,
            TotalUnidades = filas.Sum(x => x.Cantidad),
            ValorCosto = filas.Sum(x => x.ValorCosto),
            ValorVenta = filas.Sum(x => x.ValorVenta),
            PorProducto = Agrupar(filas, x => x.ProductoId, x => x.Producto),
            PorMarca = Agrupar(filas, x => x.MarcaId, x => x.Marca),
            PorModelo = Agrupar(filas, x => x.ModeloId, x => x.Modelo),
            PorColor = Agrupar(filas, x => x.ColorId, x => x.Color),
            PorTalla = Agrupar(filas, x => x.TallaId, x => x.Talla),
            Variantes = filas
        };
    }

    private static List<InventarioDimensionResumenDto> Agrupar(
        IEnumerable<InventarioVarianteFilaDto> filas,
        Func<InventarioVarianteFilaDto, int?> selectorId,
        Func<InventarioVarianteFilaDto, string?> selectorNombre) =>
        filas.GroupBy(x => new { Id = selectorId(x), Nombre = selectorNombre(x) })
            .Select(g => new InventarioDimensionResumenDto
            {
                Id = g.Key.Id,
                Nombre = g.Key.Nombre,
                Variantes = g.Count(),
                Unidades = g.Sum(x => x.Cantidad),
                ValorCosto = g.Sum(x => x.ValorCosto),
                ValorVenta = g.Sum(x => x.ValorVenta)
            })
            .OrderByDescending(x => x.Unidades)
            .ThenBy(x => x.Nombre)
            .ToList();

    private static string ConstruirEtiqueta(ProductoVariante variante)
    {
        var partes = new[]
        {
            variante.Marca?.Nombre,
            variante.Modelo?.Nombre,
            variante.Color?.Nombre,
            variante.Talla?.Nombre,
            variante.Sku
        }.Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(" · ", partes);
    }
}
