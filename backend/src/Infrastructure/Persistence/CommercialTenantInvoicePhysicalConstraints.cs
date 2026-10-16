using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>FKs compuestas verificadas para documentos comerciales dependientes.</summary>
public static class CommercialTenantInvoicePhysicalConstraints
{
    private static readonly string[] Statements =
    {
        """ALTER TABLE `FacturaDetalles` ADD CONSTRAINT `FK_FacturaDetalles_Factura_Tenant` FOREIGN KEY (`EmpresaId`, `FacturaId`) REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `FacturaPagos` ADD CONSTRAINT `FK_FacturaPagos_Factura_Tenant` FOREIGN KEY (`EmpresaId`, `FacturaId`) REFERENCES `Facturas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `CompraDocumentos` ADD CONSTRAINT `FK_CompraDocumentos_Compra_Tenant` FOREIGN KEY (`EmpresaId`, `CompraId`) REFERENCES `Compras` (`EmpresaId`, `Id`) ON DELETE RESTRICT;"""
    };
    public static async Task ApplyFreshAsync(AppDbContext db, CancellationToken ct = default)
    {
        foreach (var statement in Statements)
            await db.Database.ExecuteSqlRawAsync(statement, ct);
    }
    public static async Task VerifyAsync(AppDbContext db, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*) AS Value
            FROM (
                SELECT CONSTRAINT_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE TABLE_SCHEMA = DATABASE()
                  AND CONSTRAINT_NAME IN (
                    'FK_FacturaDetalles_Factura_Tenant',
                    'FK_FacturaPagos_Factura_Tenant',
                    'FK_CompraDocumentos_Compra_Tenant'
                  )
                GROUP BY CONSTRAINT_NAME
                HAVING COUNT(*) = 2
            ) AS verified
            """;
        var count = await db.Database.SqlQueryRaw<long>(sql).SingleAsync(ct);
        if (count != 3)
            throw new InvalidOperationException(
                $"Contrato físico de documentos multiempresa incompleto: {count}/3.");
    }
}
