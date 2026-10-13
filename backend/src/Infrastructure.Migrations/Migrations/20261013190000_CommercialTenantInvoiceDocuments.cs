using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013190000_CommercialTenantInvoiceDocuments")]
public partial class CommercialTenantInvoiceDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Factura es padre compuesto de pagos y lineas, sin atribuir filas legacy.
        migrationBuilder.CreateIndex(
            name: "UX_Facturas_Empresa_Id", table: "Facturas",
            columns: new[] { "EmpresaId", "Id" }, unique: true);

        foreach (var entry in new[]
        {
            (Table: "FacturaDetalles", Principal: "Factura", Key: "FacturaId"),
            (Table: "FacturaPagos", Principal: "Factura", Key: "FacturaId"),
            (Table: "CompraDocumentos", Principal: "Compra", Key: "CompraId")
        })
        {
            migrationBuilder.AddColumn<int>("EmpresaId", entry.Table, type: "int", nullable: true);
            migrationBuilder.CreateIndex(
                name: $"IX_{entry.Table}_Empresa_{entry.Principal}",
                table: entry.Table, columns: new[] { "EmpresaId", entry.Key });
            migrationBuilder.AddForeignKey(
                name: $"FK_{entry.Table}_Empresas_EmpresaId",
                table: entry.Table, column: "EmpresaId",
                principalTable: "Empresas", principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        migrationBuilder.Sql("""
            ALTER TABLE `FacturaDetalles` ADD CONSTRAINT `FK_FacturaDetalles_Factura_Tenant`
              FOREIGN KEY (`EmpresaId`, `FacturaId`)
              REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE CASCADE;
            """);
        migrationBuilder.Sql("""
            ALTER TABLE `FacturaPagos` ADD CONSTRAINT `FK_FacturaPagos_Factura_Tenant`
              FOREIGN KEY (`EmpresaId`, `FacturaId`)
              REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;
            """);
        migrationBuilder.Sql("""
            ALTER TABLE `CompraDocumentos` ADD CONSTRAINT `FK_CompraDocumentos_Compra_Tenant`
              FOREIGN KEY (`EmpresaId`, `CompraId`)
              REFERENCES `Compras` (`EmpresaId`, `Id`) ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "No destruir ownership multiempresa mediante rollback; restaurar backup DEV validado.");
}
