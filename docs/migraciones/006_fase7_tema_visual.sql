-- =====================================================================
-- Migración manual — Fase 7 (prompt 6 partes): tema visual centralizado
-- NO generada con `dotnet ef migrations add` (sin acceso a NuGet). Revisar
-- antes de aplicar. Respaldar la base antes de ejecutar. MySQL 8 / Aiven.
-- =====================================================================

START TRANSACTION;

CREATE TABLE IF NOT EXISTS `TemaVisual` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `ColorPrimario` VARCHAR(9) NOT NULL DEFAULT '#4f46e5',
  `ColorSecundario` VARCHAR(9) NOT NULL DEFAULT '#4338ca',
  `ColorAcento` VARCHAR(9) NOT NULL DEFAULT '#6366f1',
  `FondoPrincipal` VARCHAR(9) NOT NULL DEFAULT '#f5f6fb',
  `FondoTarjetas` VARCHAR(9) NOT NULL DEFAULT '#ffffff',
  `MenuLateral` VARCHAR(9) NOT NULL DEFAULT '#1f2333',
  `BarraSuperior` VARCHAR(9) NOT NULL DEFAULT '#ffffff',
  `Encabezados` VARCHAR(9) NOT NULL DEFAULT '#1f2333',
  `BotonesPrincipales` VARCHAR(9) NOT NULL DEFAULT '#4f46e5',
  `TextoPrincipal` VARCHAR(9) NOT NULL DEFAULT '#1f2333',
  `TextoSecundario` VARCHAR(9) NOT NULL DEFAULT '#6b7280',
  `ColorExito` VARCHAR(9) NOT NULL DEFAULT '#10b981',
  `ColorAdvertencia` VARCHAR(9) NOT NULL DEFAULT '#f59e0b',
  `ColorError` VARCHAR(9) NOT NULL DEFAULT '#ef4444',
  `ColorInformacion` VARCHAR(9) NOT NULL DEFAULT '#3b82f6',
  `FechaActualizacion` DATETIME(6) NULL,
  `ActualizadoPorUsuarioId` INT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Fila única inicial (idempotente) con los mismos valores que ya vivían en
-- frontend/src/styles.scss antes de esta fase.
INSERT INTO `TemaVisual` (`Id`)
SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM `TemaVisual` WHERE `Id` = 1);

COMMIT;

-- VERIFICACIONES
SELECT * FROM `TemaVisual`;
