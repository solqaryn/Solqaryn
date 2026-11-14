using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>Contrato MySQL físico de pertenencia de impuestos/descuentos aplicados.</summary>
public static class CommercialTenantTaxDiscountPhysicalConstraints
{
    private static readonly string[] Statements =
    {
        """ALTER TABLE `VentaImpuestos` ADD CONSTRAINT `FK_VentaImpuestos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `CompraImpuestos` ADD CONSTRAINT `FK_CompraImpuestos_Compra_Tenant` FOREIGN KEY (`EmpresaId`, `CompraId`) REFERENCES `Compras` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `VentaDescuentos` ADD CONSTRAINT `FK_VentaDescuentos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `HistorialUsoDescuentos` ADD CONSTRAINT `FK_HistorialUsoDescuentos_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `HistorialUsoDescuentos` ADD CONSTRAINT `FK_HistorialUsoDescuentos_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;"""
    };

    public static async Task ApplyFreshAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var statement in Statements)
            await db.Database.ExecuteSqlRawAsync(statement, ct);
    }

    public static async Task VerifyAsync(AppDbContext db, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) AS Value FROM (
                SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                  AND CONSTRAINT_NAME IN (
                    'FK_VentaImpuestos_Venta_Tenant',
                    'FK_CompraImpuestos_Compra_Tenant',
                    'FK_VentaDescuentos_Venta_Tenant',
                    'FK_HistorialUsoDescuentos_Venta_Tenant',
                    'FK_HistorialUsoDescuentos_Cliente_Tenant'
                  )
                GROUP BY CONSTRAINT_NAME
                HAVING COUNT(*) = 2
            ) c
            """;
        var count = await db.Database.SqlQueryRaw<long>(sql).SingleAsync(ct);
        if (count != 5)
            throw new InvalidOperationException(
                $"Faltan FKs tenant para impuestos/descuentos aplicados: {count}/5.");
    }
}
