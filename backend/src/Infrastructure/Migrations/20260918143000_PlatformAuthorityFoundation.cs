using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// Fase 1 SuperAdministrador: crea el fundamento persistente que separa
/// autoridad empresarial y autoridad global de plataforma sin conceder aún
/// privilegios a ninguna cuenta.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260918143000_PlatformAuthorityFoundation")]
public partial class PlatformAuthorityFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Ambito",
            table: "Roles",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "EmpresaId",
            table: "Roles",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Ambito",
            table: "Permisos",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<int>(
            name: "EmpresaId",
            table: "RegistrosAuditoria",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "AmbitoAutoridad",
            table: "RegistrosAuditoria",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "OrigenAutorizacion",
            table: "RegistrosAuditoria",
            type: "int",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "UsuarioRolesPlataforma",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UsuarioId = table.Column<int>(type: "int", nullable: false),
                RolId = table.Column<int>(type: "int", nullable: false),
                Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UsuarioRolesPlataforma", x => x.Id);
                table.ForeignKey(
                    name: "FK_UsuarioRolesPlataforma_Roles_RolId",
                    column: x => x.RolId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioRolesPlataforma_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_Roles_EmpresaId",
            table: "Roles",
            column: "EmpresaId");

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Ambito_Activo",
            table: "Roles",
            columns: new[] { "Ambito", "Activo" });

        migrationBuilder.CreateIndex(
            name: "IX_Permisos_Ambito_Activo",
            table: "Permisos",
            columns: new[] { "Ambito", "Activo" });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_EmpresaId",
            table: "RegistrosAuditoria",
            column: "EmpresaId");

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_Ambito_Fecha",
            table: "RegistrosAuditoria",
            columns: new[] { "AmbitoAutoridad", "Fecha" });

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRolesPlataforma_RolId",
            table: "UsuarioRolesPlataforma",
            column: "RolId");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioRolesPlataforma_UsuarioId_Activa",
            table: "UsuarioRolesPlataforma",
            columns: new[] { "UsuarioId", "Activa" });

        migrationBuilder.CreateIndex(
            name: "UX_UsuarioRolesPlataforma_UsuarioId_RolId",
            table: "UsuarioRolesPlataforma",
            columns: new[] { "UsuarioId", "RolId" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Roles_Empresas_EmpresaId",
            table: "Roles",
            column: "EmpresaId",
            principalTable: "Empresas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_RegistrosAuditoria_Empresas_EmpresaId",
            table: "RegistrosAuditoria",
            column: "EmpresaId",
            principalTable: "Empresas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Roles_Ambito",
            table: "Roles",
            sql: "`Ambito` IN (1, 2)");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Roles_Ambito_Empresa",
            table: "Roles",
            sql: "(`Ambito` = 2 AND `EmpresaId` IS NULL) OR (`Ambito` = 1)");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Roles_EmpresaId_Positivo",
            table: "Roles",
            sql: "`EmpresaId` IS NULL OR `EmpresaId` > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Permisos_Ambito",
            table: "Permisos",
            sql: "`Ambito` IN (1, 2)");
        migrationBuilder.AddCheckConstraint(
            name: "CK_RegistrosAuditoria_AmbitoAutoridad",
            table: "RegistrosAuditoria",
            sql: "`AmbitoAutoridad` IS NULL OR `AmbitoAutoridad` IN (1, 2)");
        migrationBuilder.AddCheckConstraint(
            name: "CK_RegistrosAuditoria_OrigenAutorizacion",
            table: "RegistrosAuditoria",
            sql: "`OrigenAutorizacion` IS NULL OR `OrigenAutorizacion` IN (1, 2)");
        migrationBuilder.AddCheckConstraint(
            name: "CK_RegistrosAuditoria_EmpresaId_Positivo",
            table: "RegistrosAuditoria",
            sql: "`EmpresaId` IS NULL OR `EmpresaId` > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_UsuarioRolesPlataforma_UsuarioId",
            table: "UsuarioRolesPlataforma",
            sql: "`UsuarioId` > 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_UsuarioRolesPlataforma_RolId",
            table: "UsuarioRolesPlataforma",
            sql: "`RolId` > 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UsuarioRolesPlataforma");

        migrationBuilder.DropForeignKey(
            name: "FK_RegistrosAuditoria_Empresas_EmpresaId",
            table: "RegistrosAuditoria");
        migrationBuilder.DropForeignKey(
            name: "FK_Roles_Empresas_EmpresaId",
            table: "Roles");

        migrationBuilder.DropIndex(
            name: "IX_RegistrosAuditoria_Ambito_Fecha",
            table: "RegistrosAuditoria");
        migrationBuilder.DropIndex(
            name: "IX_RegistrosAuditoria_EmpresaId",
            table: "RegistrosAuditoria");
        migrationBuilder.DropIndex(
            name: "IX_Permisos_Ambito_Activo",
            table: "Permisos");
        migrationBuilder.DropIndex(
            name: "IX_Roles_Ambito_Activo",
            table: "Roles");
        migrationBuilder.DropIndex(
            name: "IX_Roles_EmpresaId",
            table: "Roles");

        migrationBuilder.DropCheckConstraint("CK_RegistrosAuditoria_AmbitoAutoridad", "RegistrosAuditoria");
        migrationBuilder.DropCheckConstraint("CK_RegistrosAuditoria_OrigenAutorizacion", "RegistrosAuditoria");
        migrationBuilder.DropCheckConstraint("CK_RegistrosAuditoria_EmpresaId_Positivo", "RegistrosAuditoria");
        migrationBuilder.DropCheckConstraint("CK_Permisos_Ambito", "Permisos");
        migrationBuilder.DropCheckConstraint("CK_Roles_Ambito", "Roles");
        migrationBuilder.DropCheckConstraint("CK_Roles_Ambito_Empresa", "Roles");
        migrationBuilder.DropCheckConstraint("CK_Roles_EmpresaId_Positivo", "Roles");

        migrationBuilder.DropColumn(name: "AmbitoAutoridad", table: "RegistrosAuditoria");
        migrationBuilder.DropColumn(name: "OrigenAutorizacion", table: "RegistrosAuditoria");
        migrationBuilder.DropColumn(name: "EmpresaId", table: "RegistrosAuditoria");
        migrationBuilder.DropColumn(name: "Ambito", table: "Permisos");
        migrationBuilder.DropColumn(name: "EmpresaId", table: "Roles");
        migrationBuilder.DropColumn(name: "Ambito", table: "Roles");
    }
}
