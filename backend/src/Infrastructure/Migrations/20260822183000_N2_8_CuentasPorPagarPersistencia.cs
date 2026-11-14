using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260822183000_N2_8_CuentasPorPagarPersistencia")]
    public partial class N2_8_CuentasPorPagarPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N28CGuard;
                CREATE TEMPORARY TABLE __N28CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N28C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N28CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('FacturasProveedor','Proveedores');
                INSERT INTO __N28CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('CuentasPorPagar','AplicacionesCuentaPorPagar');
                DROP TEMPORARY TABLE __N28CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "CuentasPorPagar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CondicionPago = table.Column<int>(type: "int", nullable: false),
                    FechaEmisionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaVencimientoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MontoOriginal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaAnulacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasPorPagar", x => x.Id);
                    table.CheckConstraint("CK_CuentasPorPagar_IdsValidos", "FacturaProveedorId > 0 AND ProveedorId > 0");
                    table.CheckConstraint("CK_CuentasPorPagar_CondicionPagoValida", "CondicionPago IN (1, 2)");
                    table.CheckConstraint("CK_CuentasPorPagar_EstadoValido", "Estado IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_CuentasPorPagar_MonedaIso3", "CHAR_LENGTH(TRIM(Moneda)) = 3");
                    table.CheckConstraint("CK_CuentasPorPagar_MontoOriginalPositivo", "MontoOriginal > 0");
                    table.CheckConstraint("CK_CuentasPorPagar_FechasValidas", "FechaVencimientoUtc >= FechaEmisionUtc");
                    table.CheckConstraint("CK_CuentasPorPagar_ContadoVenceEmision", "CondicionPago <> 1 OR FechaVencimientoUtc = FechaEmisionUtc");
                    table.CheckConstraint("CK_CuentasPorPagar_CreditoVenceDespues", "CondicionPago <> 2 OR FechaVencimientoUtc > FechaEmisionUtc");
                    table.ForeignKey(
                        name: "FK_CuentasPorPagar_FacturasProveedor_FacturaProveedorId",
                        column: x => x.FacturaProveedorId,
                        principalTable: "FacturasProveedor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasPorPagar_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AplicacionesCuentaPorPagar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CuentaPorPagarId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferenciaExterna = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaAplicacionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Revertida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaReversionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MotivoReversion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesCuentaPorPagar", x => x.Id);
                    table.CheckConstraint("CK_AplicacionesCuentaPorPagar_CuentaValida", "CuentaPorPagarId > 0");
                    table.CheckConstraint("CK_AplicacionesCuentaPorPagar_TipoValido", "Tipo IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_AplicacionesCuentaPorPagar_MontoPositivo", "Monto > 0");
                    table.ForeignKey(
                        name: "FK_AplicacionesCuentaPorPagar_CuentasPorPagar_CuentaPorPagarId",
                        column: x => x.CuentaPorPagarId,
                        principalTable: "CuentasPorPagar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_CuentasPorPagar_FacturaProveedorId",
                table: "CuentasPorPagar",
                column: "FacturaProveedorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorPagar_Proveedor_Estado_Vencimiento",
                table: "CuentasPorPagar",
                columns: new[] { "ProveedorId", "Estado", "FechaVencimientoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPorPagar_Estado_Vencimiento",
                table: "CuentasPorPagar",
                columns: new[] { "Estado", "FechaVencimientoUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey",
                table: "AplicacionesCuentaPorPagar",
                columns: new[] { "CuentaPorPagarId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesCuentaPorPagar_Cuenta_Fecha",
                table: "AplicacionesCuentaPorPagar",
                columns: new[] { "CuentaPorPagarId", "FechaAplicacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesCuentaPorPagar_Tipo_Fecha",
                table: "AplicacionesCuentaPorPagar",
                columns: new[] { "Tipo", "FechaAplicacionUtc" });

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N28CPostGuard;
                CREATE TEMPORARY TABLE __N28CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N28C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N28CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('CuentasPorPagar','AplicacionesCuentaPorPagar');
                INSERT INTO __N28CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'CuentasPorPagar'
                   AND index_name = 'UX_CuentasPorPagar_FacturaProveedorId'
                   AND non_unique = 0;
                INSERT INTO __N28CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'AplicacionesCuentaPorPagar'
                   AND index_name = 'UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey'
                   AND non_unique = 0;
                DROP TEMPORARY TABLE __N28CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N28CDownGuard;
                CREATE TEMPORARY TABLE __N28CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N28C_DownGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N28CDownGuard (Id, Violaciones)
                SELECT 1,
                       (SELECT COUNT(*) FROM CuentasPorPagar) +
                       (SELECT COUNT(*) FROM AplicacionesCuentaPorPagar);
                DROP TEMPORARY TABLE __N28CDownGuard;
                """);

            migrationBuilder.DropTable(name: "AplicacionesCuentaPorPagar");
            migrationBuilder.DropTable(name: "CuentasPorPagar");
        }
    }
}
