using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration(QaCommercialOwnershipV2.MigrationId)]
public sealed class QaCommercialOwnershipMigrationV2 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        foreach (var table in QaCommercialOwnershipV2.Tables)
        {
            m.AddColumn<int>("EmpresaId", table.Table, type: "int", nullable: true);
            m.CreateIndex(QaCommercialOwnershipV1.OwnerIndex(table.Table), table.Table, new[] { "EmpresaId", "Id" }, unique: true);
            m.AddForeignKey(QaCommercialOwnershipV1.OwnerForeignKey(table.Table), table.Table, "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
        // Keep the original single-column FK support before replacing the unique indexes.
        m.CreateIndex("IX_DashboardKpiConfiguraciones_UsuarioId", "DashboardKpiConfiguraciones", "UsuarioId");
        m.CreateIndex("IX_DashboardKpiConfiguraciones_RolId", "DashboardKpiConfiguraciones", "RolId");
        foreach (var key in QaCommercialOwnershipV2.UniqueKeys)
        {
            m.DropIndex(key.Index, key.Table);
            m.CreateIndex(key.Index, key.Table, new[] { "EmpresaId" }.Concat(key.Columns).ToArray(), unique: true);
        }
        foreach (var edge in QaCommercialOwnershipV2.Relations)
        {
            m.CreateIndex(edge.Index, edge.Child, edge.Columns);
            m.Sql(QaCommercialOwnershipV1.RelationDdl(edge));
        }
    }
    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Restore verified pre-migration backup. Do not collapse tenant ownership.");
}
