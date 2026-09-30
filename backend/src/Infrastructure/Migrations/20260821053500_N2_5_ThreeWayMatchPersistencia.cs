using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260821053500_N2_5_ThreeWayMatchPersistencia")]
    public partial class N2_5_ThreeWayMatchPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N25CGuard;
                CREATE TEMPORARY TABLE __N25CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N25C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N25CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'OrdenesCompra';
                INSERT INTO __N25CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('ThreeWayMatchResultados','ThreeWayMatchDiscrepancias');
                DROP TEMPORARY TABLE __N25CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "ThreeWayMatchResultados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ThreeWayMatchResultados", x => x.Id);
                    table.CheckConstraint("CK_ThreeWayMatchResultados_OrdenCompraValida", "OrdenCompraId > 0");
                    table.CheckConstraint("CK_ThreeWayMatchResultados_EstadoValido", "Estado IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchResultados_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ThreeWayMatchDiscrepancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ThreeWayMatchResultId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraDetalleId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    EsperadoOrdenado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorRecepcion = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ValorFacturado = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Mensaje = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsperadoTexto = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorFacturadoTexto = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreeWayMatchDiscrepancias", x => x.Id);
                    table.CheckConstraint(
                        "CK_ThreeWayMatchDiscrepancias_OrdenDetalleSentinela",
                        "OrdenCompraDetalleId >= 0");
                    table.CheckConstraint(
                        "CK_ThreeWayMatchDiscrepancias_TipoValido",
                        "Tipo IN (1, 2, 3, 4, 5)");
                    table.ForeignKey(
                        name: "FK_ThreeWayMatchDiscrepancias_ThreeWayMatchResultados_ResultId",
                        column: x => x.ThreeWayMatchResultId,
                        principalTable: "ThreeWayMatchResultados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchResultados_OrdenCompraId",
                table: "ThreeWayMatchResultados",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchResultados_OrdenCompra_Fecha",
                table: "ThreeWayMatchResultados",
                columns: new[] { "OrdenCompraId", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchDiscrepancias_ResultId",
                table: "ThreeWayMatchDiscrepancias",
                column: "ThreeWayMatchResultId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreeWayMatchDiscrepancias_OrdenDetalleId",
                table: "ThreeWayMatchDiscrepancias",
                column: "OrdenCompraDetalleId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ThreeWayMatchDiscrepancias");
            migrationBuilder.DropTable(name: "ThreeWayMatchResultados");
        }
    }
}
