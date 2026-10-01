using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.7 C: completa la paridad física del índice de la FK nullable de variante
/// que EF mantiene además del índice compuesto ProductoId/ProductoVarianteId.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260814014000_N0_7_AjusteInventarioVarianteIndex")]
public sealed class N0_7_AjusteInventarioVarianteIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IX_AjusteInventarioDetalles_ProductoVarianteId
                ON AjusteInventarioDetalles (ProductoVarianteId);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IX_AjusteInventarioDetalles_ProductoVarianteId
                ON AjusteInventarioDetalles;
            """);
    }
}
