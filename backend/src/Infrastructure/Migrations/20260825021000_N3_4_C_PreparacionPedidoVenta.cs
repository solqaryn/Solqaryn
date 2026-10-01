using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260825021000_N3_4_C_PreparacionPedidoVenta")]
    public partial class N3_4_C_PreparacionPedidoVenta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreparacionesPedidoVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PedidoVentaId = table.Column<int>(type: "int", nullable: false),
                    ReservaInventarioId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaPickingCompletadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaPackingCompletadoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaDespachoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaEntregaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaCancelacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UltimoUsuarioId = table.Column<int>(type: "int", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparacionesPedidoVenta", x => x.Id);
                    table.CheckConstraint("CK_PreparacionesPedidoVenta_Estado", "`Estado` IN (1, 2, 3, 4, 5, 6)");
                    table.ForeignKey("FK_PreparacionesPedidoVenta_PedidosVenta_PedidoVentaId", x => x.PedidoVentaId, "PedidosVenta", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PreparacionesPedidoVenta_ReservasInventario_ReservaInventarioId", x => x.ReservaInventarioId, "ReservasInventario", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PreparacionPedidoVentaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PreparacionPedidoVentaId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: false),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionAlmacenId = table.Column<int>(type: "int", nullable: true),
                    UbicacionNormalizada = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(`UbicacionAlmacenId`, 0)", stored: true),
                    CantidadPreparar = table.Column<int>(type: "int", nullable: false),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparacionPedidoVentaDetalles", x => x.Id);
                    table.CheckConstraint("CK_PreparacionPedidoVentaDetalles_CantidadPreparar", "`CantidadPreparar` > 0");
                    table.ForeignKey("FK_PreparacionPedidoVentaDetalles_PreparacionesPedidoVenta_PreparacionPedidoVentaId", x => x.PreparacionPedidoVentaId, "PreparacionesPedidoVenta", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_PreparacionPedidoVentaDetalles_ProductoVariantes_ProductoVarianteId", x => x.ProductoVarianteId, "ProductoVariantes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PreparacionPedidoVentaDetalles_Almacenes_AlmacenId", x => x.AlmacenId, "Almacenes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_PreparacionPedidoVentaDetalles_Ubicacion_MismoAlmacen", x => new { x.AlmacenId, x.UbicacionAlmacenId }, "UbicacionesAlmacen", new[] { "AlmacenId", "Id" }, onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("UX_PreparacionesPedidoVenta_PedidoVentaId", "PreparacionesPedidoVenta", "PedidoVentaId", unique: true);
            migrationBuilder.CreateIndex("UX_PreparacionesPedidoVenta_ReservaInventarioId", "PreparacionesPedidoVenta", "ReservaInventarioId", unique: true);
            migrationBuilder.CreateIndex("IX_PreparacionesPedidoVenta_Estado", "PreparacionesPedidoVenta", "Estado");
            migrationBuilder.CreateIndex("IX_PreparacionPedidoVentaDetalles_ProductoVarianteId", "PreparacionPedidoVentaDetalles", "ProductoVarianteId");
            migrationBuilder.CreateIndex("IX_PreparacionPedidoVentaDetalles_AlmacenId", "PreparacionPedidoVentaDetalles", "AlmacenId");
            migrationBuilder.CreateIndex("UX_PreparacionPedidoVentaDetalles_ClaveFisica", "PreparacionPedidoVentaDetalles", new[] { "PreparacionPedidoVentaId", "ProductoVarianteId", "AlmacenId", "UbicacionNormalizada" }, unique: true);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N34CPostGuard;
                CREATE TEMPORARY TABLE __N34CPostGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N34C_PostGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N34CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'PreparacionesPedidoVenta' AND index_name = 'UX_PreparacionesPedidoVenta_PedidoVentaId';
                INSERT INTO __N34CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'PreparacionPedidoVentaDetalles' AND index_name = 'UX_PreparacionPedidoVentaDetalles_ClaveFisica';
                DROP TEMPORARY TABLE __N34CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N34CDownGuard;
                CREATE TEMPORARY TABLE __N34CDownGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N34C_DownGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N34CDownGuard (Id, Violaciones)
                SELECT 1, (SELECT COUNT(*) FROM PreparacionesPedidoVenta) + (SELECT COUNT(*) FROM PreparacionPedidoVentaDetalles);
                DROP TEMPORARY TABLE __N34CDownGuard;
                """);
            migrationBuilder.DropTable("PreparacionPedidoVentaDetalles");
            migrationBuilder.DropTable("PreparacionesPedidoVenta");
        }
    }
}
