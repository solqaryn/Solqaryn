using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration(QaCommercialOwnershipV1.MigrationId)]
public sealed class QaCommercialOwnershipMigrationV1 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // No IAM mutation, no DELETE, no inferred backfill.
        foreach (var table in QaCommercialOwnershipV1.Tables)
        {
            migrationBuilder.AddColumn<int>("EmpresaId", table.Table, type: "int", nullable: true);
            migrationBuilder.CreateIndex(QaCommercialOwnershipV1.OwnerIndex(table.Table), table.Table,
                new[] { "EmpresaId", "Id" }, unique: true);
            migrationBuilder.AddForeignKey(QaCommercialOwnershipV1.OwnerForeignKey(table.Table), table.Table,
                "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
        migrationBuilder.CreateIndex(QaCommercialOwnershipV1.VariantKey, "ProductoVariantes",
            new[] { "EmpresaId", "ProductoId", "Id" }, unique: true);
        foreach (var group in QaCommercialOwnershipV1.NaturalKeys.GroupBy(k => k.Table))
        {
            migrationBuilder.AddColumn<int>(QaCommercialOwnershipV1.ScopeColumn, group.Key,
                type: "int", nullable: false, computedColumnSql: "COALESCE(`EmpresaId`, 0)", stored: true);
            foreach (var key in group)
            {
                migrationBuilder.DropIndex(key.Index, key.Table);
                migrationBuilder.CreateIndex(key.Index, key.Table,
                    new[] { QaCommercialOwnershipV1.ScopeColumn, key.Column }, unique: true);
            }
        }
        foreach (var relation in QaCommercialOwnershipV1.Relations)
        {
            migrationBuilder.CreateIndex(relation.Index, relation.Child, relation.Columns);
            migrationBuilder.Sql(QaCommercialOwnershipV1.RelationDdl(relation));
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore the verified pre-migration backup; do not collapse tenant uniqueness.");
}
