using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260822063000_N2_7_NotaCreditoProveedorPersistencia")]
    public partial class N2_7_NotaCreditoProveedorPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N27CGuard;
                CREATE TEMPORARY TABLE __N27CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N27C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N27CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Proveedores','FacturasProveedor','DevolucionesProveedor','Usuarios');
                INSERT INTO __N27CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'NotasCreditoProveedor';
                DROP TEMPORARY TABLE __N27CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "NotasCreditoProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NumeroNotaCredito = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    FacturaProveedorId = table.Column<int>(type: "int", nullable: false),
                    DevolucionProveedorId = table.Column<int>(type: "int", nullable: true),
                    ProveedorNombreSnapshot = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    FechaEmisionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReferenciaFiscal = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                    SubtotalCredito = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImpuestoCredito = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_NotasCreditoProveedor", x => x.Id);
                    table.CheckConstraint("CK_NotasCreditoProveedor_IdsValidos", "ProveedorId > 0 AND FacturaProveedorId > 0 AND (DevolucionProveedorId IS NULL OR DevolucionProveedorId > 0)");
                    table.CheckConstraint("CK_NotasCreditoProveedor_EstadoValido", "Estado IN (1, 2, 3)");
                    table.CheckConstraint("CK_NotasCreditoProveedor_MonedaIso3", "CHAR_LENGTH(TRIM(Moneda)) = 3");
                    table.CheckConstraint("CK_NotasCreditoProveedor_ImportesNoNegativos", "SubtotalCredito >= 0 AND ImpuestoCredito >= 0 AND (SubtotalCredito + ImpuestoCredito) > 0");
                    table.ForeignKey("FK_NotasCreditoProveedor_Proveedores_ProveedorId", x => x.ProveedorId, "Proveedores", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_NotasCreditoProveedor_FacturasProveedor_FacturaProveedorId", x => x.FacturaProveedorId, "FacturasProveedor", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_NotasCreditoProveedor_DevolucionesProveedor_DevolucionProveedorId", x => x.DevolucionProveedorId, "DevolucionesProveedor", "Id", onDelete: ReferentialAction.Restrict);
                }).Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_NotasCreditoProveedor_Proveedor_Numero",
                table: "NotasCreditoProveedor",
                columns: new[] { "ProveedorId", "NumeroNotaCredito" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_NotasCreditoProveedor_FacturaProveedorId",
                table: "NotasCreditoProveedor",
                column: "FacturaProveedorId");
            migrationBuilder.CreateIndex(
                name: "IX_NotasCreditoProveedor_DevolucionProveedorId",
                table: "NotasCreditoProveedor",
                column: "DevolucionProveedorId");
            migrationBuilder.CreateIndex(
                name: "IX_NotasCreditoProveedor_Proveedor_Estado",
                table: "NotasCreditoProveedor",
                columns: new[] { "ProveedorId", "Estado" });
            migrationBuilder.CreateIndex(
                name: "IX_NotasCreditoProveedor_FechaEmisionUtc",
                table: "NotasCreditoProveedor",
                column: "FechaEmisionUtc");

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N27CPostGuard;
                CREATE TEMPORARY TABLE __N27CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N27C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N27CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'NotasCreditoProveedor';
                INSERT INTO __N27CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'NotasCreditoProveedor'
                   AND index_name = 'UX_NotasCreditoProveedor_Proveedor_Numero'
                   AND non_unique = 0;
                DROP TEMPORARY TABLE __N27CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "NotasCreditoProveedor");
        }
    }
}
