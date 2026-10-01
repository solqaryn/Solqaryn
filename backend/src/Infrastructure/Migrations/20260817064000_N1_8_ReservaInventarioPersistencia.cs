using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260817064000_N1_8_ReservaInventarioPersistencia")]
    public partial class N1_8_ReservaInventarioPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N18CGuard;
                CREATE TEMPORARY TABLE __N18CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N18C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N18CGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('ReservasInventario','ReservaInventarioDetalles');
                INSERT INTO __N18CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Ventas','ProductoVariantes','Almacenes','UbicacionesAlmacen');
                INSERT INTO __N18CGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id'
                   AND constraint_type = 'UNIQUE';
                DROP TEMPORARY TABLE __N18CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "ReservasInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Numero = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaActivacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ActivadaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaConsumo = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsumidaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaLiberacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LiberadaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoLiberacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaExpiracionAplicada = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiradaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    FechaCancelacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CanceladaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservasInventario", x => x.Id);
                    table.ForeignKey("FK_ReservasInventario_Ventas_VentaId", x => x.VentaId, "Ventas", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservaInventarioDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReservaInventarioId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    CantidadReservada = table.Column<int>(type: "int", nullable: false),
                    CantidadConsumida = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    UbicacionNormalizada = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(`UbicacionAlmacenId`, 0)", stored: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaInventarioDetalles", x => x.Id);
                    table.ForeignKey("FK_ReservaInventarioDetalles_ReservasInventario_ReservaInventarioId", x => x.ReservaInventarioId, "ReservasInventario", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ReservaDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ReservaDetalles_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaDetalles_Ubicacion_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionAlmacenId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex("UX_ReservasInventario_Numero", "ReservasInventario", "Numero", unique: true);
            migrationBuilder.CreateIndex("IX_ReservasInventario_Estado_Expiracion", "ReservasInventario", new[] { "Estado", "FechaExpiracion" });
            migrationBuilder.CreateIndex("IX_ReservasInventario_VentaId", "ReservasInventario", "VentaId");
            migrationBuilder.CreateIndex("IX_ReservaDetalles_AlmacenId_UbicacionAlmacenId", "ReservaInventarioDetalles", new[] { "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex("IX_ReservaDetalles_ExistenciaFisica", "ReservaInventarioDetalles", new[] { "ProductoVarianteId", "AlmacenId", "UbicacionAlmacenId" });
            migrationBuilder.CreateIndex("IX_ReservaDetalles_ReservaInventarioId", "ReservaInventarioDetalles", "ReservaInventarioId");
            migrationBuilder.CreateIndex("UX_ReservaDetalles_ClaveFisica", "ReservaInventarioDetalles", new[] { "ReservaInventarioId", "ProductoVarianteId", "AlmacenId", "UbicacionNormalizada" }, unique: true);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N18CPostGuard;
                CREATE TEMPORARY TABLE __N18CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N18C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N18CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('ReservasInventario','ReservaInventarioDetalles');
                INSERT INTO __N18CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'ReservaInventarioDetalles'
                   AND index_name = 'UX_ReservaDetalles_ClaveFisica'
                   AND non_unique = 0
                   AND seq_in_index = 1;
                INSERT INTO __N18CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'ReservaInventarioDetalles'
                   AND constraint_name = 'FK_ReservaDetalles_Ubicacion_MismoAlmacen'
                   AND constraint_type = 'FOREIGN KEY';
                DROP TEMPORARY TABLE __N18CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ReservaInventarioDetalles");
            migrationBuilder.DropTable(name: "ReservasInventario");
        }
    }
}
