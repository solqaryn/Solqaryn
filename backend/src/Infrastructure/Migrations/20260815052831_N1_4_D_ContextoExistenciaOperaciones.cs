using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260815052831_N1_4_D_ContextoExistenciaOperaciones")]
    public partial class N1_4_D_ContextoExistenciaOperaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "VentaDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionAlmacenId",
                table: "VentaDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "MovimientosInventario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionAlmacenId",
                table: "MovimientosInventario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "ConsumoInsumoDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionAlmacenId",
                table: "ConsumoInsumoDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionAlmacenId",
                table: "CompraDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlmacenId",
                table: "AjusteInventarioDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UbicacionAlmacenId",
                table: "AjusteInventarioDetalles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VentaDetalles_Almacen_Ubicacion",
                table: "VentaDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_VentaDetalles_Ubicacion_RequiereAlmacen",
                table: "VentaDetalles",
                sql: "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Almacen_Ubicacion",
                table: "MovimientosInventario",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_MovimientosInventario_Ubicacion_RequiereAlmacen",
                table: "MovimientosInventario",
                sql: "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumoInsumoDetalles_Almacen_Ubicacion",
                table: "ConsumoInsumoDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ConsumoInsumoDetalles_Ubicacion_RequiereAlmacen",
                table: "ConsumoInsumoDetalles",
                sql: "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_Almacen_Ubicacion",
                table: "CompraDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CompraDetalles_Ubicacion_RequiereAlmacen",
                table: "CompraDetalles",
                sql: "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AjusteInventarioDetalles_Almacen_Ubicacion",
                table: "AjusteInventarioDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AjusteInventarioDetalles_Ubicacion_RequiereAlmacen",
                table: "AjusteInventarioDetalles",
                sql: "`UbicacionAlmacenId` IS NULL OR `AlmacenId` IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AjusteInventarioDetalles_Almacenes_AlmacenId_N14",
                table: "AjusteInventarioDetalles",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AjusteInventarioDetalles_Ubicacion_MismoAlmacen_N14",
                table: "AjusteInventarioDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" },
                principalTable: "UbicacionesAlmacen",
                principalColumns: new[] { "AlmacenId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalles_Almacenes_AlmacenId_N14",
                table: "CompraDetalles",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalles_Ubicacion_MismoAlmacen_N14",
                table: "CompraDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" },
                principalTable: "UbicacionesAlmacen",
                principalColumns: new[] { "AlmacenId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumoInsumoDetalles_Almacenes_AlmacenId_N14",
                table: "ConsumoInsumoDetalles",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumoInsumoDetalles_Ubicacion_MismoAlmacen_N14",
                table: "ConsumoInsumoDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" },
                principalTable: "UbicacionesAlmacen",
                principalColumns: new[] { "AlmacenId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Almacenes_AlmacenId_N14",
                table: "MovimientosInventario",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosInventario_Ubicacion_MismoAlmacen_N14",
                table: "MovimientosInventario",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" },
                principalTable: "UbicacionesAlmacen",
                principalColumns: new[] { "AlmacenId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VentaDetalles_Almacenes_AlmacenId_N14",
                table: "VentaDetalles",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VentaDetalles_Ubicacion_MismoAlmacen_N14",
                table: "VentaDetalles",
                columns: new[] { "AlmacenId", "UbicacionAlmacenId" },
                principalTable: "UbicacionesAlmacen",
                principalColumns: new[] { "AlmacenId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AjusteInventarioDetalles_Almacenes_AlmacenId_N14",
                table: "AjusteInventarioDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_AjusteInventarioDetalles_Ubicacion_MismoAlmacen_N14",
                table: "AjusteInventarioDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalles_Almacenes_AlmacenId_N14",
                table: "CompraDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalles_Ubicacion_MismoAlmacen_N14",
                table: "CompraDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsumoInsumoDetalles_Almacenes_AlmacenId_N14",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsumoInsumoDetalles_Ubicacion_MismoAlmacen_N14",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Almacenes_AlmacenId_N14",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosInventario_Ubicacion_MismoAlmacen_N14",
                table: "MovimientosInventario");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaDetalles_Almacenes_AlmacenId_N14",
                table: "VentaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_VentaDetalles_Ubicacion_MismoAlmacen_N14",
                table: "VentaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_VentaDetalles_Almacen_Ubicacion",
                table: "VentaDetalles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VentaDetalles_Ubicacion_RequiereAlmacen",
                table: "VentaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosInventario_Almacen_Ubicacion",
                table: "MovimientosInventario");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MovimientosInventario_Ubicacion_RequiereAlmacen",
                table: "MovimientosInventario");

            migrationBuilder.DropIndex(
                name: "IX_ConsumoInsumoDetalles_Almacen_Ubicacion",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ConsumoInsumoDetalles_Ubicacion_RequiereAlmacen",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropIndex(
                name: "IX_CompraDetalles_Almacen_Ubicacion",
                table: "CompraDetalles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CompraDetalles_Ubicacion_RequiereAlmacen",
                table: "CompraDetalles");

            migrationBuilder.DropIndex(
                name: "IX_AjusteInventarioDetalles_Almacen_Ubicacion",
                table: "AjusteInventarioDetalles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AjusteInventarioDetalles_Ubicacion_RequiereAlmacen",
                table: "AjusteInventarioDetalles");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "UbicacionAlmacenId",
                table: "VentaDetalles");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "UbicacionAlmacenId",
                table: "MovimientosInventario");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropColumn(
                name: "UbicacionAlmacenId",
                table: "ConsumoInsumoDetalles");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "UbicacionAlmacenId",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "AjusteInventarioDetalles");

            migrationBuilder.DropColumn(
                name: "UbicacionAlmacenId",
                table: "AjusteInventarioDetalles");
        }
    }
}
