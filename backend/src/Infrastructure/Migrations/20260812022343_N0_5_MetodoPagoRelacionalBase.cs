using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class N0_5_MetodoPagoRelacionalBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MetodoPagoId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPagoId",
                table: "MovimientosFinancieros",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoPagoId",
                table: "FacturaPagos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MetodosPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    RequiereReferencia = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RequiereBanco = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    PermiteCambio = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Metadata = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoNormalizado = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, computedColumnSql: "LOWER(TRIM(Codigo))", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetodosPago", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_MetodoPagoId",
                table: "Ventas",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFinancieros_MetodoPagoId",
                table: "MovimientosFinancieros",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPagos_MetodoPagoId",
                table: "FacturaPagos",
                column: "MetodoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_Estado_Orden",
                table: "MetodosPago",
                columns: new[] { "Activo", "Eliminado", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_MetodosPago_Nombre",
                table: "MetodosPago",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_MetodosPago_Codigo_Normalizado",
                table: "MetodosPago",
                column: "CodigoNormalizado",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturaPagos_MetodosPago_MetodoPagoId",
                table: "FacturaPagos",
                column: "MetodoPagoId",
                principalTable: "MetodosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosFinancieros_MetodosPago_MetodoPagoId",
                table: "MovimientosFinancieros",
                column: "MetodoPagoId",
                principalTable: "MetodosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_MetodosPago_MetodoPagoId",
                table: "Ventas",
                column: "MetodoPagoId",
                principalTable: "MetodosPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FacturaPagos_MetodosPago_MetodoPagoId",
                table: "FacturaPagos");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosFinancieros_MetodosPago_MetodoPagoId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_MetodosPago_MetodoPagoId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "MetodosPago");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_MetodoPagoId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosFinancieros_MetodoPagoId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropIndex(
                name: "IX_FacturaPagos_MetodoPagoId",
                table: "FacturaPagos");

            migrationBuilder.DropColumn(
                name: "MetodoPagoId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MetodoPagoId",
                table: "MovimientosFinancieros");

            migrationBuilder.DropColumn(
                name: "MetodoPagoId",
                table: "FacturaPagos");
        }
    }
}
