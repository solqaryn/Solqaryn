using Solqaryn.Domain.Common;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// Añade la correlación durable del Kardex empresarial de ERP-N1.5.
/// Los registros históricos reciben cadena vacía como marcador explícito de
/// pre-cutover; toda escritura nueva debe persistir un CorrelationId no vacío
/// mediante el contrato de aplicación N1.5.B.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260815202500_N1_5_KardexCorrelation")]
public sealed class N1_5_KardexCorrelation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            table: "MovimientosInventario",
            type: $"varchar({ContextoFisicoMovimientoInventario.MaxCorrelationIdLength})",
            maxLength: ContextoFisicoMovimientoInventario.MaxCorrelationIdLength,
            nullable: false,
            defaultValue: string.Empty)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_CorrelationId_Fecha",
            table: "MovimientosInventario",
            columns: new[] { "CorrelationId", "Fecha" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MovimientosInventario_CorrelationId_Fecha",
            table: "MovimientosInventario");

        migrationBuilder.DropColumn(
            name: "CorrelationId",
            table: "MovimientosInventario");
    }
}
