using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261013181500_CommercialTenantScopedRootIndexes")]
    public partial class CommercialTenantScopedRootIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No atribuir ningún fixture legacy: MySQL permite NULL en prefijos
            // de UNIQUE compuestos. Las operaciones nuevas deben tener owner RBAC.
            migrationBuilder.DropIndex(
                name: "UX_Clientes_IdentidadORTN_Normalizada", table: "Clientes");
            migrationBuilder.DropIndex(
                name: "UX_Proveedores_Documento_Normalizado", table: "Proveedores");
            migrationBuilder.DropIndex(
                name: "IX_Compras_NumeroCompra", table: "Compras");
            migrationBuilder.DropIndex(
                name: "IX_Ventas_NumeroVenta", table: "Ventas");

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_Empresa_IdentidadORTN",
                table: "Clientes", columns: new[] { "EmpresaId", "IdentidadORTNNormalizada" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "UX_Proveedores_Empresa_Documento",
                table: "Proveedores", columns: new[] { "EmpresaId", "DocumentoNormalizado" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "UX_Compras_Empresa_NumeroCompra",
                table: "Compras", columns: new[] { "EmpresaId", "NumeroCompra" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "UX_Ventas_Empresa_NumeroVenta",
                table: "Ventas", columns: new[] { "EmpresaId", "NumeroVenta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Productos_Empresa_Id", table: "Productos",
                columns: new[] { "EmpresaId", "Id" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_Empresa_Id", table: "Clientes",
                columns: new[] { "EmpresaId", "Id" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Proveedores_Empresa_Id", table: "Proveedores",
                columns: new[] { "EmpresaId", "Id" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Compras_Empresa_Id", table: "Compras",
                columns: new[] { "EmpresaId", "Id" }, unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Ventas_Empresa_Id", table: "Ventas",
                columns: new[] { "EmpresaId", "Id" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convivencia entre tenants e índices únicos globales antiguos
            // no es reversible sin comprobar duplicados por número/identidad.
            throw new NotSupportedException(
                "No revertir índices multiempresa mediante DDL destructivo; restaurar backup validado.");
        }
    }
}
