using InventoryApp.Application.Exceptions;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Domain.Entities.Contabilidad;
using InventoryApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IPublicStoreCache? _publicStoreCache;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IPublicStoreCache? publicStoreCache = null) : base(options)
    {
        _publicStoreCache = publicStoreCache;
    }

    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoVariante> ProductoVariantes => Set<ProductoVariante>();
    public DbSet<ProductoImagen> ProductoImagenes => Set<ProductoImagen>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Modelo> Modelos => Set<Modelo>();
    public DbSet<Color> Colores => Set<Color>();
    public DbSet<Talla> Tallas => Set<Talla>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<TiendaCuentaCliente> TiendaCuentasCliente => Set<TiendaCuentaCliente>();
    public DbSet<TiendaSesionCliente> TiendaSesionesCliente => Set<TiendaSesionCliente>();
    public DbSet<TiendaDireccionCliente> TiendaDireccionesCliente => Set<TiendaDireccionCliente>();
    public DbSet<TiendaFavoritoCliente> TiendaFavoritosCliente => Set<TiendaFavoritoCliente>();
    public DbSet<CreditoCliente> CreditosCliente => Set<CreditoCliente>();
    public DbSet<TipoCliente> TipoClientes => Set<TipoCliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioEmpresa> UsuarioEmpresas => Set<UsuarioEmpresa>();
    public DbSet<UsuarioRolPlataforma> UsuarioRolesPlataforma => Set<UsuarioRolPlataforma>();
    public DbSet<SecuenciaDocumento> SecuenciasDocumento => Set<SecuenciaDocumento>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<CompraDocumento> CompraDocumentos => Set<CompraDocumento>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<ConsumoInsumo> ConsumosInsumos => Set<ConsumoInsumo>();
    public DbSet<ConsumoInsumoDetalle> ConsumoInsumoDetalles => Set<ConsumoInsumoDetalle>();
    public DbSet<AjusteInventario> AjustesInventario => Set<AjusteInventario>();
    public DbSet<AjusteInventarioDetalle> AjusteInventarioDetalles => Set<AjusteInventarioDetalle>();
    public DbSet<ExistenciaVariante> ExistenciasVariante => Set<ExistenciaVariante>();
    public DbSet<LoteInventario> LotesInventario => Set<LoteInventario>();
    public DbSet<SerieInventario> SeriesInventario => Set<SerieInventario>();
    public DbSet<ReservaInventario> ReservasInventario => Set<ReservaInventario>();
    public DbSet<ReservaInventarioDetalle> ReservaInventarioDetalles => Set<ReservaInventarioDetalle>();
    public DbSet<MovimientoFinanciero> MovimientosFinancieros => Set<MovimientoFinanciero>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaDetalle> VentaDetalles => Set<VentaDetalle>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<FacturaDetalle> FacturaDetalles => Set<FacturaDetalle>();
    public DbSet<FacturaPago> FacturaPagos => Set<FacturaPago>();
    public DbSet<FacturaProveedor> FacturasProveedor => Set<FacturaProveedor>();
    public DbSet<FacturaProveedorDetalle> FacturaProveedorDetalles => Set<FacturaProveedorDetalle>();
    public DbSet<EmpresaConfiguracion> EmpresaConfiguraciones => Set<EmpresaConfiguracion>();
    public DbSet<RevisionFinanciera> RevisionesFinancieras => Set<RevisionFinanciera>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionDetalle> CotizacionDetalles => Set<CotizacionDetalle>();
    public DbSet<InventoryApp.Domain.Entities.Bancos.CuentaBancaria> CuentasBancarias => Set<InventoryApp.Domain.Entities.Bancos.CuentaBancaria>();
    public DbSet<PreparacionPedidoVenta> PreparacionesPedidoVenta => Set<PreparacionPedidoVenta>();
    public DbSet<PreparacionPedidoVentaDetalle> PreparacionPedidoVentaDetalles => Set<PreparacionPedidoVentaDetalle>();

    public DbSet<AsientoContable> AsientosContables => Set<AsientoContable>();
    public DbSet<AsientoDetalle> AsientoDetalles => Set<AsientoDetalle>();
    public DbSet<PeriodoContable> PeriodosContables => Set<PeriodoContable>();

    public DbSet<Descuento> Descuentos => Set<Descuento>();
    public DbSet<DescuentoProducto> DescuentoProductos => Set<DescuentoProducto>();
    public DbSet<DescuentoCategoria> DescuentoCategorias => Set<DescuentoCategoria>();
    public DbSet<DescuentoCliente> DescuentoClientes => Set<DescuentoCliente>();
    public DbSet<DescuentoRol> DescuentoRoles => Set<DescuentoRol>();
    public DbSet<HistorialUsoDescuento> HistorialUsoDescuentos => Set<HistorialUsoDescuento>();
    public DbSet<VentaDescuento> VentaDescuentos => Set<VentaDescuento>();

    public DbSet<Impuesto> Impuestos => Set<Impuesto>();
    public DbSet<ImpuestoProducto> ImpuestoProductos => Set<ImpuestoProducto>();
    public DbSet<ImpuestoCategoria> ImpuestoCategorias => Set<ImpuestoCategoria>();
    public DbSet<ImpuestoOperacion> ImpuestoOperaciones => Set<ImpuestoOperacion>();
    public DbSet<ImpuestoCliente> ImpuestoClientes => Set<ImpuestoCliente>();
    public DbSet<ImpuestoProveedor> ImpuestoProveedores => Set<ImpuestoProveedor>();
    public DbSet<HistorialAplicacionImpuesto> HistorialAplicacionImpuestos => Set<HistorialAplicacionImpuesto>();
    public DbSet<VentaImpuesto> VentaImpuestos => Set<VentaImpuesto>();
    public DbSet<CompraImpuesto> CompraImpuestos => Set<CompraImpuesto>();

    public DbSet<CostoEnvio> CostosEnvio => Set<CostoEnvio>();

    public DbSet<EnlacePublicoFactura> EnlacesPublicosFactura => Set<EnlacePublicoFactura>();
    public DbSet<HistorialEnvioFactura> HistorialEnviosFactura => Set<HistorialEnvioFactura>();
    public DbSet<TemaVisual> TemasVisuales => Set<TemaVisual>();

    public DbSet<CargaMasiva> CargasMasivas => Set<CargaMasiva>();
    public DbSet<CargaMasivaError> CargaMasivaErrores => Set<CargaMasivaError>();
    public DbSet<DashboardKpiConfiguracion> DashboardKpiConfiguraciones => Set<DashboardKpiConfiguracion>();
    public DbSet<MensajeOutbox> MensajesOutbox => Set<MensajeOutbox>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var cacheSegments = DetectarInvalidacionesStorefront();

        await ValidarAislamientoComercialVentasAsync(cancellationToken);
        await PrepararValorizacionComprasAsync(cancellationToken);
        var guardados = await base.SaveChangesAsync(cancellationToken);

        if (guardados > 0 && cacheSegments.Count > 0)
            _publicStoreCache?.InvalidateAll(cacheSegments.ToArray());

        return guardados;
    }

    private HashSet<string> DetectarInvalidacionesStorefront()
    {
        var segments = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            switch (entry.Entity)
            {
                case Empresa:
                case EmpresaConfiguracion:
                case ConfiguracionWhatsAppEmpresa:
                    segments.Add(PublicStoreCacheSegments.Identity);
                    break;

                case TemaVisual:
                    segments.Add(PublicStoreCacheSegments.Theme);
                    break;

                case Categoria:
                    segments.Add(PublicStoreCacheSegments.Categories);
                    segments.Add(PublicStoreCacheSegments.Products);
                    segments.Add(PublicStoreCacheSegments.Featured);
                    break;

                case Producto:
                case ProductoVariante:
                case ProductoImagen:
                case ExistenciaVariante:
                case Marca:
                case Modelo:
                case Descuento:
                case DescuentoProducto:
                case DescuentoCategoria:
                    segments.Add(PublicStoreCacheSegments.Products);
                    segments.Add(PublicStoreCacheSegments.Featured);
                    break;
            }
        }

        return segments;
    }

    private async Task ValidarAislamientoComercialVentasAsync(CancellationToken cancellationToken)
    {
        var productoIds = ChangeTracker.Entries<VentaDetalle>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity.ProductoId)
            .Where(id => id > 0)
            .ToHashSet();

        var ventasConfirmandose = ChangeTracker.Entries<Venta>()
            .Where(e => e.State == EntityState.Modified &&
                        e.OriginalValues.GetValue<EstadoDocumento>(nameof(Venta.Estado)) == EstadoDocumento.Borrador &&
                        e.Entity.Estado == EstadoDocumento.Confirmada)
            .Select(e => e.Entity.Id)
            .Where(id => id > 0)
            .ToList();

        if (ventasConfirmandose.Count > 0)
        {
            var idsPersistidos = await VentaDetalles
                .AsNoTracking()
                .Where(d => ventasConfirmandose.Contains(d.VentaId))
                .Select(d => d.ProductoId)
                .Distinct()
                .ToListAsync(cancellationToken);
            productoIds.UnionWith(idsPersistidos);
        }

        if (productoIds.Count == 0)
            return;

        var productosRastreados = ChangeTracker.Entries<Producto>()
            .Where(e => productoIds.Contains(e.Entity.Id))
            .Select(e => e.Entity)
            .ToList();

        var insumoRastreado = productosRastreados
            .FirstOrDefault(p => p.TipoInventario == TipoInventario.InsumoAdministrativo);
        if (insumoRastreado is not null)
        {
            throw new BusinessRuleException(
                $"El producto '{insumoRastreado.Nombre}' es un insumo administrativo y no puede venderse ni facturarse.");
        }

        var idsRastreados = productosRastreados.Select(p => p.Id).ToHashSet();
        var idsPorConsultar = productoIds.Where(id => !idsRastreados.Contains(id)).ToList();
        if (idsPorConsultar.Count == 0)
            return;

        var insumoPersistido = await Productos
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => idsPorConsultar.Contains(p.Id) &&
                        p.TipoInventario == TipoInventario.InsumoAdministrativo)
            .Select(p => new { p.Id, p.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        if (insumoPersistido is not null)
        {
            throw new BusinessRuleException(
                $"El producto '{insumoPersistido.Nombre}' es un insumo administrativo y no puede venderse ni facturarse.");
        }
    }

    private async Task PrepararValorizacionComprasAsync(CancellationToken cancellationToken)
    {
        var transiciones = ChangeTracker.Entries<Compra>()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => new
            {
                Entry = e,
                Anterior = e.OriginalValues.GetValue<EstadoDocumento>(nameof(Compra.Estado)),
                Actual = e.Entity.Estado
            })
            .ToList();

        foreach (var transicion in transiciones)
        {
            if (transicion.Anterior == EstadoDocumento.Borrador &&
                transicion.Actual == EstadoDocumento.Confirmada)
            {
                CapturarSnapshotsValorizacion(transicion.Entry.Entity);
            }
            else if (transicion.Anterior == EstadoDocumento.Confirmada &&
                     transicion.Actual == EstadoDocumento.Anulada)
            {
                await RestaurarValorizacionAsync(transicion.Entry.Entity, cancellationToken);
            }
        }
    }

    private void CapturarSnapshotsValorizacion(Compra compra)
    {
        foreach (var grupoProducto in compra.Detalles.GroupBy(d => d.ProductoId))
        {
            var productoEntry = ChangeTracker.Entries<Producto>()
                .SingleOrDefault(e => e.Entity.Id == grupoProducto.Key)
                ?? throw new BusinessRuleException(
                    $"No se pudo capturar la valoración anterior del producto {grupoProducto.Key}.");

            var stockProductoAnterior = productoEntry.OriginalValues.GetValue<int>(nameof(Producto.Cantidad));
            var costoProductoAnterior = productoEntry.OriginalValues.GetValue<decimal>(nameof(Producto.Costo));
            var stockProductoNuevo = productoEntry.Entity.Cantidad;
            var costoProductoNuevo = productoEntry.Entity.Costo;

            foreach (var detalle in grupoProducto)
            {
                detalle.StockProductoAnteriorSnapshot = stockProductoAnterior;
                detalle.CostoProductoAnteriorSnapshot = costoProductoAnterior;
                detalle.StockProductoNuevoSnapshot = stockProductoNuevo;
                detalle.CostoProductoNuevoSnapshot = costoProductoNuevo;
            }

            foreach (var grupoVariante in grupoProducto
                         .Where(d => d.ProductoVarianteId.HasValue)
                         .GroupBy(d => d.ProductoVarianteId!.Value))
            {
                var varianteEntry = ChangeTracker.Entries<ProductoVariante>()
                    .SingleOrDefault(e => e.Entity.Id == grupoVariante.Key)
                    ?? throw new BusinessRuleException(
                        $"No se pudo capturar la valoración anterior de la variante {grupoVariante.Key}.");

                var stockVarianteAnterior = varianteEntry.OriginalValues.GetValue<int>(nameof(ProductoVariante.Cantidad));
                var costoVarianteAnterior = varianteEntry.OriginalValues.GetValue<decimal?>(nameof(ProductoVariante.Costo));
                var stockVarianteNuevo = varianteEntry.Entity.Cantidad;
                var costoVarianteNuevo = varianteEntry.Entity.Costo;

                foreach (var detalle in grupoVariante)
                {
                    detalle.StockVarianteAnteriorSnapshot = stockVarianteAnterior;
                    detalle.CostoVarianteAnteriorSnapshot = costoVarianteAnterior;
                    detalle.StockVarianteNuevoSnapshot = stockVarianteNuevo;
                    detalle.CostoVarianteNuevoSnapshot = costoVarianteNuevo;
                }
            }
        }
    }

    private async Task RestaurarValorizacionAsync(Compra compra, CancellationToken cancellationToken)
    {
        foreach (var grupoProducto in compra.Detalles.GroupBy(d => d.ProductoId))
        {
            var detalleBase = grupoProducto.First();
            ValidarSnapshotProducto(detalleBase, compra.NumeroCompra);

            var productoEntry = ChangeTracker.Entries<Producto>()
                .SingleOrDefault(e => e.Entity.Id == grupoProducto.Key)
                ?? throw new BusinessRuleException(
                    $"No se pudo restaurar la valoración del producto {grupoProducto.Key}.");

            var gruposVariante = grupoProducto
                .Where(d => d.ProductoVarianteId.HasValue)
                .GroupBy(d => d.ProductoVarianteId!.Value)
                .ToList();

            if (gruposVariante.Count == 0)
            {
                productoEntry.Entity.Cantidad = detalleBase.StockProductoAnteriorSnapshot!.Value;
                productoEntry.Entity.Costo = detalleBase.CostoProductoAnteriorSnapshot!.Value;
                continue;
            }

            foreach (var grupoVariante in gruposVariante)
            {
                var snapshot = grupoVariante.First();
                ValidarSnapshotVariante(snapshot, compra.NumeroCompra);

                var varianteEntry = ChangeTracker.Entries<ProductoVariante>()
                    .SingleOrDefault(e => e.Entity.Id == grupoVariante.Key)
                    ?? throw new BusinessRuleException(
                        $"No se pudo restaurar la valoración anterior de la variante {grupoVariante.Key}.");

                var cantidadEsperadaTrasAnular = snapshot.StockVarianteAnteriorSnapshot!.Value;
                if (varianteEntry.Entity.Cantidad != cantidadEsperadaTrasAnular)
                {
                    throw new BusinessRuleException(
                        $"La variante {grupoVariante.Key} no coincide con el stock esperado tras la anulación; se revierte toda la operación.");
                }

                varianteEntry.Entity.Cantidad = snapshot.StockVarianteAnteriorSnapshot.Value;
                varianteEntry.Entity.Costo = snapshot.CostoVarianteAnteriorSnapshot;
            }

            var variantes = await ProductoVariantes
                .Where(v => v.ProductoId == grupoProducto.Key && !v.Eliminado)
                .OrderBy(v => v.Id)
                .ToListAsync(cancellationToken);

            var stockTotal = variantes.Sum(v => v.Cantidad);
            productoEntry.Entity.Cantidad = stockTotal;
            productoEntry.Entity.Costo = stockTotal > 0
                ? Math.Round(
                    variantes.Sum(v => (v.Costo ?? 0m) * v.Cantidad) / stockTotal,
                    2,
                    MidpointRounding.AwayFromZero)
                : detalleBase.CostoProductoAnteriorSnapshot!.Value;
        }
    }

    private static void ValidarSnapshotProducto(CompraDetalle detalle, string numeroCompra)
    {
        if (!detalle.StockProductoAnteriorSnapshot.HasValue ||
            !detalle.StockProductoNuevoSnapshot.HasValue ||
            !detalle.CostoProductoAnteriorSnapshot.HasValue ||
            !detalle.CostoProductoNuevoSnapshot.HasValue)
        {
            throw new BusinessRuleException(
                $"La compra {numeroCompra} no posee snapshots de valoración completos. Por seguridad, no puede anularse automáticamente.");
        }
    }

    private static void ValidarSnapshotVariante(CompraDetalle detalle, string numeroCompra)
    {
        if (!detalle.StockVarianteAnteriorSnapshot.HasValue ||
            !detalle.StockVarianteNuevoSnapshot.HasValue ||
            !detalle.CostoVarianteNuevoSnapshot.HasValue)
        {
            throw new BusinessRuleException(
                $"La compra {numeroCompra} no posee snapshots de variante completos. Por seguridad, no puede anularse automáticamente.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
