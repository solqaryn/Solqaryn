using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// ERP-N1.6.C: persistencia normalizada de transferencias internas de inventario.
/// No mueve stock ni crea Kardex; esas transiciones pertenecen a N1.6.D.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260816035500_N1_6_TransferenciaInventarioPersistencia")]
public sealed class N1_6_TransferenciaInventarioPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N16CGuard;
            CREATE TEMPORARY TABLE __N16CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N16C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N16CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('TransferenciasInventario', 'TransferenciaInventarioDetalles');

            INSERT INTO __N16CGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('Almacenes', 'UbicacionesAlmacen', 'ProductoVariantes');

            DROP TEMPORARY TABLE __N16CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `TransferenciasInventario` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Numero` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
                `AlmacenOrigenId` int NOT NULL,
                `AlmacenDestinoId` int NOT NULL,
                `Estado` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
                `Observaciones` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `FechaSolicitud` datetime(6) NULL,
                `SolicitadaPorUsuarioId` int NULL,
                `FechaAprobacion` datetime(6) NULL,
                `AprobadaPorUsuarioId` int NULL,
                `FechaDespacho` datetime(6) NULL,
                `DespachadaPorUsuarioId` int NULL,
                `FechaRecepcion` datetime(6) NULL,
                `RecibidaPorUsuarioId` int NULL,
                `FechaCancelacion` datetime(6) NULL,
                `CanceladaPorUsuarioId` int NULL,
                `MotivoCancelacion` varchar(500) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_TransferenciasInventario` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_TransferenciasInventario_AlmacenesDistintos`
                    CHECK (`AlmacenOrigenId` <> `AlmacenDestinoId`),
                CONSTRAINT `CK_TransferenciasInventario_Estado_Valido`
                    CHECK (`Estado` IN ('Borrador','Solicitada','Aprobada','EnTransito','Recibida','Cancelada')),
                CONSTRAINT `FK_TransferenciasInventario_Almacenes_Origen`
                    FOREIGN KEY (`AlmacenOrigenId`) REFERENCES `Almacenes` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_TransferenciasInventario_Almacenes_Destino`
                    FOREIGN KEY (`AlmacenDestinoId`) REFERENCES `Almacenes` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_TransferenciasInventario_Numero`
                ON `TransferenciasInventario` (`Numero`);
            CREATE INDEX `IX_TransferenciasInventario_Estado_FechaSolicitud`
                ON `TransferenciasInventario` (`Estado`, `FechaSolicitud`);
            CREATE INDEX `IX_TransferenciasInventario_Origen_Estado`
                ON `TransferenciasInventario` (`AlmacenOrigenId`, `Estado`);
            CREATE INDEX `IX_TransferenciasInventario_Destino_Estado`
                ON `TransferenciasInventario` (`AlmacenDestinoId`, `Estado`);

            CREATE TABLE `TransferenciaInventarioDetalles` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `TransferenciaInventarioId` int NOT NULL,
                `ProductoVarianteId` int NOT NULL,
                `UbicacionOrigenId` int NULL,
                `UbicacionDestinoId` int NULL,
                `CantidadSolicitada` int NOT NULL,
                `CantidadAprobada` int NOT NULL DEFAULT 0,
                `CantidadDespachada` int NOT NULL DEFAULT 0,
                `CantidadRecibida` int NOT NULL DEFAULT 0,
                `CantidadFaltante` int NOT NULL DEFAULT 0,
                `CantidadSobrante` int NOT NULL DEFAULT 0,
                `CantidadDanada` int NOT NULL DEFAULT 0,
                `ProductoSkuSnapshot` varchar(80) CHARACTER SET utf8mb4 NULL,
                `ProductoMarcaSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `ProductoModeloSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `ProductoColorSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `ProductoTallaSnapshot` varchar(100) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_TransferenciaInventarioDetalles` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_TransferenciaInventarioDetalles_CantidadesNoNegativas`
                    CHECK (`CantidadSolicitada` > 0 AND `CantidadAprobada` >= 0 AND `CantidadDespachada` >= 0 AND `CantidadRecibida` >= 0 AND `CantidadFaltante` >= 0 AND `CantidadSobrante` >= 0 AND `CantidadDanada` >= 0),
                CONSTRAINT `CK_TransferenciaInventarioDetalles_Aprobada`
                    CHECK (`CantidadAprobada` <= `CantidadSolicitada`),
                CONSTRAINT `CK_TransferenciaInventarioDetalles_Despachada`
                    CHECK (`CantidadDespachada` <= `CantidadAprobada`),
                CONSTRAINT `CK_TransferenciaInventarioDetalles_Recepcion`
                    CHECK (`CantidadRecibida` + `CantidadFaltante` + `CantidadDanada` <= `CantidadDespachada`),
                CONSTRAINT `FK_TransferenciaInventarioDetalles_TransferenciasInventario`
                    FOREIGN KEY (`TransferenciaInventarioId`) REFERENCES `TransferenciasInventario` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_TransferenciaInventarioDetalles_ProductoVariantes`
                    FOREIGN KEY (`ProductoVarianteId`) REFERENCES `ProductoVariantes` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_TransferenciaInventarioDetalles_Ubicaciones_Origen`
                    FOREIGN KEY (`UbicacionOrigenId`) REFERENCES `UbicacionesAlmacen` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_TransferenciaInventarioDetalles_Ubicaciones_Destino`
                    FOREIGN KEY (`UbicacionDestinoId`) REFERENCES `UbicacionesAlmacen` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE INDEX `IX_TransferenciaInventarioDetalles_TransferenciaId`
                ON `TransferenciaInventarioDetalles` (`TransferenciaInventarioId`);
            CREATE INDEX `IX_TransferenciaInventarioDetalles_Variante_Transferencia`
                ON `TransferenciaInventarioDetalles` (`ProductoVarianteId`, `TransferenciaInventarioId`);
            CREATE INDEX `IX_TransferenciaInventarioDetalles_UbicacionOrigen`
                ON `TransferenciaInventarioDetalles` (`UbicacionOrigenId`);
            CREATE INDEX `IX_TransferenciaInventarioDetalles_UbicacionDestino`
                ON `TransferenciaInventarioDetalles` (`UbicacionDestinoId`);
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_TransferenciasInventario_AlmacenesActivos_Insert
            BEFORE INSERT ON TransferenciasInventario
            FOR EACH ROW
            BEGIN
                IF (SELECT COUNT(*)
                      FROM Almacenes a
                     WHERE a.Id IN (NEW.AlmacenOrigenId, NEW.AlmacenDestinoId)
                       AND a.Activo = 1
                       AND a.Eliminado = 0) <> 2 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Los almacenes origen y destino deben existir, estar activos y no eliminados.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_TransferenciasInventario_AlmacenesActivos_Update
            BEFORE UPDATE ON TransferenciasInventario
            FOR EACH ROW
            BEGIN
                IF (SELECT COUNT(*)
                      FROM Almacenes a
                     WHERE a.Id IN (NEW.AlmacenOrigenId, NEW.AlmacenDestinoId)
                       AND a.Activo = 1
                       AND a.Eliminado = 0) <> 2 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Los almacenes origen y destino deben existir, estar activos y no eliminados.';
                END IF;

                IF (SELECT COUNT(*)
                      FROM TransferenciaInventarioDetalles d
                      LEFT JOIN UbicacionesAlmacen uo ON uo.Id = d.UbicacionOrigenId
                      LEFT JOIN UbicacionesAlmacen ud ON ud.Id = d.UbicacionDestinoId
                     WHERE d.TransferenciaInventarioId = NEW.Id
                       AND ((d.UbicacionOrigenId IS NOT NULL AND
                             (uo.AlmacenId <> NEW.AlmacenOrigenId OR uo.Activa = 0 OR uo.Eliminado = 1))
                         OR (d.UbicacionDestinoId IS NOT NULL AND
                             (ud.AlmacenId <> NEW.AlmacenDestinoId OR ud.Activa = 0 OR ud.Eliminado = 1)))) > 0 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cambio de almacenes dejaría ubicaciones de detalle fuera de su topologia.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_TransferenciaInventarioDetalles_Topologia_Insert
            BEFORE INSERT ON TransferenciaInventarioDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.UbicacionOrigenId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM UbicacionesAlmacen u
                      JOIN TransferenciasInventario t ON t.Id = NEW.TransferenciaInventarioId
                     WHERE u.Id = NEW.UbicacionOrigenId
                       AND u.AlmacenId = t.AlmacenOrigenId
                       AND u.Activa = 1
                       AND u.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La ubicacion origen no pertenece al almacen origen activo de la transferencia.';
                END IF;

                IF NEW.UbicacionDestinoId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM UbicacionesAlmacen u
                      JOIN TransferenciasInventario t ON t.Id = NEW.TransferenciaInventarioId
                     WHERE u.Id = NEW.UbicacionDestinoId
                       AND u.AlmacenId = t.AlmacenDestinoId
                       AND u.Activa = 1
                       AND u.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La ubicacion destino no pertenece al almacen destino activo de la transferencia.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER TR_TransferenciaInventarioDetalles_Topologia_Update
            BEFORE UPDATE ON TransferenciaInventarioDetalles
            FOR EACH ROW
            BEGIN
                IF NEW.UbicacionOrigenId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM UbicacionesAlmacen u
                      JOIN TransferenciasInventario t ON t.Id = NEW.TransferenciaInventarioId
                     WHERE u.Id = NEW.UbicacionOrigenId
                       AND u.AlmacenId = t.AlmacenOrigenId
                       AND u.Activa = 1
                       AND u.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La ubicacion origen no pertenece al almacen origen activo de la transferencia.';
                END IF;

                IF NEW.UbicacionDestinoId IS NOT NULL AND
                   (SELECT COUNT(*)
                      FROM UbicacionesAlmacen u
                      JOIN TransferenciasInventario t ON t.Id = NEW.TransferenciaInventarioId
                     WHERE u.Id = NEW.UbicacionDestinoId
                       AND u.AlmacenId = t.AlmacenDestinoId
                       AND u.Activa = 1
                       AND u.Eliminado = 0) <> 1 THEN
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La ubicacion destino no pertenece al almacen destino activo de la transferencia.';
                END IF;
            END
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N16CPostGuard;
            CREATE TEMPORARY TABLE __N16CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N16C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N16CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('TransferenciasInventario', 'TransferenciaInventarioDetalles');

            INSERT INTO __N16CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
              FROM information_schema.triggers
             WHERE trigger_schema = DATABASE()
               AND trigger_name IN (
                    'TR_TransferenciasInventario_AlmacenesActivos_Insert',
                    'TR_TransferenciasInventario_AlmacenesActivos_Update',
                    'TR_TransferenciaInventarioDetalles_Topologia_Insert',
                    'TR_TransferenciaInventarioDetalles_Topologia_Update');

            INSERT INTO __N16CPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'TransferenciaInventarioDetalles'
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_TransferenciaInventarioDetalles_CantidadesNoNegativas',
                    'CK_TransferenciaInventarioDetalles_Aprobada',
                    'CK_TransferenciaInventarioDetalles_Despachada',
                    'CK_TransferenciaInventarioDetalles_Recepcion');

            INSERT INTO __N16CPostGuard (Id, Violaciones)
            SELECT 4, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'TransferenciasInventario'
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_TransferenciasInventario_AlmacenesDistintos',
                    'CK_TransferenciasInventario_Estado_Valido');

            INSERT INTO __N16CPostGuard (Id, Violaciones)
            SELECT 5, CASE WHEN COUNT(*) = 6 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name IN ('TransferenciasInventario', 'TransferenciaInventarioDetalles')
               AND constraint_name IN (
                    'FK_TransferenciasInventario_Almacenes_Origen',
                    'FK_TransferenciasInventario_Almacenes_Destino',
                    'FK_TransferenciaInventarioDetalles_TransferenciasInventario',
                    'FK_TransferenciaInventarioDetalles_ProductoVariantes',
                    'FK_TransferenciaInventarioDetalles_Ubicaciones_Origen',
                    'FK_TransferenciaInventarioDetalles_Ubicaciones_Destino');

            DROP TEMPORARY TABLE __N16CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N16CDownGuard;
            CREATE TEMPORARY TABLE __N16CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N16C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N16CDownGuard (Id, Violaciones)
            SELECT 1,
                   (SELECT COUNT(*) FROM TransferenciasInventario) +
                   (SELECT COUNT(*) FROM TransferenciaInventarioDetalles);

            DROP TEMPORARY TABLE __N16CDownGuard;
            """);

        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_TransferenciaInventarioDetalles_Topologia_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_TransferenciaInventarioDetalles_Topologia_Insert;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_TransferenciasInventario_AlmacenesActivos_Update;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_TransferenciasInventario_AlmacenesActivos_Insert;");
        migrationBuilder.DropTable(name: "TransferenciaInventarioDetalles");
        migrationBuilder.DropTable(name: "TransferenciasInventario");
    }
}
