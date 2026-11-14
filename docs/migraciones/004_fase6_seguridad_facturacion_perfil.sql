START TRANSACTION;

ALTER TABLE `Ventas` ADD `Eliminado` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Ventas` ADD `EliminadoPorUsuarioId` int NULL;

ALTER TABLE `Ventas` ADD `FechaEliminacion` datetime(6) NULL;

ALTER TABLE `VentaImpuestos` ADD `IncluidoEnPrecioSnapshot` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Usuarios` ADD `FotoPerfilPublicId` varchar(255) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Usuarios` ADD `FotoPerfilUrl` varchar(500) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Productos` ADD `Activo` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `Productos` ADD `Eliminado` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Productos` ADD `EliminadoPorUsuarioId` int NULL;

ALTER TABLE `Productos` ADD `FechaEliminacion` datetime(6) NULL;

ALTER TABLE `Compras` ADD `Eliminado` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Compras` ADD `EliminadoPorUsuarioId` int NULL;

ALTER TABLE `Compras` ADD `FechaEliminacion` datetime(6) NULL;

ALTER TABLE `CompraImpuestos` ADD `IncluidoEnPrecioSnapshot` tinyint(1) NOT NULL DEFAULT FALSE;

CREATE TABLE `CompraDocumentos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CompraId` int NOT NULL,
    `NombreOriginal` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ContentType` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `SizeBytes` bigint NOT NULL,
    `Url` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `PublicId` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `ResourceType` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Eliminado` tinyint(1) NOT NULL DEFAULT FALSE,
    `FechaCreacion` datetime(6) NOT NULL,
    `CreadoPorUsuarioId` int NULL,
    `CreadoPorNombreUsuario` longtext CHARACTER SET utf8mb4 NULL,
    `FechaEliminacion` datetime(6) NULL,
    `EliminadoPorUsuarioId` int NULL,
    CONSTRAINT `PK_CompraDocumentos` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CompraDocumentos_Compras_CompraId` FOREIGN KEY (`CompraId`) REFERENCES `Compras` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

UPDATE `Usuarios` SET `FotoPerfilPublicId` = NULL, `FotoPerfilUrl` = NULL
WHERE `Id` = 1;
SELECT ROW_COUNT();


CREATE INDEX `IX_Ventas_Eliminado` ON `Ventas` (`Eliminado`);

CREATE INDEX `IX_Productos_Estado` ON `Productos` (`Eliminado`, `Activo`);

CREATE INDEX `IX_Compras_Eliminado` ON `Compras` (`Eliminado`);

CREATE INDEX `IX_CompraDocumentos_CompraId_Eliminado` ON `CompraDocumentos` (`CompraId`, `Eliminado`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260722142118_Fase6SeguridadFacturacionPerfil', '8.0.2');

COMMIT;

