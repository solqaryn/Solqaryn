using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261013121500_CommercialTenantRootExpand")]
    public partial class CommercialTenantRootExpand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Expand no destructivo: conservar NULL en registros históricos.
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_EmpresaId",
                table: "Productos",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Empresas_EmpresaId",
                table: "Productos",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Clientes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_EmpresaId",
                table: "Clientes",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Empresas_EmpresaId",
                table: "Clientes",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_EmpresaId",
                table: "Proveedores",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proveedores_Empresas_EmpresaId",
                table: "Proveedores",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Compras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_EmpresaId",
                table: "Compras",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Empresas_EmpresaId",
                table: "Compras",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_EmpresaId",
                table: "Ventas",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Empresas_EmpresaId",
                table: "Ventas",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No eliminar ownership ya atribuido mediante un rollback destructivo.
            throw new NotSupportedException(
                "CommercialTenantRootExpand no admite rollback destructivo: restaurar backup DEV certificado.");
        }
    }
}
