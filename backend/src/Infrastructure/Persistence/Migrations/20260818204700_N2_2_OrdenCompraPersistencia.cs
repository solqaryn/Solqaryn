using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// ERP-N2.2.C: persistencia aditiva y fail-closed de órdenes de compra.
/// La orden continúa siendo documental: no recibe mercancía ni afecta stock, Kardex, costeo o finanzas.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260818204700_N2_2_OrdenCompraPersistencia")]
public sealed class N2_2_OrdenCompraPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22CGuard;
            CREATE TEMPORARY TABLE __N22CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('OrdenesCompra', 'OrdenCompraDetalles');

            INSERT INTO __N22CGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('Proveedores', 'SolicitudesCompra', 'Productos', 'ProductoVariantes');

            DROP TEMPORARY TABLE __N22CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `OrdenesCompra` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `NumeroOrden` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
                `Estado` int NOT NULL,
                `SolicitudCompraId` int NULL,
                `ProveedorId` int NOT NULL,
                `ProveedorNombreSnapshot` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
                `ProveedorDocumentoSnapshot` varchar(120) CHARACTER SET utf8mb4 NULL,
                `Moneda` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
                `CondicionesCompra` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `FechaEsperadaUtc` datetime(6) NULL,
                `Observaciones` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `FechaEnvioAprobacionUtc` datetime(6) NULL,
                `EnviadaAprobacionPorUsuarioId` int NULL,
                `FechaAprobacionUtc` datetime(6) NULL,
                `AprobadaPorUsuarioId` int NULL,
                `AprobadaPorNombreSnapshot` varchar(150) CHARACTER SET utf8mb4 NULL,
                `FechaCancelacionUtc` datetime(6) NULL,
                `CanceladaPorUsuarioId` int NULL,
                `MotivoCancelacion` varchar(500) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_OrdenesCompra` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_OrdenesCompra_Estado_Valido`
                    CHECK (`Estado` IN (1, 2, 3, 4)),
                CONSTRAINT `CK_OrdenesCompra_Moneda_ISO3`
                    CHECK (CHAR_LENGTH(TRIM(`Moneda`)) = 3),
                CONSTRAINT `CK_OrdenesCompra_AprobacionConsistente`
                    CHECK ((`Estado` = 1 AND `FechaEnvioAprobacionUtc` IS NULL AND `EnviadaAprobacionPorUsuarioId` IS NULL AND `FechaAprobacionUtc` IS NULL AND `AprobadaPorUsuarioId` IS NULL)
                        OR (`Estado` = 2 AND `FechaEnvioAprobacionUtc` IS NOT NULL AND `EnviadaAprobacionPorUsuarioId` IS NOT NULL AND `FechaAprobacionUtc` IS NULL AND `AprobadaPorUsuarioId` IS NULL)
                        OR (`Estado` = 3 AND `FechaEnvioAprobacionUtc` IS NOT NULL AND `EnviadaAprobacionPorUsuarioId` IS NOT NULL AND `FechaAprobacionUtc` IS NOT NULL AND `AprobadaPorUsuarioId` IS NOT NULL)
                        OR (`Estado` = 4)),
                CONSTRAINT `CK_OrdenesCompra_CancelacionConsistente`
                    CHECK ((`Estado` <> 4 AND `FechaCancelacionUtc` IS NULL AND `CanceladaPorUsuarioId` IS NULL AND `MotivoCancelacion` IS NULL)
                        OR (`Estado` = 4 AND `FechaCancelacionUtc` IS NOT NULL AND `CanceladaPorUsuarioId` IS NOT NULL AND `MotivoCancelacion` IS NOT NULL AND CHAR_LENGTH(TRIM(`MotivoCancelacion`)) > 0)),
                CONSTRAINT `FK_OrdenesCompra_Proveedores_ProveedorId`
                    FOREIGN KEY (`ProveedorId`) REFERENCES `Proveedores` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_OrdenesCompra_SolicitudesCompra_SolicitudCompraId`
                    FOREIGN KEY (`SolicitudCompraId`) REFERENCES `SolicitudesCompra` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_OrdenesCompra_NumeroOrden`
                ON `OrdenesCompra` (`NumeroOrden`);
            CREATE INDEX `IX_OrdenesCompra_Estado_FechaEsperada`
                ON `OrdenesCompra` (`Estado`, `FechaEsperadaUtc`);
            CREATE INDEX `IX_OrdenesCompra_ProveedorId`
                ON `OrdenesCompra` (`ProveedorId`);
            CREATE INDEX `IX_OrdenesCompra_SolicitudCompraId`
                ON `OrdenesCompra` (`SolicitudCompraId`);

            CREATE TABLE `OrdenCompraDetalles` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `OrdenCompraId` int NOT NULL,
                `ProductoId` int NOT NULL,
                `ProductoVarianteId` int NULL,
                `CantidadOrdenada` decimal(18,4) NOT NULL,
                `PrecioUnitario` decimal(18,4) NOT NULL,
                `Descuento` decimal(18,4) NOT NULL,
                `Impuesto` decimal(18,4) NOT NULL,
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
                CONSTRAINT `PK_OrdenCompraDetalles` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_OrdenCompraDetalles_CantidadPositiva`
                    CHECK (`CantidadOrdenada` > 0),
                CONSTRAINT `CK_OrdenCompraDetalles_PrecioNoNegativo`
                    CHECK (`PrecioUnitario` >= 0),
                CONSTRAINT `CK_OrdenCompraDetalles_DescuentoNoNegativo`
                    CHECK (`Descuento` >= 0),
                CONSTRAINT `CK_OrdenCompraDetalles_ImpuestoNoNegativo`
                    CHECK (`Impuesto` >= 0),
                CONSTRAINT `CK_OrdenCompraDetalles_DescuentoNoSuperaSubtotal`
                    CHECK (`Descuento` <= (`CantidadOrdenada` * `PrecioUnitario`)),
                CONSTRAINT `FK_OrdenCompraDetalles_OrdenesCompra_OrdenCompraId`
                    FOREIGN KEY (`OrdenCompraId`) REFERENCES `OrdenesCompra` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_OrdenCompraDetalles_Productos_ProductoId`
                    FOREIGN KEY (`ProductoId`) REFERENCES `Productos` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_OrdenCompraDetalles_ProductoVariantes_ProductoVarianteId`
                    FOREIGN KEY (`ProductoVarianteId`) REFERENCES `ProductoVariantes` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE INDEX `IX_OrdenCompraDetalles_OrdenCompraId`
                ON `OrdenCompraDetalles` (`OrdenCompraId`);
            CREATE INDEX `IX_OrdenCompraDetalles_Producto_Variante`
                ON `OrdenCompraDetalles` (`ProductoId`, `ProductoVarianteId`);
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_OrdenCompraDetalles_ProductoVariante_Insert
            BEFORE INSERT ON OrdenCompraDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.ProductoVarianteId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM ProductoVariantes v
                     WHERE v.Id = NEW.ProductoVarianteId
                       AND v.ProductoId = NEW.ProductoId
                       AND v.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La variante debe pertenecer al producto de la orden y no estar eliminada.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_OrdenCompraDetalles_ProductoVariante_Update
            BEFORE UPDATE ON OrdenCompraDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.ProductoVarianteId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM ProductoVariantes v
                     WHERE v.Id = NEW.ProductoVarianteId
                       AND v.ProductoId = NEW.ProductoId
                       AND v.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La variante debe pertenecer al producto de la orden y no estar eliminada.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22CPostGuard;
            CREATE TEMPORARY TABLE __N22CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('OrdenesCompra', 'OrdenCompraDetalles');

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.triggers
             WHERE trigger_schema = DATABASE()
               AND trigger_name IN (
                    'TR_OrdenCompraDetalles_ProductoVariante_Insert',
                    'TR_OrdenCompraDetalles_ProductoVariante_Update');

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 9 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_OrdenesCompra_Estado_Valido',
                    'CK_OrdenesCompra_Moneda_ISO3',
                    'CK_OrdenesCompra_AprobacionConsistente',
                    'CK_OrdenesCompra_CancelacionConsistente',
                    'CK_OrdenCompraDetalles_CantidadPositiva',
                    'CK_OrdenCompraDetalles_PrecioNoNegativo',
                    'CK_OrdenCompraDetalles_DescuentoNoNegativo',
                    'CK_OrdenCompraDetalles_ImpuestoNoNegativo',
                    'CK_OrdenCompraDetalles_DescuentoNoSuperaSubtotal');

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 4, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_name IN (
                    'FK_OrdenesCompra_Proveedores_ProveedorId',
                    'FK_OrdenesCompra_SolicitudesCompra_SolicitudCompraId',
                    'FK_OrdenCompraDetalles_OrdenesCompra_OrdenCompraId',
                    'FK_OrdenCompraDetalles_Productos_ProductoId',
                    'FK_OrdenCompraDetalles_ProductoVariantes_ProductoVarianteId');

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 5, CASE WHEN COUNT(DISTINCT index_name) = 6 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND index_name IN (
                    'UX_OrdenesCompra_NumeroOrden',
                    'IX_OrdenesCompra_Estado_FechaEsperada',
                    'IX_OrdenesCompra_ProveedorId',
                    'IX_OrdenesCompra_SolicitudCompraId',
                    'IX_OrdenCompraDetalles_OrdenCompraId',
                    'IX_OrdenCompraDetalles_Producto_Variante');

            INSERT INTO __N22CPostGuard (Id, Violaciones)
            SELECT 6,
                   (SELECT COUNT(*) FROM OrdenesCompra) +
                   (SELECT COUNT(*) FROM OrdenCompraDetalles);

            DROP TEMPORARY TABLE __N22CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N22CDownGuard;
            CREATE TEMPORARY TABLE __N22CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N22C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N22CDownGuard (Id, Violaciones)
            SELECT 1,
                   (SELECT COUNT(*) FROM OrdenesCompra) +
                   (SELECT COUNT(*) FROM OrdenCompraDetalles);

            DROP TEMPORARY TABLE __N22CDownGuard;
            """);

        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_OrdenCompraDetalles_ProductoVariante_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_OrdenCompraDetalles_ProductoVariante_Insert;");
        migrationBuilder.DropTable(name: "OrdenCompraDetalles");
        migrationBuilder.DropTable(name: "OrdenesCompra");
    }
}
