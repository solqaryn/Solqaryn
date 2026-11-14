using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260905093000_N4_9_PeriodoContablePersistencia")]
public partial class N4_9_PeriodoContablePersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PeriodosContables",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Estado = table.Column<int>(type: "int", nullable: false),
                CerradoEnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PeriodosContables", x => x.Id);
                table.CheckConstraint("CK_PeriodosContables_Rango", "`FechaFin` >= `FechaInicio`");
                table.CheckConstraint("CK_PeriodosContables_Estado", "`Estado` IN (1, 2)");
                table.CheckConstraint("CK_PeriodosContables_Cierre", "(`Estado` = 1 AND `CerradoEnUtc` IS NULL) OR (`Estado` = 2 AND `CerradoEnUtc` IS NOT NULL)");
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_PeriodosContables_Estado_Rango",
            table: "PeriodosContables",
            columns: new[] { "Estado", "FechaInicio", "FechaFin" });

        migrationBuilder.CreateIndex(
            name: "UX_PeriodosContables_Rango",
            table: "PeriodosContables",
            columns: new[] { "FechaInicio", "FechaFin" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PeriodosContables");
    }
}
