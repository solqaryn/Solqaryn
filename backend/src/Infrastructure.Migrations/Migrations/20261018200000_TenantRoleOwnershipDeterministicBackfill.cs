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
        // Históricos creados antes de exigir EmpresaId. Sólo una membresía
        // perteneciente a exactamente UN tenant permite identificar dueño.
        // No reutilizar Usuario.RolId legacy ni la empresa activa como fuente.
        // Una colisión de nombre por empresa mantiene el rol en cuarentena.
        migrationBuilder.Sql("""
            UPDATE Roles AS r
            INNER JOIN (
                SELECT RolId, MIN(EmpresaId) AS EmpresaId
                FROM UsuarioEmpresas
                GROUP BY RolId
                HAVING COUNT(DISTINCT EmpresaId) = 1
            ) AS m ON m.RolId = r.Id
            LEFT JOIN Roles AS colision
                ON colision.EmpresaId = m.EmpresaId
               AND colision.NombreNormalizado = r.NombreNormalizado
               AND colision.Id <> r.Id
            SET r.EmpresaId = m.EmpresaId
            WHERE r.Ambito = 1 AND r.EsSistema = 0 AND r.EmpresaId IS NULL
              AND colision.Id IS NULL;
            """);

        // Sólo la instalación estrictamente monoempresa permite atribuir
        // roles sin ninguna membresía. Con A/B jamás adivinar propietario.
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

        // Ambiguos (varias empresas, sin membresías o colisión) quedan NULL,
        // nunca se hacen plantillas. El guard RBAC/EF los rechaza.
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "No se puede desatribuir de forma segura la propiedad de roles sin un respaldo verificado.");
}
