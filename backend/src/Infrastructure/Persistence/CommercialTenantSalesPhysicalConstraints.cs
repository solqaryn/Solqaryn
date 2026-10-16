using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>Integridad material de documentos venta: dueño del padre = dueño del hijo.</summary>
public static class CommercialTenantSalesPhysicalConstraints
{
    private static readonly string[] Statements =
    {
        """ALTER TABLE `Cotizaciones` ADD CONSTRAINT `FK_Cotizaciones_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `PedidosVenta` ADD CONSTRAINT `FK_PedidosVenta_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `PedidosVenta` ADD CONSTRAINT `FK_PedidosVenta_Cotizacion_Tenant` FOREIGN KEY (`EmpresaId`, `CotizacionId`) REFERENCES `Cotizaciones` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Cotizacion_Tenant` FOREIGN KEY (`EmpresaId`, `CotizacionId`) REFERENCES `Cotizaciones` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `CotizacionDetalles` ADD CONSTRAINT `FK_CotizacionDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Pedido_Tenant` FOREIGN KEY (`EmpresaId`, `PedidoVentaId`) REFERENCES `PedidosVenta` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `PedidoVentaDetalles` ADD CONSTRAINT `FK_PedidoVentaDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;"""
    };
    public static async Task ApplyFreshAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var sql in Statements)
            await db.Database.ExecuteSqlRawAsync(sql, ct);
    }
    public static async Task VerifyAsync(AppDbContext db, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) AS Value FROM (
                SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE() AND CONSTRAINT_NAME IN (
                  'FK_Cotizaciones_Cliente_Tenant',
                  'FK_PedidosVenta_Cliente_Tenant',
                  'FK_PedidosVenta_Cotizacion_Tenant',
                  'FK_CotizacionDetalles_Cotizacion_Tenant',
                  'FK_CotizacionDetalles_Producto_Tenant',
                  'FK_CotizacionDetalles_Variante_Tenant',
                  'FK_PedidoVentaDetalles_Pedido_Tenant',
                  'FK_PedidoVentaDetalles_Producto_Tenant',
                  'FK_PedidoVentaDetalles_Variante_Tenant'
                ) GROUP BY CONSTRAINT_NAME HAVING COUNT(*) = 2
            ) v
            """;
        var count=await db.Database.SqlQueryRaw<long>(sql).SingleAsync(ct);
        if(count!=9)
            throw new InvalidOperationException($"Cotizaciones/pedidos requieren 9 FKs tenant: {count}/9.");
    }
}
