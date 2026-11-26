using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>Immutable ownership contract for QA. Legacy NULL means quarantined.</summary>
public static class QaCommercialOwnershipV1
{
    public const string MigrationId = "20261126170000_QaCommercialOwnershipV1";
    public const string TenantFilter = "QaCommercialTenant";
    public const string SoftDeleteFilter = "QaCommercialSoftDelete";
    public const string ScopeColumn = "EmpresaUnicidad";
    public const string VariantKey = "UX_QACT_Variante_Owner_Product_Id";
    public sealed record Owned(Type Entity, string Table);
    public sealed record Edge(string Child, string[] Columns, string Parent, string[] Principal,
        string Index, string ForeignKey, bool Cascade);
    public sealed record Natural(string Table, string Column, string Index);
    public static IReadOnlyList<Owned> Tables { get; } = new Owned[]
    {
        new(typeof(Producto), "Productos"),
        new(typeof(ProductoVariante), "ProductoVariantes"),
        new(typeof(ProductoImagen), "ProductoImagenes"),
        new(typeof(Cliente), "Clientes"),
        new(typeof(Proveedor), "Proveedores"),
        new(typeof(Compra), "Compras"),
        new(typeof(Venta), "Ventas"),
        new(typeof(CompraDetalle), "CompraDetalles"),
        new(typeof(VentaDetalle), "VentaDetalles"),
        new(typeof(Factura), "Facturas"),
        new(typeof(MovimientoInventario), "MovimientosInventario"),
        new(typeof(FacturaDetalle), "FacturaDetalles"),
        new(typeof(FacturaPago), "FacturaPagos"),
        new(typeof(CompraDocumento), "CompraDocumentos"),
        new(typeof(ExistenciaVariante), "ExistenciasVariante"),
        new(typeof(ConsumoInsumo), "ConsumosInsumos"),
        new(typeof(ConsumoInsumoDetalle), "ConsumoInsumoDetalles"),
        new(typeof(AjusteInventario), "AjustesInventario"),
        new(typeof(AjusteInventarioDetalle), "AjusteInventarioDetalles"),
        new(typeof(TransferenciaInventario), "TransferenciasInventario"),
        new(typeof(TransferenciaInventarioDetalle), "TransferenciaInventarioDetalles"),
        new(typeof(RecepcionCompra), "RecepcionesCompra"),
        new(typeof(RecepcionCompraDetalle), "RecepcionCompraDetalles"),
        new(typeof(OrdenCompra), "OrdenesCompra"),
        new(typeof(OrdenCompraDetalle), "OrdenCompraDetalles"),
        new(typeof(SolicitudCompra), "SolicitudesCompra"),
        new(typeof(SolicitudCompraDetalle), "SolicitudCompraDetalles"),
    };
    public static IReadOnlyList<Edge> Relations { get; } = new Edge[]
    {
        new("ProductoVariantes", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_001", "FK_QACT_001", false),
        new("ProductoImagenes", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_002", "FK_QACT_002", true),
        new("Compras", new[] { "EmpresaId", "ProveedorId" }, "Proveedores", new[] { "EmpresaId", "Id" }, "IX_QACT_003", "FK_QACT_003", false),
        new("Ventas", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QACT_004", "FK_QACT_004", false),
        new("CompraDetalles", new[] { "EmpresaId", "CompraId" }, "Compras", new[] { "EmpresaId", "Id" }, "IX_QACT_005", "FK_QACT_005", true),
        new("VentaDetalles", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QACT_006", "FK_QACT_006", true),
        new("Facturas", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QACT_007", "FK_QACT_007", false),
        new("CompraDocumentos", new[] { "EmpresaId", "CompraId" }, "Compras", new[] { "EmpresaId", "Id" }, "IX_QACT_008", "FK_QACT_008", false),
        new("FacturaDetalles", new[] { "EmpresaId", "FacturaId" }, "Facturas", new[] { "EmpresaId", "Id" }, "IX_QACT_009", "FK_QACT_009", true),
        new("FacturaPagos", new[] { "EmpresaId", "FacturaId" }, "Facturas", new[] { "EmpresaId", "Id" }, "IX_QACT_010", "FK_QACT_010", false),
        new("ExistenciasVariante", new[] { "EmpresaId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "Id" }, "IX_QACT_011", "FK_QACT_011", false),
        new("ConsumoInsumoDetalles", new[] { "EmpresaId", "ConsumoInsumoId" }, "ConsumosInsumos", new[] { "EmpresaId", "Id" }, "IX_QACT_012", "FK_QACT_012", true),
        new("AjusteInventarioDetalles", new[] { "EmpresaId", "AjusteInventarioId" }, "AjustesInventario", new[] { "EmpresaId", "Id" }, "IX_QACT_013", "FK_QACT_013", true),
        new("TransferenciaInventarioDetalles", new[] { "EmpresaId", "TransferenciaInventarioId" }, "TransferenciasInventario", new[] { "EmpresaId", "Id" }, "IX_QACT_014", "FK_QACT_014", true),
        new("TransferenciaInventarioDetalles", new[] { "EmpresaId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "Id" }, "IX_QACT_015", "FK_QACT_015", false),
        new("RecepcionesCompra", new[] { "EmpresaId", "OrdenCompraId" }, "OrdenesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_016", "FK_QACT_016", false),
        new("RecepcionCompraDetalles", new[] { "EmpresaId", "RecepcionCompraId" }, "RecepcionesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_017", "FK_QACT_017", true),
        new("RecepcionCompraDetalles", new[] { "EmpresaId", "OrdenCompraDetalleId" }, "OrdenCompraDetalles", new[] { "EmpresaId", "Id" }, "IX_QACT_018", "FK_QACT_018", false),
        new("OrdenesCompra", new[] { "EmpresaId", "ProveedorId" }, "Proveedores", new[] { "EmpresaId", "Id" }, "IX_QACT_019", "FK_QACT_019", false),
        new("OrdenesCompra", new[] { "EmpresaId", "SolicitudCompraId" }, "SolicitudesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_020", "FK_QACT_020", false),
        new("OrdenCompraDetalles", new[] { "EmpresaId", "OrdenCompraId" }, "OrdenesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_021", "FK_QACT_021", true),
        new("SolicitudesCompra", new[] { "EmpresaId", "ProveedorId" }, "Proveedores", new[] { "EmpresaId", "Id" }, "IX_QACT_022", "FK_QACT_022", false),
        new("SolicitudCompraDetalles", new[] { "EmpresaId", "SolicitudCompraId" }, "SolicitudesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_023", "FK_QACT_023", true),
        new("ProductoImagenes", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_024", "FK_QACT_024", false),
        new("CompraDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_025", "FK_QACT_025", false),
        new("CompraDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_026", "FK_QACT_026", false),
        new("VentaDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_027", "FK_QACT_027", false),
        new("VentaDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_028", "FK_QACT_028", false),
        new("MovimientosInventario", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_029", "FK_QACT_029", false),
        new("MovimientosInventario", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_030", "FK_QACT_030", false),
        new("FacturaDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_031", "FK_QACT_031", false),
        new("FacturaDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_032", "FK_QACT_032", false),
        new("ConsumoInsumoDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_033", "FK_QACT_033", false),
        new("ConsumoInsumoDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_034", "FK_QACT_034", false),
        new("AjusteInventarioDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_035", "FK_QACT_035", false),
        new("AjusteInventarioDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_036", "FK_QACT_036", false),
        new("RecepcionCompraDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_037", "FK_QACT_037", false),
        new("RecepcionCompraDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_038", "FK_QACT_038", false),
        new("OrdenCompraDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_039", "FK_QACT_039", false),
        new("OrdenCompraDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_040", "FK_QACT_040", false),
        new("SolicitudCompraDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QACT_041", "FK_QACT_041", false),
        new("SolicitudCompraDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QACT_042", "FK_QACT_042", false),
        new("MovimientosInventario", new[] { "EmpresaId", "CompraId" }, "Compras", new[] { "EmpresaId", "Id" }, "IX_QACT_043", "FK_QACT_043", false),
        new("MovimientosInventario", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QACT_044", "FK_QACT_044", false),
        new("MovimientosInventario", new[] { "EmpresaId", "ConsumoInsumoId" }, "ConsumosInsumos", new[] { "EmpresaId", "Id" }, "IX_QACT_045", "FK_QACT_045", false),
        new("MovimientosInventario", new[] { "EmpresaId", "AjusteInventarioId" }, "AjustesInventario", new[] { "EmpresaId", "Id" }, "IX_QACT_046", "FK_QACT_046", false),
        new("MovimientosInventario", new[] { "EmpresaId", "TransferenciaInventarioId" }, "TransferenciasInventario", new[] { "EmpresaId", "Id" }, "IX_QACT_047", "FK_QACT_047", false),
        new("MovimientosInventario", new[] { "EmpresaId", "RecepcionCompraId" }, "RecepcionesCompra", new[] { "EmpresaId", "Id" }, "IX_QACT_048", "FK_QACT_048", false),
    };
    public static IReadOnlyList<Natural> NaturalKeys { get; } = new Natural[]
    {
        new("ProductoVariantes", "Sku", "IX_ProductoVariantes_Sku"),
        new("ProductoVariantes", "CodigoBarras", "IX_ProductoVariantes_CodigoBarras"),
        new("Clientes", "IdentidadORTNNormalizada", "UX_Clientes_IdentidadORTN_Normalizada"),
        new("Proveedores", "DocumentoNormalizado", "UX_Proveedores_Documento_Normalizado"),
        new("Compras", "NumeroCompra", "IX_Compras_NumeroCompra"),
        new("Ventas", "NumeroVenta", "IX_Ventas_NumeroVenta"),
        new("Facturas", "NumeroFactura", "IX_Facturas_NumeroFactura"),
        new("ConsumosInsumos", "NumeroConsumo", "IX_ConsumosInsumos_NumeroConsumo"),
        new("AjustesInventario", "NumeroAjuste", "UX_AjustesInventario_Numero"),
        new("TransferenciasInventario", "Numero", "UX_TransferenciasInventario_Numero"),
        new("RecepcionesCompra", "IdempotencyKey", "UX_RecepcionesCompra_IdempotencyKey"),
        new("RecepcionesCompra", "NumeroRecepcion", "UX_RecepcionesCompra_NumeroRecepcion"),
        new("OrdenesCompra", "IdempotencyKey", "UX_OrdenesCompra_IdempotencyKey"),
        new("OrdenesCompra", "NumeroOrden", "UX_OrdenesCompra_NumeroOrden"),
        new("SolicitudesCompra", "NumeroSolicitud", "UX_SolicitudesCompra_NumeroSolicitud"),
    };
    public static string OwnerIndex(string table) => $"UX_QACT_{table}_OwnerId";
    public static string OwnerForeignKey(string table) => $"FK_QACT_{table}_Empresa";
    private static string Quote(string value) => $"`{value}`";
    private static string Columns(IEnumerable<string> values) => string.Join(", ", values.Select(Quote));
    public static string RelationDdl(Edge edge) =>
        $"ALTER TABLE {Quote(edge.Child)} ADD CONSTRAINT {Quote(edge.ForeignKey)} " +
        $"FOREIGN KEY ({Columns(edge.Columns)}) REFERENCES {Quote(edge.Parent)} " +
        $"({Columns(edge.Principal)}) ON DELETE {(edge.Cascade ? "CASCADE" : "RESTRICT")};";

    public static async Task InstallFreshPhysicalAsync(AppDbContext db, CancellationToken ct = default)
    {
        // Fresh EnsureCreated has the model columns and indexes. Nullable legacy
        // ownership cannot be an EF alternate key; install the physical FKs.
        foreach (var edge in Relations)
        {
            var exists = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS " +
                "WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME={0} AND CONSTRAINT_NAME={1}",
                edge.Child, edge.ForeignKey).SingleAsync(ct);
            if (exists == 0)
                await db.Database.ExecuteSqlRawAsync(RelationDdl(edge), ct);
        }
        await VerifyPhysicalAsync(db, ct);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
              `MigrationId` varchar(150) NOT NULL,
              `ProductVersion` varchar(32) NOT NULL,
              PRIMARY KEY (`MigrationId`)
            ) ENGINE=InnoDB;
            """, ct);
        await db.Database.ExecuteSqlRawAsync(
            "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`,`ProductVersion`) VALUES ({0},{1})",
            new object[] { MigrationId, "10.0.12" }, ct);
    }

    public static async Task VerifyPhysicalAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var table in Tables)
        {
            var n = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS " +
                "WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND COLUMN_NAME='EmpresaId' " +
                "AND DATA_TYPE='int' AND IS_NULLABLE='YES'", table.Table).SingleAsync(ct);
            if (n != 1) throw new InvalidOperationException($"Ownership column mismatch: {table.Table}");
            await VerifyIndexAsync(db, table.Table, OwnerIndex(table.Table), new[] { "EmpresaId", "Id" }, true, ct);
            await VerifyFkAsync(db, table.Table, OwnerForeignKey(table.Table), new[] { "EmpresaId" },
                "Empresas", new[] { "Id" }, "RESTRICT", ct);
        }
        await VerifyIndexAsync(db, "ProductoVariantes", VariantKey,
            new[] { "EmpresaId", "ProductoId", "Id" }, true, ct);
        foreach (var e in Relations)
        {
            await VerifyIndexAsync(db, e.Child, e.Index, e.Columns, false, ct);
            await VerifyFkAsync(db, e.Child, e.ForeignKey, e.Columns, e.Parent, e.Principal,
                e.Cascade ? "CASCADE" : "RESTRICT", ct);
        }
        foreach (var k in NaturalKeys)
        {
            var n = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() " +
                "AND TABLE_NAME={0} AND COLUMN_NAME='EmpresaUnicidad' AND EXTRA LIKE '%STORED GENERATED%'",
                k.Table).SingleAsync(ct);
            if (n != 1) throw new InvalidOperationException($"Scoped uniqueness mismatch: {k.Table}");
            await VerifyIndexAsync(db, k.Table, k.Index, new[] { ScopeColumn, k.Column }, true, ct);
        }
    }

    private static async Task VerifyIndexAsync(AppDbContext db, string table, string name,
        string[] columns, bool unique, CancellationToken ct)
    {
        var actual = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) AS Value FROM INFORMATION_SCHEMA.STATISTICS " +
            "WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND INDEX_NAME={1} ORDER BY SEQ_IN_INDEX",
            table, name).ToListAsync(ct);
        if (!actual.SequenceEqual(columns.Select((c, i) => $"{i + 1}:{c}:{(unique ? 0 : 1)}")))
            throw new InvalidOperationException($"Ownership index mismatch: {table}.{name}");
    }

    private static async Task VerifyFkAsync(AppDbContext db, string table, string name,
        string[] columns, string parent, string[] principal, string rule, CancellationToken ct)
    {
        var actual = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(k.ORDINAL_POSITION,':',k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':'," +
            "k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) AS Value FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k " +
            "JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA " +
            "AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME " +
            "WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME={0} AND k.CONSTRAINT_NAME={1} ORDER BY k.ORDINAL_POSITION",
            table, name).ToListAsync(ct);
        if (!actual.SequenceEqual(columns.Select((c, i) => $"{i + 1}:{c}:{parent}:{principal[i]}:{rule}")))
            throw new InvalidOperationException($"Ownership FK mismatch: {table}.{name}");
    }
}
