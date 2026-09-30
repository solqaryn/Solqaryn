using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase4VariantesInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductoColorSnapshot",
                table: "VentaDetalles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProductoSkuSnapshot",
                table: "VentaDetalles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "VentaDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductoColorSnapshot",
                table: "MovimientosInventario",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProductoSkuSnapshot",
                table: "MovimientosInventario",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "MovimientosInventario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductoColorSnapshot",
                table: "CompraDetalles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ProductoSkuSnapshot",
                table: "CompraDetalles",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO ProductoVariantes
                    (ProductoId, ColorId, Sku, CodigoBarras, Cantidad, UmbralStockBajo,
                     Costo, Precio, Activo, Eliminado, FechaEliminacion, EliminadoPorUsuarioId,
                     FechaCreacion, FechaActualizacion, CreadoPorUsuarioId, CreadoPorNombreUsuario,
                     ActualizadoPorUsuarioId, ActualizadoPorNombreUsuario)
                SELECT
                    p.Id,
                    p.ColorId,
                    CONCAT('LEGACY-', LPAD(p.Id, 8, '0')),
                    NULL,
                    p.Cantidad,
                    p.UmbralStockBajo,
                    p.Costo,
                    p.Precio,
                    p.Activo,
                    0,
                    NULL,
                    NULL,
                    UTC_TIMESTAMP(6),
                    UTC_TIMESTAMP(6),
                    NULL,
                    'Migración Fase 4',
                    NULL,
                    NULL
                FROM Productos p
                WHERE p.ColorId IS NOT NULL
                  AND p.Eliminado = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ProductoVariantes pv
                      WHERE pv.ProductoId = p.Id
                        AND pv.Eliminado = 0
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_ProductoVarianteId",
                table: "VentaDetalles",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_ProductoVarianteId",
                table: "MovimientosInventario",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_ProductoVarianteId",
                table: "CompraDetalles",
                column: "ProductoVarianteId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalles_ProductoVariantes_ProductoVarianteId",
                table: "CompraDetalles",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_ProductoVariantes_ProductoVarianteId",
                table: "MovimientosInventario",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VentaDetalles_ProductoVariantes_ProductoVarianteId",
                table: "VentaDetalles",
                column: "ProductoVarianteId",
                principalTable: "ProductoVariantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalles_ProductoVariantes_ProductoVarianteId",
                table: "CompraDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_ProductoVariantes_ProductoVarianteId",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaDetalles_ProductoVariantes_ProductoVarianteId",
                table: "VentaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_VentaDetalles_ProductoVarianteId",
                table: "VentaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_ProductoVarianteId",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_CompraDetalles_ProductoVarianteId",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoColorSnapshot",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoSkuSnapshot",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoColorSnapshot",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "ProductoSkuSnapshot",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "ProductoColorSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoSkuSnapshot",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "CompraDetalles");
        }
    }
}
