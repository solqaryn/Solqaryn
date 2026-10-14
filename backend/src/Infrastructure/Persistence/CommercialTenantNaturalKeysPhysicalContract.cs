using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Comprueba MySQL físico: 13 claves naturales únicas por EmpresaId, 13
/// índices legacy ya NO únicos y unicidad de plantillas de rol sin empresa.
/// </summary>
public static class CommercialTenantNaturalKeysPhysicalContract
{
    public static async Task VerifyAsync(AppDbContext db, CancellationToken ct = default)
    {
        var scoped = await db.Database.SqlQueryRaw<long>(
            """
            SELECT COUNT(*) AS Value FROM (
            SELECT e.Tabla, e.Indice
            FROM (
                SELECT 'DevolucionesCliente' AS Tabla, 'UX_DevolucionesCliente_Empresa_IdempotencyKey' AS Indice, 'EmpresaId,IdempotencyKey' AS Columnas
                UNION ALL SELECT 'DevolucionesProveedor' , 'UX_DevolucionesProveedor_Empresa_IdempotencyKey' , 'EmpresaId,IdempotencyKey' 
                UNION ALL SELECT 'DevolucionesProveedor' , 'UX_DevolucionesProveedor_Empresa_NumeroDevolucion' , 'EmpresaId,NumeroDevolucion' 
                UNION ALL SELECT 'OrdenesCompra' , 'UX_OrdenesCompra_Empresa_IdempotencyKey' , 'EmpresaId,IdempotencyKey' 
                UNION ALL SELECT 'OrdenesCompra' , 'UX_OrdenesCompra_Empresa_NumeroOrden' , 'EmpresaId,NumeroOrden' 
                UNION ALL SELECT 'RecepcionesCompra' , 'UX_RecepcionesCompra_Empresa_IdempotencyKey' , 'EmpresaId,IdempotencyKey' 
                UNION ALL SELECT 'RecepcionesCompra' , 'UX_RecepcionesCompra_Empresa_NumeroRecepcion' , 'EmpresaId,NumeroRecepcion' 
                UNION ALL SELECT 'ReservasInventario' , 'UX_ReservasInventario_Empresa_Numero' , 'EmpresaId,Numero' 
                UNION ALL SELECT 'SeriesInventario' , 'UX_SeriesInventario_Empresa_NumeroSerie' , 'EmpresaId,NumeroSerie' 
                UNION ALL SELECT 'SolicitudesCompra' , 'UX_SolicitudesCompra_Empresa_NumeroSolicitud' , 'EmpresaId,NumeroSolicitud' 
                UNION ALL SELECT 'Sucursales' , 'UX_Sucursales_Empresa_CodigoActivoUnico' , 'EmpresaId,CodigoActivoUnico' 
                UNION ALL SELECT 'Roles' , 'UX_Roles_Empresa_NombreNormalizado' , 'EmpresaId,NombreNormalizado' 
            ) e
            JOIN INFORMATION_SCHEMA.STATISTICS s
              ON s.TABLE_SCHEMA = DATABASE()
             AND s.TABLE_NAME = e.Tabla AND s.INDEX_NAME = e.Indice
            GROUP BY e.Tabla, e.Indice, e.Columnas
            HAVING MIN(s.NON_UNIQUE) = 0
               AND MAX(s.NON_UNIQUE) = 0
               AND GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ',') = e.Columnas
            ) AS validos
            """).SingleAsync(ct);
        if (scoped != 12)
            throw new InvalidOperationException(
                $"Unicidades empresariales incompletas: {scoped}/12.");

        var legacyNonUnique = await db.Database.SqlQueryRaw<long>(
            """
            SELECT COUNT(*) AS Value FROM (
            SELECT e.Tabla, e.Indice
            FROM (
                SELECT 'DevolucionesCliente' AS Tabla, 'UX_DevolucionesCliente_IdempotencyKey' AS Indice, 'IdempotencyKey' AS Columnas
                UNION ALL SELECT 'DevolucionesProveedor' , 'UX_DevolucionesProveedor_IdempotencyKey' , 'IdempotencyKey' 
                UNION ALL SELECT 'DevolucionesProveedor' , 'UX_DevolucionesProveedor_NumeroDevolucion' , 'NumeroDevolucion' 
                UNION ALL SELECT 'OrdenesCompra' , 'UX_OrdenesCompra_IdempotencyKey' , 'IdempotencyKey' 
                UNION ALL SELECT 'OrdenesCompra' , 'UX_OrdenesCompra_NumeroOrden' , 'NumeroOrden' 
                UNION ALL SELECT 'RecepcionesCompra' , 'UX_RecepcionesCompra_IdempotencyKey' , 'IdempotencyKey' 
                UNION ALL SELECT 'RecepcionesCompra' , 'UX_RecepcionesCompra_NumeroRecepcion' , 'NumeroRecepcion' 
                UNION ALL SELECT 'ReservasInventario' , 'UX_ReservasInventario_Numero' , 'Numero' 
                UNION ALL SELECT 'SeriesInventario' , 'UX_SeriesInventario_NumeroSerie' , 'NumeroSerie' 
                UNION ALL SELECT 'SolicitudesCompra' , 'UX_SolicitudesCompra_NumeroSolicitud' , 'NumeroSolicitud' 
                UNION ALL SELECT 'Sucursales' , 'UX_Sucursales_Codigo_Activo' , 'CodigoActivoUnico' 
                UNION ALL SELECT 'Roles' , 'IX_Roles_NombreNormalizado' , 'NombreNormalizado' 
            ) e
            JOIN INFORMATION_SCHEMA.STATISTICS s
              ON s.TABLE_SCHEMA = DATABASE()
             AND s.TABLE_NAME = e.Tabla AND s.INDEX_NAME = e.Indice
            GROUP BY e.Tabla, e.Indice, e.Columnas
            HAVING MIN(s.NON_UNIQUE) = 1
               AND MAX(s.NON_UNIQUE) = 1
               AND GROUP_CONCAT(s.COLUMN_NAME ORDER BY s.SEQ_IN_INDEX SEPARATOR ',') = e.Columnas
            ) AS validos
            """).SingleAsync(ct);
        if (legacyNonUnique != 12)
            throw new InvalidOperationException(
                $"Persisten unicidades globales de negocio: {12-legacyNonUnique}.");

        // Contrato ya integrado en la migración de cuentas de tienda:
        // conservar el nombre del índice actual y comprobar sus 2 columnas.
        var storefront = await db.Database.SqlQueryRaw<long>(
            """
            SELECT COUNT(*) AS Value FROM (
                SELECT INDEX_NAME FROM INFORMATION_SCHEMA.STATISTICS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'TiendaCuentasCliente'
                  AND INDEX_NAME = 'UX_TiendaCuentasCliente_Correo'
                GROUP BY INDEX_NAME
                HAVING COUNT(*) = 2 AND MIN(NON_UNIQUE) = 0
                   AND MAX(NON_UNIQUE) = 0
                   AND GROUP_CONCAT(COLUMN_NAME ORDER BY SEQ_IN_INDEX SEPARATOR ',')
                       = 'EmpresaId,CorreoNormalizado'
            ) x
            """).SingleAsync(ct);
        if (storefront != 1)
            throw new InvalidOperationException("La cuenta pública no tiene unicidad por tienda/Empresa.");

        var templates = await db.Database.SqlQueryRaw<long>(
            """
            SELECT COUNT(*) AS Value
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'Roles'
              AND INDEX_NAME = 'UX_Roles_NombreNormalizadoGlobalUnico'
              AND COLUMN_NAME = 'NombreNormalizadoGlobalUnico'
              AND NON_UNIQUE = 0
            """).SingleAsync(ct);
        if (templates != 1)
            throw new InvalidOperationException(
                "Los roles compartidos no tienen unicidad global independiente.");
    }
}
