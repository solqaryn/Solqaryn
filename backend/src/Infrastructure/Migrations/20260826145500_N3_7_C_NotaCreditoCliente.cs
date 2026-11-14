using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260826145500_N3_7_C_NotaCreditoCliente")]
    public partial class N3_7_C_NotaCreditoCliente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotasCreditoCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    MontoCredito = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasCreditoCliente", x => x.Id);
                    table.CheckConstraint("CK_NotasCreditoCliente_FacturaId", "`FacturaId` > 0");
                    table.CheckConstraint("CK_NotasCreditoCliente_VentaId", "`VentaId` > 0");
                    table.CheckConstraint("CK_NotasCreditoCliente_Moneda", "CHAR_LENGTH(`Moneda`) = 3");
                    table.CheckConstraint("CK_NotasCreditoCliente_MontoCredito", "`MontoCredito` > 0");
                    table.ForeignKey("FK_NotasCreditoCliente_Facturas_FacturaId", x => x.FacturaId, "Facturas", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_NotasCreditoCliente_FacturaId", "NotasCreditoCliente", "FacturaId");
            migrationBuilder.CreateIndex("IX_NotasCreditoCliente_VentaId", "NotasCreditoCliente", "VentaId");

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N37CPostGuard;
                CREATE TEMPORARY TABLE __N37CPostGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N37C_PostGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N37CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'NotasCreditoCliente' AND index_name = 'IX_NotasCreditoCliente_FacturaId';
                INSERT INTO __N37CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END FROM information_schema.statistics
                 WHERE table_schema = DATABASE() AND table_name = 'NotasCreditoCliente' AND index_name = 'IX_NotasCreditoCliente_VentaId';
                DROP TEMPORARY TABLE __N37CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N37CDownGuard;
                CREATE TEMPORARY TABLE __N37CDownGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N37C_DownGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N37CDownGuard (Id, Violaciones)
                SELECT 1, (SELECT COUNT(*) FROM NotasCreditoCliente);
                DROP TEMPORARY TABLE __N37CDownGuard;
                """);
            migrationBuilder.DropTable("NotasCreditoCliente");
        }
    }
}
