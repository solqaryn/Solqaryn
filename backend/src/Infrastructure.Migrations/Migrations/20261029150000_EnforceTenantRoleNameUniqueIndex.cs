using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261029150000_EnforceTenantRoleNameUniqueIndex")]
public partial class EnforceTenantRoleNameUniqueIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Reassert the contract atomically. DEV's prior migration history says this
        // index exists, but INFORMATION_SCHEMA reported it as NON_UNIQUE=1.
        migrationBuilder.Sql(
            "ALTER TABLE `Roles` " +
            "DROP INDEX `UX_Roles_Empresa_NombreNormalizado`, " +
            "ADD UNIQUE INDEX `UX_Roles_Empresa_NombreNormalizado` " +
            "(`EmpresaId`, `NombreNormalizado`)");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "No degradar la unicidad tenant de roles; restaurar solo mediante un cambio protegido revisado.");
}
