using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// N6.2.C: establece la FK de ownership tenant-aware sin asignar una Empresa arbitraria.
/// EmpresaId conserva nullabilidad temporal hasta que N6.2.D adapte los flujos de
/// aplicación; referencias no nulas huérfanas fallan cerrado al crear la restricción.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260911002500_N62CTenantAwareEmpresaId")]
public partial class N62CTenantAwareEmpresaId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE `Sucursales`
                ADD CONSTRAINT `FK_Sucursales_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE `Sucursales`
                DROP FOREIGN KEY `FK_Sucursales_Empresas_EmpresaId`;
            """);
    }
}
