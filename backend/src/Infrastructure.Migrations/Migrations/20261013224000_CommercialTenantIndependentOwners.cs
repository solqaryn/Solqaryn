using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013224000_CommercialTenantIndependentOwners")]
public partial class CommercialTenantIndependentOwners : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Fase expand: 41 nuevos dueños, sin DML/backfill sobre los registros de prueba.
        m.AddColumn<int>("EmpresaId", "AjustesInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Almacenes", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "AsientoDetalles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "AsientosContables", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CajaMovimientos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Cajas", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CajaSesiones", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CargaMasivaErrores", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CargasMasivas", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Categorias", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CentrosCosto", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Colores", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConciliacionesBancarias", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConfiguracionesContables", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConsumosInsumos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ConteosInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CostosEnvio", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CuentasBancarias", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "CuentasContables", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DashboardKpiConfiguraciones", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DescuentoCategorias", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "DescuentoRoles", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Descuentos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "EmpresaConfiguraciones", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "EnlacesPublicosFactura", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "HistorialAplicacionImpuestos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "HistorialEnviosFactura", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ImpuestoCategorias", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "ImpuestoOperaciones", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Impuestos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Marcas", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Modelos", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "MovimientosEstadoCuenta", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "PeriodosContables", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "PoliticasCosteoInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "RevisionesFinancieras", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "Tallas", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TemaVisual", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TipoClientes", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "TransferenciasInventario", type: "int", nullable: true);
        m.AddColumn<int>("EmpresaId", "UbicacionesAlmacen", type: "int", nullable: true);
        m.CreateIndex("UX_AjustesInventario_Empresa_Id", "AjustesInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_AjustesInventario_NumeroAjuste_ForeignKeySupport", "AjustesInventario", new[] { "NumeroAjuste" });
        m.DropIndex("UX_AjustesInventario_Numero", "AjustesInventario");
        m.CreateIndex("UX_AjustesInventario_Empresa_NumeroAjuste", "AjustesInventario",
            new[] { "EmpresaId", "NumeroAjuste" }, unique: true);
        m.CreateIndex("UX_Almacenes_Empresa_Id", "Almacenes",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Almacenes_CodigoActivoUnico_ForeignKeySupport", "Almacenes", new[] { "CodigoActivoUnico" });
        m.DropIndex("UX_Almacenes_Codigo_Activo", "Almacenes");
        m.CreateIndex("UX_Almacenes_Empresa_CodigoActivoUnico", "Almacenes",
            new[] { "EmpresaId", "CodigoActivoUnico" }, unique: true);
        m.CreateIndex("UX_AsientoDetalles_Empresa_Id", "AsientoDetalles",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_AsientosContables_Empresa_Id", "AsientosContables",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_AsientosContables_Numero_ForeignKeySupport", "AsientosContables", new[] { "Numero" });
        m.DropIndex("UX_AsientosContables_Numero", "AsientosContables");
        m.CreateIndex("UX_AsientosContables_Empresa_Numero", "AsientosContables",
            new[] { "EmpresaId", "Numero" }, unique: true);
        m.CreateIndex("UX_CajaMovimientos_Empresa_Id", "CajaMovimientos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_Cajas_Empresa_Id", "Cajas",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Cajas_SesionActivaId_ForeignKeySupport", "Cajas", new[] { "SesionActivaId" });
        m.DropIndex("UX_Cajas_SesionActivaId", "Cajas");
        m.CreateIndex("UX_Cajas_Empresa_SesionActivaId", "Cajas",
            new[] { "EmpresaId", "SesionActivaId" }, unique: true);
        m.CreateIndex("UX_CajaSesiones_Empresa_Id", "CajaSesiones",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_CargaMasivaErrores_Empresa_Id", "CargaMasivaErrores",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_CargasMasivas_Empresa_Id", "CargasMasivas",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_CargasMasivas_Tipo_HashArchivo_ForeignKeySupport", "CargasMasivas", new[] { "Tipo", "HashArchivo" });
        m.DropIndex("IX_CargasMasivas_Tipo_HashArchivo", "CargasMasivas");
        m.CreateIndex("UX_CargasMasivas_Empresa_Tipo_HashArchivo", "CargasMasivas",
            new[] { "EmpresaId", "Tipo", "HashArchivo" }, unique: true);
        m.CreateIndex("UX_Categorias_Empresa_Id", "Categorias",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_CentrosCosto_Empresa_Id", "CentrosCosto",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_CentrosCosto_CodigoActivoUnico_ForeignKeySupport", "CentrosCosto", new[] { "CodigoActivoUnico" });
        m.DropIndex("UX_CentrosCosto_Codigo_Activo", "CentrosCosto");
        m.CreateIndex("UX_CentrosCosto_Empresa_CodigoActivoUnico", "CentrosCosto",
            new[] { "EmpresaId", "CodigoActivoUnico" }, unique: true);
        m.CreateIndex("UX_Colores_Empresa_Id", "Colores",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Colores_NombreActivoUnico_ForeignKeySupport", "Colores", new[] { "NombreActivoUnico" });
        m.DropIndex("UX_Colores_Nombre_Activo", "Colores");
        m.CreateIndex("UX_Colores_Empresa_NombreActivoUnico", "Colores",
            new[] { "EmpresaId", "NombreActivoUnico" }, unique: true);
        m.CreateIndex("UX_ConciliacionesBancarias_Empresa_Id", "ConciliacionesBancarias",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_ConfiguracionesContables_Empresa_Id", "ConfiguracionesContables",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_ConfiguracionesContables_Evento_ForeignKeySupport", "ConfiguracionesContables", new[] { "Evento" });
        m.DropIndex("UX_ConfiguracionesContables_Evento", "ConfiguracionesContables");
        m.CreateIndex("UX_ConfiguracionesContables_Empresa_Evento", "ConfiguracionesContables",
            new[] { "EmpresaId", "Evento" }, unique: true);
        m.CreateIndex("UX_ConsumosInsumos_Empresa_Id", "ConsumosInsumos",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_ConsumosInsumos_NumeroConsumo_ForeignKeySupport", "ConsumosInsumos", new[] { "NumeroConsumo" });
        m.DropIndex("IX_ConsumosInsumos_NumeroConsumo", "ConsumosInsumos");
        m.CreateIndex("UX_ConsumosInsumos_Empresa_NumeroConsumo", "ConsumosInsumos",
            new[] { "EmpresaId", "NumeroConsumo" }, unique: true);
        m.CreateIndex("UX_ConteosInventario_Empresa_Id", "ConteosInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_ConteosInventario_Numero_ForeignKeySupport", "ConteosInventario", new[] { "Numero" });
        m.DropIndex("UX_ConteosInventario_Numero", "ConteosInventario");
        m.CreateIndex("UX_ConteosInventario_Empresa_Numero", "ConteosInventario",
            new[] { "EmpresaId", "Numero" }, unique: true);
        m.CreateIndex("UX_CostosEnvio_Empresa_Id", "CostosEnvio",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_CostosEnvio_PredeterminadoActivoUnico_ForeignKeySupport", "CostosEnvio", new[] { "PredeterminadoActivoUnico" });
        m.DropIndex("UX_CostosEnvio_PredeterminadoActivo", "CostosEnvio");
        m.CreateIndex("UX_CostosEnvio_Empresa_PredeterminadoActivoUnico", "CostosEnvio",
            new[] { "EmpresaId", "PredeterminadoActivoUnico" }, unique: true);
        m.CreateIndex("UX_CuentasBancarias_Empresa_Id", "CuentasBancarias",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_CuentasBancarias_BancoId_NumeroCuenta_ForeignKeySupport", "CuentasBancarias", new[] { "BancoId", "NumeroCuenta" });
        m.DropIndex("UX_CuentasBancarias_BancoId_NumeroCuenta", "CuentasBancarias");
        m.CreateIndex("UX_CuentasBancarias_Empresa_BancoId_NumeroCuenta", "CuentasBancarias",
            new[] { "EmpresaId", "BancoId", "NumeroCuenta" }, unique: true);
        m.CreateIndex("UX_CuentasContables_Empresa_Id", "CuentasContables",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_CuentasContables_Codigo_ForeignKeySupport", "CuentasContables", new[] { "Codigo" });
        m.DropIndex("UX_CuentasContables_Codigo", "CuentasContables");
        m.CreateIndex("UX_CuentasContables_Empresa_Codigo", "CuentasContables",
            new[] { "EmpresaId", "Codigo" }, unique: true);
        m.CreateIndex("UX_DashboardKpiConfiguraciones_Empresa_Id", "DashboardKpiConfiguraciones",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_DashboardKpiConfiguraciones_RolId_MetricKey_ForeignKeySupport", "DashboardKpiConfiguraciones", new[] { "RolId", "MetricKey" });
        m.DropIndex("UX_DashboardKpiConfiguraciones_Rol_MetricKey", "DashboardKpiConfiguraciones");
        m.CreateIndex("UX_DashboardKpiConfiguraciones_Empresa_RolId_MetricKey", "DashboardKpiConfiguraciones",
            new[] { "EmpresaId", "RolId", "MetricKey" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_DashboardKpiConfiguraciones_UsuarioId_MetricKey_Fore_52c90cbb", "DashboardKpiConfiguraciones", new[] { "UsuarioId", "MetricKey" });
        m.DropIndex("UX_DashboardKpiConfiguraciones_Usuario_MetricKey", "DashboardKpiConfiguraciones");
        m.CreateIndex("UX_DashboardKpiConfiguraciones_Empresa_UsuarioId_MetricKey", "DashboardKpiConfiguraciones",
            new[] { "EmpresaId", "UsuarioId", "MetricKey" }, unique: true);
        m.CreateIndex("UX_DescuentoCategorias_Empresa_Id", "DescuentoCategorias",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_DescuentoCategorias_DescuentoId_CategoriaId_ForeignKeySupport", "DescuentoCategorias", new[] { "DescuentoId", "CategoriaId" });
        m.DropIndex("UX_DescuentoCategorias_Descuento_Categoria", "DescuentoCategorias");
        m.CreateIndex("UX_DescuentoCategorias_Empresa_DescuentoId_CategoriaId", "DescuentoCategorias",
            new[] { "EmpresaId", "DescuentoId", "CategoriaId" }, unique: true);
        m.CreateIndex("UX_DescuentoRoles_Empresa_Id", "DescuentoRoles",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_DescuentoRoles_DescuentoId_RolId_ForeignKeySupport", "DescuentoRoles", new[] { "DescuentoId", "RolId" });
        m.DropIndex("UX_DescuentoRoles_Descuento_Rol", "DescuentoRoles");
        m.CreateIndex("UX_DescuentoRoles_Empresa_DescuentoId_RolId", "DescuentoRoles",
            new[] { "EmpresaId", "DescuentoId", "RolId" }, unique: true);
        m.CreateIndex("UX_Descuentos_Empresa_Id", "Descuentos",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Descuentos_CodigoPromocionalNormalizado_ForeignKeySupport", "Descuentos", new[] { "CodigoPromocionalNormalizado" });
        m.DropIndex("IX_Descuentos_CodigoPromocionalNormalizado", "Descuentos");
        m.CreateIndex("UX_Descuentos_Empresa_CodigoPromocionalNormalizado", "Descuentos",
            new[] { "EmpresaId", "CodigoPromocionalNormalizado" }, unique: true);
        m.CreateIndex("UX_EmpresaConfiguraciones_Empresa_Id", "EmpresaConfiguraciones",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_EmpresaConfiguraciones_ActivaUnica_ForeignKeySupport", "EmpresaConfiguraciones", new[] { "ActivaUnica" });
        m.DropIndex("UX_EmpresaConfiguraciones_Activa", "EmpresaConfiguraciones");
        m.CreateIndex("UX_EmpresaConfiguraciones_Empresa_ActivaUnica", "EmpresaConfiguraciones",
            new[] { "EmpresaId", "ActivaUnica" }, unique: true);
        m.CreateIndex("UX_EnlacesPublicosFactura_Empresa_Id", "EnlacesPublicosFactura",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_EnlacesPublicosFactura_Empresa_Token", "EnlacesPublicosFactura",
            new[] { "EmpresaId", "Token" }, unique: true);
        m.CreateIndex("UX_HistorialAplicacionImpuestos_Empresa_Id", "HistorialAplicacionImpuestos",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_HistorialEnviosFactura_Empresa_Id", "HistorialEnviosFactura",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_ImpuestoCategorias_Empresa_Id", "ImpuestoCategorias",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_ImpuestoCategorias_ImpuestoId_CategoriaId_ForeignKeySupport", "ImpuestoCategorias", new[] { "ImpuestoId", "CategoriaId" });
        m.DropIndex("UX_ImpuestoCategorias_Impuesto_Categoria", "ImpuestoCategorias");
        m.CreateIndex("UX_ImpuestoCategorias_Empresa_ImpuestoId_CategoriaId", "ImpuestoCategorias",
            new[] { "EmpresaId", "ImpuestoId", "CategoriaId" }, unique: true);
        m.CreateIndex("UX_ImpuestoOperaciones_Empresa_Id", "ImpuestoOperaciones",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_ImpuestoOperaciones_ImpuestoId_Operacion_ForeignKeySupport", "ImpuestoOperaciones", new[] { "ImpuestoId", "Operacion" });
        m.DropIndex("UX_ImpuestoOperaciones_Impuesto_Operacion", "ImpuestoOperaciones");
        m.CreateIndex("UX_ImpuestoOperaciones_Empresa_ImpuestoId_Operacion", "ImpuestoOperaciones",
            new[] { "EmpresaId", "ImpuestoId", "Operacion" }, unique: true);
        m.CreateIndex("UX_Impuestos_Empresa_Id", "Impuestos",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Impuestos_Codigo_ForeignKeySupport", "Impuestos", new[] { "Codigo" });
        m.DropIndex("IX_Impuestos_Codigo", "Impuestos");
        m.CreateIndex("UX_Impuestos_Empresa_Codigo", "Impuestos",
            new[] { "EmpresaId", "Codigo" }, unique: true);
        m.CreateIndex("UX_Marcas_Empresa_Id", "Marcas",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Marcas_NombreActivoUnico_ForeignKeySupport", "Marcas", new[] { "NombreActivoUnico" });
        m.DropIndex("UX_Marcas_Nombre_Activo", "Marcas");
        m.CreateIndex("UX_Marcas_Empresa_NombreActivoUnico", "Marcas",
            new[] { "EmpresaId", "NombreActivoUnico" }, unique: true);
        m.CreateIndex("UX_Modelos_Empresa_Id", "Modelos",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Modelos_NombreMarcaActivoUnico_ForeignKeySupport", "Modelos", new[] { "NombreMarcaActivoUnico" });
        m.DropIndex("UX_Modelos_Marca_Nombre_Activo", "Modelos");
        m.CreateIndex("UX_Modelos_Empresa_NombreMarcaActivoUnico", "Modelos",
            new[] { "EmpresaId", "NombreMarcaActivoUnico" }, unique: true);
        m.CreateIndex("UX_MovimientosEstadoCuenta_Empresa_Id", "MovimientosEstadoCuenta",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_MovimientosEstadoCuenta_ConciliacionBancariaId_Idemp_1aaeebd7", "MovimientosEstadoCuenta", new[] { "ConciliacionBancariaId", "IdempotencyKey" });
        m.DropIndex("UX_MovimientosEstadoCuenta_Conciliacion_IdempotencyKey", "MovimientosEstadoCuenta");
        m.CreateIndex("UX_MovimientosEstadoCuenta_Empresa_ConciliacionBancaria_ef44b7da", "MovimientosEstadoCuenta",
            new[] { "EmpresaId", "ConciliacionBancariaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("UX_PeriodosContables_Empresa_Id", "PeriodosContables",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_PeriodosContables_FechaInicio_FechaFin_ForeignKeySupport", "PeriodosContables", new[] { "FechaInicio", "FechaFin" });
        m.DropIndex("UX_PeriodosContables_Rango", "PeriodosContables");
        m.CreateIndex("UX_PeriodosContables_Empresa_FechaInicio_FechaFin", "PeriodosContables",
            new[] { "EmpresaId", "FechaInicio", "FechaFin" }, unique: true);
        m.CreateIndex("UX_PoliticasCosteoInventario_Empresa_Id", "PoliticasCosteoInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_PoliticasCosteoInventario_EmpresaConfiguracionVigent_62e166e6", "PoliticasCosteoInventario", new[] { "EmpresaConfiguracionVigenteId" });
        m.DropIndex("UX_PoliticasCosteo_Empresa_Vigente", "PoliticasCosteoInventario");
        m.CreateIndex("UX_PoliticasCosteoInventario_Empresa_EmpresaConfiguraci_fb4aa187", "PoliticasCosteoInventario",
            new[] { "EmpresaId", "EmpresaConfiguracionVigenteId" }, unique: true);
        m.CreateIndex("UX_RevisionesFinancieras_Empresa_Id", "RevisionesFinancieras",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_Tallas_Empresa_Id", "Tallas",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_Tallas_NombreActivoUnico_ForeignKeySupport", "Tallas", new[] { "NombreActivoUnico" });
        m.DropIndex("UX_Tallas_Nombre_Activo", "Tallas");
        m.CreateIndex("UX_Tallas_Empresa_NombreActivoUnico", "Tallas",
            new[] { "EmpresaId", "NombreActivoUnico" }, unique: true);
        m.CreateIndex("UX_TemaVisual_Empresa_Id", "TemaVisual",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("UX_TipoClientes_Empresa_Id", "TipoClientes",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_TipoClientes_Codigo_ForeignKeySupport", "TipoClientes", new[] { "Codigo" });
        m.DropIndex("IX_TipoClientes_Codigo", "TipoClientes");
        m.CreateIndex("UX_TipoClientes_Empresa_Codigo", "TipoClientes",
            new[] { "EmpresaId", "Codigo" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_TipoClientes_EsPredeterminadoUnico_ForeignKeySupport", "TipoClientes", new[] { "EsPredeterminadoUnico" });
        m.DropIndex("IX_TipoClientes_EsPredeterminadoUnico", "TipoClientes");
        m.CreateIndex("UX_TipoClientes_Empresa_EsPredeterminadoUnico", "TipoClientes",
            new[] { "EmpresaId", "EsPredeterminadoUnico" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_TipoClientes_NombreNormalizado_ForeignKeySupport", "TipoClientes", new[] { "NombreNormalizado" });
        m.DropIndex("IX_TipoClientes_NombreNormalizado", "TipoClientes");
        m.CreateIndex("UX_TipoClientes_Empresa_NombreNormalizado", "TipoClientes",
            new[] { "EmpresaId", "NombreNormalizado" }, unique: true);
        m.CreateIndex("UX_TransferenciasInventario_Empresa_Id", "TransferenciasInventario",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_TransferenciasInventario_Numero_ForeignKeySupport", "TransferenciasInventario", new[] { "Numero" });
        m.DropIndex("UX_TransferenciasInventario_Numero", "TransferenciasInventario");
        m.CreateIndex("UX_TransferenciasInventario_Empresa_Numero", "TransferenciasInventario",
            new[] { "EmpresaId", "Numero" }, unique: true);
        m.CreateIndex("UX_UbicacionesAlmacen_Empresa_Id", "UbicacionesAlmacen",
            new[] { "EmpresaId", "Id" }, unique: true);
        // El índice soporte antecede al DROP del UNIQUE antiguo (compatibilidad FK MySQL).
        m.CreateIndex("IX_UbicacionesAlmacen_AlmacenId_CodigoActivoUnico_Forei_44d4a87d", "UbicacionesAlmacen", new[] { "AlmacenId", "CodigoActivoUnico" });
        m.DropIndex("UX_UbicacionesAlmacen_Almacen_Codigo_Activo", "UbicacionesAlmacen");
        m.CreateIndex("UX_UbicacionesAlmacen_Empresa_AlmacenId_CodigoActivoUnico", "UbicacionesAlmacen",
            new[] { "EmpresaId", "AlmacenId", "CodigoActivoUnico" }, unique: true);
        m.AddForeignKey("FK_AjustesInventario_Empresas_EmpresaId",
            "AjustesInventario", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Almacenes_Empresas_EmpresaId",
            "Almacenes", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_AsientoDetalles_Empresas_EmpresaId",
            "AsientoDetalles", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_AsientosContables_Empresas_EmpresaId",
            "AsientosContables", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CajaMovimientos_Empresas_EmpresaId",
            "CajaMovimientos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Cajas_Empresas_EmpresaId",
            "Cajas", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CajaSesiones_Empresas_EmpresaId",
            "CajaSesiones", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CargaMasivaErrores_Empresas_EmpresaId",
            "CargaMasivaErrores", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CargasMasivas_Empresas_EmpresaId",
            "CargasMasivas", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Categorias_Empresas_EmpresaId",
            "Categorias", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CentrosCosto_Empresas_EmpresaId",
            "CentrosCosto", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Colores_Empresas_EmpresaId",
            "Colores", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConciliacionesBancarias_Empresas_EmpresaId",
            "ConciliacionesBancarias", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConfiguracionesContables_Empresas_EmpresaId",
            "ConfiguracionesContables", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConsumosInsumos_Empresas_EmpresaId",
            "ConsumosInsumos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ConteosInventario_Empresas_EmpresaId",
            "ConteosInventario", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CostosEnvio_Empresas_EmpresaId",
            "CostosEnvio", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CuentasBancarias_Empresas_EmpresaId",
            "CuentasBancarias", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_CuentasContables_Empresas_EmpresaId",
            "CuentasContables", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DashboardKpiConfiguraciones_Empresas_EmpresaId",
            "DashboardKpiConfiguraciones", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DescuentoCategorias_Empresas_EmpresaId",
            "DescuentoCategorias", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_DescuentoRoles_Empresas_EmpresaId",
            "DescuentoRoles", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Descuentos_Empresas_EmpresaId",
            "Descuentos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_EmpresaConfiguraciones_Empresas_EmpresaId",
            "EmpresaConfiguraciones", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_EnlacesPublicosFactura_Empresas_EmpresaId",
            "EnlacesPublicosFactura", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_HistorialAplicacionImpuestos_Empresas_EmpresaId",
            "HistorialAplicacionImpuestos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_HistorialEnviosFactura_Empresas_EmpresaId",
            "HistorialEnviosFactura", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ImpuestoCategorias_Empresas_EmpresaId",
            "ImpuestoCategorias", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_ImpuestoOperaciones_Empresas_EmpresaId",
            "ImpuestoOperaciones", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Impuestos_Empresas_EmpresaId",
            "Impuestos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Marcas_Empresas_EmpresaId",
            "Marcas", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Modelos_Empresas_EmpresaId",
            "Modelos", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_MovimientosEstadoCuenta_Empresas_EmpresaId",
            "MovimientosEstadoCuenta", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_PeriodosContables_Empresas_EmpresaId",
            "PeriodosContables", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_PoliticasCosteoInventario_Empresas_EmpresaId",
            "PoliticasCosteoInventario", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_RevisionesFinancieras_Empresas_EmpresaId",
            "RevisionesFinancieras", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_Tallas_Empresas_EmpresaId",
            "Tallas", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TemaVisual_Empresas_EmpresaId",
            "TemaVisual", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TipoClientes_Empresas_EmpresaId",
            "TipoClientes", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_TransferenciasInventario_Empresas_EmpresaId",
            "TransferenciasInventario", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddForeignKey("FK_UbicacionesAlmacen_Empresas_EmpresaId",
            "UbicacionesAlmacen", "EmpresaId", "Empresas",
            principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("No rollback destructivo de tenant: restaurar backup DEV verificado.");
}
