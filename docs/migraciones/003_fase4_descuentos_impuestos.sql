-- =====================================================================
-- Migración manual Fase 4: módulos de Descuentos e Impuestos (secciones 11-12)
-- NO fue generada con `dotnet ef migrations add` (sin acceso a NuGet en
-- este sandbox). Revisar cuidadosamente antes de aplicar. Respaldar la
-- base antes de ejecutar.
-- Compatible con MySQL 8 / Aiven (Pomelo), utf8mb4.
-- =====================================================================

START TRANSACTION;

CREATE TABLE IF NOT EXISTS `Descuentos` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Nombre` VARCHAR(150) NOT NULL,
  `Descripcion` VARCHAR(500) NULL,
  `CodigoPromocional` VARCHAR(50) NULL,
  `CodigoPromocionalNormalizado` VARCHAR(50) NULL,
  `Tipo` INT NOT NULL,
  `Valor` DECIMAL(18,4) NOT NULL,
  `FechaInicio` DATETIME(6) NULL,
  `FechaFin` DATETIME(6) NULL,
  `MontoMinimo` DECIMAL(18,4) NULL,
  `MontoMaximoDescuento` DECIMAL(18,4) NULL,
  `CantidadMinima` INT NULL,
  `RequiereAprobacion` TINYINT(1) NOT NULL DEFAULT 0,
  `Acumulable` TINYINT(1) NOT NULL DEFAULT 0,
  `Prioridad` INT NOT NULL DEFAULT 100,
  `LimiteTotalUsos` INT NULL,
  `LimiteUsosPorCliente` INT NULL,
  `UsosRealizados` INT NOT NULL DEFAULT 0,
  `Activo` TINYINT(1) NOT NULL DEFAULT 1,
  `Eliminado` TINYINT(1) NOT NULL DEFAULT 0,
  `FechaCreacion` DATETIME(6) NOT NULL,
  `FechaActualizacion` DATETIME(6) NULL,
  `FechaEliminacion` DATETIME(6) NULL,
  `CreadoPorUsuarioId` INT NULL,
  `ActualizadoPorUsuarioId` INT NULL,
  `EliminadoPorUsuarioId` INT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Descuentos_CodigoNormalizado` (`CodigoPromocionalNormalizado`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `DescuentoProductos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `DescuentoId` INT NOT NULL, `ProductoId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_DescuentoProductos_DescuentoId` (`DescuentoId`),
  CONSTRAINT `FK_DescuentoProductos_Descuentos` FOREIGN KEY (`DescuentoId`) REFERENCES `Descuentos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `DescuentoCategorias` (
  `Id` INT NOT NULL AUTO_INCREMENT, `DescuentoId` INT NOT NULL, `CategoriaId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_DescuentoCategorias_DescuentoId` (`DescuentoId`),
  CONSTRAINT `FK_DescuentoCategorias_Descuentos` FOREIGN KEY (`DescuentoId`) REFERENCES `Descuentos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `DescuentoClientes` (
  `Id` INT NOT NULL AUTO_INCREMENT, `DescuentoId` INT NOT NULL, `ClienteId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_DescuentoClientes_DescuentoId` (`DescuentoId`),
  CONSTRAINT `FK_DescuentoClientes_Descuentos` FOREIGN KEY (`DescuentoId`) REFERENCES `Descuentos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `DescuentoRoles` (
  `Id` INT NOT NULL AUTO_INCREMENT, `DescuentoId` INT NOT NULL, `RolId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_DescuentoRoles_DescuentoId` (`DescuentoId`),
  CONSTRAINT `FK_DescuentoRoles_Descuentos` FOREIGN KEY (`DescuentoId`) REFERENCES `Descuentos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HistorialUsoDescuentos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `DescuentoId` INT NOT NULL, `VentaId` INT NOT NULL,
  `ClienteId` INT NULL, `MontoAplicado` DECIMAL(18,4) NOT NULL, `Fecha` DATETIME(6) NOT NULL, `UsuarioId` INT NULL,
  PRIMARY KEY (`Id`), KEY `IX_HistorialUsoDescuentos_DescuentoId` (`DescuentoId`),
  CONSTRAINT `FK_HistorialUsoDescuentos_Descuentos` FOREIGN KEY (`DescuentoId`) REFERENCES `Descuentos`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `VentaDescuentos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `VentaId` INT NOT NULL, `DescuentoId` INT NOT NULL,
  `DescuentoNombreSnapshot` VARCHAR(150) NOT NULL, `DescuentoCodigoSnapshot` VARCHAR(50) NOT NULL,
  `TipoSnapshot` INT NOT NULL, `ValorSnapshot` DECIMAL(18,4) NOT NULL, `MontoAplicado` DECIMAL(18,4) NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_VentaDescuentos_VentaId` (`VentaId`),
  CONSTRAINT `FK_VentaDescuentos_Ventas` FOREIGN KEY (`VentaId`) REFERENCES `Ventas`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `Impuestos` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Nombre` VARCHAR(150) NOT NULL, `Codigo` VARCHAR(50) NOT NULL, `Descripcion` VARCHAR(500) NULL,
  `Tipo` INT NOT NULL, `Tasa` DECIMAL(9,4) NOT NULL, `MontoFijo` DECIMAL(18,4) NULL,
  `FechaInicio` DATETIME(6) NULL, `FechaFin` DATETIME(6) NULL,
  `IncluidoEnPrecio` TINYINT(1) NOT NULL DEFAULT 0, `SeCalculaAntesDescuento` TINYINT(1) NOT NULL DEFAULT 0,
  `Acumulativo` TINYINT(1) NOT NULL DEFAULT 1, `Prioridad` INT NOT NULL DEFAULT 100,
  `RequiereRetencion` TINYINT(1) NOT NULL DEFAULT 0,
  `Activo` TINYINT(1) NOT NULL DEFAULT 1, `Eliminado` TINYINT(1) NOT NULL DEFAULT 0,
  `FechaCreacion` DATETIME(6) NOT NULL, `FechaActualizacion` DATETIME(6) NULL, `FechaEliminacion` DATETIME(6) NULL,
  `CreadoPorUsuarioId` INT NULL, `ActualizadoPorUsuarioId` INT NULL, `EliminadoPorUsuarioId` INT NULL,
  PRIMARY KEY (`Id`), UNIQUE KEY `UX_Impuestos_Codigo` (`Codigo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `ImpuestoProductos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL, `ProductoId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_ImpuestoProductos_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_ImpuestoProductos_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ImpuestoCategorias` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL, `CategoriaId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_ImpuestoCategorias_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_ImpuestoCategorias_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ImpuestoOperaciones` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL, `Operacion` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_ImpuestoOperaciones_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_ImpuestoOperaciones_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ImpuestoClientes` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL, `ClienteId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_ImpuestoClientes_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_ImpuestoClientes_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ImpuestoProveedores` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL, `ProveedorId` INT NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_ImpuestoProveedores_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_ImpuestoProveedores_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HistorialAplicacionImpuestos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `ImpuestoId` INT NOT NULL,
  `DocumentoTipo` VARCHAR(20) NOT NULL, `DocumentoId` INT NOT NULL,
  `BaseImponible` DECIMAL(18,4) NOT NULL, `TasaAplicada` DECIMAL(9,4) NOT NULL, `MontoAplicado` DECIMAL(18,4) NOT NULL,
  `Fecha` DATETIME(6) NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_HistorialAplicacionImpuestos_ImpuestoId` (`ImpuestoId`),
  CONSTRAINT `FK_HistorialAplicacionImpuestos_Impuestos` FOREIGN KEY (`ImpuestoId`) REFERENCES `Impuestos`(`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `VentaImpuestos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `VentaId` INT NOT NULL, `ImpuestoId` INT NOT NULL,
  `ImpuestoNombreSnapshot` VARCHAR(150) NOT NULL, `TasaSnapshot` DECIMAL(9,4) NOT NULL,
  `BaseImponible` DECIMAL(18,4) NOT NULL, `MontoAplicado` DECIMAL(18,4) NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_VentaImpuestos_VentaId` (`VentaId`),
  CONSTRAINT `FK_VentaImpuestos_Ventas` FOREIGN KEY (`VentaId`) REFERENCES `Ventas`(`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `CompraImpuestos` (
  `Id` INT NOT NULL AUTO_INCREMENT, `CompraId` INT NOT NULL, `ImpuestoId` INT NOT NULL,
  `ImpuestoNombreSnapshot` VARCHAR(150) NOT NULL, `TasaSnapshot` DECIMAL(9,4) NOT NULL,
  `BaseImponible` DECIMAL(18,4) NOT NULL, `MontoAplicado` DECIMAL(18,4) NOT NULL,
  PRIMARY KEY (`Id`), KEY `IX_CompraImpuestos_CompraId` (`CompraId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed de permisos del catálogo para Descuentos e Impuestos (idempotente)
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT * FROM (
  SELECT 'DESCUENTOS.VER','Descuentos: Ver',16,1,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.CREAR','Descuentos: Crear',16,2,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.EDITAR','Descuentos: Editar',16,3,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.ACTIVAR','Descuentos: Activar',16,8,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.DESACTIVAR','Descuentos: Desactivar',16,9,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.ELIMINARLOGICO','Descuentos: EliminarLogico',16,10,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.ELIMINARPERMANENTE','Descuentos: EliminarPermanente',16,11,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.DUPLICAR','Descuentos: Duplicar',16,22,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.APLICAR','Descuentos: Aplicar',16,21,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'DESCUENTOS.CONSULTARHISTORIAL','Descuentos: ConsultarHistorial',16,20,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.VER','Impuestos: Ver',17,1,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.CREAR','Impuestos: Crear',17,2,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.EDITAR','Impuestos: Editar',17,3,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.ACTIVAR','Impuestos: Activar',17,8,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.DESACTIVAR','Impuestos: Desactivar',17,9,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.ELIMINARLOGICO','Impuestos: EliminarLogico',17,10,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.ELIMINARPERMANENTE','Impuestos: EliminarPermanente',17,11,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.DUPLICAR','Impuestos: Duplicar',17,22,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.APLICAR','Impuestos: Aplicar',17,21,1,1,0,UTC_TIMESTAMP(6) UNION ALL
  SELECT 'IMPUESTOS.CONSULTARHISTORIAL','Impuestos: ConsultarHistorial',17,20,1,1,0,UTC_TIMESTAMP(6)
) AS nuevos(Codigo,Nombre,Modulo,Accion,EsSistema,Activo,Eliminado,FechaCreacion)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` p WHERE p.`Codigo` = nuevos.Codigo);

COMMIT;

-- VERIFICACIONES
SELECT COUNT(*) AS total_descuentos FROM `Descuentos`;
SELECT COUNT(*) AS total_impuestos FROM `Impuestos`;
SELECT COUNT(*) AS permisos_descuentos_impuestos FROM `Permisos` WHERE `Modulo` IN (16,17);
