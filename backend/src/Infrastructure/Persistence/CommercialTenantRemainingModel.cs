using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Propiedad física tenant en 41 módulos comerciales/operativos y unicidad
/// empresarial en sus identificadores y reglas de negocio. No cambia los
/// registros históricos NULL ni elimina la cuarentena de lectura.
/// </summary>
public static class CommercialTenantRemainingModel
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder Get(string name)
        {
            var type = modelBuilder.Model.FindEntityType(name)
                ?? throw new InvalidOperationException($"Modelo comercial ausente: {name}");
            return modelBuilder.Entity(type.ClrType);
        }

        {
            var b = Get("Solqaryn.Domain.Entities.AjusteInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_AjustesInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "NumeroAjuste" }).IsUnique().HasDatabaseName("UX_AjustesInventario_Empresa_NumeroAjuste");
            b.HasIndex(new[] { "NumeroAjuste" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AjustesInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Almacen");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Almacenes_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "SucursalId" }).HasDatabaseName("IX_Almacenes_EmpresaId_SucursalId");
            b.HasIndex(new[] { "EmpresaId", "CodigoActivoUnico" }).IsUnique().HasDatabaseName("UX_Almacenes_Empresa_CodigoActivoUnico");
            b.HasIndex(new[] { "CodigoActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Almacenes_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_AsientoDetalles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "AsientoContableId" }).HasDatabaseName("IX_AsientoDetalles_EmpresaId_AsientoContableId");
            b.HasIndex(new[] { "EmpresaId", "CuentaContableId" }).HasDatabaseName("IX_AsientoDetalles_EmpresaId_CuentaContableId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsientoDetalles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.AsientoContable");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_AsientosContables_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique().HasDatabaseName("UX_AsientosContables_Empresa_Numero");
            b.HasIndex(new[] { "Numero" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_AsientosContables_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.CajaMovimiento");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CajaMovimientos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CajaSesionId" }).HasDatabaseName("IX_CajaMovimientos_EmpresaId_CajaSesionId");
            b.HasIndex(new[] { "EmpresaId", "UsuarioId" }).HasDatabaseName("IX_CajaMovimientos_EmpresaId_UsuarioId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CajaMovimientos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.Caja");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Cajas_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "SesionActivaId" }).HasDatabaseName("IX_Cajas_EmpresaId_SesionActivaId");
            b.HasIndex(new[] { "EmpresaId", "SesionActivaId" }).IsUnique().HasDatabaseName("UX_Cajas_Empresa_SesionActivaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Cajas_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Cajas.CajaSesion");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CajaSesiones_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CajaId" }).HasDatabaseName("IX_CajaSesiones_EmpresaId_CajaId");
            b.HasIndex(new[] { "EmpresaId", "UsuarioId" }).HasDatabaseName("IX_CajaSesiones_EmpresaId_UsuarioId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CajaSesiones_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CargaMasivaError");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CargaMasivaErrores_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CargaMasivaId" }).HasDatabaseName("IX_CargaMasivaErrores_EmpresaId_CargaMasivaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CargaMasivaErrores_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CargaMasiva");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CargasMasivas_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "Tipo", "HashArchivo" }).IsUnique().HasDatabaseName("UX_CargasMasivas_Empresa_Tipo_HashArchivo");
            b.HasIndex(new[] { "Tipo", "HashArchivo" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CargasMasivas_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Categoria");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Categorias_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Categorias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.CentroCosto");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CentrosCosto_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "SucursalId" }).HasDatabaseName("IX_CentrosCosto_EmpresaId_SucursalId");
            b.HasIndex(new[] { "EmpresaId", "CodigoActivoUnico" }).IsUnique().HasDatabaseName("UX_CentrosCosto_Empresa_CodigoActivoUnico");
            b.HasIndex(new[] { "CodigoActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CentrosCosto_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Color");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Colores_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique().HasDatabaseName("UX_Colores_Empresa_NombreActivoUnico");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Colores_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConciliacionesBancarias_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaBancariaId" }).HasDatabaseName("IX_ConciliacionesBancarias_EmpresaId_CuentaBancariaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConciliacionesBancarias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConfiguracionesContables_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaDebeId" }).HasDatabaseName("IX_ConfiguracionesContables_EmpresaId_CuentaDebeId");
            b.HasIndex(new[] { "EmpresaId", "CuentaHaberId" }).HasDatabaseName("IX_ConfiguracionesContables_EmpresaId_CuentaHaberId");
            b.HasIndex(new[] { "EmpresaId", "Evento" }).IsUnique().HasDatabaseName("UX_ConfiguracionesContables_Empresa_Evento");
            b.HasIndex(new[] { "Evento" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConfiguracionesContables_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConsumoInsumo");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConsumosInsumos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "NumeroConsumo" }).IsUnique().HasDatabaseName("UX_ConsumosInsumos_Empresa_NumeroConsumo");
            b.HasIndex(new[] { "NumeroConsumo" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConsumosInsumos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ConteoInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConteosInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId" }).HasDatabaseName("IX_ConteosInventario_EmpresaId_AlmacenId");
            b.HasIndex(new[] { "EmpresaId", "CategoriaId" }).HasDatabaseName("IX_ConteosInventario_EmpresaId_CategoriaId");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_ConteosInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique().HasDatabaseName("UX_ConteosInventario_Empresa_Numero");
            b.HasIndex(new[] { "Numero" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ConteosInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CostoEnvio");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CostosEnvio_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "PredeterminadoActivoUnico" }).IsUnique().HasDatabaseName("UX_CostosEnvio_Empresa_PredeterminadoActivoUnico");
            b.HasIndex(new[] { "PredeterminadoActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CostosEnvio_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.CuentaBancaria");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CuentasBancarias_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "BancoId" }).HasDatabaseName("IX_CuentasBancarias_EmpresaId_BancoId");
            b.HasIndex(new[] { "EmpresaId", "BancoId", "NumeroCuenta" }).IsUnique().HasDatabaseName("UX_CuentasBancarias_Empresa_BancoId_NumeroCuenta");
            b.HasIndex(new[] { "BancoId", "NumeroCuenta" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CuentasBancarias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.CuentaContable");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CuentasContables_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CuentaPadreId" }).HasDatabaseName("IX_CuentasContables_EmpresaId_CuentaPadreId");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique().HasDatabaseName("UX_CuentasContables_Empresa_Codigo");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CuentasContables_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DashboardKpiConfiguracion");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "RolId" }).HasDatabaseName("IX_DashboardKpiConfiguraciones_EmpresaId_RolId");
            b.HasIndex(new[] { "EmpresaId", "UsuarioId" }).HasDatabaseName("IX_DashboardKpiConfiguraciones_EmpresaId_UsuarioId");
            b.HasIndex(new[] { "EmpresaId", "RolId", "MetricKey" }).IsUnique().HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_RolId_MetricKey");
            b.HasIndex(new[] { "RolId", "MetricKey" }).IsUnique(false);
            b.HasIndex(new[] { "EmpresaId", "UsuarioId", "MetricKey" }).IsUnique().HasDatabaseName("UX_DashboardKpiConfiguraciones_Empresa_UsuarioId_MetricKey");
            b.HasIndex(new[] { "UsuarioId", "MetricKey" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DashboardKpiConfiguraciones_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoCategoria");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_DescuentoCategorias_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CategoriaId" }).HasDatabaseName("IX_DescuentoCategorias_EmpresaId_CategoriaId");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId" }).HasDatabaseName("IX_DescuentoCategorias_EmpresaId_DescuentoId");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId", "CategoriaId" }).IsUnique().HasDatabaseName("UX_DescuentoCategorias_Empresa_DescuentoId_CategoriaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoCategorias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.DescuentoRol");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_DescuentoRoles_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId" }).HasDatabaseName("IX_DescuentoRoles_EmpresaId_DescuentoId");
            b.HasIndex(new[] { "EmpresaId", "RolId" }).HasDatabaseName("IX_DescuentoRoles_EmpresaId_RolId");
            b.HasIndex(new[] { "EmpresaId", "DescuentoId", "RolId" }).IsUnique().HasDatabaseName("UX_DescuentoRoles_Empresa_DescuentoId_RolId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DescuentoRoles_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Descuento");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Descuentos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CodigoPromocionalNormalizado" }).IsUnique().HasDatabaseName("UX_Descuentos_Empresa_CodigoPromocionalNormalizado").HasFilter("`CodigoPromocionalNormalizado` IS NOT NULL");
            b.HasIndex(new[] { "CodigoPromocionalNormalizado" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Descuentos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.EmpresaConfiguracion");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_EmpresaConfiguraciones_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ActivaUnica" }).IsUnique().HasDatabaseName("UX_EmpresaConfiguraciones_Empresa_ActivaUnica");
            b.HasIndex(new[] { "ActivaUnica" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EmpresaConfiguraciones_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.EnlacePublicoFactura");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_EnlacesPublicosFactura_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "Token" }).IsUnique().HasDatabaseName("UX_EnlacesPublicosFactura_Empresa_Token");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_EnlacesPublicosFactura_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.HistorialAplicacionImpuesto");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_HistorialAplicacionImpuestos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId" }).HasDatabaseName("IX_HistorialAplicacionImpuestos_EmpresaId_ImpuestoId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialAplicacionImpuestos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.HistorialEnvioFactura");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_HistorialEnviosFactura_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HistorialEnviosFactura_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoCategoria");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ImpuestoCategorias_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "CategoriaId" }).HasDatabaseName("IX_ImpuestoCategorias_EmpresaId_CategoriaId");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId" }).HasDatabaseName("IX_ImpuestoCategorias_EmpresaId_ImpuestoId");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId", "CategoriaId" }).IsUnique().HasDatabaseName("UX_ImpuestoCategorias_Empresa_ImpuestoId_CategoriaId");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoCategorias_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.ImpuestoOperacion");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ImpuestoOperaciones_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId" }).HasDatabaseName("IX_ImpuestoOperaciones_EmpresaId_ImpuestoId");
            b.HasIndex(new[] { "EmpresaId", "ImpuestoId", "Operacion" }).IsUnique().HasDatabaseName("UX_ImpuestoOperaciones_Empresa_ImpuestoId_Operacion");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ImpuestoOperaciones_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Impuesto");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Impuestos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique().HasDatabaseName("UX_Impuestos_Empresa_Codigo");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Impuestos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Marca");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Marcas_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique().HasDatabaseName("UX_Marcas_Empresa_NombreActivoUnico");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Marcas_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Modelo");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Modelos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "MarcaId" }).HasDatabaseName("IX_Modelos_EmpresaId_MarcaId");
            b.HasIndex(new[] { "EmpresaId", "NombreMarcaActivoUnico" }).IsUnique().HasDatabaseName("UX_Modelos_Empresa_NombreMarcaActivoUnico");
            b.HasIndex(new[] { "NombreMarcaActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Modelos_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_MovimientosEstadoCuenta_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "ConciliacionBancariaId" }).HasDatabaseName("IX_MovimientosEstadoCuenta_EmpresaId_ConciliacionBancariaId");
            b.HasIndex(new[] { "EmpresaId", "ConciliacionBancariaId", "IdempotencyKey" }).IsUnique().HasDatabaseName("UX_MovimientosEstadoCuenta_Empresa_ConciliacionBancaria_ef44b7da");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_MovimientosEstadoCuenta_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Contabilidad.PeriodoContable");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_PeriodosContables_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "FechaInicio", "FechaFin" }).IsUnique().HasDatabaseName("UX_PeriodosContables_Empresa_FechaInicio_FechaFin");
            b.HasIndex(new[] { "FechaInicio", "FechaFin" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PeriodosContables_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.PoliticaCosteoInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_PoliticasCosteoInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "EmpresaConfiguracionId" }).HasDatabaseName("IX_PoliticasCosteoInventario_EmpresaId_EmpresaConfiguracionId");
            b.HasIndex(new[] { "EmpresaId", "EmpresaConfiguracionVigenteId" }).IsUnique().HasDatabaseName("UX_PoliticasCosteoInventario_Empresa_EmpresaConfiguraci_fb4aa187");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PoliticasCosteoInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.RevisionFinanciera");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_RevisionesFinancieras_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RevisionesFinancieras_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.Talla");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Tallas_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "NombreActivoUnico" }).IsUnique().HasDatabaseName("UX_Tallas_Empresa_NombreActivoUnico");
            b.HasIndex(new[] { "NombreActivoUnico" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Tallas_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TemaVisual");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_TemaVisual_Empresa_Id");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TemaVisual_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TipoCliente");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_TipoClientes_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "Codigo" }).IsUnique().HasDatabaseName("UX_TipoClientes_Empresa_Codigo");
            b.HasIndex(new[] { "Codigo" }).IsUnique(false);
            b.HasIndex(new[] { "EmpresaId", "EsPredeterminadoUnico" }).IsUnique().HasDatabaseName("UX_TipoClientes_Empresa_EsPredeterminadoUnico");
            b.HasIndex(new[] { "EsPredeterminadoUnico" }).IsUnique(false);
            b.HasIndex(new[] { "EmpresaId", "NombreNormalizado" }).IsUnique().HasDatabaseName("UX_TipoClientes_Empresa_NombreNormalizado");
            b.HasIndex(new[] { "NombreNormalizado" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TipoClientes_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.TransferenciaInventario");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_TransferenciasInventario_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "AlmacenDestinoId" }).HasDatabaseName("IX_TransferenciasInventario_EmpresaId_AlmacenDestinoId");
            b.HasIndex(new[] { "EmpresaId", "AlmacenOrigenId" }).HasDatabaseName("IX_TransferenciasInventario_EmpresaId_AlmacenOrigenId");
            b.HasIndex(new[] { "EmpresaId", "Numero" }).IsUnique().HasDatabaseName("UX_TransferenciasInventario_Empresa_Numero");
            b.HasIndex(new[] { "Numero" }).IsUnique(false);
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_TransferenciasInventario_Empresas_EmpresaId");
        }

        {
            var b = Get("Solqaryn.Domain.Entities.UbicacionAlmacen");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_UbicacionesAlmacen_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId" }).HasDatabaseName("IX_UbicacionesAlmacen_EmpresaId_AlmacenId");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionPadreId" }).HasDatabaseName("IX_UbicacionesAlmacen_EmpresaId_AlmacenId_UbicacionPadreId");
            b.HasIndex(new[] { "EmpresaId", "AlmacenId", "CodigoActivoUnico" }).IsUnique().HasDatabaseName("UX_UbicacionesAlmacen_Empresa_AlmacenId_CodigoActivoUnico");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_UbicacionesAlmacen_Empresas_EmpresaId");
        }

        Get("Solqaryn.Domain.Entities.ConfigEmpresa").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConfigEmpresas_Empresa_Id");

        Get("Solqaryn.Domain.Entities.ConfiguracionWhatsAppEmpresa").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_ConfiguracionesWhatsAppEmpresa_Empresa_Id");

        Get("Solqaryn.Domain.Entities.CotizacionDetalle").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_CotizacionDetalles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.EmailEmpresarial").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_EmailsEmpresariales_Empresa_Id");

        Get("Solqaryn.Domain.Entities.MensajeOutbox").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_MensajesOutbox_Empresa_Id");

        Get("Solqaryn.Domain.Entities.PedidoVentaDetalle").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_PedidoVentaDetalles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.PlantillaCorreoEmpresa").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_PlantillasCorreoEmpresa_Empresa_Id");

        Get("Solqaryn.Domain.Entities.PlantillaEmailEmpresarial").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_PlantillasEmailEmpresarial_Empresa_Id");

        Get("Solqaryn.Domain.Entities.RegistroAuditoria").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_RegistrosAuditoria_Empresa_Id");

        Get("Solqaryn.Domain.Entities.Rol").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Roles_Empresa_Id");

        Get("Solqaryn.Domain.Entities.SecuenciaDocumento").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_SecuenciasDocumento_Empresa_Id");

        Get("Solqaryn.Domain.Entities.Sucursal").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Sucursales_Empresa_Id");

        Get("Solqaryn.Domain.Entities.Suscripcion").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_Suscripciones_Empresa_Id");

        Get("Solqaryn.Domain.Entities.UsuarioEmpresa").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_UsuarioEmpresas_Empresa_Id");

        Get("Solqaryn.Domain.Entities.WebhookEntrante").HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_WebhooksEntrantes_Empresa_Id");

        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AjusteInventarioId");

        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.CapaCostoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.CapaCostoInventario").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_CapasCostoInventario_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.Cliente").HasIndex(new[] { "EmpresaId", "TipoClienteId" })
            .HasDatabaseName("IX_Clientes_EmpresaId_TipoClienteId");

        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_CompraDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_CompraDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.CompraImpuesto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_CompraImpuestos_EmpresaId_ImpuestoId");

        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "ConsumoInsumoId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_ConsumoInsumoId");

        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AjusteInventarioId");

        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "ConteoInventarioId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_ConteoInventarioId");

        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_UbicacionAlmacenId");

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

        Get("Solqaryn.Domain.Entities.ExistenciaVariante").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ExistenciasVariante_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.HistorialUsoDescuento").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_HistorialUsoDescuentos_EmpresaId_DescuentoId");

        Get("Solqaryn.Domain.Entities.ImpuestoCliente").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoClientes_EmpresaId_ImpuestoId");

        Get("Solqaryn.Domain.Entities.ImpuestoProducto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoProductos_EmpresaId_ImpuestoId");

        Get("Solqaryn.Domain.Entities.ImpuestoProveedor").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_ImpuestoProveedores_EmpresaId_ImpuestoId");

        Get("Solqaryn.Domain.Entities.Bancos.MatchConciliacion").HasIndex(new[] { "EmpresaId", "MovimientoEstadoCuentaId" })
            .HasDatabaseName("IX_MatchesConciliacion_EmpresaId_MovimientoEstadoCuentaId");

        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AjusteInventarioId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_AjusteInventarioId");

        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "ConsumoInsumoId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_ConsumoInsumoId");

        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "TransferenciaInventarioId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_TransferenciaInventarioId");

        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_MovimientosInventario_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_UbicacionAlmacenId");

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

        Get("Solqaryn.Domain.Entities.RecepcionCompraDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "TransferenciaInventarioId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_Transferen_8b22a8d8");

        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionDestinoId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_UbicacionDestinoId");

        Get("Solqaryn.Domain.Entities.TransferenciaInventarioDetalle").HasIndex(new[] { "EmpresaId", "UbicacionOrigenId" })
            .HasDatabaseName("IX_TransferenciaInventarioDetalles_EmpresaId_UbicacionOrigenId");

        Get("Solqaryn.Domain.Entities.VentaDescuento").HasIndex(new[] { "EmpresaId", "DescuentoId" })
            .HasDatabaseName("IX_VentaDescuentos_EmpresaId_DescuentoId");

        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId" })
            .HasDatabaseName("IX_VentaDetalles_EmpresaId_AlmacenId");

        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex(new[] { "EmpresaId", "UbicacionAlmacenId" })
            .HasDatabaseName("IX_VentaDetalles_EmpresaId_UbicacionAlmacenId");

        Get("Solqaryn.Domain.Entities.VentaImpuesto").HasIndex(new[] { "EmpresaId", "ImpuestoId" })
            .HasDatabaseName("IX_VentaImpuestos_EmpresaId_ImpuestoId");
        {
            var b = Get("Solqaryn.Domain.Entities.RolPermiso");
            b.Property<int?>("EmpresaId");
            b.HasIndex("EmpresaId", "Id").IsUnique().HasDatabaseName("UX_RolPermisos_Empresa_Id");
            b.HasIndex(new[] { "EmpresaId", "RolId" }).HasDatabaseName("IX_RolPermisos_Empresa_RolId");
            b.HasIndex(new[] { "EmpresaId", "RolId", "PermisoId" }).IsUnique()
                .HasDatabaseName("UX_RolPermisos_Empresa_Rol_Permiso");
            b.HasOne(typeof(Empresa), navigationName: null).WithMany()
                .HasForeignKey("EmpresaId").OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_RolPermisos_Empresas_EmpresaId");
        }
        Get("Solqaryn.Domain.Entities.AjusteInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_AjusteInventarioDetalles_EmpresaId_AlmacenId_Ubicaci_7edecb64");
        Get("Solqaryn.Domain.Entities.CapaCostoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_CapasCostoInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.CompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_CompraDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.ConsumoInsumoDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_ConsumoInsumoDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_AlmacenId_Ubicaci_8bcaedc4");
        Get("Solqaryn.Domain.Entities.ConteoInventarioDetalle").HasIndex(new[] { "EmpresaId", "ConteoInventarioId", "AlmacenId" }).HasDatabaseName("IX_ConteoInventarioDetalles_EmpresaId_ConteoInventarioI_d5c0a907");
        Get("Solqaryn.Domain.Entities.DocumentoFiscalEmision").HasIndex(new[] { "EmpresaId", "FacturaId" }).HasDatabaseName("IX_DocumentosFiscalesEmision_EmpresaId_FacturaId");
        Get("Solqaryn.Domain.Entities.DocumentoFiscalEmision").HasIndex(new[] { "EmpresaId", "SucursalId" }).HasDatabaseName("IX_DocumentosFiscalesEmision_EmpresaId_SucursalId");
        Get("Solqaryn.Domain.Entities.ExistenciaVariante").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_ExistenciasVariante_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.MovimientoInventario").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_MovimientosInventario_EmpresaId_AlmacenId_UbicacionAlmacenId");
        Get("Solqaryn.Domain.Entities.PagoOnline").HasIndex(new[] { "EmpresaId", "FacturaId" }).HasDatabaseName("IX_PagosOnline_EmpresaId_FacturaId");
        Get("Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_PreparacionPedidoVentaDetalles_EmpresaId_AlmacenId_U_ab46262c");
        Get("Solqaryn.Domain.Entities.RecepcionCompraDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_RecepcionCompraDetalles_EmpresaId_AlmacenId_Ubicacio_8a10442f");
        Get("Solqaryn.Domain.Entities.ReservaInventarioDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_ReservaInventarioDetalles_EmpresaId_AlmacenId_Ubicac_b786f94a");
        Get("Solqaryn.Domain.Entities.SecuenciaDocumento").HasIndex(new[] { "EmpresaId", "SucursalId" }).HasDatabaseName("IX_SecuenciasDocumento_EmpresaId_SucursalId");
        Get("Solqaryn.Domain.Entities.UsuarioEmpresa").HasIndex(new[] { "EmpresaId", "RolId" }).HasDatabaseName("IX_UsuarioEmpresas_EmpresaId_RolId");
        Get("Solqaryn.Domain.Entities.VentaDetalle").HasIndex(new[] { "EmpresaId", "AlmacenId", "UbicacionAlmacenId" }).HasDatabaseName("IX_VentaDetalles_EmpresaId_AlmacenId_UbicacionAlmacenId");
    }
}
