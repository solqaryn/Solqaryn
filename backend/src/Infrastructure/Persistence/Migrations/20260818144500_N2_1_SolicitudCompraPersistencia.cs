using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// ERP-N2.1.C: persistencia aditiva de solicitudes de compra.
/// No crea compras, stock, Kardex, costeo ni movimientos financieros y no backfillea historico.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260818144500_N2_1_SolicitudCompraPersistencia")]
public sealed class N2_1_SolicitudCompraPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N21CGuard;
            CREATE TEMPORARY TABLE __N21CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N21C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N21CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('SolicitudesCompra', 'SolicitudCompraDetalles');

            INSERT INTO __N21CGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('Proveedores', 'Productos', 'ProductoVariantes');

            DROP TEMPORARY TABLE __N21CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `SolicitudesCompra` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `NumeroSolicitud` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
                `Estado` int NOT NULL,
                `ProveedorId` int NULL,
                `Notas` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `FechaSolicitudUtc` datetime(6) NULL,
                `SolicitadaPorUsuarioId` int NULL,
                `SolicitadaPorNombreSnapshot` varchar(150) CHARACTER SET utf8mb4 NULL,
                `FechaDecisionUtc` datetime(6) NULL,
                `DecididaPorUsuarioId` int NULL,
                `DecididaPorNombreSnapshot` varchar(150) CHARACTER SET utf8mb4 NULL,
                `MotivoRechazo` varchar(500) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_SolicitudesCompra` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_SolicitudesCompra_Estado_Valido`
                    CHECK (`Estado` IN (1, 2, 3, 4)),
                CONSTRAINT `CK_SolicitudesCompra_SolicitudConsistente`
                    CHECK ((`Estado` = 1 AND `FechaSolicitudUtc` IS NULL AND `SolicitadaPorUsuarioId` IS NULL)
                        OR (`Estado` IN (2, 3, 4) AND `FechaSolicitudUtc` IS NOT NULL AND `SolicitadaPorUsuarioId` IS NOT NULL)),
                CONSTRAINT `CK_SolicitudesCompra_DecisionConsistente`
                    CHECK ((`Estado` IN (1, 2) AND `FechaDecisionUtc` IS NULL AND `DecididaPorUsuarioId` IS NULL AND `MotivoRechazo` IS NULL)
                        OR (`Estado` = 3 AND `FechaDecisionUtc` IS NOT NULL AND `DecididaPorUsuarioId` IS NOT NULL AND `MotivoRechazo` IS NULL)
                        OR (`Estado` = 4 AND `FechaDecisionUtc` IS NOT NULL AND `DecididaPorUsuarioId` IS NOT NULL AND `MotivoRechazo` IS NOT NULL AND CHAR_LENGTH(TRIM(`MotivoRechazo`)) > 0)),
                CONSTRAINT `FK_SolicitudesCompra_Proveedores_ProveedorId`
                    FOREIGN KEY (`ProveedorId`) REFERENCES `Proveedores` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_SolicitudesCompra_NumeroSolicitud`
                ON `SolicitudesCompra` (`NumeroSolicitud`);
            CREATE INDEX `IX_SolicitudesCompra_Estado_FechaSolicitud`
                ON `SolicitudesCompra` (`Estado`, `FechaSolicitudUtc`);
            CREATE INDEX `IX_SolicitudesCompra_ProveedorId`
                ON `SolicitudesCompra` (`ProveedorId`);

            CREATE TABLE `SolicitudCompraDetalles` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `SolicitudCompraId` int NOT NULL,
                `ProductoId` int NOT NULL,
                `ProductoVarianteId` int NULL,
                `CantidadSolicitada` decimal(18,4) NOT NULL,
                `CostoEstimadoUnitario` decimal(18,4) NULL,
                `Observacion` varchar(500) CHARACTER SET utf8mb4 NULL,
                `ProductoSkuSnapshot` varchar(120) CHARACTER SET utf8mb4 NULL,
                `ProductoNombreSnapshot` varchar(250) CHARACTER SET utf8mb4 NULL,
                `ProductoMarcaSnapshot` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ProductoModeloSnapshot` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ProductoColorSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `ProductoTallaSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_SolicitudCompraDetalles` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_SolicitudCompraDetalles_CantidadPositiva`
                    CHECK (`CantidadSolicitada` > 0),
                CONSTRAINT `CK_SolicitudCompraDetalles_CostoNoNegativo`
                    CHECK (`CostoEstimadoUnitario` IS NULL OR `CostoEstimadoUnitario` >= 0),
                CONSTRAINT `FK_SolicitudCompraDetalles_SolicitudesCompra_SolicitudCompraId`
                    FOREIGN KEY (`SolicitudCompraId`) REFERENCES `SolicitudesCompra` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_SolicitudCompraDetalles_Productos_ProductoId`
                    FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_SolicitudCompraDetalles_ProductoVariantes_ProductoVarianteId`
                    FOREIGN KEY (`ProductoVarianteId`) REFERENCES `ProductoVariantes` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE INDEX `IX_SolicitudCompraDetalles_SolicitudCompraId`
                ON `SolicitudCompraDetalles` (`SolicitudCompraId`);
            CREATE INDEX `IX_SolicitudCompraDetalles_Producto_Variante`
                ON `SolicitudCompraDetalles` (`ProductoId`, `ProductoVarianteId`);
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_SolicitudCompraDetalles_ProductoVariante_Insert
            BEFORE INSERT ON SolicitudCompraDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.ProductoVarianteId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM ProductoVariantes v
                     WHERE v.Id = NEW.ProductoVarianteId
                       AND v.ProductoId = NEW.ProductoId
                       AND v.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La variante debe pertenecer al producto solicitado y no estar eliminada.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_SolicitudCompraDetalles_ProductoVariante_Update
            BEFORE UPDATE ON SolicitudCompraDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.ProductoVarianteId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM ProductoVariantes v
                     WHERE v.Id = NEW.ProductoVarianteId
                       AND v.ProductoId = NEW.ProductoId
                       AND v.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La variante debe pertenecer al producto solicitado y no estar eliminada.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N21CPostGuard;
            CREATE TEMPORARY TABLE __N21CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N21C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('SolicitudesCompra', 'SolicitudCompraDetalles');

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.triggers
             WHERE trigger_schema = DATABASE()
               AND trigger_name IN (
                    'TR_SolicitudCompraDetalles_ProductoVariante_Insert',
                    'TR_SolicitudCompraDetalles_ProductoVariante_Update');

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_SolicitudesCompra_Estado_Valido',
                    'CK_SolicitudesCompra_SolicitudConsistente',
                    'CK_SolicitudesCompra_DecisionConsistente',
                    'CK_SolicitudCompraDetalles_CantidadPositiva',
                    'CK_SolicitudCompraDetalles_CostoNoNegativo');

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 4, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_name IN (
                    'FK_SolicitudesCompra_Proveedores_ProveedorId',
                    'FK_SolicitudCompraDetalles_SolicitudesCompra_SolicitudCompraId',
                    'FK_SolicitudCompraDetalles_Productos_ProductoId',
                    'FK_SolicitudCompraDetalles_ProductoVariantes_ProductoVarianteId');

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 5, CASE WHEN COUNT(DISTINCT index_name) = 5 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND index_name IN (
                    'UX_SolicitudesCompra_NumeroSolicitud',
                    'IX_SolicitudesCompra_Estado_FechaSolicitud',
                    'IX_SolicitudesCompra_ProveedorId',
                    'IX_SolicitudCompraDetalles_SolicitudCompraId',
                    'IX_SolicitudCompraDetalles_Producto_Variante');

            INSERT INTO __N21CPostGuard (Id, Violaciones)
            SELECT 6,
                   (SELECT COUNT(*) FROM SolicitudesCompra) +
                   (SELECT COUNT(*) FROM SolicitudCompraDetalles);

            DROP TEMPORARY TABLE __N21CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N21CDownGuard;
            CREATE TEMPORARY TABLE __N21CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N21C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N21CDownGuard (Id, Violaciones)
            SELECT 1,
                   (SELECT COUNT(*) FROM SolicitudesCompra) +
                   (SELECT COUNT(*) FROM SolicitudCompraDetalles);

            DROP TEMPORARY TABLE __N21CDownGuard;
            """);

        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_SolicitudCompraDetalles_ProductoVariante_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_SolicitudCompraDetalles_ProductoVariante_Insert;");
        migrationBuilder.DropTable(name: "SolicitudCompraDetalles");
        migrationBuilder.DropTable(name: "SolicitudesCompra");
    }
}
