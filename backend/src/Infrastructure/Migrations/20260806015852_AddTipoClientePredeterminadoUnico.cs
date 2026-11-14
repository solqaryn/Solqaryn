using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoClientePredeterminadoUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EsPredeterminadoUnico",
                table: "TipoClientes",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                computedColumnSql: "IF(EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0, 'DEFAULT', NULL)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TipoClientes_EsPredeterminadoUnico",
                table: "TipoClientes",
                column: "EsPredeterminadoUnico",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TipoClientes_EsPredeterminadoUnico",
                table: "TipoClientes");

            migrationBuilder.DropColumn(
                name: "EsPredeterminadoUnico",
                table: "TipoClientes");
        }
    }
}
