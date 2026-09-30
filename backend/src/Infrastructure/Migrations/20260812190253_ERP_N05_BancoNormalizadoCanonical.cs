using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ERP_N05_BancoNormalizadoCanonical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BancoCodigoSnapshot",
                table: "FacturaPagos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "BancoId",
                table: "FacturaPagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BancoNombreSnapshot",
                table: "FacturaPagos",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SwiftBic = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoNormalizado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, computedColumnSql: "LOWER(TRIM(Codigo))", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPagos_BancoId",
                table: "FacturaPagos",
                column: "BancoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_Estado",
                table: "Bancos",
                columns: new[] { "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_Nombre",
                table: "Bancos",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_Bancos_Codigo_Normalizado",
                table: "Bancos",
                column: "CodigoNormalizado",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaPagos_Bancos_BancoId",
                table: "FacturaPagos",
                column: "BancoId",
                principalTable: "Bancos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaPagos_Bancos_BancoId",
                table: "FacturaPagos");

            migrationBuilder.DropTable(
                name: "Bancos");

            migrationBuilder.DropIndex(
                name: "IX_FacturaPagos_BancoId",
                table: "FacturaPagos");

            migrationBuilder.DropColumn(
                name: "BancoCodigoSnapshot",
                table: "FacturaPagos");

            migrationBuilder.DropColumn(
                name: "BancoId",
                table: "FacturaPagos");

            migrationBuilder.DropColumn(
                name: "BancoNombreSnapshot",
                table: "FacturaPagos");
        }
    }
}
