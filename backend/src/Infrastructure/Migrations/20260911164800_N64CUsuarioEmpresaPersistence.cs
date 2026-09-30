using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// N6.4.C: materializa la membresía tenant-aware Usuario↔Empresa.
///
/// La membresía tiene identidad técnica propia, pero la regla de negocio se protege
/// físicamente con unicidad (UsuarioId, EmpresaId). Los usuarios legacy no contienen
/// EmpresaId persistido; por eso esta migración no inventa membresías durante el
/// backfill. Permanecen sin fila en UsuarioEmpresas hasta que una unidad posterior
/// realice una asignación explícita y auditable, manteniendo semántica fail-closed.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260911164800_N64CUsuarioEmpresaPersistence")]
public partial class N64CUsuarioEmpresaPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UsuarioEmpresas",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UsuarioId = table.Column<int>(type: "int", nullable: false),
                EmpresaId = table.Column<int>(type: "int", nullable: false),
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
                table.PrimaryKey("PK_UsuarioEmpresas", x => x.Id);
                table.ForeignKey(
                    name: "FK_UsuarioEmpresas_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioEmpresas_Empresas_EmpresaId",
                    column: x => x.EmpresaId,
                    principalTable: "Empresas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_UsuarioEmpresas_Roles_RolId",
                    column: x => x.RolId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioEmpresas_EmpresaId_Activa",
            table: "UsuarioEmpresas",
            columns: new[] { "EmpresaId", "Activa" });

        migrationBuilder.CreateIndex(
            name: "IX_UsuarioEmpresas_RolId",
            table: "UsuarioEmpresas",
            column: "RolId");

        migrationBuilder.CreateIndex(
            name: "UX_UsuarioEmpresas_UsuarioId_EmpresaId",
            table: "UsuarioEmpresas",
            columns: new[] { "UsuarioId", "EmpresaId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "UsuarioEmpresas");
    }
}
