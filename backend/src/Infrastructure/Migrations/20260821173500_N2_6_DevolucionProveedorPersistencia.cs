using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260821173500_N2_6_DevolucionProveedorPersistencia")]
    public partial class N2_6_DevolucionProveedorPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N26CGuard;
                CREATE TEMPORARY TABLE __N26CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N26C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N26CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 10 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Proveedores','OrdenesCompra','RecepcionesCompra','FacturasProveedor','RecepcionCompraDetalles','OrdenCompraDetalles','Productos','ProductoVariantes','Almacenes','UbicacionesAlmacen');
                INSERT INTO __N26CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('DevolucionesProveedor','DevolucionProveedorDetalles');
                DROP TEMPORARY TABLE __N26CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "DevolucionesProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroDevolucion = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    RecepcionCompraId = table.Column<int>(type: "int", nullable: false),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: false),
                    ProveedorNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaConfirmacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfirmadaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ConfirmadaPorNombreSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DevolucionesProveedor", x => x.Id);
                    table.CheckConstraint("CK_DevolucionesProveedor_IdsValidos", "ProveedorId > 0 AND OrdenCompraId > 0 AND RecepcionCompraId > 0 AND FacturaProveedorId > 0");
                    table.CheckConstraint("CK_DevolucionesProveedor_EstadoValido", "Estado IN (1, 2, 3)");
                    table.CheckConstraint("CK_DevolucionesProveedor_MonedaIso3", "CHAR_LENGTH(TRIM(Moneda)) = 3");
                    table.CheckConstraint("CK_DevolucionesProveedor_IdempotenciaAtomica", "(IdempotencyKey IS NULL AND IdempotencyFingerprint IS NULL) OR (IdempotencyKey IS NOT NULL AND IdempotencyFingerprint IS NOT NULL)");
                    table.ForeignKey("FK_DevolucionesProveedor_Proveedores_ProveedorId", x => x.ProveedorId, "Proveedores", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionesProveedor_OrdenesCompra_OrdenCompraId", x => x.OrdenCompraId, "OrdenesCompra", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionesProveedor_RecepcionesCompra_RecepcionCompraId", x => x.RecepcionCompraId, "RecepcionesCompra", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionesProveedor_FacturasProveedor_FacturaProveedorId", x => x.FacturaProveedorId, "FacturasProveedor", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DevolucionProveedorDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DevolucionProveedorId = table.Column<int>(type: "int", nullable: false),
                    RecepcionCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImpuestoUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionProveedorDetalles", x => x.Id);
                    table.CheckConstraint("CK_DevolucionProveedorDetalles_IdsValidos", "DevolucionProveedorId > 0 AND RecepcionCompraDetalleId > 0 AND OrdenCompraDetalleId > 0 AND ProductoId > 0 AND AlmacenId > 0");
                    table.CheckConstraint("CK_DevolucionProveedorDetalles_CantidadPositiva", "Cantidad > 0");
                    table.CheckConstraint("CK_DevolucionProveedorDetalles_CostosNoNegativos", "CostoUnitarioSnapshot >= 0 AND ImpuestoUnitarioSnapshot >= 0");
                    table.ForeignKey("FK_DevolucionProveedorDetalles_DevolucionesProveedor_DevolucionProveedorId", x => x.DevolucionProveedorId, "DevolucionesProveedor", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_RecepcionCompraDetalles_RecepcionCompraDetalleId", x => x.RecepcionCompraDetalleId, "RecepcionCompraDetalles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_OrdenCompraDetalles_OrdenCompraDetalleId", x => x.OrdenCompraDetalleId, "OrdenCompraDetalles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_Productos_ProductoId", x => x.ProductoId, "Productos", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionProveedorDetalles_UbicacionesAlmacen_UbicacionAlmacenId", x => x.UbicacionAlmacenId, "UbicacionesAlmacen", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_DevolucionesProveedor_NumeroDevolucion", "DevolucionesProveedor", "NumeroDevolucion", unique: true);
            migrationBuilder.CreateIndex("IX_DevolucionesProveedor_Proveedor_Estado", "DevolucionesProveedor", new[] { "ProveedorId", "Estado" });
            migrationBuilder.CreateIndex("IX_DevolucionesProveedor_OrdenCompraId", "DevolucionesProveedor", "OrdenCompraId");
            migrationBuilder.CreateIndex("IX_DevolucionesProveedor_RecepcionCompraId", "DevolucionesProveedor", "RecepcionCompraId");
            migrationBuilder.CreateIndex("IX_DevolucionesProveedor_FacturaProveedorId", "DevolucionesProveedor", "FacturaProveedorId");
            migrationBuilder.CreateIndex("UX_DevolucionesProveedor_IdempotencyKey", "DevolucionesProveedor", "IdempotencyKey", unique: true);

            migrationBuilder.CreateIndex("UX_DevolucionProveedorDetalles_Devolucion_RecepcionDetalle", "DevolucionProveedorDetalles", new[] { "DevolucionProveedorId", "RecepcionCompraDetalleId" }, unique: true);
            migrationBuilder.CreateIndex("IX_DevolucionProveedorDetalles_OrdenCompraDetalleId", "DevolucionProveedorDetalles", "OrdenCompraDetalleId");
            migrationBuilder.CreateIndex("IX_DevolucionProveedorDetalles_ProductoId", "DevolucionProveedorDetalles", "ProductoId");
            migrationBuilder.CreateIndex("IX_DevolucionProveedorDetalles_ProductoVarianteId", "DevolucionProveedorDetalles", "ProductoVarianteId");
            migrationBuilder.CreateIndex("IX_DevolucionProveedorDetalles_AlmacenId", "DevolucionProveedorDetalles", "AlmacenId");
            migrationBuilder.CreateIndex("IX_DevolucionProveedorDetalles_UbicacionAlmacenId", "DevolucionProveedorDetalles", "UbicacionAlmacenId");

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N26CPostGuard;
                CREATE TEMPORARY TABLE __N26CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N26C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N26CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('DevolucionesProveedor','DevolucionProveedorDetalles');
                INSERT INTO __N26CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'DevolucionesProveedor'
                   AND index_name = 'UX_DevolucionesProveedor_NumeroDevolucion'
                   AND non_unique = 0;
                INSERT INTO __N26CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'DevolucionProveedorDetalles'
                   AND index_name = 'UX_DevolucionProveedorDetalles_Devolucion_RecepcionDetalle'
                   AND non_unique = 0;
                DROP TEMPORARY TABLE __N26CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DevolucionProveedorDetalles");
            migrationBuilder.DropTable(name: "DevolucionesProveedor");
        }
    }
}
