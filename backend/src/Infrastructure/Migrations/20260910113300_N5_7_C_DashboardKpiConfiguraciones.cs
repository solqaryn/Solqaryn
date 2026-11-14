using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910113300_N5_7_C_DashboardKpiConfiguraciones")]
public partial class N5_7_C_DashboardKpiConfiguraciones : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DashboardKpiConfiguraciones",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                MetricKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UsuarioId = table.Column<int>(type: "int", nullable: true),
                RolId = table.Column<int>(type: "int", nullable: true),
                Habilitado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                EtiquetaVisible = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DashboardKpiConfiguraciones", x => x.Id);
                table.CheckConstraint(
                    "CK_DashboardKpiConfiguraciones_Owner",
                    "(`UsuarioId` IS NOT NULL AND `RolId` IS NULL) OR (`UsuarioId` IS NULL AND `RolId` IS NOT NULL)");
                table.ForeignKey(
                    name: "FK_DashboardKpiConfiguraciones_Roles_RolId",
                    column: x => x.RolId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DashboardKpiConfiguraciones_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "UX_DashboardKpiConfiguraciones_Rol_MetricKey",
            table: "DashboardKpiConfiguraciones",
            columns: new[] { "RolId", "MetricKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_DashboardKpiConfiguraciones_Usuario_MetricKey",
            table: "DashboardKpiConfiguraciones",
            columns: new[] { "UsuarioId", "MetricKey" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DashboardKpiConfiguraciones");
    }
}
