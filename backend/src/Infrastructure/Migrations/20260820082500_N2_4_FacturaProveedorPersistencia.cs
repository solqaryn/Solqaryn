using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260820082500_N2_4_FacturaProveedorPersistencia")]
    public partial class N2_4_FacturaProveedorPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N24CGuard;
                CREATE TEMPORARY TABLE __N24CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N24C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N24CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Proveedores','OrdenesCompra','OrdenCompraDetalles','Productos','ProductoVariantes');
                INSERT INTO __N24CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('FacturasProveedor','FacturaProveedorDetalles');
                DROP TEMPORARY TABLE __N24CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "FacturasProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroFactura = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    ProveedorNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorDocumentoSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaEmisionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaVencimientoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReferenciaFiscal = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RegistradaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    RegistradaPorNombreSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAnulacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasProveedor", x => x.Id);
                    table.CheckConstraint("CK_FacturasProveedor_IdsValidos", "ProveedorId > 0 AND OrdenCompraId > 0");
                    table.CheckConstraint("CK_FacturasProveedor_EstadoValido", "Estado IN (1, 2, 3)");
                    table.CheckConstraint("CK_FacturasProveedor_MonedaIso3", "CHAR_LENGTH(TRIM(Moneda)) = 3");
                    table.CheckConstraint("CK_FacturasProveedor_FechasValidas", "FechaVencimientoUtc IS NULL OR FechaVencimientoUtc >= FechaEmisionUtc");
                    table.ForeignKey("FK_FacturasProveedor_Proveedores_ProveedorId", x => x.ProveedorId, "Proveedores", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_FacturasProveedor_OrdenesCompra_OrdenCompraId", x => x.OrdenCompraId, "OrdenesCompra", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FacturaProveedorDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    CantidadFacturada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DescuentoSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImpuestoSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Observacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaProveedorDetalles", x => x.Id);
                    table.CheckConstraint("CK_FacturaProveedorDetalles_IdsValidos", "OrdenCompraDetalleId > 0 AND ProductoId > 0 AND (ProductoVarianteId IS NULL OR ProductoVarianteId > 0)");
                    table.CheckConstraint("CK_FacturaProveedorDetalles_ImportesValidos", "CantidadFacturada > 0 AND PrecioUnitarioSnapshot >= 0 AND DescuentoSnapshot >= 0 AND ImpuestoSnapshot >= 0");
                    table.CheckConstraint("CK_FacturaProveedorDetalles_DescuentoValido", "DescuentoSnapshot <= CantidadFacturada * PrecioUnitarioSnapshot");
                    table.ForeignKey("FK_FacturaProveedorDetalles_FacturasProveedor_FacturaProveedorId", x => x.FacturaProveedorId, "FacturasProveedor", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_FacturaProveedorDetalles_OrdenCompraDetalles_OrdenCompraDetalleId", x => x.OrdenCompraDetalleId, "OrdenCompraDetalles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_FacturaProveedorDetalles_Productos_ProductoId", x => x.ProductoId, "Productos", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_FacturaProveedorDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_FacturasProveedor_Proveedor_NumeroFactura", "FacturasProveedor", new[] { "ProveedorId", "NumeroFactura" }, unique: true);
            migrationBuilder.CreateIndex("IX_FacturasProveedor_OrdenCompraId", "FacturasProveedor", "OrdenCompraId");
            migrationBuilder.CreateIndex("IX_FacturasProveedor_Estado_FechaEmision", "FacturasProveedor", new[] { "Estado", "FechaEmisionUtc" });
            migrationBuilder.CreateIndex("IX_FacturasProveedor_FechaVencimiento", "FacturasProveedor", "FechaVencimientoUtc");
            migrationBuilder.CreateIndex("IX_FacturaProveedorDetalles_FacturaProveedorId", "FacturaProveedorDetalles", "FacturaProveedorId");
            migrationBuilder.CreateIndex("UX_FacturaProveedorDetalles_Factura_OrdenDetalle", "FacturaProveedorDetalles", new[] { "FacturaProveedorId", "OrdenCompraDetalleId" }, unique: true);
            migrationBuilder.CreateIndex("IX_FacturaProveedorDetalles_OrdenCompraDetalleId", "FacturaProveedorDetalles", "OrdenCompraDetalleId");
            migrationBuilder.CreateIndex("IX_FacturaProveedorDetalles_ProductoId", "FacturaProveedorDetalles", "ProductoId");
            migrationBuilder.CreateIndex("IX_FacturaProveedorDetalles_ProductoVarianteId", "FacturaProveedorDetalles", "ProductoVarianteId");
            migrationBuilder.CreateIndex("IX_FacturaProveedorDetalles_Producto_Variante", "FacturaProveedorDetalles", new[] { "ProductoId", "ProductoVarianteId" });

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N24CPostGuard;
                CREATE TEMPORARY TABLE __N24CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N24C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N24CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('FacturasProveedor','FacturaProveedorDetalles');
                INSERT INTO __N24CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'FacturasProveedor'
                   AND index_name = 'UX_FacturasProveedor_Proveedor_NumeroFactura'
                   AND non_unique = 0;
                INSERT INTO __N24CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'FacturaProveedorDetalles'
                   AND index_name = 'UX_FacturaProveedorDetalles_Factura_OrdenDetalle'
                   AND non_unique = 0;
                DROP TEMPORARY TABLE __N24CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FacturaProveedorDetalles");
            migrationBuilder.DropTable(name: "FacturasProveedor");
        }
    }
}
