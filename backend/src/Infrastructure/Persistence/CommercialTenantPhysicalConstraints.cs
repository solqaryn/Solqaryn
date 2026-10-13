using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Contrato físico adicional: empresa + referencia deben coincidir en MySQL.
/// EF10 no admite EmpresaId nullable dentro de alternate keys hasta purgar legacy,
/// por ello las FKs compuestas se versionan también como DDL canónico verificable.
/// Las nuevas escrituras reciben EmpresaId desde el guard y no desde headers.
/// </summary>
public static class CommercialTenantPhysicalConstraints
{
    private static readonly string[] CreateStatements =
    {
        """ALTER TABLE `Ventas` ADD CONSTRAINT `FK_Ventas_Cliente_Tenant` FOREIGN KEY (`EmpresaId`, `ClienteId`) REFERENCES `Clientes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `Compras` ADD CONSTRAINT `FK_Compras_Proveedor_Tenant` FOREIGN KEY (`EmpresaId`, `ProveedorId`) REFERENCES `Proveedores` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `ProductoVariantes` ADD CONSTRAINT `FK_ProductoVariantes_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `ProductoImagenes` ADD CONSTRAINT `FK_ProductoImagenes_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `ProductoImagenes` ADD CONSTRAINT `FK_ProductoImagenes_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `VentaDetalles` ADD CONSTRAINT `FK_VentaDetalles_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `VentaDetalles` ADD CONSTRAINT `FK_VentaDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `VentaDetalles` ADD CONSTRAINT `FK_VentaDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `CompraDetalles` ADD CONSTRAINT `FK_CompraDetalles_Compra_Tenant` FOREIGN KEY (`EmpresaId`, `CompraId`) REFERENCES `Compras` (`EmpresaId`, `Id`) ON DELETE CASCADE;""",
        """ALTER TABLE `CompraDetalles` ADD CONSTRAINT `FK_CompraDetalles_Producto_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoId`) REFERENCES `Productos` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `CompraDetalles` ADD CONSTRAINT `FK_CompraDetalles_Variante_Tenant` FOREIGN KEY (`EmpresaId`, `ProductoVarianteId`) REFERENCES `ProductoVariantes` (`EmpresaId`, `Id`) ON DELETE RESTRICT;""",
        """ALTER TABLE `Facturas` ADD CONSTRAINT `FK_Facturas_Venta_Tenant` FOREIGN KEY (`EmpresaId`, `VentaId`) REFERENCES `Ventas` (`EmpresaId`, `Id`) ON DELETE RESTRICT;"""
    };

    public static async Task ApplyFreshAsync(
        AppDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var statement in CreateStatements)
            await db.Database.ExecuteSqlRawAsync(statement, cancellationToken);
    }

    public static async Task VerifyAsync(
        AppDbContext db, CancellationToken cancellationToken = default)
    {
        // Una FK consta de exactamente dos columnas: EmpresaId y su referencia.
        const string sql = """
            SELECT COUNT(*) AS Value
            FROM (
              SELECT CONSTRAINT_NAME
              FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
              WHERE TABLE_SCHEMA = DATABASE()
                AND CONSTRAINT_NAME IN (
                  'FK_Ventas_Cliente_Tenant',
                  'FK_Compras_Proveedor_Tenant',
                  'FK_ProductoVariantes_Producto_Tenant',
                  'FK_ProductoImagenes_Producto_Tenant',
                  'FK_ProductoImagenes_Variante_Tenant',
                  'FK_VentaDetalles_Venta_Tenant',
                  'FK_VentaDetalles_Producto_Tenant',
                  'FK_VentaDetalles_Variante_Tenant',
                  'FK_CompraDetalles_Compra_Tenant',
                  'FK_CompraDetalles_Producto_Tenant',
                  'FK_CompraDetalles_Variante_Tenant',
                  'FK_Facturas_Venta_Tenant'
                )
              GROUP BY CONSTRAINT_NAME
              HAVING COUNT(*) = 2
            ) AS verified
            """;
        var count = await db.Database.SqlQueryRaw<long>(sql)
            .SingleAsync(cancellationToken);
        if (count != 12)
            throw new InvalidOperationException(
                $"Contrato físico de aislamiento comercial incompleto: {count}/12 FKs compuestas.");
    }
}
