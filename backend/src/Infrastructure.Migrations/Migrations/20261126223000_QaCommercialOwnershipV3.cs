using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration(QaCommercialOwnershipV3.MigrationId)]
public sealed class QaCommercialOwnershipMigrationV3 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        foreach (var table in QaCommercialOwnershipV3.Tables)
        {
            m.AddColumn<int>("EmpresaId", table.Table, type: "int", nullable: true);
            m.CreateIndex(QaCommercialOwnershipV3.OwnerIndex(table.Table), table.Table, new[] { "EmpresaId", "Id" }, unique: true);
            m.AddForeignKey(QaCommercialOwnershipV3.OwnerForeignKey(table.Table), table.Table, "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
        foreach (var key in QaCommercialOwnershipV3.ExtraKeys)
            m.CreateIndex(key.Index, key.Table, key.Columns, unique: true);
        foreach (var group in QaCommercialOwnershipV3.NaturalKeys.GroupBy(k => k.Table))
        {
            m.Sql($"ALTER TABLE `{group.Key}` ADD COLUMN {QaCommercialOwnershipV1.ScopeColumnDefinition}");
            foreach (var key in group)
            {
                m.DropIndex(key.Index, key.Table);
                m.CreateIndex(key.Index, key.Table, new[] { QaCommercialOwnershipV1.ScopeColumn }.Concat(key.Columns).ToArray(), unique: true);
            }
        }
        m.AddColumn<int>(QaCommercialOwnershipV3.GalleryColumn, "CategoriasGalerias", type: "int", nullable: true);
        foreach (var edge in QaCommercialOwnershipV3.Relations.Append(QaCommercialOwnershipV3.GalleryRelation))
        {
            m.CreateIndex(edge.Index, edge.Child, edge.Columns);
            m.Sql(QaCommercialOwnershipV1.RelationDdl(edge));
        }
        foreach (var trigger in QaCommercialOwnershipV3.Triggers)
            m.Sql(trigger.Ddl);
        foreach (var sql in QaCommercialOwnershipV3.DefaultStatements(false))
            m.Sql(sql);
    }
    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Restore verified pre-migration backup. Do not collapse tenant ownership.");
}
