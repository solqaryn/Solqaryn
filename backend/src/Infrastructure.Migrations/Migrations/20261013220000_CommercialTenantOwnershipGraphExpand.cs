using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013220000_CommercialTenantOwnershipGraphExpand")]
public partial class CommercialTenantOwnershipGraphExpand : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Expand aditivo, sin DML ni backfill: NULL legacy en cuarentena.
        m.AddColumn<int>("EmpresaId", "AjusteInventarioDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "AplicacionesCuentaPorPagar", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "AsignacionesCostoMovimientoInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CapasCostoInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConsumoInsumoDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConteoInventarioDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CostosEstandarInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CreditosCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CuentasPorPagar", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DescuentoClientes", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DescuentoProductos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DevolucionClienteDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DevolucionesCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DevolucionesProveedor", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DevolucionProveedorDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "EvaluacionesProveedor", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ExistenciasVariante", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "FacturaProveedorDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "FacturasProveedor", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ImpuestoClientes", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ImpuestoProductos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ImpuestoProveedores", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "LotesInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "MatchesConciliacion", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "MovimientosFinancieros", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "MovimientosInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "NotasCreditoCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "NotasCreditoProveedor", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "OrdenCompraDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "OrdenesCompra", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "PreparacionesPedidoVenta", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "PreparacionPedidoVentaDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "RecepcionCompraDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "RecepcionesCompra", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ReservaInventarioDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ReservasInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "SeriesInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "SolicitudCompraDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "SolicitudesCompra", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ThreeWayMatchResultados", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TiendaCuentasCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TiendaDireccionesCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TiendaFavoritosCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TiendaSesionesCliente", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TransferenciaInventarioDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "VariacionesCostoEstandarInventario", type: "int", nullable: true);

        m.CreateIndex("UX_AjusteInventarioDetalles_Empresa_Id", "AjusteInventarioDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_AjusteInventarioDetalles_EmpresaId_ProductoId", "AjusteInventarioDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_AjusteInventarioDetalles_EmpresaId_ProductoVarianteId", "AjusteInventarioDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_AplicacionesCuentaPorPagar_Empresa_Id", "AplicacionesCuentaPorPagar",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_AplicacionesCuentaPorPagar_EmpresaId_CuentaPorPagarId", "AplicacionesCuentaPorPagar",
            new[] { "EmpresaId", "CuentaPorPagarId" });
        m.CreateIndex("UX_AsignacionesCostoMovimientoInventario_Empresa_Id", "AsignacionesCostoMovimientoInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Movi_c0073000", "AsignacionesCostoMovimientoInventario",
            new[] { "EmpresaId", "MovimientoInventarioId" });
        m.CreateIndex("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Prod_6be01dc2", "AsignacionesCostoMovimientoInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Prod_edc1fd1c", "AsignacionesCostoMovimientoInventario",
            new[] { "EmpresaId", "ProductoVarianteId", "CapaCostoInventarioId" });
        m.CreateIndex("UX_CapasCostoInventario_Empresa_Id", "CapasCostoInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_CapasCostoInventario_EmpresaId_MovimientoInventarioOrigenId", "CapasCostoInventario",
            new[] { "EmpresaId", "MovimientoInventarioOrigenId" });
        m.CreateIndex("IX_CapasCostoInventario_EmpresaId_ProductoVarianteId", "CapasCostoInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_CapasCostoInventario_EmpresaId_ProductoVarianteId_Ca_a7c2ed8c", "CapasCostoInventario",
            new[] { "EmpresaId", "ProductoVarianteId", "CapaCostoOrigenId" });
        m.CreateIndex("UX_ConsumoInsumoDetalles_Empresa_Id", "ConsumoInsumoDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ConsumoInsumoDetalles_EmpresaId_ProductoId", "ConsumoInsumoDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_ConsumoInsumoDetalles_EmpresaId_ProductoVarianteId", "ConsumoInsumoDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_ConteoInventarioDetalles_Empresa_Id", "ConteoInventarioDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ConteoInventarioDetalles_EmpresaId_ProductoVarianteId", "ConteoInventarioDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_CostosEstandarInventario_Empresa_Id", "CostosEstandarInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_CostosEstandarInventario_EmpresaId_ProductoVarianteId", "CostosEstandarInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_CreditosCliente_Empresa_Id", "CreditosCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_CreditosCliente_EmpresaId_ClienteId", "CreditosCliente",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("UX_CuentasPorPagar_Empresa_Id", "CuentasPorPagar",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_CuentasPorPagar_EmpresaId_FacturaProveedorId", "CuentasPorPagar",
            new[] { "EmpresaId", "FacturaProveedorId" });
        m.CreateIndex("IX_CuentasPorPagar_EmpresaId_ProveedorId", "CuentasPorPagar",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("UX_DescuentoClientes_Empresa_Id", "DescuentoClientes",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DescuentoClientes_EmpresaId_ClienteId", "DescuentoClientes",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("UX_DescuentoProductos_Empresa_Id", "DescuentoProductos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DescuentoProductos_EmpresaId_ProductoId", "DescuentoProductos",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("UX_DevolucionClienteDetalles_Empresa_Id", "DevolucionClienteDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DevolucionClienteDetalles_EmpresaId_DevolucionClienteId", "DevolucionClienteDetalles",
            new[] { "EmpresaId", "DevolucionClienteId" });
        m.CreateIndex("IX_DevolucionClienteDetalles_EmpresaId_VentaDetalleId", "DevolucionClienteDetalles",
            new[] { "EmpresaId", "VentaDetalleId" });
        m.CreateIndex("UX_DevolucionesCliente_Empresa_Id", "DevolucionesCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DevolucionesCliente_EmpresaId_FacturaId", "DevolucionesCliente",
            new[] { "EmpresaId", "FacturaId" });
        m.CreateIndex("IX_DevolucionesCliente_EmpresaId_VentaId", "DevolucionesCliente",
            new[] { "EmpresaId", "VentaId" });
        m.CreateIndex("UX_DevolucionesProveedor_Empresa_Id", "DevolucionesProveedor",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DevolucionesProveedor_EmpresaId_FacturaProveedorId", "DevolucionesProveedor",
            new[] { "EmpresaId", "FacturaProveedorId" });
        m.CreateIndex("IX_DevolucionesProveedor_EmpresaId_OrdenCompraId", "DevolucionesProveedor",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("IX_DevolucionesProveedor_EmpresaId_ProveedorId", "DevolucionesProveedor",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("IX_DevolucionesProveedor_EmpresaId_RecepcionCompraId", "DevolucionesProveedor",
            new[] { "EmpresaId", "RecepcionCompraId" });
        m.CreateIndex("UX_DevolucionProveedorDetalles_Empresa_Id", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_DevolucionProveedorDetalles_EmpresaId_DevolucionProveedorId", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "DevolucionProveedorId" });
        m.CreateIndex("IX_DevolucionProveedorDetalles_EmpresaId_OrdenCompraDetalleId", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "OrdenCompraDetalleId" });
        m.CreateIndex("IX_DevolucionProveedorDetalles_EmpresaId_ProductoId", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_DevolucionProveedorDetalles_EmpresaId_ProductoVarianteId", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_DevolucionProveedorDetalles_EmpresaId_RecepcionCompr_9e48e60d", "DevolucionProveedorDetalles",
            new[] { "EmpresaId", "RecepcionCompraDetalleId" });
        m.CreateIndex("UX_EvaluacionesProveedor_Empresa_Id", "EvaluacionesProveedor",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_EvaluacionesProveedor_EmpresaId_OrdenCompraId", "EvaluacionesProveedor",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("IX_EvaluacionesProveedor_EmpresaId_ProveedorId", "EvaluacionesProveedor",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("IX_EvaluacionesProveedor_EmpresaId_RecepcionCompraId", "EvaluacionesProveedor",
            new[] { "EmpresaId", "RecepcionCompraId" });
        m.CreateIndex("UX_ExistenciasVariante_Empresa_Id", "ExistenciasVariante",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ExistenciasVariante_EmpresaId_ProductoVarianteId", "ExistenciasVariante",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_FacturaProveedorDetalles_Empresa_Id", "FacturaProveedorDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_FacturaProveedorDetalles_EmpresaId_FacturaProveedorId", "FacturaProveedorDetalles",
            new[] { "EmpresaId", "FacturaProveedorId" });
        m.CreateIndex("IX_FacturaProveedorDetalles_EmpresaId_OrdenCompraDetalleId", "FacturaProveedorDetalles",
            new[] { "EmpresaId", "OrdenCompraDetalleId" });
        m.CreateIndex("IX_FacturaProveedorDetalles_EmpresaId_ProductoId", "FacturaProveedorDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_FacturaProveedorDetalles_EmpresaId_ProductoVarianteId", "FacturaProveedorDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_FacturasProveedor_Empresa_Id", "FacturasProveedor",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_FacturasProveedor_EmpresaId_OrdenCompraId", "FacturasProveedor",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("IX_FacturasProveedor_EmpresaId_ProveedorId", "FacturasProveedor",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("UX_ImpuestoClientes_Empresa_Id", "ImpuestoClientes",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ImpuestoClientes_EmpresaId_ClienteId", "ImpuestoClientes",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("UX_ImpuestoProductos_Empresa_Id", "ImpuestoProductos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ImpuestoProductos_EmpresaId_ProductoId", "ImpuestoProductos",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("UX_ImpuestoProveedores_Empresa_Id", "ImpuestoProveedores",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ImpuestoProveedores_EmpresaId_ProveedorId", "ImpuestoProveedores",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("UX_LotesInventario_Empresa_Id", "LotesInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_LotesInventario_EmpresaId_ProductoVarianteId", "LotesInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_MatchesConciliacion_Empresa_Id", "MatchesConciliacion",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_MatchesConciliacion_EmpresaId_MovimientoFinancieroId", "MatchesConciliacion",
            new[] { "EmpresaId", "MovimientoFinancieroId" });
        m.CreateIndex("UX_MovimientosFinancieros_Empresa_Id", "MovimientosFinancieros",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_MovimientosFinancieros_EmpresaId_CompraId", "MovimientosFinancieros",
            new[] { "EmpresaId", "CompraId" });
        m.CreateIndex("IX_MovimientosFinancieros_EmpresaId_FacturaId", "MovimientosFinancieros",
            new[] { "EmpresaId", "FacturaId" });
        m.CreateIndex("IX_MovimientosFinancieros_EmpresaId_VentaId", "MovimientosFinancieros",
            new[] { "EmpresaId", "VentaId" });
        m.CreateIndex("UX_MovimientosInventario_Empresa_Id", "MovimientosInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_MovimientosInventario_EmpresaId_CompraId", "MovimientosInventario",
            new[] { "EmpresaId", "CompraId" });
        m.CreateIndex("IX_MovimientosInventario_EmpresaId_ProductoId", "MovimientosInventario",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_MovimientosInventario_EmpresaId_ProductoVarianteId", "MovimientosInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_MovimientosInventario_EmpresaId_RecepcionCompraId", "MovimientosInventario",
            new[] { "EmpresaId", "RecepcionCompraId" });
        m.CreateIndex("IX_MovimientosInventario_EmpresaId_VentaId", "MovimientosInventario",
            new[] { "EmpresaId", "VentaId" });
        m.CreateIndex("UX_NotasCreditoCliente_Empresa_Id", "NotasCreditoCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_NotasCreditoCliente_EmpresaId_FacturaId", "NotasCreditoCliente",
            new[] { "EmpresaId", "FacturaId" });
        m.CreateIndex("UX_NotasCreditoProveedor_Empresa_Id", "NotasCreditoProveedor",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_NotasCreditoProveedor_EmpresaId_DevolucionProveedorId", "NotasCreditoProveedor",
            new[] { "EmpresaId", "DevolucionProveedorId" });
        m.CreateIndex("IX_NotasCreditoProveedor_EmpresaId_FacturaProveedorId", "NotasCreditoProveedor",
            new[] { "EmpresaId", "FacturaProveedorId" });
        m.CreateIndex("IX_NotasCreditoProveedor_EmpresaId_ProveedorId", "NotasCreditoProveedor",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("UX_OrdenCompraDetalles_Empresa_Id", "OrdenCompraDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_OrdenCompraDetalles_EmpresaId_OrdenCompraId", "OrdenCompraDetalles",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("IX_OrdenCompraDetalles_EmpresaId_ProductoId", "OrdenCompraDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_OrdenCompraDetalles_EmpresaId_ProductoVarianteId", "OrdenCompraDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_OrdenesCompra_Empresa_Id", "OrdenesCompra",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_OrdenesCompra_EmpresaId_ProveedorId", "OrdenesCompra",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("IX_OrdenesCompra_EmpresaId_SolicitudCompraId", "OrdenesCompra",
            new[] { "EmpresaId", "SolicitudCompraId" });
        m.CreateIndex("UX_PreparacionesPedidoVenta_Empresa_Id", "PreparacionesPedidoVenta",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_PreparacionesPedidoVenta_EmpresaId_PedidoVentaId", "PreparacionesPedidoVenta",
            new[] { "EmpresaId", "PedidoVentaId" });
        m.CreateIndex("IX_PreparacionesPedidoVenta_EmpresaId_ReservaInventarioId", "PreparacionesPedidoVenta",
            new[] { "EmpresaId", "ReservaInventarioId" });
        m.CreateIndex("UX_PreparacionPedidoVentaDetalles_Empresa_Id", "PreparacionPedidoVentaDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_PreparacionPedidoVentaDetalles_EmpresaId_Preparacion_4eed0df2", "PreparacionPedidoVentaDetalles",
            new[] { "EmpresaId", "PreparacionPedidoVentaId" });
        m.CreateIndex("IX_PreparacionPedidoVentaDetalles_EmpresaId_ProductoVarianteId", "PreparacionPedidoVentaDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_RecepcionCompraDetalles_Empresa_Id", "RecepcionCompraDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_RecepcionCompraDetalles_EmpresaId_OrdenCompraDetalleId", "RecepcionCompraDetalles",
            new[] { "EmpresaId", "OrdenCompraDetalleId" });
        m.CreateIndex("IX_RecepcionCompraDetalles_EmpresaId_ProductoId", "RecepcionCompraDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_RecepcionCompraDetalles_EmpresaId_ProductoVarianteId", "RecepcionCompraDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_RecepcionCompraDetalles_EmpresaId_RecepcionCompraId", "RecepcionCompraDetalles",
            new[] { "EmpresaId", "RecepcionCompraId" });
        m.CreateIndex("UX_RecepcionesCompra_Empresa_Id", "RecepcionesCompra",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_RecepcionesCompra_EmpresaId_OrdenCompraId", "RecepcionesCompra",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("UX_ReservaInventarioDetalles_Empresa_Id", "ReservaInventarioDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ReservaInventarioDetalles_EmpresaId_ProductoVarianteId", "ReservaInventarioDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_ReservaInventarioDetalles_EmpresaId_ReservaInventarioId", "ReservaInventarioDetalles",
            new[] { "EmpresaId", "ReservaInventarioId" });
        m.CreateIndex("UX_ReservasInventario_Empresa_Id", "ReservasInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ReservasInventario_EmpresaId_PedidoVentaId", "ReservasInventario",
            new[] { "EmpresaId", "PedidoVentaId" });
        m.CreateIndex("IX_ReservasInventario_EmpresaId_VentaId", "ReservasInventario",
            new[] { "EmpresaId", "VentaId" });
        m.CreateIndex("UX_SeriesInventario_Empresa_Id", "SeriesInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_SeriesInventario_EmpresaId_ProductoVarianteId", "SeriesInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_SeriesInventario_EmpresaId_ProductoVarianteId_LoteIn_d93488e3", "SeriesInventario",
            new[] { "EmpresaId", "ProductoVarianteId", "LoteInventarioId" });
        m.CreateIndex("UX_SolicitudCompraDetalles_Empresa_Id", "SolicitudCompraDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_SolicitudCompraDetalles_EmpresaId_ProductoId", "SolicitudCompraDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_SolicitudCompraDetalles_EmpresaId_ProductoVarianteId", "SolicitudCompraDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_SolicitudCompraDetalles_EmpresaId_SolicitudCompraId", "SolicitudCompraDetalles",
            new[] { "EmpresaId", "SolicitudCompraId" });
        m.CreateIndex("UX_SolicitudesCompra_Empresa_Id", "SolicitudesCompra",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_SolicitudesCompra_EmpresaId_ProveedorId", "SolicitudesCompra",
            new[] { "EmpresaId", "ProveedorId" });
        m.CreateIndex("UX_ThreeWayMatchResultados_Empresa_Id", "ThreeWayMatchResultados",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_ThreeWayMatchResultados_EmpresaId_OrdenCompraId", "ThreeWayMatchResultados",
            new[] { "EmpresaId", "OrdenCompraId" });
        m.CreateIndex("UX_TiendaCuentasCliente_Empresa_Id", "TiendaCuentasCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_TiendaCuentasCliente_EmpresaId_ClienteId", "TiendaCuentasCliente",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("UX_TiendaDireccionesCliente_Empresa_Id", "TiendaDireccionesCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_TiendaDireccionesCliente_EmpresaId_CuentaClienteId", "TiendaDireccionesCliente",
            new[] { "EmpresaId", "CuentaClienteId" });
        m.CreateIndex("UX_TiendaFavoritosCliente_Empresa_Id", "TiendaFavoritosCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_TiendaFavoritosCliente_EmpresaId_CuentaClienteId", "TiendaFavoritosCliente",
            new[] { "EmpresaId", "CuentaClienteId" });
        m.CreateIndex("IX_TiendaFavoritosCliente_EmpresaId_ProductoId", "TiendaFavoritosCliente",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("UX_TiendaSesionesCliente_Empresa_Id", "TiendaSesionesCliente",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_TiendaSesionesCliente_EmpresaId_CuentaClienteId", "TiendaSesionesCliente",
            new[] { "EmpresaId", "CuentaClienteId" });
        m.CreateIndex("UX_TransferenciaInventarioDetalles_Empresa_Id", "TransferenciaInventarioDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_TransferenciaInventarioDetalles_EmpresaId_ProductoVarianteId", "TransferenciaInventarioDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_VariacionesCostoEstandarInventario_Empresa_Id", "VariacionesCostoEstandarInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_VariacionesCostoEstandarInventario_EmpresaId_Movimie_1aa39239", "VariacionesCostoEstandarInventario",
            new[] { "EmpresaId", "MovimientoInventarioId" });
        m.CreateIndex("IX_VariacionesCostoEstandarInventario_EmpresaId_Product_d20c727f", "VariacionesCostoEstandarInventario",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("IX_VariacionesCostoEstandarInventario_EmpresaId_Product_8b3e934e", "VariacionesCostoEstandarInventario",
            new[] { "EmpresaId", "ProductoVarianteId", "CostoEstandarInventarioId" });
        m.CreateIndex("UX_CompraDetalles_Empresa_Id", "CompraDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_CompraDocumentos_Empresa_Id", "CompraDocumentos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_CompraImpuestos_Empresa_Id", "CompraImpuestos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_DocumentosFiscalesEmision_Empresa_Id", "DocumentosFiscalesEmision",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_FacturaDetalles_Empresa_Id", "FacturaDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_FacturaPagos_Empresa_Id", "FacturaPagos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_HistorialUsoDescuentos_Empresa_Id", "HistorialUsoDescuentos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_PagosOnline_Empresa_Id", "PagosOnline",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_ProductoImagenes_Empresa_Id", "ProductoImagenes",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_VentaDescuentos_Empresa_Id", "VentaDescuentos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_VentaDetalles_Empresa_Id", "VentaDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_VentaImpuestos_Empresa_Id", "VentaImpuestos",
            new[] { "EmpresaId", "Id" }, unique: true);

        m.AddForeignKey("FK_AjusteInventarioDetalles_Empresas_EmpresaId", "AjusteInventarioDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_AplicacionesCuentaPorPagar_Empresas_EmpresaId", "AplicacionesCuentaPorPagar", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_AsignacionesCostoMovimientoInventario_Empresas_EmpresaId", "AsignacionesCostoMovimientoInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CapasCostoInventario_Empresas_EmpresaId", "CapasCostoInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConsumoInsumoDetalles_Empresas_EmpresaId", "ConsumoInsumoDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConteoInventarioDetalles_Empresas_EmpresaId", "ConteoInventarioDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CostosEstandarInventario_Empresas_EmpresaId", "CostosEstandarInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CreditosCliente_Empresas_EmpresaId", "CreditosCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CuentasPorPagar_Empresas_EmpresaId", "CuentasPorPagar", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DescuentoClientes_Empresas_EmpresaId", "DescuentoClientes", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DescuentoProductos_Empresas_EmpresaId", "DescuentoProductos", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DevolucionClienteDetalles_Empresas_EmpresaId", "DevolucionClienteDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DevolucionesCliente_Empresas_EmpresaId", "DevolucionesCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DevolucionesProveedor_Empresas_EmpresaId", "DevolucionesProveedor", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DevolucionProveedorDetalles_Empresas_EmpresaId", "DevolucionProveedorDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_EvaluacionesProveedor_Empresas_EmpresaId", "EvaluacionesProveedor", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ExistenciasVariante_Empresas_EmpresaId", "ExistenciasVariante", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_FacturaProveedorDetalles_Empresas_EmpresaId", "FacturaProveedorDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_FacturasProveedor_Empresas_EmpresaId", "FacturasProveedor", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ImpuestoClientes_Empresas_EmpresaId", "ImpuestoClientes", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ImpuestoProductos_Empresas_EmpresaId", "ImpuestoProductos", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ImpuestoProveedores_Empresas_EmpresaId", "ImpuestoProveedores", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_LotesInventario_Empresas_EmpresaId", "LotesInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_MatchesConciliacion_Empresas_EmpresaId", "MatchesConciliacion", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_MovimientosFinancieros_Empresas_EmpresaId", "MovimientosFinancieros", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_MovimientosInventario_Empresas_EmpresaId", "MovimientosInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_NotasCreditoCliente_Empresas_EmpresaId", "NotasCreditoCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_NotasCreditoProveedor_Empresas_EmpresaId", "NotasCreditoProveedor", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_OrdenCompraDetalles_Empresas_EmpresaId", "OrdenCompraDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_OrdenesCompra_Empresas_EmpresaId", "OrdenesCompra", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_PreparacionesPedidoVenta_Empresas_EmpresaId", "PreparacionesPedidoVenta", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_PreparacionPedidoVentaDetalles_Empresas_EmpresaId", "PreparacionPedidoVentaDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_RecepcionCompraDetalles_Empresas_EmpresaId", "RecepcionCompraDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_RecepcionesCompra_Empresas_EmpresaId", "RecepcionesCompra", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ReservaInventarioDetalles_Empresas_EmpresaId", "ReservaInventarioDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ReservasInventario_Empresas_EmpresaId", "ReservasInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_SeriesInventario_Empresas_EmpresaId", "SeriesInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_SolicitudCompraDetalles_Empresas_EmpresaId", "SolicitudCompraDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_SolicitudesCompra_Empresas_EmpresaId", "SolicitudesCompra", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ThreeWayMatchResultados_Empresas_EmpresaId", "ThreeWayMatchResultados", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TiendaCuentasCliente_Empresas_EmpresaId", "TiendaCuentasCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TiendaDireccionesCliente_Empresas_EmpresaId", "TiendaDireccionesCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TiendaFavoritosCliente_Empresas_EmpresaId", "TiendaFavoritosCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TiendaSesionesCliente_Empresas_EmpresaId", "TiendaSesionesCliente", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TransferenciaInventarioDetalles_Empresas_EmpresaId", "TransferenciaInventarioDetalles", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_VariacionesCostoEstandarInventario_Empresas_EmpresaId", "VariacionesCostoEstandarInventario", "EmpresaId",
            "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }
    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("No deshacer ownership tenant: restaurar backup DEV verificado.");
}
