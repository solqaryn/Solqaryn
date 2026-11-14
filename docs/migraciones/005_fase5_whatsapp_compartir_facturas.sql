-- =====================================================================
-- Migración manual — Fase 5 (prompt 6 partes): compartir facturas por WhatsApp
-- NO generada con `dotnet ef migrations add` (sin acceso a NuGet). Revisar
-- antes de aplicar. Respaldar la base antes de ejecutar. MySQL 8 / Aiven.
-- =====================================================================

START TRANSACTION;

CREATE TABLE IF NOT EXISTS `EnlacesPublicosFactura` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Token` VARCHAR(64) NOT NULL,
  `FacturaId` INT NOT NULL,
  `FechaCreacion` DATETIME(6) NOT NULL,
  `FechaExpiracion` DATETIME(6) NOT NULL,
  `CreadoPorUsuarioId` INT NULL,
  `VecesAccedido` INT NOT NULL DEFAULT 0,
  `UltimoAcceso` DATETIME(6) NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_EnlacesPublicosFactura_Token` (`Token`),
  KEY `IX_EnlacesPublicosFactura_FacturaId` (`FacturaId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `HistorialEnviosFactura` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `FacturaId` INT NOT NULL,
  `Canal` VARCHAR(20) NOT NULL,
  `Destinatario` VARCHAR(150) NOT NULL,
  `Resultado` VARCHAR(20) NOT NULL,
  `Error` VARCHAR(500) NULL,
  `UsuarioId` INT NULL,
  `UsuarioNombre` VARCHAR(150) NULL,
  `Fecha` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistorialEnviosFactura_FacturaId` (`FacturaId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Seed del permiso Compartir para el módulo Facturación (idempotente)
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FACTURACION.COMPARTIR','Facturacion: Compartir',8,23,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=8 AND `Accion`=23);

COMMIT;

-- VERIFICACIONES
SELECT COUNT(*) AS enlaces_creados FROM `EnlacesPublicosFactura`;
SELECT COUNT(*) AS intentos_envio FROM `HistorialEnviosFactura`;
SELECT * FROM `Permisos` WHERE `Codigo` = 'FACTURACION.COMPARTIR';
