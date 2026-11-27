using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>QA V3 ownership. Unassigned historical rows remain quarantined.</summary>
public static class QaCommercialOwnershipV3
{
    public const string MigrationId = "20261126223000_QaCommercialOwnershipV3";
    public const string GalleryColumn = "CategoriaVerificadaId";
    public sealed record ScopedKey(string Table, string Index, string[] Columns);
    public sealed record Trigger(string Name, string Table, string Action, string Body)
    {
        public string Ddl => $"CREATE TRIGGER `{Name}` BEFORE {Action} ON `{Table}` FOR EACH ROW BEGIN {Body} END";
    }
    public static IReadOnlyList<QaCommercialOwnershipV1.Owned> Tables { get; } = new QaCommercialOwnershipV1.Owned[]
    {
        new(typeof(global::Solqaryn.Domain.Entities.Categoria), "Categorias"),
        new(typeof(global::Solqaryn.Domain.Entities.Marca), "Marcas"),
        new(typeof(global::Solqaryn.Domain.Entities.Modelo), "Modelos"),
        new(typeof(global::Solqaryn.Domain.Entities.Color), "Colores"),
        new(typeof(global::Solqaryn.Domain.Entities.Talla), "Tallas"),
        new(typeof(global::Solqaryn.Domain.Entities.TipoCliente), "TipoClientes"),
        new(typeof(global::Solqaryn.Domain.Entities.Catalogos.MetodoPago), "MetodosPago"),
        new(typeof(global::Solqaryn.Domain.Entities.Descuento), "Descuentos"),
        new(typeof(global::Solqaryn.Domain.Entities.DescuentoCategoria), "DescuentoCategorias"),
        new(typeof(global::Solqaryn.Domain.Entities.DescuentoCliente), "DescuentoClientes"),
        new(typeof(global::Solqaryn.Domain.Entities.DescuentoProducto), "DescuentoProductos"),
        new(typeof(global::Solqaryn.Domain.Entities.DescuentoRol), "DescuentoRoles"),
        new(typeof(global::Solqaryn.Domain.Entities.HistorialUsoDescuento), "HistorialUsoDescuentos"),
        new(typeof(global::Solqaryn.Domain.Entities.VentaDescuento), "VentaDescuentos"),
        new(typeof(global::Solqaryn.Domain.Entities.Impuesto), "Impuestos"),
        new(typeof(global::Solqaryn.Domain.Entities.ImpuestoCategoria), "ImpuestoCategorias"),
        new(typeof(global::Solqaryn.Domain.Entities.ImpuestoCliente), "ImpuestoClientes"),
        new(typeof(global::Solqaryn.Domain.Entities.ImpuestoOperacion), "ImpuestoOperaciones"),
        new(typeof(global::Solqaryn.Domain.Entities.ImpuestoProducto), "ImpuestoProductos"),
        new(typeof(global::Solqaryn.Domain.Entities.ImpuestoProveedor), "ImpuestoProveedores"),
        new(typeof(global::Solqaryn.Domain.Entities.HistorialAplicacionImpuesto), "HistorialAplicacionImpuestos"),
        new(typeof(global::Solqaryn.Domain.Entities.VentaImpuesto), "VentaImpuestos"),
        new(typeof(global::Solqaryn.Domain.Entities.CompraImpuesto), "CompraImpuestos"),
        new(typeof(global::Solqaryn.Domain.Entities.ConteoInventario), "ConteosInventario"),
        new(typeof(global::Solqaryn.Domain.Entities.ConteoInventarioDetalle), "ConteoInventarioDetalles"),
        new(typeof(global::Solqaryn.Domain.Entities.Bancos.MatchConciliacion), "MatchesConciliacion"),
        new(typeof(global::Solqaryn.Domain.Entities.Bancos.CuentaBancaria), "CuentasBancarias"),
        new(typeof(global::Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria), "ConciliacionesBancarias"),
        new(typeof(global::Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta), "MovimientosEstadoCuenta"),
    };
    public static IReadOnlyList<QaCommercialOwnershipV1.Edge> Relations { get; } = new QaCommercialOwnershipV1.Edge[]
    {
        new("Productos", new[] { "EmpresaId", "CategoriaId" }, "Categorias", new[] { "EmpresaId", "Id" }, "IX_QAC3_01", "FK_QAC3_01", false),
        new("Productos", new[] { "EmpresaId", "MarcaId" }, "Marcas", new[] { "EmpresaId", "Id" }, "IX_QAC3_02", "FK_QAC3_02", false),
        new("Productos", new[] { "EmpresaId", "ModeloId" }, "Modelos", new[] { "EmpresaId", "Id" }, "IX_QAC3_03", "FK_QAC3_03", false),
        new("Productos", new[] { "EmpresaId", "ColorId" }, "Colores", new[] { "EmpresaId", "Id" }, "IX_QAC3_04", "FK_QAC3_04", false),
        new("Productos", new[] { "EmpresaId", "TallaId" }, "Tallas", new[] { "EmpresaId", "Id" }, "IX_QAC3_05", "FK_QAC3_05", false),
        new("ProductoVariantes", new[] { "EmpresaId", "MarcaId" }, "Marcas", new[] { "EmpresaId", "Id" }, "IX_QAC3_06", "FK_QAC3_06", false),
        new("ProductoVariantes", new[] { "EmpresaId", "ModeloId" }, "Modelos", new[] { "EmpresaId", "Id" }, "IX_QAC3_07", "FK_QAC3_07", false),
        new("ProductoVariantes", new[] { "EmpresaId", "ColorId" }, "Colores", new[] { "EmpresaId", "Id" }, "IX_QAC3_08", "FK_QAC3_08", false),
        new("ProductoVariantes", new[] { "EmpresaId", "TallaId" }, "Tallas", new[] { "EmpresaId", "Id" }, "IX_QAC3_09", "FK_QAC3_09", false),
        new("ProductoVariantes", new[] { "EmpresaId", "MarcaId", "ModeloId" }, "Modelos", new[] { "EmpresaId", "MarcaId", "Id" }, "IX_QAC3_10", "FK_QAC3_10", false),
        new("Modelos", new[] { "EmpresaId", "MarcaId" }, "Marcas", new[] { "EmpresaId", "Id" }, "IX_QAC3_11", "FK_QAC3_11", false),
        new("Clientes", new[] { "EmpresaId", "TipoClienteId" }, "TipoClientes", new[] { "EmpresaId", "Id" }, "IX_QAC3_12", "FK_QAC3_12", false),
        new("Compras", new[] { "EmpresaId", "MetodoPagoId" }, "MetodosPago", new[] { "EmpresaId", "Id" }, "IX_QAC3_13", "FK_QAC3_13", false),
        new("Ventas", new[] { "EmpresaId", "MetodoPagoId" }, "MetodosPago", new[] { "EmpresaId", "Id" }, "IX_QAC3_14", "FK_QAC3_14", false),
        new("FacturaPagos", new[] { "EmpresaId", "MetodoPagoId" }, "MetodosPago", new[] { "EmpresaId", "Id" }, "IX_QAC3_15", "FK_QAC3_15", false),
        new("MovimientosFinancieros", new[] { "EmpresaId", "MetodoPagoId" }, "MetodosPago", new[] { "EmpresaId", "Id" }, "IX_QAC3_16", "FK_QAC3_16", false),
        new("DescuentoCategorias", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_17", "FK_QAC3_17", true),
        new("DescuentoClientes", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_18", "FK_QAC3_18", true),
        new("DescuentoProductos", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_19", "FK_QAC3_19", true),
        new("DescuentoRoles", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_20", "FK_QAC3_20", true),
        new("HistorialUsoDescuentos", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_21", "FK_QAC3_21", false),
        new("VentaDescuentos", new[] { "EmpresaId", "DescuentoId" }, "Descuentos", new[] { "EmpresaId", "Id" }, "IX_QAC3_22", "FK_QAC3_22", false),
        new("DescuentoCategorias", new[] { "EmpresaId", "CategoriaId" }, "Categorias", new[] { "EmpresaId", "Id" }, "IX_QAC3_23", "FK_QAC3_23", false),
        new("DescuentoClientes", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QAC3_24", "FK_QAC3_24", false),
        new("DescuentoProductos", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QAC3_25", "FK_QAC3_25", false),
        new("DescuentoRoles", new[] { "EmpresaId", "RolId" }, "Roles", new[] { "EmpresaId", "Id" }, "IX_QAC3_26", "FK_QAC3_26", false),
        new("HistorialUsoDescuentos", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QAC3_27", "FK_QAC3_27", false),
        new("HistorialUsoDescuentos", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QAC3_28", "FK_QAC3_28", false),
        new("VentaDescuentos", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QAC3_29", "FK_QAC3_29", true),
        new("ImpuestoCategorias", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_30", "FK_QAC3_30", true),
        new("ImpuestoClientes", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_31", "FK_QAC3_31", true),
        new("ImpuestoOperaciones", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_32", "FK_QAC3_32", true),
        new("ImpuestoProductos", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_33", "FK_QAC3_33", true),
        new("ImpuestoProveedores", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_34", "FK_QAC3_34", true),
        new("HistorialAplicacionImpuestos", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_35", "FK_QAC3_35", false),
        new("VentaImpuestos", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_36", "FK_QAC3_36", false),
        new("CompraImpuestos", new[] { "EmpresaId", "ImpuestoId" }, "Impuestos", new[] { "EmpresaId", "Id" }, "IX_QAC3_37", "FK_QAC3_37", false),
        new("ImpuestoCategorias", new[] { "EmpresaId", "CategoriaId" }, "Categorias", new[] { "EmpresaId", "Id" }, "IX_QAC3_38", "FK_QAC3_38", false),
        new("ImpuestoClientes", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QAC3_39", "FK_QAC3_39", false),
        new("ImpuestoProductos", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QAC3_40", "FK_QAC3_40", false),
        new("ImpuestoProveedores", new[] { "EmpresaId", "ProveedorId" }, "Proveedores", new[] { "EmpresaId", "Id" }, "IX_QAC3_41", "FK_QAC3_41", false),
        new("VentaImpuestos", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QAC3_42", "FK_QAC3_42", true),
        new("CompraImpuestos", new[] { "EmpresaId", "CompraId" }, "Compras", new[] { "EmpresaId", "Id" }, "IX_QAC3_43", "FK_QAC3_43", true),
        new("ConteosInventario", new[] { "EmpresaId", "CategoriaId" }, "Categorias", new[] { "EmpresaId", "Id" }, "IX_QAC3_44", "FK_QAC3_44", false),
        new("ConteoInventarioDetalles", new[] { "EmpresaId", "AlmacenId", "ConteoInventarioId" }, "ConteosInventario", new[] { "EmpresaId", "AlmacenId", "Id" }, "IX_QAC3_45", "FK_QAC3_45", true),
        new("ConteoInventarioDetalles", new[] { "EmpresaId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "Id" }, "IX_QAC3_46", "FK_QAC3_46", false),
        new("ConteoInventarioDetalles", new[] { "EmpresaId", "AjusteInventarioId" }, "AjustesInventario", new[] { "EmpresaId", "Id" }, "IX_QAC3_47", "FK_QAC3_47", false),
        new("MatchesConciliacion", new[] { "EmpresaId", "MovimientoFinancieroId" }, "MovimientosFinancieros", new[] { "EmpresaId", "Id" }, "IX_QAC3_48", "FK_QAC3_48", false),
        new("ConciliacionesBancarias", new[] { "EmpresaId", "CuentaBancariaId" }, "CuentasBancarias", new[] { "EmpresaId", "Id" }, "IX_QAC3_49", "FK_QAC3_49", false),
        new("MovimientosEstadoCuenta", new[] { "EmpresaId", "ConciliacionBancariaId" }, "ConciliacionesBancarias", new[] { "EmpresaId", "Id" }, "IX_QAC3_50", "FK_QAC3_50", true),
        new("MatchesConciliacion", new[] { "EmpresaId", "MovimientoEstadoCuentaId" }, "MovimientosEstadoCuenta", new[] { "EmpresaId", "Id" }, "IX_QAC3_51", "FK_QAC3_51", true),
    };
    public static QaCommercialOwnershipV1.Edge GalleryRelation { get; } = new("CategoriasGalerias", new[] { "EmpresaId", "CategoriaVerificadaId" }, "Categorias", new[] { "EmpresaId", "Id" }, "IX_QAC3_Galeria_Verificada", "FK_QAC3_Galeria_Verificada", false);
    public static IReadOnlyList<ScopedKey> NaturalKeys { get; } = new ScopedKey[]
    {
        new("Marcas", "UX_Marcas_Nombre_Activo", new[] { "NombreActivoUnico" }),
        new("Modelos", "UX_Modelos_Marca_Nombre_Activo", new[] { "NombreMarcaActivoUnico" }),
        new("Colores", "UX_Colores_Nombre_Activo", new[] { "NombreActivoUnico" }),
        new("Tallas", "UX_Tallas_Nombre_Activo", new[] { "NombreActivoUnico" }),
        new("TipoClientes", "IX_TipoClientes_Codigo", new[] { "Codigo" }),
        new("TipoClientes", "IX_TipoClientes_EsPredeterminadoUnico", new[] { "EsPredeterminadoUnico" }),
        new("TipoClientes", "IX_TipoClientes_NombreNormalizado", new[] { "NombreNormalizado" }),
        new("MetodosPago", "UX_MetodosPago_Codigo_Normalizado", new[] { "CodigoNormalizado" }),
        new("Descuentos", "IX_Descuentos_CodigoPromocionalNormalizado", new[] { "CodigoPromocionalNormalizado" }),
        new("Impuestos", "IX_Impuestos_Codigo", new[] { "Codigo" }),
        new("ConteosInventario", "UX_ConteosInventario_Numero", new[] { "Numero" }),
        new("CuentasBancarias", "UX_CuentasBancarias_BancoId_NumeroCuenta", new[] { "BancoId", "NumeroCuenta" }),
    };
    public static IReadOnlyList<ScopedKey> ExtraKeys { get; } = new ScopedKey[]
    {
        new("Roles", "UX_QAC3_Roles_OwnerId", new[] { "EmpresaId", "Id" }),
        new("Modelos", "UX_QAC3_Modelos_OwnerMarcaId", new[] { "EmpresaId", "MarcaId", "Id" }),
        new("ConteosInventario", "UX_QAC3_Conteos_OwnerAlmacenId", new[] { "EmpresaId", "AlmacenId", "Id" }),
    };
    public static IReadOnlyList<Trigger> Triggers { get; } = new Trigger[]
    {
        new("TR_QAC3_Galeria_BI", "CategoriasGalerias", "INSERT", "IF NEW.CategoriaVerificadaId IS NULL OR NEW.CategoriaVerificadaId <> NEW.CategoriaId THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Verified tenant category required'; END IF;"),
        new("TR_QAC3_ImpuestoDocumento_BI", "HistorialAplicacionImpuestos", "INSERT", "IF NEW.EmpresaId IS NOT NULL AND NOT ((BINARY NEW.DocumentoTipo = BINARY 'Venta' AND EXISTS (SELECT 1 FROM Ventas v WHERE v.Id=NEW.DocumentoId AND v.EmpresaId=NEW.EmpresaId)) OR (BINARY NEW.DocumentoTipo = BINARY 'Compra' AND EXISTS (SELECT 1 FROM Compras c WHERE c.Id=NEW.DocumentoId AND c.EmpresaId=NEW.EmpresaId))) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Document tenant mismatch'; END IF;"),
        new("TR_QAC3_Conteo_BI", "ConteosInventario", "INSERT", "IF NEW.EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE a.Id=NEW.AlmacenId AND s.EmpresaId=NEW.EmpresaId) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Warehouse tenant mismatch'; END IF;"),
        new("TR_QAC3_ConteoDetalle_BI", "ConteoInventarioDetalles", "INSERT", "IF NEW.EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE a.Id=NEW.AlmacenId AND s.EmpresaId=NEW.EmpresaId) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Warehouse tenant mismatch'; END IF;"),
        new("TR_QAC3_Galeria_BU", "CategoriasGalerias", "UPDATE", "IF NEW.CategoriaVerificadaId IS NULL OR NEW.CategoriaVerificadaId <> NEW.CategoriaId THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Verified tenant category required'; END IF;"),
        new("TR_QAC3_ImpuestoDocumento_BU", "HistorialAplicacionImpuestos", "UPDATE", "IF NEW.EmpresaId IS NOT NULL AND NOT ((BINARY NEW.DocumentoTipo = BINARY 'Venta' AND EXISTS (SELECT 1 FROM Ventas v WHERE v.Id=NEW.DocumentoId AND v.EmpresaId=NEW.EmpresaId)) OR (BINARY NEW.DocumentoTipo = BINARY 'Compra' AND EXISTS (SELECT 1 FROM Compras c WHERE c.Id=NEW.DocumentoId AND c.EmpresaId=NEW.EmpresaId))) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Document tenant mismatch'; END IF;"),
        new("TR_QAC3_Conteo_BU", "ConteosInventario", "UPDATE", "IF NEW.EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE a.Id=NEW.AlmacenId AND s.EmpresaId=NEW.EmpresaId) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Warehouse tenant mismatch'; END IF;"),
        new("TR_QAC3_ConteoDetalle_BU", "ConteoInventarioDetalles", "UPDATE", "IF NEW.EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Almacenes a JOIN Sucursales s ON s.Id=a.SucursalId WHERE a.Id=NEW.AlmacenId AND s.EmpresaId=NEW.EmpresaId) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Warehouse tenant mismatch'; END IF;"),
        new("TR_QAC3_Almacen_Conteo_BU", "Almacenes", "UPDATE", "IF NOT (NEW.SucursalId <=> OLD.SucursalId) AND EXISTS (SELECT 1 FROM ConteosInventario c WHERE c.AlmacenId=OLD.Id AND c.EmpresaId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Sucursales s WHERE s.Id=NEW.SucursalId AND s.EmpresaId=c.EmpresaId)) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Warehouse move would invalidate count owner'; END IF;"),
        new("TR_QAC3_Sucursal_Conteo_BU", "Sucursales", "UPDATE", "IF NOT (NEW.EmpresaId <=> OLD.EmpresaId) AND EXISTS (SELECT 1 FROM ConteosInventario c JOIN Almacenes a ON a.Id=c.AlmacenId WHERE a.SucursalId=OLD.Id AND c.EmpresaId IS NOT NULL AND NOT (c.EmpresaId <=> NEW.EmpresaId)) THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Branch move would invalidate count owner'; END IF;"),
    };
    public static bool Owns(string table) => Tables.Any(t => t.Table == table);
    public static string OwnerIndex(string table) => $"UX_QAC3_{table}_OwnerId";
    public static string OwnerForeignKey(string table) => $"FK_QAC3_{table}_Empresa";
    private static string Q(string value) => $"`{value}`";
    private static string Cols(IEnumerable<string> names) => string.Join(", ", names.Select(Q));

    // Provision new canonical defaults only; never assign or edit a legacy row.
    public static IEnumerable<string> DefaultStatements(bool scoped)
    {
        var scope = scoped ? " AND e.Id={0}" : "";
        yield return """
            INSERT INTO TipoClientes (EmpresaId,Codigo,EsSistema,Nombre,NombreNormalizado,Descripcion,ColorHex,Activo,Orden,EsPredeterminado,Eliminado,FechaCreacion,FechaActualizacion)
            SELECT e.Id,'SIN_CLASIFICAR',1,'Sin clasificar','SIN CLASIFICAR','Cliente sin clasificar','#9E9E9E',1,0,
                   NOT EXISTS (SELECT 1 FROM TipoClientes d WHERE d.EmpresaId=e.Id AND d.EsPredeterminado=1 AND d.Activo=1 AND d.Eliminado=0),
                   0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6)
            FROM Empresas e WHERE e.Activa=1 AND NOT EXISTS
                (SELECT 1 FROM TipoClientes t WHERE t.EmpresaId=e.Id AND t.Codigo='SIN_CLASIFICAR')
            """ + scope + ";";
        foreach (var (name, order, legacy) in new[] { ("Efectivo",10,1), ("Transferencia",20,2), ("Tarjeta",30,3), ("Otro",40,4) })
        {
            yield return $"""
                INSERT INTO MetodosPago (EmpresaId,Codigo,Nombre,Tipo,Activo,RequiereReferencia,RequiereBanco,PermiteCambio,Orden,Metadata,Eliminado,FechaCreacion,FechaActualizacion,CreadoPorNombreUsuario,ActualizadoPorNombreUsuario)
                SELECT e.Id,'{name}','{name}','{name}',1,0,0,0,{order},JSON_OBJECT('phase7CanonicalSeed',true,'legacyEnum',{legacy}),0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),'SOLQARYN-F7','SOLQARYN-F7'
                FROM Empresas e WHERE e.Activa=1 AND NOT EXISTS
                    (SELECT 1 FROM MetodosPago m WHERE m.EmpresaId=e.Id AND LOWER(TRIM(m.Codigo))=LOWER('{name}'))
                """ + scope + ";";
        }
    }

    public static async Task ProvisionDefaultsAsync(AppDbContext db, int empresaId, CancellationToken ct = default)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("La provisión empresarial requiere una transacción.");
        var locked = await db.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Empresas WHERE Id={0} AND Activa=1 FOR UPDATE", empresaId).ToListAsync(ct);
        if (locked.Count != 1) throw new InvalidOperationException("Empresa activa no disponible para provisión.");
        foreach (var sql in DefaultStatements(true))
            await db.Database.ExecuteSqlRawAsync(sql, new object[] { empresaId }, ct);
    }

    public static async Task InstallFreshPhysicalAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var group in NaturalKeys.GroupBy(k => k.Table))
        {
            if (await HasExactScopeAsync(db, group.Key, ct)) continue;
            var count = await db.Database.SqlQueryRaw<long>($"SELECT COUNT(*) AS Value FROM {Q(group.Key)}").SingleAsync(ct);
            if (count != 0) throw new InvalidOperationException("V3 fresh DDL requires empty table: " + group.Key);
            foreach (var key in group)
                await VerifyIndexAsync(db, key.Table, key.Index, new[] { QaCommercialOwnershipV1.ScopeColumn }.Concat(key.Columns).ToArray(), true, ct);
            var clauses = group.Select(k => $"DROP INDEX {Q(k.Index)}").ToList();
            clauses.Add($"DROP COLUMN {Q(QaCommercialOwnershipV1.ScopeColumn)}");
            clauses.Add($"ADD COLUMN {QaCommercialOwnershipV1.ScopeColumnDefinition}");
            clauses.AddRange(group.Select(k => $"ADD UNIQUE INDEX {Q(k.Index)} ({Cols(new[] { QaCommercialOwnershipV1.ScopeColumn }.Concat(k.Columns))})"));
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {Q(group.Key)} {string.Join(", ", clauses)};", ct);
        }
        foreach (var edge in Relations.Append(GalleryRelation))
        {
            var count = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME={0} AND CONSTRAINT_NAME={1}",
                edge.Child, edge.ForeignKey).SingleAsync(ct);
            if (count == 0) await db.Database.ExecuteSqlRawAsync(QaCommercialOwnershipV1.RelationDdl(edge), ct);
        }
        foreach (var trigger in Triggers)
        {
            var count = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND TRIGGER_NAME={0}",
                trigger.Name).SingleAsync(ct);
            if (count == 0) await db.Database.ExecuteSqlRawAsync(trigger.Ddl, ct);
        }
        await VerifyPhysicalAsync(db, ct);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`,`ProductVersion`) VALUES ({0},{1})",
            new object[] { MigrationId, "10.0.12" }, ct);
    }

    public static async Task VerifyPhysicalAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var table in Tables)
        {
            var n = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND COLUMN_NAME='EmpresaId' AND DATA_TYPE='int' AND IS_NULLABLE='YES'",
                table.Table).SingleAsync(ct);
            if (n != 1) throw new InvalidOperationException("V3 owner column mismatch: " + table.Table);
            await VerifyIndexAsync(db, table.Table, OwnerIndex(table.Table), new[] { "EmpresaId", "Id" }, true, ct);
            await VerifyFkAsync(db, table.Table, OwnerForeignKey(table.Table), new[] { "EmpresaId" }, "Empresas", new[] { "Id" }, "RESTRICT", ct);
        }
        foreach (var key in ExtraKeys)
            await VerifyIndexAsync(db, key.Table, key.Index, key.Columns, true, ct);
        foreach (var key in NaturalKeys)
        {
            if (!await HasExactScopeAsync(db, key.Table, ct))
                throw new InvalidOperationException("V3 scope column mismatch: " + key.Table);
            await VerifyIndexAsync(db, key.Table, key.Index, new[] { QaCommercialOwnershipV1.ScopeColumn }.Concat(key.Columns).ToArray(), true, ct);
        }
        foreach (var edge in Relations.Append(GalleryRelation))
        {
            await VerifyIndexAsync(db, edge.Child, edge.Index, edge.Columns, false, ct);
            await VerifyFkAsync(db, edge.Child, edge.ForeignKey, edge.Columns, edge.Parent, edge.Principal, edge.Cascade ? "CASCADE" : "RESTRICT", ct);
        }
        foreach (var trigger in Triggers)
        {
            var n = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE() AND TRIGGER_NAME={0} AND EVENT_OBJECT_TABLE={1} AND EVENT_MANIPULATION={2} AND ACTION_TIMING='BEFORE'",
                trigger.Name, trigger.Table, trigger.Action).SingleAsync(ct);
            if (n != 1) throw new InvalidOperationException("V3 trigger mismatch: " + trigger.Name);
        }
    }

    private static async Task<bool> HasExactScopeAsync(AppDbContext db, string table, CancellationToken ct)
    {
        var n = await db.Database.SqlQueryRaw<long>(
            "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND COLUMN_NAME='EmpresaUnicidad' AND DATA_TYPE='int' AND IS_NULLABLE='NO' AND EXTRA LIKE '%STORED GENERATED%' AND LOWER(REPLACE(REPLACE(GENERATION_EXPRESSION,'`',''),' ',''))='coalesce(empresaid,0)'",
            table).SingleAsync(ct);
        return n == 1;
    }

    private static async Task VerifyIndexAsync(AppDbContext db, string table, string name, string[] columns, bool unique, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) AS Value FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND INDEX_NAME={1} ORDER BY SEQ_IN_INDEX",
            table,name).ToListAsync(ct);
        if (!rows.SequenceEqual(columns.Select((c,i) => $"{i+1}:{c}:{(unique ? 0 : 1)}")))
            throw new InvalidOperationException("V3 index mismatch: " + table + "." + name);
    }

    private static async Task VerifyFkAsync(AppDbContext db, string table, string name, string[] columns, string parent, string[] principal, string rule, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(k.ORDINAL_POSITION,':',k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':',k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) AS Value FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME={0} AND k.CONSTRAINT_NAME={1} ORDER BY k.ORDINAL_POSITION",
            table,name).ToListAsync(ct);
        if (!rows.SequenceEqual(columns.Select((c,i) => $"{i+1}:{c}:{parent}:{principal[i]}:{rule}")))
            throw new InvalidOperationException("V3 foreign key mismatch: " + table + "." + name);
    }
}
