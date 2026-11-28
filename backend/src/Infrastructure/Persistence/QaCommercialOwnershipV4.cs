using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>QA ownership of quotations, orders, reservations and fulfillment. Historical owner remains NULL until verified.</summary>
public static class QaCommercialOwnershipV4
{
    public const string MigrationId = "20261128150000_QaCommercialOwnershipV4";
    public static IReadOnlyList<QaCommercialOwnershipV1.Owned> Tables { get; } = new QaCommercialOwnershipV1.Owned[]
    {
        new(typeof(Cotizacion), "Cotizaciones"),
        new(typeof(CotizacionDetalle), "CotizacionDetalles"),
        new(typeof(PedidoVenta), "PedidosVenta"),
        new(typeof(PedidoVentaDetalle), "PedidoVentaDetalles"),
        new(typeof(ReservaInventario), "ReservasInventario"),
        new(typeof(ReservaInventarioDetalle), "ReservaInventarioDetalles"),
        new(typeof(PreparacionPedidoVenta), "PreparacionesPedidoVenta"),
        new(typeof(PreparacionPedidoVentaDetalle), "PreparacionPedidoVentaDetalles"),
    };
    public static IReadOnlyList<QaCommercialOwnershipV1.Edge> Relations { get; } = new QaCommercialOwnershipV1.Edge[]
    {
        new("Cotizaciones", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QAC4_01", "FK_QAC4_01", false),
        new("CotizacionDetalles", new[] { "EmpresaId", "CotizacionId" }, "Cotizaciones", new[] { "EmpresaId", "Id" }, "IX_QAC4_02", "FK_QAC4_02", true),
        new("CotizacionDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QAC4_03", "FK_QAC4_03", false),
        new("CotizacionDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QAC4_04", "FK_QAC4_04", false),
        new("PedidosVenta", new[] { "EmpresaId", "ClienteId" }, "Clientes", new[] { "EmpresaId", "Id" }, "IX_QAC4_05", "FK_QAC4_05", false),
        new("PedidosVenta", new[] { "EmpresaId", "CotizacionId" }, "Cotizaciones", new[] { "EmpresaId", "Id" }, "IX_QAC4_06", "FK_QAC4_06", false),
        new("PedidoVentaDetalles", new[] { "EmpresaId", "PedidoVentaId" }, "PedidosVenta", new[] { "EmpresaId", "Id" }, "IX_QAC4_07", "FK_QAC4_07", true),
        new("PedidoVentaDetalles", new[] { "EmpresaId", "ProductoId" }, "Productos", new[] { "EmpresaId", "Id" }, "IX_QAC4_08", "FK_QAC4_08", false),
        new("PedidoVentaDetalles", new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "ProductoId", "Id" }, "IX_QAC4_09", "FK_QAC4_09", false),
        new("ReservasInventario", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QAC4_10", "FK_QAC4_10", false),
        new("ReservasInventario", new[] { "EmpresaId", "PedidoVentaId" }, "PedidosVenta", new[] { "EmpresaId", "Id" }, "IX_QAC4_11", "FK_QAC4_11", false),
        new("ReservaInventarioDetalles", new[] { "EmpresaId", "ReservaInventarioId" }, "ReservasInventario", new[] { "EmpresaId", "Id" }, "IX_QAC4_12", "FK_QAC4_12", true),
        new("ReservaInventarioDetalles", new[] { "EmpresaId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "Id" }, "IX_QAC4_13", "FK_QAC4_13", false),
        new("PreparacionesPedidoVenta", new[] { "EmpresaId", "PedidoVentaId" }, "PedidosVenta", new[] { "EmpresaId", "Id" }, "IX_QAC4_14", "FK_QAC4_14", false),
        new("PreparacionesPedidoVenta", new[] { "EmpresaId", "ReservaInventarioId" }, "ReservasInventario", new[] { "EmpresaId", "Id" }, "IX_QAC4_15", "FK_QAC4_15", false),
        new("PreparacionPedidoVentaDetalles", new[] { "EmpresaId", "PreparacionPedidoVentaId" }, "PreparacionesPedidoVenta", new[] { "EmpresaId", "Id" }, "IX_QAC4_16", "FK_QAC4_16", true),
        new("PreparacionPedidoVentaDetalles", new[] { "EmpresaId", "ProductoVarianteId" }, "ProductoVariantes", new[] { "EmpresaId", "Id" }, "IX_QAC4_17", "FK_QAC4_17", false),
    };
    public sealed record ScopedUnique(string Table, string Index, string[] Columns);
    public static IReadOnlyList<ScopedUnique> UniqueKeys { get; } = new ScopedUnique[]
    {
        new("PedidosVenta", "UX_PedidosVenta_IdempotencyKey", new[] { "IdempotencyKey" }),
        new("ReservasInventario", "UX_ReservasInventario_Numero", new[] { "Numero" }),
    };

    public static async Task InstallFreshPhysicalAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var edge in Relations)
        {
            var count = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE() AND TABLE_NAME={0} AND CONSTRAINT_NAME={1}",
                edge.Child, edge.ForeignKey).SingleAsync(ct);
            if (count == 0)
                await db.Database.ExecuteSqlRawAsync(QaCommercialOwnershipV1.RelationDdl(edge), ct);
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
            var count = await db.Database.SqlQueryRaw<long>(
                "SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND COLUMN_NAME='EmpresaId' AND DATA_TYPE='int' AND IS_NULLABLE='YES'",
                table.Table).SingleAsync(ct);
            if (count != 1) throw new InvalidOperationException("V4 owner column missing: " + table.Table);
            await VerifyIndexAsync(db, table.Table, QaCommercialOwnershipV1.OwnerIndex(table.Table), new[] { "EmpresaId", "Id" }, true, ct);
            await VerifyFkAsync(db, table.Table, QaCommercialOwnershipV1.OwnerForeignKey(table.Table), new[] { "EmpresaId" }, "Empresas", new[] { "Id" }, ct);
        }
        foreach (var key in UniqueKeys)
            await VerifyIndexAsync(db, key.Table, key.Index, new[] { "EmpresaId" }.Concat(key.Columns).ToArray(), true, ct);
        foreach (var edge in Relations)
        {
            await VerifyIndexAsync(db, edge.Child, edge.Index, edge.Columns, false, ct);
            await VerifyFkAsync(db, edge.Child, edge.ForeignKey, edge.Columns, edge.Parent, edge.Principal, ct, edge.Cascade);
        }
    }
    private static async Task VerifyIndexAsync(AppDbContext db, string table, string name, string[] cols, bool unique, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) AS Value FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND INDEX_NAME={1} ORDER BY SEQ_IN_INDEX",
            table, name).ToListAsync(ct);
        if (!rows.SequenceEqual(cols.Select((c,i) => $"{i+1}:{c}:{(unique ? 0 : 1)}")))
            throw new InvalidOperationException("V4 index mismatch: " + table + "." + name);
    }
    private static async Task VerifyFkAsync(AppDbContext db, string table, string name, string[] cols, string parent, string[] principal, CancellationToken ct, bool cascade = false)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(k.ORDINAL_POSITION,':',k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':',k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) AS Value FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME={0} AND k.CONSTRAINT_NAME={1} ORDER BY k.ORDINAL_POSITION",
            table, name).ToListAsync(ct);
        if (!rows.SequenceEqual(cols.Select((c,i) => $"{i+1}:{c}:{parent}:{principal[i]}:{(cascade ? "CASCADE" : "RESTRICT")}")))
            throw new InvalidOperationException("V4 FK mismatch: " + table + "." + name);
    }
}
