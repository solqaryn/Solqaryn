using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase8FacturacionPagosCostosEnvioVariantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoInterno",
                table: "Facturas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CondicionPago",
                table: "Facturas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvio",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostoEnvioId",
                table: "Facturas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostoEnvioMontoSnapshot",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostoEnvioNombreSnapshot",
                table: "Facturas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EnvioExonerado",
                table: "Facturas",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Facturas",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteBruto",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "Facturas",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MotivoExoneracionEnvio",
                table: "Facturas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "Facturas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "SaldoPendiente",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPagado",
                table: "Facturas",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Impuesto",
                table: "FacturaDetalles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "FacturaDetalles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProductoVarianteId",
                table: "FacturaDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLinea",
                table: "FacturaDetalles",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VarianteColor",
                table: "FacturaDetalles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VarianteSku",
                table: "FacturaDetalles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CostosEnvio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VigenteDesde = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VigenteHasta = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    EsPredeterminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CostosEnvio", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FacturaPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MetodoPago = table.Column<int>(type: "int", nullable: false),
                    Referencia = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Anulado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AnuladoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_FacturaPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacturaPagos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductoVariantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ColorId = table.Column<int>(type: "int", nullable: true),
                    Sku = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoBarras = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    UmbralStockBajo = table.Column<int>(type: "int", nullable: false),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ProductoVariantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoVariantes_CatalogosProducto_ColorId",
                        column: x => x.ColorId,
                        principalTable: "CatalogosProducto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductoVariantes_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "CostosEnvio",
                columns: new[] { "Id", "Nombre", "Descripcion", "Monto", "Prioridad", "EsPredeterminado", "Activo", "Eliminado", "FechaCreacion", "FechaActualizacion" },
                values: new object[] { 1, "Envío estándar", "Costo de envío predeterminado de Storefront", 80.00m, 1, true, true, false, new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_CostosEnvio_Activo_EsPredeterminado",
                table: "CostosEnvio",
                columns: new[] { "Activo", "EsPredeterminado" });

            migrationBuilder.CreateIndex(
                name: "IX_CostosEnvio_Nombre",
                table: "CostosEnvio",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaPagos_FacturaId_FechaPago",
                table: "FacturaPagos",
                columns: new[] { "FacturaId", "FechaPago" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_CodigoBarras",
                table: "ProductoVariantes",
                column: "CodigoBarras",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ColorId",
                table: "ProductoVariantes",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_ProductoId_ColorId",
                table: "ProductoVariantes",
                columns: new[] { "ProductoId", "ColorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoVariantes_Sku",
                table: "ProductoVariantes",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostosEnvio");

            migrationBuilder.DropTable(
                name: "FacturaPagos");

            migrationBuilder.DropTable(
                name: "ProductoVariantes");

            migrationBuilder.DropColumn(
                name: "CodigoInterno",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CondicionPago",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioMontoSnapshot",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CostoEnvioNombreSnapshot",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "EnvioExonerado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "ImporteBruto",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "MotivoExoneracionEnvio",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SaldoPendiente",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "TotalPagado",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "Impuesto",
                table: "FacturaDetalles");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "FacturaDetalles");

            migrationBuilder.DropColumn(
                name: "ProductoVarianteId",
                table: "FacturaDetalles");

            migrationBuilder.DropColumn(
                name: "TotalLinea",
                table: "FacturaDetalles");

            migrationBuilder.DropColumn(
                name: "VarianteColor",
                table: "FacturaDetalles");

            migrationBuilder.DropColumn(
                name: "VarianteSku",
                table: "FacturaDetalles");
        }
    }
}
