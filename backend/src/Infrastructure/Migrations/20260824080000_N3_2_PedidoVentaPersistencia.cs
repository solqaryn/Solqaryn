using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824080000_N3_2_PedidoVentaPersistencia")]
    public partial class N3_2_PedidoVentaPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N32CGuard;
                CREATE TEMPORARY TABLE __N32CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N32C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N32CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Cotizaciones', 'Clientes', 'Productos', 'ProductoVariantes');
                INSERT INTO __N32CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('PedidosVenta', 'PedidoVentaDetalles');
                DROP TEMPORARY TABLE __N32CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "PedidosVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CotizacionId = table.Column<int>(type: "int", nullable: true),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    ClienteNombreSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClienteDocumentoSnapshot = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyFingerprint = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ConfirmadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AnuladoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_PedidosVenta", x => x.Id);
                    table.CheckConstraint("CK_PedidosVenta_Estado", "`Estado` IN (1, 2, 3)");
                    table.CheckConstraint(
                        "CK_PedidosVenta_Idempotencia_Atomica",
                        "((`IdempotencyKey` IS NULL AND `IdempotencyFingerprint` IS NULL) OR (`IdempotencyKey` IS NOT NULL AND `IdempotencyFingerprint` IS NOT NULL))");
                    table.CheckConstraint(
                        "CK_PedidosVenta_IdempotencyFingerprint_Sha256",
                        "(`IdempotencyFingerprint` IS NULL OR (CHAR_LENGTH(`IdempotencyFingerprint`) = 64 AND `IdempotencyFingerprint` REGEXP '^[0-9a-f]{64}$'))");
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Cotizaciones_CotizacionId",
                        column: x => x.CotizacionId,
                        principalTable: "Cotizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PedidoVentaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoVentaId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoNombreSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_PedidoVentaDetalles", x => x.Id);
                    table.CheckConstraint("CK_PedidoVentaDetalles_Cantidad_Positiva", "`Cantidad` > 0");
                    table.CheckConstraint("CK_PedidoVentaDetalles_PrecioUnitario_NoNegativo", "`PrecioUnitario` >= 0");
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalles_PedidosVenta_PedidoVentaId",
                        column: x => x.PedidoVentaId,
                        principalTable: "PedidosVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalles_ProductoVariantes_ProductoVarianteId",
                        column: x => x.ProductoVarianteId,
                        principalTable: "ProductoVariantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_PedidosVenta_CotizacionId",
                table: "PedidosVenta",
                column: "CotizacionId",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "UX_PedidosVenta_IdempotencyKey",
                table: "PedidosVenta",
                column: "IdempotencyKey",
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_Cliente_Estado",
                table: "PedidosVenta",
                columns: new[] { "ClienteId", "Estado" });
            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalles_PedidoVentaId",
                table: "PedidoVentaDetalles",
                column: "PedidoVentaId");
            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalles_ProductoId",
                table: "PedidoVentaDetalles",
                column: "ProductoId");
            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalles_ProductoVarianteId",
                table: "PedidoVentaDetalles",
                column: "ProductoVarianteId");

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N32CPostGuard;
                CREATE TEMPORARY TABLE __N32CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N32C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N32CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('PedidosVenta', 'PedidoVentaDetalles');
                INSERT INTO __N32CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND constraint_name IN
                       ('FK_PedidosVenta_Cotizaciones_CotizacionId',
                        'FK_PedidosVenta_Clientes_ClienteId',
                        'FK_PedidoVentaDetalles_PedidosVenta_PedidoVentaId',
                        'FK_PedidoVentaDetalles_Productos_ProductoId',
                        'FK_PedidoVentaDetalles_ProductoVariantes_ProductoVarianteId');
                DROP TEMPORARY TABLE __N32CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N32CDownGuard;
                CREATE TEMPORARY TABLE __N32CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N32C_DownGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N32CDownGuard (Id, Violaciones)
                SELECT 1,
                       (SELECT COUNT(*) FROM PedidosVenta) +
                       (SELECT COUNT(*) FROM PedidoVentaDetalles);
                DROP TEMPORARY TABLE __N32CDownGuard;
                """);

            migrationBuilder.DropTable(name: "PedidoVentaDetalles");
            migrationBuilder.DropTable(name: "PedidosVenta");
        }
    }
}
