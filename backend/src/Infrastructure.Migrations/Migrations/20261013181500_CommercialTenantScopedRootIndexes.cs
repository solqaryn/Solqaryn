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
            // Desactivar unicidades globales antes de introducir claves por tenant.
            migrationBuilder.DropIndex("IX_ProductoVariantes_Sku", "ProductoVariantes");
            migrationBuilder.DropIndex("IX_ProductoVariantes_CodigoBarras", "ProductoVariantes");
            migrationBuilder.DropIndex("IX_Facturas_NumeroFactura", "Facturas");

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
            migrationBuilder.AddColumn<int>("EmpresaId", "ProductoVariantes", "int", nullable: true);
            migrationBuilder.CreateIndex("IX_ProductoVariantes_Empresa_Referencia", "ProductoVariantes", new[] { "EmpresaId", "ProductoId" });
            migrationBuilder.AddForeignKey("FK_ProductoVariantes_Empresas_EmpresaId", "ProductoVariantes", "EmpresaId", "Empresas", "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.CreateIndex("UX_ProductoVariantes_Empresa_Sku", "ProductoVariantes", new[] { "EmpresaId", "Sku" }, unique: true);
            migrationBuilder.CreateIndex("UX_ProductoVariantes_Empresa_CodigoBarras", "ProductoVariantes", new[] { "EmpresaId", "CodigoBarras" }, unique: true);

            migrationBuilder.AddColumn<int>("EmpresaId", "ProductoImagenes", "int", nullable: true);
            migrationBuilder.CreateIndex("IX_ProductoImagenes_Empresa_Referencia", "ProductoImagenes", new[] { "EmpresaId", "ProductoId" });
            migrationBuilder.AddForeignKey("FK_ProductoImagenes_Empresas_EmpresaId", "ProductoImagenes", "EmpresaId", "Empresas", "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>("EmpresaId", "VentaDetalles", "int", nullable: true);
            migrationBuilder.CreateIndex("IX_VentaDetalles_Empresa_Referencia", "VentaDetalles", new[] { "EmpresaId", "VentaId" });
            migrationBuilder.AddForeignKey("FK_VentaDetalles_Empresas_EmpresaId", "VentaDetalles", "EmpresaId", "Empresas", "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>("EmpresaId", "CompraDetalles", "int", nullable: true);
            migrationBuilder.CreateIndex("IX_CompraDetalles_Empresa_Referencia", "CompraDetalles", new[] { "EmpresaId", "CompraId" });
            migrationBuilder.AddForeignKey("FK_CompraDetalles_Empresas_EmpresaId", "CompraDetalles", "EmpresaId", "Empresas", "Id", onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<int>("EmpresaId", "Facturas", "int", nullable: true);
            migrationBuilder.CreateIndex("IX_Facturas_Empresa_Referencia", "Facturas", new[] { "EmpresaId", "VentaId" });
            migrationBuilder.AddForeignKey("FK_Facturas_Empresas_EmpresaId", "Facturas", "EmpresaId", "Empresas", "Id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.CreateIndex("UX_Facturas_Empresa_NumeroFactura", "Facturas", new[] { "EmpresaId", "NumeroFactura" }, unique: true);
            migrationBuilder.CreateIndex("IX_Ventas_Empresa_Cliente", "Ventas", new[] { "EmpresaId", "ClienteId" });
            migrationBuilder.CreateIndex("IX_Compras_Empresa_Proveedor", "Compras", new[] { "EmpresaId", "ProveedorId" });
            migrationBuilder.CreateIndex("UX_ProductoVariantes_Empresa_Id", "ProductoVariantes", new[] { "EmpresaId", "Id" }, unique: true);
            migrationBuilder.CreateIndex("IX_ProductoImagenes_Empresa_Variante", "ProductoImagenes", new[] { "EmpresaId", "ProductoVarianteId" });
            migrationBuilder.CreateIndex("IX_VentaDetalles_Empresa_Producto", "VentaDetalles", new[] { "EmpresaId", "ProductoId" });
            migrationBuilder.CreateIndex("IX_VentaDetalles_Empresa_Variante", "VentaDetalles", new[] { "EmpresaId", "ProductoVarianteId" });
            migrationBuilder.CreateIndex("IX_CompraDetalles_Empresa_Producto", "CompraDetalles", new[] { "EmpresaId", "ProductoId" });
            migrationBuilder.CreateIndex("IX_CompraDetalles_Empresa_Variante", "CompraDetalles", new[] { "EmpresaId", "ProductoVarianteId" });
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
