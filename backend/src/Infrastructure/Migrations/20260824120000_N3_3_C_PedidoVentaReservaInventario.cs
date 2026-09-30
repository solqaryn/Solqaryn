using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260824120000_N3_3_C_PedidoVentaReservaInventario")]
    public partial class N3_3_C_PedidoVentaReservaInventario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PedidoVentaId",
                table: "ReservasInventario",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReservasInventario_PedidoVentaId",
                table: "ReservasInventario",
                column: "PedidoVentaId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservasInventario_PedidosVenta_PedidoVentaId",
                table: "ReservasInventario",
                column: "PedidoVentaId",
                principalTable: "PedidosVenta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N33CPostGuard;
                CREATE TEMPORARY TABLE __N33CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N33C_PostGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N33CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'ReservasInventario'
                   AND index_name = 'UX_ReservasInventario_PedidoVentaId';
                INSERT INTO __N33CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND constraint_name = 'FK_ReservasInventario_PedidosVenta_PedidoVentaId';
                DROP TEMPORARY TABLE __N33CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N33CDownGuard;
                CREATE TEMPORARY TABLE __N33CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N33C_DownGuard_Cero CHECK (Violaciones = 0)
                );
                INSERT INTO __N33CDownGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM ReservasInventario WHERE PedidoVentaId IS NOT NULL;
                DROP TEMPORARY TABLE __N33CDownGuard;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ReservasInventario_PedidosVenta_PedidoVentaId",
                table: "ReservasInventario");

            migrationBuilder.DropIndex(
                name: "UX_ReservasInventario_PedidoVentaId",
                table: "ReservasInventario");

            migrationBuilder.DropColumn(
                name: "PedidoVentaId",
                table: "ReservasInventario");
        }
    }
}
