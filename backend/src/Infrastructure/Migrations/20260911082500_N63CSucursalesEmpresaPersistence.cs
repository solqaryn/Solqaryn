using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// N6.3.C: materializa Empresa -> multiples Sucursales sin inventar owner para filas
/// legacy. EmpresaId permanece nullable; la unicidad de Codigo pasa a ser por Empresa
/// para filas con owner y conserva un namespace LEGACY global para filas sin owner.
/// El indice objetivo se crea antes de retirar el indice anterior, de modo que una
/// colision inesperada falle cerrado sin perder la proteccion existente.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260911082500_N63CSucursalesEmpresaPersistence")]
public partial class N63CSucursalesEmpresaPersistence : Migration
{
    private const string EmpresaScopedExpression =
        "IF(Eliminado = 0, CONCAT(IF(EmpresaId IS NULL, 'LEGACY', CONCAT('E:', EmpresaId)), ':', UPPER(TRIM(Codigo))), NULL)";

    private const string GlobalLegacyExpression =
        "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Precheck material: construir primero la clave/indice objetivo. Si existiera
        // una colision bajo la nueva semantica, CREATE UNIQUE INDEX falla y el indice
        // global previo sigue intacto. No se ejecuta backfill arbitrario de EmpresaId.
        migrationBuilder.Sql(
            $"""
            ALTER TABLE `Sucursales`
                ADD COLUMN `CodigoActivoEmpresaN63C` varchar(64)
                GENERATED ALWAYS AS ({EmpresaScopedExpression}) STORED;

            CREATE UNIQUE INDEX `UX_Sucursales_Empresa_Codigo_Activo_N63C`
                ON `Sucursales` (`CodigoActivoEmpresaN63C`);
            """);

        // MySQL 8 usa DDL atomico por ALTER TABLE: solo despues de validar el indice
        // objetivo se reemplaza la clave global anterior conservando el nombre canonico.
        migrationBuilder.Sql(
            $"""
            ALTER TABLE `Sucursales`
                DROP INDEX `UX_Sucursales_Codigo_Activo`,
                DROP COLUMN `CodigoActivoUnico`,
                CHANGE COLUMN `CodigoActivoEmpresaN63C` `CodigoActivoUnico` varchar(64)
                    GENERATED ALWAYS AS ({EmpresaScopedExpression}) STORED,
                RENAME INDEX `UX_Sucursales_Empresa_Codigo_Activo_N63C`
                    TO `UX_Sucursales_Codigo_Activo`;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback fail-closed: valida primero que los datos actuales vuelvan a cumplir
        // la unicidad global antigua. Si dos Empresas ya usan el mismo Codigo activo,
        // este indice temporal falla y evita un rollback destructivo/ambiguo.
        migrationBuilder.Sql(
            $"""
            ALTER TABLE `Sucursales`
                ADD COLUMN `CodigoActivoGlobalRollback` varchar(40)
                GENERATED ALWAYS AS ({GlobalLegacyExpression}) STORED;

            CREATE UNIQUE INDEX `UX_Sucursales_Codigo_Global_Rollback`
                ON `Sucursales` (`CodigoActivoGlobalRollback`);
            """);

        migrationBuilder.Sql(
            $"""
            ALTER TABLE `Sucursales`
                DROP INDEX `UX_Sucursales_Codigo_Activo`,
                DROP COLUMN `CodigoActivoUnico`,
                CHANGE COLUMN `CodigoActivoGlobalRollback` `CodigoActivoUnico` varchar(40)
                    GENERATED ALWAYS AS ({GlobalLegacyExpression}) STORED,
                RENAME INDEX `UX_Sucursales_Codigo_Global_Rollback`
                    TO `UX_Sucursales_Codigo_Activo`;
            """);
    }
}
