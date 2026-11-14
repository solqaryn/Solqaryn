using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FaseInsumosAdministrativosBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoInventario",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Causa",
                table: "MovimientosInventario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill histórico no destructivo. Los movimientos inequívocos se
            // clasifican según su referencia y dirección; los casos ambiguos
            // permanecen en NoEspecificada (0) para no inventar trazabilidad.
            migrationBuilder.Sql(@"
                UPDATE MovimientosInventario
                SET Causa = CASE
                    WHEN ReferenciaTipo = 'Compra' AND Tipo = 'Entrada' THEN 1
                    WHEN ReferenciaTipo = 'Venta' AND Tipo = 'Salida' THEN 2
                    WHEN Tipo = 'Ajuste' THEN 4
                    WHEN ReferenciaTipo = 'Compra' AND Tipo = 'Reversion' THEN 5
                    WHEN ReferenciaTipo = 'Venta' AND Tipo = 'Reversion' THEN 6
                    ELSE 0
                END;");

            migrationBuilder.CreateTable(
                name: "ConsumosInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroConsumo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaConsumo = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AreaDestino = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfirmadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ConfirmadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AnuladoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotivoAnulacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
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
                    table.PrimaryKey("PK_ConsumosInsumos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConsumoInsumoDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConsumoInsumoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CostoUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostoTotalSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NombreSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkuSnapshot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumoInsumoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumoInsumoDetalles_ConsumosInsumos_ConsumoInsumoId",
                        column: x => x.ConsumoInsumoId,
                        principalTable: "ConsumosInsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumoInsumoDetalles_ProductoVariantes_ProductoVarianteId",
                        column: x => x.ProductoVarianteId,
                        principalTable: "ProductoVariantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumoInsumoDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_TipoInventario_Estado",
                table: "Productos",
                columns: new[] { "TipoInventario", "Eliminado", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Causa_Fecha",
                table: "MovimientosInventario",
                columns: new[] { "Causa", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoInsumoDetalles_ConsumoInsumoId",
                table: "ConsumoInsumoDetalles",
                column: "ConsumoInsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoInsumoDetalles_ProductoId_ProductoVarianteId",
                table: "ConsumoInsumoDetalles",
                columns: new[] { "ProductoId", "ProductoVarianteId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoInsumoDetalles_ProductoVarianteId",
                table: "ConsumoInsumoDetalles",
                column: "ProductoVarianteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosInsumos_Eliminado",
                table: "ConsumosInsumos",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosInsumos_Estado_FechaConsumo",
                table: "ConsumosInsumos",
                columns: new[] { "Estado", "FechaConsumo" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumosInsumos_NumeroConsumo",
                table: "ConsumosInsumos",
                column: "NumeroConsumo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsumoInsumoDetalles");

            migrationBuilder.DropTable(
                name: "ConsumosInsumos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_TipoInventario_Estado",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_Causa_Fecha",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "TipoInventario",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Causa",
                table: "MovimientosInventario");
        }
    }
}
