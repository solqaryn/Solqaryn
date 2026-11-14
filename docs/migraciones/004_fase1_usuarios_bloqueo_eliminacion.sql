-- =====================================================================
-- Migración manual — Fase 1 (prompt 6 partes): Usuarios al estándar de Roles
-- NO generada con `dotnet ef migrations add` (sin acceso a NuGet en este
-- sandbox). Revisar antes de aplicar. Respaldar la base antes de ejecutar.
-- Compatible con MySQL 8 / Aiven.
-- =====================================================================

START TRANSACTION;

ALTER TABLE `Usuarios`
  ADD COLUMN IF NOT EXISTS `Bloqueado` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `MotivoBloqueo` VARCHAR(300) NULL,
  ADD COLUMN IF NOT EXISTS `FechaBloqueo` DATETIME(6) NULL,
  ADD COLUMN IF NOT EXISTS `BloqueadoPorUsuarioId` INT NULL,
  ADD COLUMN IF NOT EXISTS `Eliminado` TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS `FechaEliminacion` DATETIME(6) NULL,
  ADD COLUMN IF NOT EXISTS `EliminadoPorUsuarioId` INT NULL,
  ADD COLUMN IF NOT EXISTS `CreadoPorUsuarioId` INT NULL,
  ADD COLUMN IF NOT EXISTS `ActualizadoPorUsuarioId` INT NULL,
  ADD COLUMN IF NOT EXISTS `FechaActualizacion` DATETIME(6) NULL;

CREATE INDEX IF NOT EXISTS `IX_Usuarios_Eliminado` ON `Usuarios` (`Eliminado`);

COMMIT;

-- VERIFICACIONES
SELECT COUNT(*) AS usuarios_bloqueados FROM `Usuarios` WHERE `Bloqueado` = 1;
SELECT COUNT(*) AS usuarios_eliminados FROM `Usuarios` WHERE `Eliminado` = 1;
SELECT `Id`, `NombreUsuario`, `Activo`, `Bloqueado`, `Eliminado` FROM `Usuarios`;
