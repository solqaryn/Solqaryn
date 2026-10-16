using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013231000_CommercialTenantRemainingScopedFks")]
public partial class CommercialTenantRemainingScopedFks : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""ALTER TABLE `AjusteInventarioDetalles` ADD CONSTRAINT `FK_AjusteInventarioDetalles_AjusteInventarioId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AjusteInventarioId`) REFERENCES `AjustesInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `AjusteInventarioDetalles` ADD CONSTRAINT `FK_AjusteInventarioDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `AjusteInventarioDetalles` ADD CONSTRAINT `FK_AjusteInventarioDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Almacenes` ADD CONSTRAINT `FK_Almacenes_SucursalId_TenantFinal` FOREIGN KEY (`EmpresaId`, `SucursalId`) REFERENCES `Sucursales` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `AsientoDetalles` ADD CONSTRAINT `FK_AsientoDetalles_AsientoContableId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AsientoContableId`) REFERENCES `AsientosContables` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `AsientoDetalles` ADD CONSTRAINT `FK_AsientoDetalles_CuentaContableId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CuentaContableId`) REFERENCES `CuentasContables` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CajaMovimientos` ADD CONSTRAINT `FK_CajaMovimientos_CajaSesionId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CajaSesionId`) REFERENCES `CajaSesiones` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CajaSesiones` ADD CONSTRAINT `FK_CajaSesiones_CajaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CajaId`) REFERENCES `Cajas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Cajas` ADD CONSTRAINT `FK_Cajas_SesionActivaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `SesionActivaId`) REFERENCES `CajaSesiones` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CapasCostoInventario` ADD CONSTRAINT `FK_CapasCostoInventario_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CapasCostoInventario` ADD CONSTRAINT `FK_CapasCostoInventario_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CargaMasivaErrores` ADD CONSTRAINT `FK_CargaMasivaErrores_CargaMasivaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CargaMasivaId`) REFERENCES `CargasMasivas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CentrosCosto` ADD CONSTRAINT `FK_CentrosCosto_SucursalId_TenantFinal` FOREIGN KEY (`EmpresaId`, `SucursalId`) REFERENCES `Sucursales` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Clientes` ADD CONSTRAINT `FK_Clientes_TipoClienteId_TenantFinal` FOREIGN KEY (`EmpresaId`, `TipoClienteId`) REFERENCES `TipoClientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CompraDetalles` ADD CONSTRAINT `FK_CompraDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CompraDetalles` ADD CONSTRAINT `FK_CompraDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CompraImpuestos` ADD CONSTRAINT `FK_CompraImpuestos_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConciliacionesBancarias` ADD CONSTRAINT `FK_ConciliacionesBancarias_CuentaBancariaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CuentaBancariaId`) REFERENCES `CuentasBancarias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConfiguracionesContables` ADD CONSTRAINT `FK_ConfiguracionesContables_CuentaDebeId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CuentaDebeId`) REFERENCES `CuentasContables` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConfiguracionesContables` ADD CONSTRAINT `FK_ConfiguracionesContables_CuentaHaberId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CuentaHaberId`) REFERENCES `CuentasContables` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConsumoInsumoDetalles` ADD CONSTRAINT `FK_ConsumoInsumoDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConsumoInsumoDetalles` ADD CONSTRAINT `FK_ConsumoInsumoDetalles_ConsumoInsumoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ConsumoInsumoId`) REFERENCES `ConsumosInsumos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConsumoInsumoDetalles` ADD CONSTRAINT `FK_ConsumoInsumoDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteoInventarioDetalles` ADD CONSTRAINT `FK_ConteoInventarioDetalles_AjusteInventarioId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AjusteInventarioId`) REFERENCES `AjustesInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteoInventarioDetalles` ADD CONSTRAINT `FK_ConteoInventarioDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteoInventarioDetalles` ADD CONSTRAINT `FK_ConteoInventarioDetalles_ConteoInventarioId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ConteoInventarioId`) REFERENCES `ConteosInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteoInventarioDetalles` ADD CONSTRAINT `FK_ConteoInventarioDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteosInventario` ADD CONSTRAINT `FK_ConteosInventario_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteosInventario` ADD CONSTRAINT `FK_ConteosInventario_CategoriaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CategoriaId`) REFERENCES `Categorias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ConteosInventario` ADD CONSTRAINT `FK_ConteosInventario_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CuentasContables` ADD CONSTRAINT `FK_CuentasContables_CuentaPadreId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CuentaPadreId`) REFERENCES `CuentasContables` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DashboardKpiConfiguraciones` ADD CONSTRAINT `FK_DashboardKpiConfiguraciones_RolId_TenantFinal` FOREIGN KEY (`EmpresaId`, `RolId`) REFERENCES `Roles` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoCategorias` ADD CONSTRAINT `FK_DescuentoCategorias_CategoriaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CategoriaId`) REFERENCES `Categorias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoCategorias` ADD CONSTRAINT `FK_DescuentoCategorias_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoClientes` ADD CONSTRAINT `FK_DescuentoClientes_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoProductos` ADD CONSTRAINT `FK_DescuentoProductos_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoRoles` ADD CONSTRAINT `FK_DescuentoRoles_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DescuentoRoles` ADD CONSTRAINT `FK_DescuentoRoles_RolId_TenantFinal` FOREIGN KEY (`EmpresaId`, `RolId`) REFERENCES `Roles` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DevolucionProveedorDetalles` ADD CONSTRAINT `FK_DevolucionProveedorDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `DevolucionProveedorDetalles` ADD CONSTRAINT `FK_DevolucionProveedorDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ExistenciasVariante` ADD CONSTRAINT `FK_ExistenciasVariante_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ExistenciasVariante` ADD CONSTRAINT `FK_ExistenciasVariante_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `HistorialAplicacionImpuestos` ADD CONSTRAINT `FK_HistorialAplicacionImpuestos_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `HistorialUsoDescuentos` ADD CONSTRAINT `FK_HistorialUsoDescuentos_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoCategorias` ADD CONSTRAINT `FK_ImpuestoCategorias_CategoriaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CategoriaId`) REFERENCES `Categorias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoCategorias` ADD CONSTRAINT `FK_ImpuestoCategorias_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoClientes` ADD CONSTRAINT `FK_ImpuestoClientes_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoOperaciones` ADD CONSTRAINT `FK_ImpuestoOperaciones_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoProductos` ADD CONSTRAINT `FK_ImpuestoProductos_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ImpuestoProveedores` ADD CONSTRAINT `FK_ImpuestoProveedores_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MatchesConciliacion` ADD CONSTRAINT `FK_MatchesConciliacion_MovimientoEstadoCuentaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `MovimientoEstadoCuentaId`) REFERENCES `MovimientosEstadoCuenta` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Modelos` ADD CONSTRAINT `FK_Modelos_MarcaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `MarcaId`) REFERENCES `Marcas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosEstadoCuenta` ADD CONSTRAINT `FK_MovimientosEstadoCuenta_ConciliacionBancariaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ConciliacionBancariaId`) REFERENCES `ConciliacionesBancarias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosInventario` ADD CONSTRAINT `FK_MovimientosInventario_AjusteInventarioId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AjusteInventarioId`) REFERENCES `AjustesInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosInventario` ADD CONSTRAINT `FK_MovimientosInventario_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosInventario` ADD CONSTRAINT `FK_MovimientosInventario_ConsumoInsumoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ConsumoInsumoId`) REFERENCES `ConsumosInsumos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosInventario` ADD CONSTRAINT `FK_MovimientosInventario_TransferenciaInventarioId_TenantFinal` FOREIGN KEY (`EmpresaId`, `TransferenciaInventarioId`) REFERENCES `TransferenciasInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `MovimientosInventario` ADD CONSTRAINT `FK_MovimientosInventario_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PoliticasCosteoInventario` ADD CONSTRAINT `FK_PoliticasCosteoInventario_EmpresaConfiguracionId_TenantFinal` FOREIGN KEY (`EmpresaId`, `EmpresaConfiguracionId`) REFERENCES `EmpresaConfiguraciones` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PreparacionPedidoVentaDetalles` ADD CONSTRAINT `FK_PreparacionPedidoVentaDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PreparacionPedidoVentaDetalles` ADD CONSTRAINT `FK_PreparacionPedidoVentaDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Productos` ADD CONSTRAINT `FK_Productos_CategoriaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `CategoriaId`) REFERENCES `Categorias` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Productos` ADD CONSTRAINT `FK_Productos_ColorId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ColorId`) REFERENCES `Colores` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Productos` ADD CONSTRAINT `FK_Productos_MarcaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `MarcaId`) REFERENCES `Marcas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Productos` ADD CONSTRAINT `FK_Productos_ModeloId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ModeloId`) REFERENCES `Modelos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `Productos` ADD CONSTRAINT `FK_Productos_TallaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `TallaId`) REFERENCES `Tallas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ProductoVariantes` ADD CONSTRAINT `FK_ProductoVariantes_ColorId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ColorId`) REFERENCES `Colores` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ProductoVariantes` ADD CONSTRAINT `FK_ProductoVariantes_MarcaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `MarcaId`) REFERENCES `Marcas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ProductoVariantes` ADD CONSTRAINT `FK_ProductoVariantes_ModeloId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ModeloId`) REFERENCES `Modelos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ProductoVariantes` ADD CONSTRAINT `FK_ProductoVariantes_TallaId_TenantFinal` FOREIGN KEY (`EmpresaId`, `TallaId`) REFERENCES `Tallas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `RecepcionCompraDetalles` ADD CONSTRAINT `FK_RecepcionCompraDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `RecepcionCompraDetalles` ADD CONSTRAINT `FK_RecepcionCompraDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ReservaInventarioDetalles` ADD CONSTRAINT `FK_ReservaInventarioDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `ReservaInventarioDetalles` ADD CONSTRAINT `FK_ReservaInventarioDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `TransferenciaInventarioDetalles` ADD CONSTRAINT `FK_TransferenciaInventarioDetalles_TransferenciaInventa_4603f210` FOREIGN KEY (`EmpresaId`, `TransferenciaInventarioId`) REFERENCES `TransferenciasInventario` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `TransferenciaInventarioDetalles` ADD CONSTRAINT `FK_TransferenciaInventarioDetalles_UbicacionDestinoId_T_13502c15` FOREIGN KEY (`EmpresaId`, `UbicacionDestinoId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `TransferenciaInventarioDetalles` ADD CONSTRAINT `FK_TransferenciaInventarioDetalles_UbicacionOrigenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionOrigenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `TransferenciasInventario` ADD CONSTRAINT `FK_TransferenciasInventario_AlmacenDestinoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenDestinoId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `TransferenciasInventario` ADD CONSTRAINT `FK_TransferenciasInventario_AlmacenOrigenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenOrigenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `UbicacionesAlmacen` ADD CONSTRAINT `FK_UbicacionesAlmacen_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `UbicacionesAlmacen` ADD CONSTRAINT `FK_UbicacionesAlmacen_UbicacionPadreId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionPadreId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `VentaDescuentos` ADD CONSTRAINT `FK_VentaDescuentos_DescuentoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `DescuentoId`) REFERENCES `Descuentos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `VentaDetalles` ADD CONSTRAINT `FK_VentaDetalles_AlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `AlmacenId`) REFERENCES `Almacenes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `VentaDetalles` ADD CONSTRAINT `FK_VentaDetalles_UbicacionAlmacenId_TenantFinal` FOREIGN KEY (`EmpresaId`, `UbicacionAlmacenId`) REFERENCES `UbicacionesAlmacen` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `VentaImpuestos` ADD CONSTRAINT `FK_VentaImpuestos_ImpuestoId_TenantFinal` FOREIGN KEY (`EmpresaId`, `ImpuestoId`) REFERENCES `Impuestos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `RolPermisos` ADD CONSTRAINT `FK_RolPermisos_RolId_TenantFinal` FOREIGN KEY (`EmpresaId`, `RolId`) REFERENCES `Roles` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `EnlacesPublicosFactura` ADD CONSTRAINT `FK_EnlacesPublicosFactura_Factura_TenantFinal` FOREIGN KEY (`EmpresaId`, `FacturaId`) REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `HistorialEnviosFactura` ADD CONSTRAINT `FK_HistorialEnviosFactura_Factura_TenantFinal` FOREIGN KEY (`EmpresaId`, `FacturaId`) REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
    }

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("No se eliminan protecciones multiempresa sin restauración DEV verificada.");
}
