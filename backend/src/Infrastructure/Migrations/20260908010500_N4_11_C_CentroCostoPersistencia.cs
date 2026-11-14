using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260908010500_N4_11_C_CentroCostoPersistencia")]
public partial class N4_11_C_CentroCostoPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CentrosCosto",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Codigo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Tipo = table.Column<int>(type: "int", nullable: false),
                SucursalId = table.Column<int>(type: "int", nullable: true),
                Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CodigoActivoUnico = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true, computedColumnSql: "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", stored: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CentrosCosto", x => x.Id);
                table.CheckConstraint("CK_CentrosCosto_Tipo", "`Tipo` IN (1, 2, 3, 4)");
                table.CheckConstraint("CK_CentrosCosto_Asociacion", "(`Tipo` = 1 AND `SucursalId` IS NOT NULL) OR (`Tipo` <> 1 AND `SucursalId` IS NULL)");
                table.ForeignKey(
                    name: "FK_CentrosCosto_Sucursales_SucursalId",
                    column: x => x.SucursalId,
                    principalTable: "Sucursales",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "UX_CentrosCosto_Codigo_Activo",
            table: "CentrosCosto",
            column: "CodigoActivoUnico",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CentrosCosto_SucursalId",
            table: "CentrosCosto",
            column: "SucursalId");

        migrationBuilder.CreateIndex(
            name: "IX_CentrosCosto_Tipo_Estado",
            table: "CentrosCosto",
            columns: new[] { "Tipo", "Activo", "Eliminado" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CentrosCosto");
    }
}
