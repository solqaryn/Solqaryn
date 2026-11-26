using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>QA residual shipping, KPI and financial ownership. No inferred legacy owner.</summary>
public static class QaCommercialOwnershipV2
{
    public const string MigrationId = "20261126211000_QaCommercialOwnershipV2";
    public static IReadOnlyList<QaCommercialOwnershipV1.Owned> Tables { get; } = new QaCommercialOwnershipV1.Owned[]
    {
        new(typeof(CostoEnvio), "CostosEnvio"),
        new(typeof(DashboardKpiConfiguracion), "DashboardKpiConfiguraciones"),
        new(typeof(MovimientoFinanciero), "MovimientosFinancieros"),
        new(typeof(RevisionFinanciera), "RevisionesFinancieras"),
    };
    public static IReadOnlyList<QaCommercialOwnershipV1.Edge> Relations { get; } = new QaCommercialOwnershipV1.Edge[]
    {
        new("Ventas", new[] { "EmpresaId", "CostoEnvioId" }, "CostosEnvio", new[] { "EmpresaId", "Id" }, "IX_QAC2_01", "FK_QAC2_01", false),
        new("Facturas", new[] { "EmpresaId", "CostoEnvioId" }, "CostosEnvio", new[] { "EmpresaId", "Id" }, "IX_QAC2_02", "FK_QAC2_02", false),
        new("MovimientosFinancieros", new[] { "EmpresaId", "CompraId" }, "Compras", new[] { "EmpresaId", "Id" }, "IX_QAC2_03", "FK_QAC2_03", false),
        new("MovimientosFinancieros", new[] { "EmpresaId", "VentaId" }, "Ventas", new[] { "EmpresaId", "Id" }, "IX_QAC2_04", "FK_QAC2_04", false),
        new("MovimientosFinancieros", new[] { "EmpresaId", "FacturaId" }, "Facturas", new[] { "EmpresaId", "Id" }, "IX_QAC2_05", "FK_QAC2_05", false),
    };
    public sealed record ScopedUnique(string Table, string Index, string[] Columns);
    public static IReadOnlyList<ScopedUnique> UniqueKeys { get; } = new ScopedUnique[]
    {
        new("CostosEnvio", "UX_CostosEnvio_PredeterminadoActivo", new[] { "PredeterminadoActivoUnico" }),
        new("DashboardKpiConfiguraciones", "UX_DashboardKpiConfiguraciones_Rol_MetricKey", new[] { "RolId", "MetricKey" }),
        new("DashboardKpiConfiguraciones", "UX_DashboardKpiConfiguraciones_Usuario_MetricKey", new[] { "UsuarioId", "MetricKey" }),
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
            if (count != 1) throw new InvalidOperationException("V2 owner column missing: " + table.Table);
            await VerifyIndexAsync(db, table.Table, QaCommercialOwnershipV1.OwnerIndex(table.Table), new[] { "EmpresaId", "Id" }, true, ct);
            await VerifyFkAsync(db, table.Table, QaCommercialOwnershipV1.OwnerForeignKey(table.Table), new[] { "EmpresaId" }, "Empresas", new[] { "Id" }, ct);
        }
        await VerifyIndexAsync(db, "DashboardKpiConfiguraciones", "IX_DashboardKpiConfiguraciones_UsuarioId", new[] { "UsuarioId" }, false, ct);
        await VerifyIndexAsync(db, "DashboardKpiConfiguraciones", "IX_DashboardKpiConfiguraciones_RolId", new[] { "RolId" }, false, ct);
        foreach (var key in UniqueKeys)
            await VerifyIndexAsync(db, key.Table, key.Index, new[] { "EmpresaId" }.Concat(key.Columns).ToArray(), true, ct);
        foreach (var edge in Relations)
        {
            await VerifyIndexAsync(db, edge.Child, edge.Index, edge.Columns, false, ct);
            await VerifyFkAsync(db, edge.Child, edge.ForeignKey, edge.Columns, edge.Parent, edge.Principal, ct);
        }
    }
    private static async Task VerifyIndexAsync(AppDbContext db, string table, string name, string[] cols, bool unique, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(SEQ_IN_INDEX,':',COLUMN_NAME,':',NON_UNIQUE) AS Value FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME={0} AND INDEX_NAME={1} ORDER BY SEQ_IN_INDEX",
            table, name).ToListAsync(ct);
        if (!rows.SequenceEqual(cols.Select((c,i) => $"{i+1}:{c}:{(unique ? 0 : 1)}")))
            throw new InvalidOperationException("V2 index mismatch: " + table + "." + name);
    }
    private static async Task VerifyFkAsync(AppDbContext db, string table, string name, string[] cols, string parent, string[] principal, CancellationToken ct)
    {
        var rows = await db.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(k.ORDINAL_POSITION,':',k.COLUMN_NAME,':',k.REFERENCED_TABLE_NAME,':',k.REFERENCED_COLUMN_NAME,':',r.DELETE_RULE) AS Value FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS r ON r.CONSTRAINT_SCHEMA=k.CONSTRAINT_SCHEMA AND r.TABLE_NAME=k.TABLE_NAME AND r.CONSTRAINT_NAME=k.CONSTRAINT_NAME WHERE k.TABLE_SCHEMA=DATABASE() AND k.TABLE_NAME={0} AND k.CONSTRAINT_NAME={1} ORDER BY k.ORDINAL_POSITION",
            table, name).ToListAsync(ct);
        if (!rows.SequenceEqual(cols.Select((c,i) => $"{i+1}:{c}:{parent}:{principal[i]}:RESTRICT")))
            throw new InvalidOperationException("V2 FK mismatch: " + table + "." + name);
    }
}
