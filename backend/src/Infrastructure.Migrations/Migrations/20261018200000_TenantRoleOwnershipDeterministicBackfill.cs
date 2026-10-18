using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261018200000_TenantRoleOwnershipDeterministicBackfill")]
public sealed class TenantRoleOwnershipDeterministicBackfill : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // NUNCA usar UsuarioEmpresa para atribuir roles históricos.
        // Una membresía cruzada/manipulada no demuestra propiedad.
        // Sólo una instalación estrictamente monoempresa puede atribuir
        // roles personalizados sin dueño; se cuentan empresas inactivas.
        // Conflictos de nombre se mantienen sin propietario (fail-closed).
        migrationBuilder.Sql("""
            UPDATE Roles AS r
            CROSS JOIN (
                SELECT MIN(Id) AS EmpresaId, COUNT(*) AS NumeroEmpresas
                FROM Empresas
            ) AS unico
            LEFT JOIN Roles AS colision
                ON colision.EmpresaId = unico.EmpresaId
               AND colision.NombreNormalizado = r.NombreNormalizado
               AND colision.Id <> r.Id
            SET r.EmpresaId = unico.EmpresaId
            WHERE r.Ambito = 1 AND r.EsSistema = 0 AND r.EmpresaId IS NULL
              AND unico.NumeroEmpresas = 1 AND colision.Id IS NULL;
            """);

        // Tras atribuir la raíz, conservar los grants históricos del rol:
        // RolPermisos.EmpresaId es shadow y debe recibir el mismo dueño.
        // La FK compuesta (EmpresaId, RolId) asegura consistencia física.
        migrationBuilder.Sql("""
            UPDATE RolPermisos AS rp
            INNER JOIN Roles AS r ON rp.RolId = r.Id
            CROSS JOIN (
                SELECT COUNT(*) AS NumeroEmpresas FROM Empresas
            ) AS unico
            SET rp.EmpresaId = r.EmpresaId
            WHERE unico.NumeroEmpresas = 1
              AND r.Ambito = 1 AND r.EsSistema = 0
              AND r.EmpresaId IS NOT NULL AND rp.EmpresaId IS NULL;
            """);

        // Con dos o más empresas, los roles legacy huérfanos permanecen NULL,
        // no se convierten en plantillas y no confieren autoridad.
        // Requieren atribución verificada explícita, nunca inferencia.
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "No desatribuir roles históricos sin respaldo y readback verificados.");
}
