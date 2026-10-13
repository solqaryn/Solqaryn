using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Tenant owner explícito para 41 recursos empresariales independientes.
/// Datos legacy NULL no se atribuyen; las altas toman la empresa RBAC verificada.
/// Unicidades comerciales se particionan por EmpresaId conservando índices soporte.
/// </summary>
public static class CommercialTenantIndependentOwnershipModel
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder Get(string name)
        {
            var type = modelBuilder.Model.FindEntityType(name)
                ?? throw new InvalidOperationException($"Entidad empresarial no mapeada: {name}");
            return modelBuilder.Entity(type.ClrType);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.AjusteInventario");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AjustesInventario_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AjustesInventario_Empresas_EmpresaId");
            b.HasIndex(new[] { "NumeroAjuste" }).IsUnique(false)
                .HasDatabaseName("IX_AjustesInventario_NumeroAjuste_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NumeroAjuste" }).IsUnique()
                .HasDatabaseName("UX_AjustesInventario_Empresa_NumeroAjuste");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Almacen");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Almacenes_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Almacenes_Empresas_EmpresaId");
            b.HasIndex(new[] { "CodigoActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_Almacenes_CodigoActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "CodigoActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Almacenes_Empresa_CodigoActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AsientoDetalles_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsientoDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.AsientoContable");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_AsientosContables_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsientosContables_Empresas_EmpresaId");
            b.HasIndex(new[] { "Numero" }).IsUnique(false)
                .HasDatabaseName("IX_AsientosContables_Numero_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique()
                .HasDatabaseName("UX_AsientosContables_Empresa_Numero");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.CajaMovimiento");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CajaMovimientos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CajaMovimientos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.Caja");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Cajas_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Cajas_Empresas_EmpresaId");
            b.HasIndex(new[] { "SesionActivaId" }).IsUnique(false)
                .HasDatabaseName("IX_Cajas_SesionActivaId_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "SesionActivaId" }).IsUnique()
                .HasDatabaseName("UX_Cajas_Empresa_SesionActivaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.CajaSesion");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CajaSesiones_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CajaSesiones_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CargaMasivaError");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CargaMasivaErrores_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CargaMasivaErrores_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CargaMasiva");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CargasMasivas_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CargasMasivas_Empresas_EmpresaId");
            b.HasIndex(new[] { "Tipo", "HashArchivo" }).IsUnique(false)
                .HasDatabaseName("IX_CargasMasivas_Tipo_HashArchivo_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Tipo", "HashArchivo" }).IsUnique()
                .HasDatabaseName("UX_CargasMasivas_Empresa_Tipo_HashArchivo");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Categoria");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Categorias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categorias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.CentroCosto");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CentrosCosto_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CentrosCosto_Empresas_EmpresaId");
            b.HasIndex(new[] { "CodigoActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_CentrosCosto_CodigoActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "CodigoActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_CentrosCosto_Empresa_CodigoActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Color");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Colores_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Colores_Empresas_EmpresaId");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_Colores_NombreActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Colores_Empresa_NombreActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConciliacionesBancarias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConciliacionesBancarias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConfiguracionesContables_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConfiguracionesContables_Empresas_EmpresaId");
            b.HasIndex(new[] { "Evento" }).IsUnique(false)
                .HasDatabaseName("IX_ConfiguracionesContables_Evento_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Evento" }).IsUnique()
                .HasDatabaseName("UX_ConfiguracionesContables_Empresa_Evento");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConsumoInsumo");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConsumosInsumos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConsumosInsumos_Empresas_EmpresaId");
            b.HasIndex(new[] { "NumeroConsumo" }).IsUnique(false)
                .HasDatabaseName("IX_ConsumosInsumos_NumeroConsumo_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NumeroConsumo" }).IsUnique()
                .HasDatabaseName("UX_ConsumosInsumos_Empresa_NumeroConsumo");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConteoInventario");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ConteosInventario_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConteosInventario_Empresas_EmpresaId");
            b.HasIndex(new[] { "Numero" }).IsUnique(false)
                .HasDatabaseName("IX_ConteosInventario_Numero_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique()
                .HasDatabaseName("UX_ConteosInventario_Empresa_Numero");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CostoEnvio");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CostosEnvio_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CostosEnvio_Empresas_EmpresaId");
            b.HasIndex(new[] { "PredeterminadoActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_CostosEnvio_PredeterminadoActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "PredeterminadoActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_CostosEnvio_Empresa_PredeterminadoActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.CuentaBancaria");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CuentasBancarias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CuentasBancarias_Empresas_EmpresaId");
            b.HasIndex(new[] { "BancoId", "NumeroCuenta" }).IsUnique(false)
                .HasDatabaseName("IX_CuentasBancarias_BancoId_NumeroCuenta_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "BancoId", "NumeroCuenta" }).IsUnique()
                .HasDatabaseName("UX_CuentasBancarias_Empresa_BancoId_NumeroCuenta");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CuentaContable");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_CuentasContables_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CuentasContables_Empresas_EmpresaId");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false)
                .HasDatabaseName("IX_CuentasContables_Codigo_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique()
                .HasDatabaseName("UX_CuentasContables_Empresa_Codigo");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DashboardKpiConfiguracion");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DashboardKpiConfiguraciones_Empresas_EmpresaId");
            b.HasIndex(new[] { "RolId", "MetricKey" }).IsUnique(false)
                .HasDatabaseName("IX_DashboardKpiConfiguraciones_RolId_MetricKey_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "RolId", "MetricKey" }).IsUnique()
                .HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_RolId_MetricKey");
            b.HasIndex(new[] { "UsuarioId", "MetricKey" }).IsUnique(false)
                .HasDatabaseName("IX_DashboardKpiConfiguraciones_UsuarioId_MetricKey_Fore_52c90cbb");
            b.HasIndex(new[] { "EmpresaId", "UsuarioId", "MetricKey" }).IsUnique()
                .HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_UsuarioId_MetricKey");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoCategoria");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DescuentoCategorias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoCategorias_Empresas_EmpresaId");
            b.HasIndex(new[] { "DescuentoId", "CategoriaId" }).IsUnique(false)
                .HasDatabaseName("IX_DescuentoCategorias_DescuentoId_CategoriaId_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId", "CategoriaId" }).IsUnique()
                .HasDatabaseName("UX_DescuentoCategorias_Empresa_DescuentoId_CategoriaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoRol");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_DescuentoRoles_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoRoles_Empresas_EmpresaId");
            b.HasIndex(new[] { "DescuentoId", "RolId" }).IsUnique(false)
                .HasDatabaseName("IX_DescuentoRoles_DescuentoId_RolId_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId", "RolId" }).IsUnique()
                .HasDatabaseName("UX_DescuentoRoles_Empresa_DescuentoId_RolId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Descuento");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Descuentos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Descuentos_Empresas_EmpresaId");
            b.HasIndex(new[] { "CodigoPromocionalNormalizado" }).IsUnique(false)
                .HasDatabaseName("IX_Descuentos_CodigoPromocionalNormalizado_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "CodigoPromocionalNormalizado" }).IsUnique()
                .HasDatabaseName("UX_Descuentos_Empresa_CodigoPromocionalNormalizado");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.EmpresaConfiguracion");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_EmpresaConfiguraciones_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EmpresaConfiguraciones_Empresas_EmpresaId");
            b.HasIndex(new[] { "ActivaUnica" }).IsUnique(false)
                .HasDatabaseName("IX_EmpresaConfiguraciones_ActivaUnica_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "ActivaUnica" }).IsUnique()
                .HasDatabaseName("UX_EmpresaConfiguraciones_Empresa_ActivaUnica");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.EnlacePublicoFactura");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_EnlacesPublicosFactura_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EnlacesPublicosFactura_Empresas_EmpresaId");
            b.HasIndex(new[] { "EmpresaId", "Token" }).IsUnique()
                .HasDatabaseName("UX_EnlacesPublicosFactura_Empresa_Token");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.HistorialAplicacionImpuesto");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_HistorialAplicacionImpuestos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialAplicacionImpuestos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.HistorialEnvioFactura");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_HistorialEnviosFactura_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialEnviosFactura_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoCategoria");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ImpuestoCategorias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoCategorias_Empresas_EmpresaId");
            b.HasIndex(new[] { "ImpuestoId", "CategoriaId" }).IsUnique(false)
                .HasDatabaseName("IX_ImpuestoCategorias_ImpuestoId_CategoriaId_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId", "CategoriaId" }).IsUnique()
                .HasDatabaseName("UX_ImpuestoCategorias_Empresa_ImpuestoId_CategoriaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoOperacion");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_ImpuestoOperaciones_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoOperaciones_Empresas_EmpresaId");
            b.HasIndex(new[] { "ImpuestoId", "Operacion" }).IsUnique(false)
                .HasDatabaseName("IX_ImpuestoOperaciones_ImpuestoId_Operacion_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId", "Operacion" }).IsUnique()
                .HasDatabaseName("UX_ImpuestoOperaciones_Empresa_ImpuestoId_Operacion");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Impuesto");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Impuestos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Impuestos_Empresas_EmpresaId");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false)
                .HasDatabaseName("IX_Impuestos_Codigo_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique()
                .HasDatabaseName("UX_Impuestos_Empresa_Codigo");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Marca");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Marcas_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Marcas_Empresas_EmpresaId");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_Marcas_NombreActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Marcas_Empresa_NombreActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Modelo");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Modelos_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Modelos_Empresas_EmpresaId");
            b.HasIndex(new[] { "NombreMarcaActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_Modelos_NombreMarcaActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NombreMarcaActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Modelos_Empresa_NombreMarcaActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_MovimientosEstadoCuenta_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovimientosEstadoCuenta_Empresas_EmpresaId");
            b.HasIndex(new[] { "ConciliacionBancariaId", "IdempotencyKey" }).IsUnique(false)
                .HasDatabaseName("IX_MovimientosEstadoCuenta_ConciliacionBancariaId_Idemp_1aaeebd7");
            b.HasIndex(new[] { "EmpresaId", "ConciliacionBancariaId", "IdempotencyKey" }).IsUnique()
                .HasDatabaseName("UX_MovimientosEstadoCuenta_Empresa_ConciliacionBancaria_ef44b7da");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.PeriodoContable");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_PeriodosContables_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PeriodosContables_Empresas_EmpresaId");
            b.HasIndex(new[] { "FechaInicio", "FechaFin" }).IsUnique(false)
                .HasDatabaseName("IX_PeriodosContables_FechaInicio_FechaFin_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "FechaInicio", "FechaFin" }).IsUnique()
                .HasDatabaseName("UX_PeriodosContables_Empresa_FechaInicio_FechaFin");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.PoliticaCosteoInventario");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_PoliticasCosteoInventario_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PoliticasCosteoInventario_Empresas_EmpresaId");
            b.HasIndex(new[] { "EmpresaConfiguracionVigenteId" }).IsUnique(false)
                .HasDatabaseName("IX_PoliticasCosteoInventario_EmpresaConfiguracionVigent_62e166e6");
            b.HasIndex(new[] { "EmpresaId", "EmpresaConfiguracionVigenteId" }).IsUnique()
                .HasDatabaseName("UX_PoliticasCosteoInventario_Empresa_EmpresaConfiguraci_fb4aa187");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RevisionFinanciera");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_RevisionesFinancieras_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RevisionesFinancieras_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Talla");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_Tallas_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Tallas_Empresas_EmpresaId");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_Tallas_NombreActivoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_Tallas_Empresa_NombreActivoUnico");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TemaVisual");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TemaVisual_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TemaVisual_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TipoCliente");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TipoClientes_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TipoClientes_Empresas_EmpresaId");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false)
                .HasDatabaseName("IX_TipoClientes_Codigo_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique()
                .HasDatabaseName("UX_TipoClientes_Empresa_Codigo");
            b.HasIndex(new[] { "EsPredeterminadoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_TipoClientes_EsPredeterminadoUnico_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "EsPredeterminadoUnico" }).IsUnique()
                .HasDatabaseName("UX_TipoClientes_Empresa_EsPredeterminadoUnico");
            b.HasIndex(new[] { "NombreNormalizado" }).IsUnique(false)
                .HasDatabaseName("IX_TipoClientes_NombreNormalizado_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "NombreNormalizado" }).IsUnique()
                .HasDatabaseName("UX_TipoClientes_Empresa_NombreNormalizado");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TransferenciaInventario");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_TransferenciasInventario_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TransferenciasInventario_Empresas_EmpresaId");
            b.HasIndex(new[] { "Numero" }).IsUnique(false)
                .HasDatabaseName("IX_TransferenciasInventario_Numero_ForeignKeySupport");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique()
                .HasDatabaseName("UX_TransferenciasInventario_Empresa_Numero");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.UbicacionAlmacen");
            b.Property<int?>("EmpresaId").HasColumnType("int");
            b.HasIndex("EmpresaId", "Id").IsUnique()
                .HasDatabaseName("UX_UbicacionesAlmacen_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UbicacionesAlmacen_Empresas_EmpresaId");
            b.HasIndex(new[] { "AlmacenId", "CodigoActivoUnico" }).IsUnique(false)
                .HasDatabaseName("IX_UbicacionesAlmacen_AlmacenId_CodigoActivoUnico_Forei_44d4a87d");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId", "CodigoActivoUnico" }).IsUnique()
                .HasDatabaseName("UX_UbicacionesAlmacen_Empresa_AlmacenId_CodigoActivoUnico");
        }
        // Soportes compuestos nuevos para relaciones catálogo/finanzas/inventario.
        Get("Solqaryn.Domain.Entities.UbicacionAlmacen").HasIndex(new[] { "EmpresaId", "AlmacenId", "Id" }).IsUnique()
            .HasDatabaseName("UX_UbicacionesAlmacen_EmpresaId_AlmacenId_Id");
        Get("Solqaryn.Domain.Entities.Sucursal").HasIndex(new[] { "EmpresaId", "Id" }).IsUnique()
            .HasDatabaseName("UX_Sucursales_EmpresaId_Id");
        Get("Solqaryn.Domain.Entities.ConteoInventario").HasIndex(new[] { "EmpresaId", "Id", "AlmacenId" }).IsUnique()
            .HasDatabaseName("UX_ConteosInventario_EmpresaId_Id_AlmacenId");
        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AjusteInventarioId");
        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AlmacenId_Ubicaci_7edecb64");
        Get("Solqaryn.Domain.Entities.Almacen").HasIndex(new[] { "EmpresaId", "SucursalId" })
            .HasDatabaseName("IX_Almacenes_EmpresaId_SucursalId");
        Get("Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle").HasIndex(new[] { "EmpresaId", "AsientoContableId" })
            .HasDatabaseName("IX_AsientoDetalles_EmpresaId_AsientoContableId");
        Get("Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle").HasIndex(new[] { "EmpresaId", "CuentaContableId" })
            .HasDatabaseName("IX_AsientoDetalles_EmpresaId_CuentaContableId");
        Get("Solqaryn.Domain.Entities.Cajas.CajaMovimiento").HasIndex(new[] { "EmpresaId", "CajaSesionId" })
            .HasDatabaseName("IX_CajaMovimientos_EmpresaId_CajaSesionId");
        Get("Solqaryn.Domain.Entities.Cajas.CajaSesion").HasIndex(new[] { "EmpresaId", "CajaId" })
            .HasDatabaseName("IX_CajaSesiones_EmpresaId_CajaId");
        Get("Solqaryn.Domain.Entities.CapaCostoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.CapaCostoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.CargaMasivaError").HasIndex(new[] { "EmpresaId", "CargaMasivaId" })
            .HasDatabaseName("IX_CargaMasivaErrores_EmpresaId_CargaMasivaId");
        Get("Solqaryn.Domain.Entities.Contabilidad.CentroCosto").HasIndex(new[] { "EmpresaId", "SucursalId" })
            .HasDatabaseName("IX_CentrosCosto_EmpresaId_SucursalId");
        Get("Solqaryn.Domain.Entities.Cliente").HasIndex(new[] { "EmpresaId", "TipoClienteId" })
            .HasDatabaseName("IX_Clientes_EmpresaId_TipoClienteId");
        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_CompraDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_CompraDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.CompraImpuesto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_CompraImpuestos_EmpresaId_ImpuestoId");
        Get("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria").HasIndex(new[] { "EmpresaId", "CuentaBancariaId" })
            .HasDatabaseName("IX_ConciliacionesBancarias_EmpresaId_CuentaBancariaId");
        Get("Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable").HasIndex(new[] { "EmpresaId", "CuentaDebeId" })
            .HasDatabaseName("IX_ConfiguracionesContables_EmpresaId_CuentaDebeId");
        Get("Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable").HasIndex(new[] { "EmpresaId", "CuentaHaberId" })
            .HasDatabaseName("IX_ConfiguracionesContables_EmpresaId_CuentaHaberId");
        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "ConsumoInsumoId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_ConsumoInsumoId");
        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AjusteInventarioId");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "ConteoInventarioId", "AlmacenId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_ConteoInventarioI_d5c0a907");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AlmacenId_Ubicaci_8bcaedc4");
        Get("Solqaryn.Domain.Entities.ConteoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ConteosInventario_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.ConteoInventario").HasIndex(new[] { "EmpresaId", "CategoriaId" })
            .HasDatabaseName("IX_ConteosInventario_EmpresaId_CategoriaId");
        Get("Solqaryn.Domain.Entities.ConteoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ConteosInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.CuentaContable").HasIndex(new[] { "EmpresaId", "CuentaPadreId" })
            .HasDatabaseName("IX_CuentasContables_EmpresaId_CuentaPadreId");
        Get("Solqaryn.Domain.Entities.DescuentoCategoria").HasIndex(new[] { "EmpresaId", "CategoriaId" })
            .HasDatabaseName("IX_DescuentoCategorias_EmpresaId_CategoriaId");
        Get("Solqaryn.Domain.Entities.DescuentoCliente").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_DescuentoClientes_EmpresaId_DescuentoId");
        Get("Solqaryn.Domain.Entities.DescuentoProducto").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_DescuentoProductos_EmpresaId_DescuentoId");
        Get("Solqaryn.Domain.Entities.DevolucionProveedorDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.DevolucionProveedorDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_DevolucionProveedorDetalles_EmpresaId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.ExistenciaVariante").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ExistenciasVariante_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.ExistenciaVariante").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ExistenciasVariante_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.HistorialAplicacionImpuesto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_HistorialAplicacionImpuestos_EmpresaId_ImpuestoId");
        Get("Solqaryn.Domain.Entities.HistorialUsoDescuento").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_HistorialUsoDescuentos_EmpresaId_DescuentoId");
        Get("Solqaryn.Domain.Entities.ImpuestoCategoria").HasIndex(new[] { "EmpresaId", "CategoriaId" })
            .HasDatabaseName("IX_ImpuestoCategorias_EmpresaId_CategoriaId");
        Get("Solqaryn.Domain.Entities.ImpuestoCliente").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoClientes_EmpresaId_ImpuestoId");
        Get("Solqaryn.Domain.Entities.ImpuestoProducto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoProductos_EmpresaId_ImpuestoId");
        Get("Solqaryn.Domain.Entities.ImpuestoProveedor").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoProveedores_EmpresaId_ImpuestoId");
        Get("Solqaryn.Domain.Entities.Bancos.MatchConciliacion").HasIndex(new[] { "EmpresaId", "MovimientoEstadoCuentaId" })
            .HasDatabaseName("IX_MatchesConciliacion_EmpresaId_MovimientoEstadoCuentaId");
        Get("Solqaryn.Domain.Entities.Modelo").HasIndex(new[] { "EmpresaId", "MarcaId" })
            .HasDatabaseName("IX_Modelos_EmpresaId_MarcaId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_AjusteInventarioId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "ConsumoInsumoId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_ConsumoInsumoId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "TransferenciaInventarioId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_TransferenciaInventarioId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.PoliticaCosteoInventario").HasIndex(new[] { "EmpresaId", "EmpresaConfiguracionId" })
            .HasDatabaseName("IX_PoliticasCosteoInventario_EmpresaId_EmpresaConfiguracionId");
        Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_AlmacenId_U_ab46262c");
        Get("Solqaryn.Domain.Entities.Producto").HasIndex(new[] { "EmpresaId", "CategoriaId" })
            .HasDatabaseName("IX_Productos_EmpresaId_CategoriaId");
        Get("Solqaryn.Domain.Entities.Producto").HasIndex(new[] { "EmpresaId", "ColorId" })
            .HasDatabaseName("IX_Productos_EmpresaId_ColorId");
        Get("Solqaryn.Domain.Entities.Producto").HasIndex(new[] { "EmpresaId", "MarcaId" })
            .HasDatabaseName("IX_Productos_EmpresaId_MarcaId");
        Get("Solqaryn.Domain.Entities.Producto").HasIndex(new[] { "EmpresaId", "ModeloId" })
            .HasDatabaseName("IX_Productos_EmpresaId_ModeloId");
        Get("Solqaryn.Domain.Entities.Producto").HasIndex(new[] { "EmpresaId", "TallaId" })
            .HasDatabaseName("IX_Productos_EmpresaId_TallaId");
        Get("Solqaryn.Domain.Entities.ProductoVariante").HasIndex(new[] { "EmpresaId", "ColorId" })
            .HasDatabaseName("IX_ProductoVariantes_EmpresaId_ColorId");
        Get("Solqaryn.Domain.Entities.ProductoVariante").HasIndex(new[] { "EmpresaId", "MarcaId" })
            .HasDatabaseName("IX_ProductoVariantes_EmpresaId_MarcaId");
        Get("Solqaryn.Domain.Entities.ProductoVariante").HasIndex(new[] { "EmpresaId", "ModeloId" })
            .HasDatabaseName("IX_ProductoVariantes_EmpresaId_ModeloId");
        Get("Solqaryn.Domain.Entities.ProductoVariante").HasIndex(new[] { "EmpresaId", "TallaId" })
            .HasDatabaseName("IX_ProductoVariantes_EmpresaId_TallaId");
        Get("Solqaryn.Domain.Entities.RecepcionCompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.RecepcionCompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_AlmacenId_Ubicacio_8a10442f");
        Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_AlmacenId_Ubicac_b786f94a");
        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "TransferenciaInventarioId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_Transferen_8b22a8d8");
        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionDestinoId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_UbicacionDestinoId");
        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionOrigenId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_UbicacionOrigenId");
        Get("Solqaryn.Domain.Entities.TransferenciaInventario").HasIndex(new[] { "EmpresaId", "AlmacenDestinoId" })
            .HasDatabaseName("IX_TransferenciasInventario_EmpresaId_AlmacenDestinoId");
        Get("Solqaryn.Domain.Entities.TransferenciaInventario").HasIndex(new[] { "EmpresaId", "AlmacenOrigenId" })
            .HasDatabaseName("IX_TransferenciasInventario_EmpresaId_AlmacenOrigenId");
        Get("Solqaryn.Domain.Entities.UbicacionAlmacen").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionPadreId" })
            .HasDatabaseName("IX_UbicacionesAlmacen_EmpresaId_AlmacenId_UbicacionPadreId");
        Get("Solqaryn.Domain.Entities.VentaDescuento").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_VentaDescuentos_EmpresaId_DescuentoId");
        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_VentaDetalles_EmpresaId_AlmacenId");
        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_VentaDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.VentaImpuesto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_VentaImpuestos_EmpresaId_ImpuestoId");
    }
}
