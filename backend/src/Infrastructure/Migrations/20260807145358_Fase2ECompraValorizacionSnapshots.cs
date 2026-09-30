using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase2ECompraValorizacionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoProductoAnteriorSnapshot",
                table: "CompraDetalles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoProductoNuevoSnapshot",
                table: "CompraDetalles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoVarianteAnteriorSnapshot",
                table: "CompraDetalles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoVarianteNuevoSnapshot",
                table: "CompraDetalles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockProductoAnteriorSnapshot",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockProductoNuevoSnapshot",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockVarianteAnteriorSnapshot",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockVarianteNuevoSnapshot",
                table: "CompraDetalles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoProductoAnteriorSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "CostoProductoNuevoSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "CostoVarianteAnteriorSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "CostoVarianteNuevoSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "StockProductoAnteriorSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "StockProductoNuevoSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "StockVarianteAnteriorSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "StockVarianteNuevoSnapshot",
                table: "CompraDetalles");
        }
    }
}
