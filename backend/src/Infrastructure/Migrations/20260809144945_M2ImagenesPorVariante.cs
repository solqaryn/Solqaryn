using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M2ImagenesPorVariante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "ProductoImagenes",
                type: "int",
                nullable: true);

            // VIRTUAL es indexable en MySQL 8.4 y evita reconstruir físicamente
            // ProductoImagenes, por lo que la FK histórica a Productos permanece
            // intacta durante toda la migración.
            migrationBuilder.AddColumn<string>(
                name: "PrincipalAmbitoKey",
                table: "ProductoImagenes",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true,
                computedColumnSql: "IF(EsPrincipal = 1, CONCAT(ProductoId, ':', IFNULL(ProductoVarianteId, 0)), NULL)",
                stored: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoImagenes_Producto_Variante_Orden",
                table: "ProductoImagenes",
                columns: new[] { "ProductoId", "ProductoVarianteId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoImagenes_ProductoVarianteId",
                table: "ProductoImagenes",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "UX_ProductoImagenes_Principal_Ambito",
                table: "ProductoImagenes",
                column: "PrincipalAmbitoKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
                table: "ProductoImagenes",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoImagenes_ProductoVariantes_ProductoVarianteId",
                table: "ProductoImagenes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoImagenes_Producto_Variante_Orden",
                table: "ProductoImagenes");

            migrationBuilder.DropIndex(
                name: "IX_ProductoImagenes_ProductoVarianteId",
                table: "ProductoImagenes");

            migrationBuilder.DropIndex(
                name: "UX_ProductoImagenes_Principal_Ambito",
                table: "ProductoImagenes");

            migrationBuilder.DropColumn(
                name: "PrincipalAmbitoKey",
                table: "ProductoImagenes");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "ProductoImagenes");
        }
    }
}
