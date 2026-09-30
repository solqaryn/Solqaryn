using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260823042000_N2_9_EvaluacionProveedorPersistencia")]
    public partial class N2_9_EvaluacionProveedorPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N29CGuard;
                CREATE TEMPORARY TABLE __N29CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N29C_Guard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N29CGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name IN ('Proveedores','OrdenesCompra','RecepcionesCompra');
                INSERT INTO __N29CGuard (Id, Violaciones)
                SELECT 2, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'EvaluacionesProveedor';
                DROP TEMPORARY TABLE __N29CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "EvaluacionesProveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    RecepcionCompraId = table.Column<int>(type: "int", nullable: false),
                    FechaEsperadaUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaRecepcionUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CantidadOrdenada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadAceptada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadDanada = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CantidadSobrante = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_EvaluacionesProveedor", x => x.Id);
                    table.CheckConstraint("CK_EvaluacionesProveedor_ProveedorId_Valido", "`ProveedorId` > 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_OrdenCompraId_Valido", "`OrdenCompraId` > 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_RecepcionCompraId_Valido", "`RecepcionCompraId` > 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_CantidadOrdenada_NoNegativa", "`CantidadOrdenada` >= 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_CantidadAceptada_NoNegativa", "`CantidadAceptada` >= 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_CantidadDanada_NoNegativa", "`CantidadDanada` >= 0");
                    table.CheckConstraint("CK_EvaluacionesProveedor_CantidadSobrante_NoNegativa", "`CantidadSobrante` >= 0");
                    table.ForeignKey(
                        name: "FK_EvaluacionesProveedor_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionesProveedor_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionesProveedor_RecepcionesCompra_RecepcionCompraId",
                        column: x => x.RecepcionCompraId,
                        principalTable: "RecepcionesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesProveedor_RecepcionCompra",
                table: "EvaluacionesProveedor",
                column: "RecepcionCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesProveedor_OrdenCompra",
                table: "EvaluacionesProveedor",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesProveedor_Proveedor_FechaRecepcion",
                table: "EvaluacionesProveedor",
                columns: new[] { "ProveedorId", "FechaRecepcionUtc" });

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N29CPostGuard;
                CREATE TEMPORARY TABLE __N29CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N29C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N29CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'EvaluacionesProveedor';
                INSERT INTO __N29CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND constraint_name IN
                       ('FK_EvaluacionesProveedor_Proveedores_ProveedorId',
                        'FK_EvaluacionesProveedor_OrdenesCompra_OrdenCompraId',
                        'FK_EvaluacionesProveedor_RecepcionesCompra_RecepcionCompraId');
                DROP TEMPORARY TABLE __N29CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N29CDownGuard;
                CREATE TEMPORARY TABLE __N29CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N29C_DownGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N29CDownGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM EvaluacionesProveedor;
                DROP TEMPORARY TABLE __N29CDownGuard;
                """);

            migrationBuilder.DropTable(name: "EvaluacionesProveedor");
        }
    }
}
