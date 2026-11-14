using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260825153500_N3_6_C_DevolucionCliente")]
    public partial class N3_6_C_DevolucionCliente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevolucionesCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    FacturaId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    IdempotencyFingerprint = table.Column<string>(type: "char(64)", maxLength: 64, nullable: true),
                    ConfirmadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ConfirmadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AnuladoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AnuladoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesCliente", x => x.Id);
                    table.CheckConstraint("CK_DevolucionesCliente_VentaId", "`VentaId` > 0");
                    table.CheckConstraint("CK_DevolucionesCliente_FacturaId", "`FacturaId` IS NULL OR `FacturaId` > 0");
                    table.CheckConstraint("CK_DevolucionesCliente_Estado", "`Estado` IN (1, 2, 3)");
                    table.CheckConstraint("CK_DevolucionesCliente_IdempotenciaAtomica", "(`IdempotencyKey` IS NULL AND `IdempotencyFingerprint` IS NULL) OR (`IdempotencyKey` IS NOT NULL AND `IdempotencyFingerprint` IS NOT NULL)");
                    table.ForeignKey("FK_DevolucionesCliente_Ventas_VentaId", x => x.VentaId, "Ventas", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_DevolucionesCliente_Facturas_FacturaId", x => x.FacturaId, "Facturas", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionClienteDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DevolucionClienteId = table.Column<int>(type: "int", nullable: false),
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ProductoVarianteId = table.Column<int>(type: "int", nullable: true),
                    ProductoSkuSnapshot = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    ProductoNombreSnapshot = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ProductoMarcaSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    ProductoModeloSnapshot = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    ProductoColorSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    ProductoTallaSnapshot = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    CantidadVendidaSnapshot = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitarioSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Resolucion = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionClienteDetalles", x => x.Id);
                    table.CheckConstraint("CK_DevolucionClienteDetalles_VentaDetalleId", "`VentaDetalleId` > 0");
                    table.CheckConstraint("CK_DevolucionClienteDetalles_ProductoId", "`ProductoId` > 0");
                    table.CheckConstraint("CK_DevolucionClienteDetalles_ProductoVarianteId", "`ProductoVarianteId` IS NULL OR `ProductoVarianteId` > 0");
                    table.CheckConstraint("CK_DevolucionClienteDetalles_Cantidades", "`Cantidad` > 0 AND `CantidadVendidaSnapshot` > 0 AND `Cantidad` <= `CantidadVendidaSnapshot`");
                    table.CheckConstraint("CK_DevolucionClienteDetalles_Precio", "`PrecioUnitarioSnapshot` >= 0");
                    table.CheckConstraint("CK_DevolucionClienteDetalles_Resolucion", "`Resolucion` IN (1, 2, 3)");
                    table.ForeignKey("FK_DevolucionClienteDetalles_DevolucionesCliente_DevolucionClienteId", x => x.DevolucionClienteId, "DevolucionesCliente", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_DevolucionClienteDetalles_VentaDetalles_VentaDetalleId", x => x.VentaDetalleId, "VentaDetalles", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_DevolucionesCliente_VentaId", "DevolucionesCliente", "VentaId");
            migrationBuilder.CreateIndex("IX_DevolucionesCliente_FacturaId", "DevolucionesCliente", "FacturaId");
            migrationBuilder.CreateIndex("IX_DevolucionesCliente_Estado", "DevolucionesCliente", "Estado");
            migrationBuilder.CreateIndex("UX_DevolucionesCliente_IdempotencyKey", "DevolucionesCliente", "IdempotencyKey", unique: true);
            migrationBuilder.CreateIndex("IX_DevolucionClienteDetalles_VentaDetalleId", "DevolucionClienteDetalles", "VentaDetalleId");
            migrationBuilder.CreateIndex("UX_DevolucionClienteDetalles_LineaVenta", "DevolucionClienteDetalles", new[] { "DevolucionClienteId", "VentaDetalleId" }, unique: true);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N36CPostGuard;
                CREATE TEMPORARY TABLE __N36CPostGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N36C_PostGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N36CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'DevolucionesCliente' AND index_name = 'UX_DevolucionesCliente_IdempotencyKey';
                INSERT INTO __N36CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'DevolucionClienteDetalles' AND index_name = 'UX_DevolucionClienteDetalles_LineaVenta';
                DROP TEMPORARY TABLE __N36CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N36CDownGuard;
                CREATE TEMPORARY TABLE __N36CDownGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N36C_DownGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N36CDownGuard (Id, Violaciones)
                SELECT 1, (SELECT COUNT(*) FROM DevolucionesCliente) + (SELECT COUNT(*) FROM DevolucionClienteDetalles);
                DROP TEMPORARY TABLE __N36CDownGuard;
                """);
            migrationBuilder.DropTable("DevolucionClienteDetalles");
            migrationBuilder.DropTable("DevolucionesCliente");
        }
    }
}
