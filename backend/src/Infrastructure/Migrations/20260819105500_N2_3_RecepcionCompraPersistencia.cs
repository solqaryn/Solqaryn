using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260819105500_N2_3_RecepcionCompraPersistencia")]
    public partial class N2_3_RecepcionCompraPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N23CGuard;
                CREATE TEMPORARY TABLE __N23CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N23C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N23CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 6 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('OrdenesCompra','OrdenCompraDetalles','Productos','ProductoVariantes','Almacenes','UbicacionesAlmacen');
                INSERT INTO __N23CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('RecepcionesCompra','RecepcionCompraDetalles');
                INSERT INTO __N23CGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.key_column_usage
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id'
                   AND ((ordinal_position = 1 AND column_name = 'AlmacenId')
                     OR (ordinal_position = 2 AND column_name = 'Id'));
                DROP TEMPORARY TABLE __N23CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "RecepcionesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroRecepcion = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaRecepcionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RecibidaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    RecibidaPorNombreSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_RecepcionesCompra", x => x.Id);
                    table.CheckConstraint(
                        "CK_RecepcionesCompra_IdempotenciaAtomica",
                        "(IdempotencyKey IS NULL AND IdempotencyFingerprint IS NULL) OR (IdempotencyKey IS NOT NULL AND CHAR_LENGTH(TRIM(IdempotencyKey)) > 0 AND IdempotencyFingerprint IS NOT NULL AND CHAR_LENGTH(IdempotencyFingerprint) = 64)");
                    table.ForeignKey(
                        name: "FK_RecepcionesCompra_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RecepcionCompraDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RecepcionCompraId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    UbicacionAlmacenIdUnica = table.Column<int>(type: "int", nullable: false, computedColumnSql: "IFNULL(UbicacionAlmacenId, 0)", stored: true),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadDanada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadFaltante = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadSobrante = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostoUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_RecepcionCompraDetalles", x => x.Id);
                    table.CheckConstraint("CK_RecepcionCompraDetalles_CantidadesNoNegativas", "CantidadRecibida >= 0 AND CantidadDanada >= 0 AND CantidadFaltante >= 0 AND CantidadSobrante >= 0");
                    table.CheckConstraint("CK_RecepcionCompraDetalles_BalanceFisico", "CantidadDanada + CantidadSobrante <= CantidadRecibida");
                    table.CheckConstraint("CK_RecepcionCompraDetalles_ActividadFisica", "CantidadRecibida > 0 OR CantidadFaltante > 0");
                    table.CheckConstraint("CK_RecepcionCompraDetalles_CostoNoNegativo", "CostoUnitarioSnapshot >= 0");
                    table.ForeignKey("FK_RecepcionCompraDetalles_RecepcionesCompra_RecepcionCompraId", x => x.RecepcionCompraId, "RecepcionesCompra", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_RecepcionCompraDetalles_OrdenCompraDetalles_OrdenCompraDetalleId", x => x.OrdenCompraDetalleId, "OrdenCompraDetalles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_RecepcionCompraDetalles_Productos_ProductoId", x => x.ProductoId, "Productos", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_RecepcionCompraDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_RecepcionCompraDetalles_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepcionCompraDetalles_Ubicacion_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionAlmacenId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_RecepcionesCompra_NumeroRecepcion", "RecepcionesCompra", "NumeroRecepcion", unique: true);
            migrationBuilder.CreateIndex("UX_RecepcionesCompra_IdempotencyKey", "RecepcionesCompra", "IdempotencyKey", unique: true);
            migrationBuilder.CreateIndex("IX_RecepcionesCompra_OrdenCompra_Estado", "RecepcionesCompra", new[] { "OrdenCompraId", "Estado" });
            migrationBuilder.CreateIndex("IX_RecepcionesCompra_FechaRecepcionUtc", "RecepcionesCompra", "FechaRecepcionUtc");
            migrationBuilder.CreateIndex("IX_RecepcionCompraDetalles_OrdenCompraDetalleId", "RecepcionCompraDetalles", "OrdenCompraDetalleId");
            migrationBuilder.CreateIndex("IX_RecepcionCompraDetalles_Producto_Variante", "RecepcionCompraDetalles", new[] { "ProductoId", "ProductoVarianteId" });
            migrationBuilder.CreateIndex("IX_RecepcionCompraDetalles_ProductoVarianteId", "RecepcionCompraDetalles", "ProductoVarianteId");
            migrationBuilder.CreateIndex("IX_RecepcionCompraDetalles_AlmacenId", "RecepcionCompraDetalles", "AlmacenId");
            migrationBuilder.CreateIndex("IX_RecepcionCompraDetalles_AlmacenId_UbicacionAlmacenId", "RecepcionCompraDetalles", new[] { "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex(
                name: "UX_RecepcionCompraDetalles_Recepcion_Linea_Almacen_Ubicacion",
                table: "RecepcionCompraDetalles",
                columns: new[] { "RecepcionCompraId", "OrdenCompraDetalleId", "AlmacenId", "UbicacionAlmacenIdUnica" },
                unique: true);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N23CPostGuard;
                CREATE TEMPORARY TABLE __N23CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N23C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N23CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('RecepcionesCompra','RecepcionCompraDetalles');
                INSERT INTO __N23CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'RecepcionesCompra'
                   AND index_name = 'UX_RecepcionesCompra_NumeroRecepcion'
                   AND non_unique = 0;
                INSERT INTO __N23CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'RecepcionCompraDetalles'
                   AND index_name = 'UX_RecepcionCompraDetalles_Recepcion_Linea_Almacen_Ubicacion';
                DROP TEMPORARY TABLE __N23CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecepcionCompraDetalles");
            migrationBuilder.DropTable(name: "RecepcionesCompra");
        }
    }
}
