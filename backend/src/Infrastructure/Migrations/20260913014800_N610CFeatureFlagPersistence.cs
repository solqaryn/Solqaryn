using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260913014800_N610CFeatureFlagPersistence")]
    public partial class N610CFeatureFlagPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Planes_Id_Codigo",
                table: "Planes",
                columns: new[] { "Id", "Codigo" });

            migrationBuilder.CreateTable(
                name: "PlanModulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    PlanCodigo = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModuloClave = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanModulos", x => x.Id);
                    table.CheckConstraint("CK_PlanModulos_PlanCodigo_NoVacio", "CHAR_LENGTH(TRIM(`PlanCodigo`)) > 0");
                    table.CheckConstraint("CK_PlanModulos_ModuloClave_NoVacio", "CHAR_LENGTH(TRIM(`ModuloClave`)) > 0");
                    table.ForeignKey(
                        name: "FK_PlanModulos_Planes_PlanId_PlanCodigo",
                        columns: x => new { x.PlanId, x.PlanCodigo },
                        principalTable: "Planes",
                        principalColumns: new[] { "Id", "Codigo" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlanModulos_PlanId_PlanCodigo",
                table: "PlanModulos",
                columns: new[] { "PlanId", "PlanCodigo" });

            migrationBuilder.CreateIndex(
                name: "UX_PlanModulos_PlanId_ModuloClave",
                table: "PlanModulos",
                columns: new[] { "PlanId", "ModuloClave" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlanModulos");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Planes_Id_Codigo",
                table: "Planes");
        }
    }
}
