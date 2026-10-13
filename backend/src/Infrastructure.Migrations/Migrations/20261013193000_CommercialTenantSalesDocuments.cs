using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261013193000_CommercialTenantSalesDocuments")]
public partial class CommercialTenantSalesDocuments : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Unicidad lógica de idempotencia por empresa, no a nivel plataforma.
        m.DropIndex("UX_PedidosVenta_CotizacionId", "PedidosVenta");
        m.DropIndex("UX_PedidosVenta_IdempotencyKey", "PedidosVenta");

        foreach (var table in new[]
        {
            "Cotizaciones", "CotizacionDetalles", "PedidosVenta", "PedidoVentaDetalles"
        })
        {
            m.AddColumn<int>("EmpresaId", table, type: "int", nullable: true);
            m.AddForeignKey($"FK_{table}_Empresas_EmpresaId", table, "EmpresaId",
                "Empresas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }

        m.CreateIndex("UX_Cotizaciones_Empresa_Id", "Cotizaciones",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_Cotizaciones_Empresa_ClienteId", "Cotizaciones",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("IX_CotizacionDetalles_Empresa_CotizacionId", "CotizacionDetalles",
            new[] { "EmpresaId", "CotizacionId" });
        m.CreateIndex("IX_CotizacionDetalles_Empresa_ProductoId", "CotizacionDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_CotizacionDetalles_Empresa_ProductoVarianteId", "CotizacionDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });
        m.CreateIndex("UX_PedidosVenta_Empresa_Id", "PedidosVenta",
            new[] { "EmpresaId", "Id" }, unique: true);
        m.CreateIndex("IX_PedidosVenta_Empresa_ClienteId", "PedidosVenta",
            new[] { "EmpresaId", "ClienteId" });
        m.CreateIndex("UX_PedidosVenta_Empresa_CotizacionId", "PedidosVenta",
            new[] { "EmpresaId", "CotizacionId" }, unique: true);
        m.CreateIndex("UX_PedidosVenta_Empresa_IdempotencyKey", "PedidosVenta",
            new[] { "EmpresaId", "IdempotencyKey" }, unique: true);
        m.CreateIndex("IX_PedidoVentaDetalles_Empresa_PedidoVentaId", "PedidoVentaDetalles",
            new[] { "EmpresaId", "PedidoVentaId" });
        m.CreateIndex("IX_PedidoVentaDetalles_Empresa_ProductoId", "PedidoVentaDetalles",
            new[] { "EmpresaId", "ProductoId" });
        m.CreateIndex("IX_PedidoVentaDetalles_Empresa_ProductoVarianteId", "PedidoVentaDetalles",
            new[] { "EmpresaId", "ProductoVarianteId" });

        m.Sql("""ALTER TABLE `Cotizaciones` ADD CONSTRAINT `FK_Cotizaciones_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PedidosVenta` ADD CONSTRAINT `FK_PedidosVenta_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PedidosVenta` ADD CONSTRAINT `FK_PedidosVenta_Cotizacion_Tenant` FOREIGN KEY (`EmpresaId`, `CotizacionId`) REFERENCES `Cotizaciones` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Cotizacion_Tenant` FOREIGN KEY (`EmpresaId`, `CotizacionId`) REFERENCES `Cotizaciones` (`EmpresaId`, `Id`) ON DELETE CASCADE;""");
        m.Sql("""ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Pedido_Tenant` FOREIGN KEY (`EmpresaId`, `PedidoVentaId`) REFERENCES `PedidosVenta` (`EmpresaId`, `Id`) ON DELETE CASCADE;""");
        m.Sql("""ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
        m.Sql("""ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""");
    }
    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Multiempresa no permite rollback DDL destructivo sin restaurar backup DEV.");
}
