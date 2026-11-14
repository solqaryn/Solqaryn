using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909234000_N5_3_C_ReportSalesIndexes")]
public partial class N5_3_C_ReportSalesIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Ventas_Fecha",
            table: "Ventas",
            column: "Fecha");

        migrationBuilder.CreateIndex(
            name: "IX_Ventas_CreadoPorUsuarioId",
            table: "Ventas",
            column: "CreadoPorUsuarioId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Ventas_Fecha",
            table: "Ventas");

        migrationBuilder.DropIndex(
            name: "IX_Ventas_CreadoPorUsuarioId",
            table: "Ventas");
    }
}
