using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013210000_CommercialTenantTaxDiscountExpand")]
public partial class CommercialTenantTaxDiscountExpand : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<int>("EmpresaId", "VentaImpuestos", type: "int", nullable: true);
        m.CreateIndex("IX_VentaImpuestos_Empresa_VentaId", "VentaImpuestos",
            new[] { "EmpresaId", "VentaId" });
        m.AddForeignKey("FK_VentaImpuestos_Empresas_EmpresaId", "VentaImpuestos",
            "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddColumn<int>("EmpresaId", "CompraImpuestos", type: "int", nullable: true);
        m.CreateIndex("IX_CompraImpuestos_Empresa_CompraId", "CompraImpuestos",
            new[] { "EmpresaId", "CompraId" });
        m.AddForeignKey("FK_CompraImpuestos_Empresas_EmpresaId", "CompraImpuestos",
            "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddColumn<int>("EmpresaId", "VentaDescuentos", type: "int", nullable: true);
        m.CreateIndex("IX_VentaDescuentos_Empresa_VentaId", "VentaDescuentos",
            new[] { "EmpresaId", "VentaId" });
        m.AddForeignKey("FK_VentaDescuentos_Empresas_EmpresaId", "VentaDescuentos",
            "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.AddColumn<int>("EmpresaId", "HistorialUsoDescuentos", type: "int", nullable: true);
        m.CreateIndex("IX_HistorialUsoDescuentos_Empresa_VentaId", "HistorialUsoDescuentos",
            new[] { "EmpresaId", "VentaId" });
        m.AddForeignKey("FK_HistorialUsoDescuentos_Empresas_EmpresaId", "HistorialUsoDescuentos",
            "EmpresaId", "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        m.CreateIndex("IX_HistorialUsoDescuentos_Empresa_ClienteId",
            "HistorialUsoDescuentos", new[] { "EmpresaId", "ClienteId" });
        m.Sql("""ALTER TABLE `VentaImpuestos` ADD CONSTRAINT `FK_VentaImpuestos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""");
        m.Sql("""ALTER TABLE `CompraImpuestos` ADD CONSTRAINT `FK_CompraImpuestos_Compra_Tenant` FOREIGN KEY (`EmpresaId`, `CompraId`) REFERENCES `Compras` (`EmpresaId`, `Id`) ON DELETE CASCADE;""");
        m.Sql("""ALTER TABLE `VentaDescuentos` ADD CONSTRAINT `FK_VentaDescuentos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""");
        m.Sql("""ALTER TABLE `HistorialUsoDescuentos` ADD CONSTRAINT `FK_HistorialUsoDescuentos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `HistorialUsoDescuentos` ADD CONSTRAINT `FK_HistorialUsoDescuentos_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
    }

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Rollback destructivo de EmpresaId prohibido; restaurar backup DEV verificado.");
}
