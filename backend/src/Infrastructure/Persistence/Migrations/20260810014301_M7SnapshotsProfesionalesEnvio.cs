using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7SnapshotsProfesionalesEnvio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioCiudadSnapshot",
                table: "Ventas",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioDepartamentoSnapshot",
                table: "Ventas",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioModalidadSnapshot",
                table: "Ventas",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioZonaSnapshot",
                table: "Ventas",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "CostoEnvioNombreSnapshot",
                table: "Facturas",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioCiudadSnapshot",
                table: "Facturas",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioDepartamentoSnapshot",
                table: "Facturas",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioModalidadSnapshot",
                table: "Facturas",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioZonaSnapshot",
                table: "Facturas",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoEnvioCiudadSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioDepartamentoSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioModalidadSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioZonaSnapshot",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioCiudadSnapshot",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioDepartamentoSnapshot",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioModalidadSnapshot",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioZonaSnapshot",
                table: "Facturas");

            migrationBuilder.AlterColumn<string>(
                name: "CostoEnvioNombreSnapshot",
                table: "Facturas",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
