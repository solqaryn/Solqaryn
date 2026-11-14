using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase3CostoEnvioEnVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvio",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostoEnvioId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvioMontoSnapshot",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioNombreSnapshot",
                table: "Ventas",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EnvioExonerado",
                table: "Ventas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteBruto",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteProductos",
                table: "Ventas",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoExoneracionEnvio",
                table: "Ventas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoEnvio",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioMontoSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioNombreSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "EnvioExonerado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ImporteBruto",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ImporteProductos",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoExoneracionEnvio",
                table: "Ventas");
        }
    }
}
