using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M7CostosEnvioProfesionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                table: "CostosEnvio",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "CostosEnvio",
                type: "varchar(120)",
                maxLength: 120,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Modalidad",
                table: "CostosEnvio",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Zona",
                table: "CostosEnvio",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CostosEnvio_Resolucion",
                table: "CostosEnvio",
                columns: new[] { "Departamento", "Ciudad", "Zona", "Modalidad", "Activo", "Prioridad" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CostosEnvio_Resolucion",
                table: "CostosEnvio");

            migrationBuilder.DropColumn(
                name: "Ciudad",
                table: "CostosEnvio");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "CostosEnvio");

            migrationBuilder.DropColumn(
                name: "Modalidad",
                table: "CostosEnvio");

            migrationBuilder.DropColumn(
                name: "Zona",
                table: "CostosEnvio");
        }
    }
}
