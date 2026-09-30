using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N7.8.D — idempotencia durable para el inicio de pagos online.
/// Persiste únicamente SHA-256 hexadecimal de Idempotency-Key, nunca la clave en claro.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914200100_N7_8_D_PagoOnlineIdempotencia")]
public sealed class N7_8_D_PagoOnlineIdempotencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE `PagosOnline`
                ADD COLUMN `ClaveIdempotenciaHash` varchar(64) CHARACTER SET utf8mb4 NULL;

            CREATE UNIQUE INDEX `UX_PagosOnline_Empresa_Proveedor_Idempotencia`
                ON `PagosOnline` (`EmpresaId`, `Proveedor`, `ClaveIdempotenciaHash`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX `UX_PagosOnline_Empresa_Proveedor_Idempotencia` ON `PagosOnline`;
            ALTER TABLE `PagosOnline` DROP COLUMN `ClaveIdempotenciaHash`;
            """);
    }
}
