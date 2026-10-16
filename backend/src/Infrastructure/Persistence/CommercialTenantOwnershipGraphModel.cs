using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Expansión explícita del grafo comercial: 46 tablas dependientes con
/// propiedad tenant nullable (histórico en cuarentena) y 12 padres existentes
/// con índices únicos (EmpresaId, Id). NO habilita el acceso multiempresa.
/// </summary>
public static class CommercialTenantOwnershipGraphModel
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder Get(string name)
        {
            var entity = modelBuilder.Model.FindEntityType(name)
                ?? throw new InvalidOperationException($"Entidad comercial no mapeada: {name}");
            return modelBuilder.Entity(entity.ClrType);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AjusteInventarioDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AjusteInventarioDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.AplicacionCuentaPorPagar");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AplicacionesCuentaPorPagar_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaPorPagarId" })
                .HasDatabaseName("IX_AplicacionesCuentaPorPagar_EmpresaId_CuentaPorPagarId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AplicacionesCuentaPorPagar_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.AsignacionCostoMovimientoInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AsignacionesCostoMovimientoInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "MovimientoInventarioId" })
                .HasDatabaseName("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Movi_c0073000");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Prod_6be01dc2");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId", "CapaCostoInventarioId" })
                .HasDatabaseName("IX_AsignacionesCostoMovimientoInventario_EmpresaId_Prod_edc1fd1c");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsignacionesCostoMovimientoInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CapaCostoInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CapasCostoInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "MovimientoInventarioOrigenId" })
                .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_MovimientoInventarioOrigenId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId", "CapaCostoOrigenId" })
                .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_ProductoVarianteId_Ca_a7c2ed8c");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CapasCostoInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConsumoInsumoDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConsumoInsumoDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConteoInventarioDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConteoInventarioDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CostoEstandarInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CostosEstandarInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_CostosEstandarInventario_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CostosEstandarInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CreditoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CreditosCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ClienteId" })
                .HasDatabaseName("IX_CreditosCliente_EmpresaId_ClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CreditosCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CuentaPorPagar");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CuentasPorPagar_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FacturaProveedorId" })
                .HasDatabaseName("IX_CuentasPorPagar_EmpresaId_FacturaProveedorId");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_CuentasPorPagar_EmpresaId_ProveedorId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CuentasPorPagar_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DescuentoClientes_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ClienteId" })
                .HasDatabaseName("IX_DescuentoClientes_EmpresaId_ClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoClientes_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoProducto");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DescuentoProductos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_DescuentoProductos_EmpresaId_ProductoId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoProductos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionClienteDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DevolucionClienteDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "DevolucionClienteId" })
                .HasDatabaseName("IX_DevolucionClienteDetalles_EmpresaId_DevolucionClienteId");
            b.HasIndex(new[] { "EmpresaId", "VentaDetalleId" })
                .HasDatabaseName("IX_DevolucionClienteDetalles_EmpresaId_VentaDetalleId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DevolucionClienteDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DevolucionesCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FacturaId" })
                .HasDatabaseName("IX_DevolucionesCliente_EmpresaId_FacturaId");
            b.HasIndex(new[] { "EmpresaId", "VentaId" })
                .HasDatabaseName("IX_DevolucionesCliente_EmpresaId_VentaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DevolucionesCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionProveedor");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DevolucionesProveedor_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FacturaProveedorId" })
                .HasDatabaseName("IX_DevolucionesProveedor_EmpresaId_FacturaProveedorId");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_DevolucionesProveedor_EmpresaId_OrdenCompraId");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_DevolucionesProveedor_EmpresaId_ProveedorId");
            b.HasIndex(new[] { "EmpresaId", "RecepcionCompraId" })
                .HasDatabaseName("IX_DevolucionesProveedor_EmpresaId_RecepcionCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DevolucionesProveedor_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DevolucionProveedorDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DevolucionProveedorDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "DevolucionProveedorId" })
                .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_DevolucionProveedorId");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraDetalleId" })
                .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_OrdenCompraDetalleId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "RecepcionCompraDetalleId" })
                .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_RecepcionCompr_9e48e60d");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DevolucionProveedorDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.EvaluacionProveedor");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_EvaluacionesProveedor_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_EvaluacionesProveedor_EmpresaId_OrdenCompraId");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_EvaluacionesProveedor_EmpresaId_ProveedorId");
            b.HasIndex(new[] { "EmpresaId", "RecepcionCompraId" })
                .HasDatabaseName("IX_EvaluacionesProveedor_EmpresaId_RecepcionCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EvaluacionesProveedor_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ExistenciaVariante");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ExistenciasVariante_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_ExistenciasVariante_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ExistenciasVariante_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.FacturaProveedorDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_FacturaProveedorDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FacturaProveedorId" })
                .HasDatabaseName("IX_FacturaProveedorDetalles_EmpresaId_FacturaProveedorId");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraDetalleId" })
                .HasDatabaseName("IX_FacturaProveedorDetalles_EmpresaId_OrdenCompraDetalleId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_FacturaProveedorDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_FacturaProveedorDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_FacturaProveedorDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.FacturaProveedor");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_FacturasProveedor_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_FacturasProveedor_EmpresaId_OrdenCompraId");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_FacturasProveedor_EmpresaId_ProveedorId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_FacturasProveedor_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ImpuestoClientes_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ClienteId" })
                .HasDatabaseName("IX_ImpuestoClientes_EmpresaId_ClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoClientes_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoProducto");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ImpuestoProductos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_ImpuestoProductos_EmpresaId_ProductoId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoProductos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoProveedor");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ImpuestoProveedores_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_ImpuestoProveedores_EmpresaId_ProveedorId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoProveedores_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.LoteInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_LotesInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_LotesInventario_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_LotesInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.MatchConciliacion");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_MatchesConciliacion_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "MovimientoFinancieroId" })
                .HasDatabaseName("IX_MatchesConciliacion_EmpresaId_MovimientoFinancieroId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MatchesConciliacion_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.MovimientoFinanciero");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_MovimientosFinancieros_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CompraId" })
                .HasDatabaseName("IX_MovimientosFinancieros_EmpresaId_CompraId");
            b.HasIndex(new[] { "EmpresaId", "FacturaId" })
                .HasDatabaseName("IX_MovimientosFinancieros_EmpresaId_FacturaId");
            b.HasIndex(new[] { "EmpresaId", "VentaId" })
                .HasDatabaseName("IX_MovimientosFinancieros_EmpresaId_VentaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovimientosFinancieros_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.MovimientoInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_MovimientosInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CompraId" })
                .HasDatabaseName("IX_MovimientosInventario_EmpresaId_CompraId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_MovimientosInventario_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_MovimientosInventario_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "RecepcionCompraId" })
                .HasDatabaseName("IX_MovimientosInventario_EmpresaId_RecepcionCompraId");
            b.HasIndex(new[] { "EmpresaId", "VentaId" })
                .HasDatabaseName("IX_MovimientosInventario_EmpresaId_VentaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovimientosInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.NotaCreditoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_NotasCreditoCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FacturaId" })
                .HasDatabaseName("IX_NotasCreditoCliente_EmpresaId_FacturaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_NotasCreditoCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.NotaCreditoProveedor");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_NotasCreditoProveedor_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "DevolucionProveedorId" })
                .HasDatabaseName("IX_NotasCreditoProveedor_EmpresaId_DevolucionProveedorId");
            b.HasIndex(new[] { "EmpresaId", "FacturaProveedorId" })
                .HasDatabaseName("IX_NotasCreditoProveedor_EmpresaId_FacturaProveedorId");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_NotasCreditoProveedor_EmpresaId_ProveedorId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_NotasCreditoProveedor_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.OrdenCompraDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_OrdenCompraDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_OrdenCompraDetalles_EmpresaId_OrdenCompraId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_OrdenCompraDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_OrdenCompraDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenCompraDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.OrdenCompra");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_OrdenesCompra_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_OrdenesCompra_EmpresaId_ProveedorId");
            b.HasIndex(new[] { "EmpresaId", "SolicitudCompraId" })
                .HasDatabaseName("IX_OrdenesCompra_EmpresaId_SolicitudCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_OrdenesCompra_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.PreparacionPedidoVenta");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_PreparacionesPedidoVenta_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "PedidoVentaId" })
                .HasDatabaseName("IX_PreparacionesPedidoVenta_EmpresaId_PedidoVentaId");
            b.HasIndex(new[] { "EmpresaId", "ReservaInventarioId" })
                .HasDatabaseName("IX_PreparacionesPedidoVenta_EmpresaId_ReservaInventarioId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PreparacionesPedidoVenta_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_PreparacionPedidoVentaDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "PreparacionPedidoVentaId" })
                .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_Preparacion_4eed0df2");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PreparacionPedidoVentaDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RecepcionCompraDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_RecepcionCompraDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraDetalleId" })
                .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_OrdenCompraDetalleId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "RecepcionCompraId" })
                .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_RecepcionCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RecepcionCompraDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RecepcionCompra");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_RecepcionesCompra_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_RecepcionesCompra_EmpresaId_OrdenCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RecepcionesCompra_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ReservaInventarioDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "ReservaInventarioId" })
                .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_ReservaInventarioId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReservaInventarioDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ReservaInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ReservasInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "PedidoVentaId" })
                .HasDatabaseName("IX_ReservasInventario_EmpresaId_PedidoVentaId");
            b.HasIndex(new[] { "EmpresaId", "VentaId" })
                .HasDatabaseName("IX_ReservasInventario_EmpresaId_VentaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ReservasInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.SerieInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_SeriesInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_SeriesInventario_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId", "LoteInventarioId" })
                .HasDatabaseName("IX_SeriesInventario_EmpresaId_ProductoVarianteId_LoteIn_d93488e3");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SeriesInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.SolicitudCompraDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_SolicitudCompraDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_SolicitudCompraDetalles_EmpresaId_ProductoId");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_SolicitudCompraDetalles_EmpresaId_ProductoVarianteId");
            b.HasIndex(new[] { "EmpresaId", "SolicitudCompraId" })
                .HasDatabaseName("IX_SolicitudCompraDetalles_EmpresaId_SolicitudCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SolicitudCompraDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.SolicitudCompra");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_SolicitudesCompra_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProveedorId" })
                .HasDatabaseName("IX_SolicitudesCompra_EmpresaId_ProveedorId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_SolicitudesCompra_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ThreeWayMatchResult");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ThreeWayMatchResultados_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "OrdenCompraId" })
                .HasDatabaseName("IX_ThreeWayMatchResultados_EmpresaId_OrdenCompraId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ThreeWayMatchResultados_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TiendaCuentaCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TiendaCuentasCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ClienteId" })
                .HasDatabaseName("IX_TiendaCuentasCliente_EmpresaId_ClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TiendaCuentasCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TiendaDireccionCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TiendaDireccionesCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaClienteId" })
                .HasDatabaseName("IX_TiendaDireccionesCliente_EmpresaId_CuentaClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TiendaDireccionesCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TiendaFavoritoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TiendaFavoritosCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaClienteId" })
                .HasDatabaseName("IX_TiendaFavoritosCliente_EmpresaId_CuentaClienteId");
            b.HasIndex(new[] { "EmpresaId", "ProductoId" })
                .HasDatabaseName("IX_TiendaFavoritosCliente_EmpresaId_ProductoId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TiendaFavoritosCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TiendaSesionCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TiendaSesionesCliente_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaClienteId" })
                .HasDatabaseName("IX_TiendaSesionesCliente_EmpresaId_CuentaClienteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TiendaSesionesCliente_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TransferenciaInventarioDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_ProductoVarianteId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TransferenciaInventarioDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.VariacionCostoEstandarInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_VariacionesCostoEstandarInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "MovimientoInventarioId" })
                .HasDatabaseName("IX_VariacionesCostoEstandarInventario_EmpresaId_Movimie_1aa39239");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId" })
                .HasDatabaseName("IX_VariacionesCostoEstandarInventario_EmpresaId_Product_d20c727f");
            b.HasIndex(new[] { "EmpresaId", "ProductoVarianteId", "CostoEstandarInventarioId" })
                .HasDatabaseName("IX_VariacionesCostoEstandarInventario_EmpresaId_Product_8b3e934e");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_VariacionesCostoEstandarInventario_Empresas_EmpresaId");
        }

        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_CompraDetalles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.CompraDocumento").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_CompraDocumentos_Empresa_Id");

        Get("Solqaryn.Domain.Entities.CompraImpuesto").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_CompraImpuestos_Empresa_Id");

        Get("Solqaryn.Domain.Entities.DocumentoFiscalEmision").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_DocumentosFiscalesEmision_Empresa_Id");

        Get("Solqaryn.Domain.Entities.FacturaDetalle").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_FacturaDetalles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.FacturaPago").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_FacturaPagos_Empresa_Id");

        Get("Solqaryn.Domain.Entities.HistorialUsoDescuento").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_HistorialUsoDescuentos_Empresa_Id");

        Get("Solqaryn.Domain.Entities.PagoOnline").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_PagosOnline_Empresa_Id");

        Get("Solqaryn.Domain.Entities.ProductoImagen").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_ProductoImagenes_Empresa_Id");

        Get("Solqaryn.Domain.Entities.VentaDescuento").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_VentaDescuentos_Empresa_Id");

        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_VentaDetalles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.VentaImpuesto").HasIndex("EmpresaId", "Id").IsUnique()
            .HasDatabaseName("UX_VentaImpuestos_Empresa_Id");
    }
}
