using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// Refuerza ERP-N1.5 con índices compuestos alineados a los filtros y al orden
/// estable del Kardex empresarial. No modifica datos ni elimina índices legacy.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260816005000_N1_5_KardexQueryIndexes")]
public sealed class N1_5_KardexQueryIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Producto_Variante_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "ProductoId", "ProductoVarianteId", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Almacen_Ubicacion_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "AlmacenId", "UbicacionAlmacenId", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Compra_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "CompraId", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Venta_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "VentaId", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Consumo_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "ConsumoInsumoId", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Ajuste_Fecha_N15",
            table: "MovimientosInventario",
            columns: new[] { "AjusteInventarioId", "Fecha" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MovInv_Ajuste_Fecha_N15",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Consumo_Fecha_N15",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Venta_Fecha_N15",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Compra_Fecha_N15",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Almacen_Ubicacion_Fecha_N15",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(
            name: "IX_MovInv_Producto_Variante_Fecha_N15",
            table: "MovimientosInventario");
    }
}
