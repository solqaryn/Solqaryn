-- =====================================================================
-- Migración manual Fase 2: catálogo dinámico de Roles + Permisos
-- NO fue generada con `dotnet ef migrations add` (sin acceso a NuGet en
-- este sandbox). Revisar cuidadosamente antes de aplicar. Se recomienda
-- respaldo completo de la base antes de ejecutar.
-- Compatible con MySQL 8 / Aiven (Pomelo), utf8mb4.
-- =====================================================================

START TRANSACTION;

-- 1. Catálogo de Roles
CREATE TABLE IF NOT EXISTS `Roles` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Nombre` VARCHAR(80) NOT NULL,
  `NombreNormalizado` VARCHAR(80) NOT NULL,
  `Descripcion` VARCHAR(300) NULL,
  `EsSistema` TINYINT(1) NOT NULL DEFAULT 0,
  `EsAdministrador` TINYINT(1) NOT NULL DEFAULT 0,
  `Activo` TINYINT(1) NOT NULL DEFAULT 1,
  `Eliminado` TINYINT(1) NOT NULL DEFAULT 0,
  `FechaCreacion` DATETIME(6) NOT NULL,
  `FechaActualizacion` DATETIME(6) NULL,
  `FechaEliminacion` DATETIME(6) NULL,
  `CreadoPorUsuarioId` INT NULL,
  `ActualizadoPorUsuarioId` INT NULL,
  `EliminadoPorUsuarioId` INT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Roles_NombreNormalizado` (`NombreNormalizado`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Semilla de los 2 roles legados (idempotente)
INSERT INTO `Roles` (`Nombre`,`NombreNormalizado`,`Descripcion`,`EsSistema`,`EsAdministrador`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'Administrador','administrador','Rol de sistema con acceso total.',1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `NombreNormalizado`='administrador');

INSERT INTO `Roles` (`Nombre`,`NombreNormalizado`,`Descripcion`,`EsSistema`,`EsAdministrador`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'Vendedor','vendedor','Rol de sistema con acceso operativo de ventas.',1,0,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Roles` WHERE `NombreNormalizado`='vendedor');

-- 2. Catálogo de Permisos
CREATE TABLE IF NOT EXISTS `Permisos` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Codigo` VARCHAR(120) NOT NULL,
  `Nombre` VARCHAR(150) NOT NULL,
  `Descripcion` VARCHAR(300) NULL,
  `Modulo` INT NOT NULL,
  `Accion` INT NOT NULL,
  `EsSistema` TINYINT(1) NOT NULL DEFAULT 0,
  `Activo` TINYINT(1) NOT NULL DEFAULT 1,
  `Eliminado` TINYINT(1) NOT NULL DEFAULT 0,
  `FechaCreacion` DATETIME(6) NOT NULL,
  `FechaActualizacion` DATETIME(6) NULL,
  `FechaEliminacion` DATETIME(6) NULL,
  `CreadoPorUsuarioId` INT NULL,
  `ActualizadoPorUsuarioId` INT NULL,
  `EliminadoPorUsuarioId` INT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UX_Permisos_Codigo` (`Codigo`),
  UNIQUE KEY `UX_Permisos_Modulo_Accion` (`Modulo`,`Accion`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Semilla del catálogo de permisos (idempotente, EsSistema=1)
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DASHBOARD.VER','Dashboard: Ver',1,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=1 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.VER','Productos: Ver',2,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.CREAR','Productos: Crear',2,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.EDITAR','Productos: Editar',2,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.ACTUALIZAR','Productos: Actualizar',2,7,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=7);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.ACTIVAR','Productos: Activar',2,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.DESACTIVAR','Productos: Desactivar',2,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.ELIMINARLOGICO','Productos: EliminarLogico',2,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.ELIMINARPERMANENTE','Productos: EliminarPermanente',2,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.EXPORTAR','Productos: Exportar',2,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PRODUCTOS.DUPLICAR','Productos: Duplicar',2,22,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=2 AND `Accion`=22);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.VER','Categorias: Ver',3,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.CREAR','Categorias: Crear',3,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.EDITAR','Categorias: Editar',3,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.ACTIVAR','Categorias: Activar',3,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.DESACTIVAR','Categorias: Desactivar',3,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.ELIMINARLOGICO','Categorias: EliminarLogico',3,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CATEGORIAS.ELIMINARPERMANENTE','Categorias: EliminarPermanente',3,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=3 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.VER','Clientes: Ver',4,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.CREAR','Clientes: Crear',4,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.EDITAR','Clientes: Editar',4,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.ACTIVAR','Clientes: Activar',4,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.DESACTIVAR','Clientes: Desactivar',4,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.ELIMINARLOGICO','Clientes: EliminarLogico',4,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.ELIMINARPERMANENTE','Clientes: EliminarPermanente',4,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CLIENTES.CONSULTARHISTORIAL','Clientes: ConsultarHistorial',4,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=4 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.VER','Proveedores: Ver',5,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.CREAR','Proveedores: Crear',5,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.EDITAR','Proveedores: Editar',5,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.ACTIVAR','Proveedores: Activar',5,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.DESACTIVAR','Proveedores: Desactivar',5,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.ELIMINARLOGICO','Proveedores: EliminarLogico',5,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.ELIMINARPERMANENTE','Proveedores: EliminarPermanente',5,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PROVEEDORES.CONSULTARHISTORIAL','Proveedores: ConsultarHistorial',5,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=5 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.VER','Compras: Ver',6,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.CREAR','Compras: Crear',6,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.EDITAR','Compras: Editar',6,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.CONFIRMAR','Compras: Confirmar',6,5,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=5);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.ANULAR','Compras: Anular',6,6,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=6);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.EXPORTAR','Compras: Exportar',6,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.IMPRIMIR','Compras: Imprimir',6,15,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=15);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'COMPRAS.CONSULTARHISTORIAL','Compras: ConsultarHistorial',6,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=6 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.VER','Ventas: Ver',7,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.CREAR','Ventas: Crear',7,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.EDITAR','Ventas: Editar',7,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.CONFIRMAR','Ventas: Confirmar',7,5,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=5);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.ANULAR','Ventas: Anular',7,6,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=6);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.EXPORTAR','Ventas: Exportar',7,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.IMPRIMIR','Ventas: Imprimir',7,15,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=15);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'VENTAS.CONSULTARHISTORIAL','Ventas: ConsultarHistorial',7,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=7 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FACTURACION.VER','Facturacion: Ver',8,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=8 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FACTURACION.EXPORTAR','Facturacion: Exportar',8,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=8 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FACTURACION.IMPRIMIR','Facturacion: Imprimir',8,15,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=8 AND `Accion`=15);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.VER','Finanzas: Ver',9,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.CREAR','Finanzas: Crear',9,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.EDITAR','Finanzas: Editar',9,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.ANULAR','Finanzas: Anular',9,6,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=6);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.EXPORTAR','Finanzas: Exportar',9,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'FINANZAS.ADMINISTRAR','Finanzas: Administrar',9,16,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=9 AND `Accion`=16);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'INVENTARIO.VER','Inventario: Ver',10,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=10 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'INVENTARIO.EXPORTAR','Inventario: Exportar',10,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=10 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'MOVIMIENTOSINVENTARIO.VER','MovimientosInventario: Ver',18,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=18 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'MOVIMIENTOSINVENTARIO.EXPORTAR','MovimientosInventario: Exportar',18,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=18 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.VER','Usuarios: Ver',11,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.CREAR','Usuarios: Crear',11,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.EDITAR','Usuarios: Editar',11,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.ACTIVAR','Usuarios: Activar',11,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.DESACTIVAR','Usuarios: Desactivar',11,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.ASIGNARROL','Usuarios: AsignarRol',11,17,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=17);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'USUARIOS.RESTABLECERCONTRASENA','Usuarios: RestablecerContrasena',11,18,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=11 AND `Accion`=18);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.VER','Roles: Ver',14,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.CREAR','Roles: Crear',14,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.EDITAR','Roles: Editar',14,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.ACTIVAR','Roles: Activar',14,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.DESACTIVAR','Roles: Desactivar',14,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.ELIMINARLOGICO','Roles: EliminarLogico',14,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.ELIMINARPERMANENTE','Roles: EliminarPermanente',14,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.DUPLICAR','Roles: Duplicar',14,22,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=22);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'ROLES.CONSULTARHISTORIAL','Roles: ConsultarHistorial',14,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=14 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.VER','Permisos: Ver',15,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.CREAR','Permisos: Crear',15,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.EDITAR','Permisos: Editar',15,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.ACTIVAR','Permisos: Activar',15,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.DESACTIVAR','Permisos: Desactivar',15,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.ELIMINARLOGICO','Permisos: EliminarLogico',15,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.ELIMINARPERMANENTE','Permisos: EliminarPermanente',15,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.ADMINISTRAR','Permisos: Administrar',15,16,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=16);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'PERMISOS.CONSULTARHISTORIAL','Permisos: ConsultarHistorial',15,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=15 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'AUDITORIA.VER','Auditoria: Ver',12,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=12 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'AUDITORIA.EXPORTAR','Auditoria: Exportar',12,14,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=12 AND `Accion`=14);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CONFIGURACION.VER','Configuracion: Ver',13,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=13 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CONFIGURACION.EDITAR','Configuracion: Editar',13,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=13 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'CONFIGURACION.ADMINISTRAR','Configuracion: Administrar',13,16,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=13 AND `Accion`=16);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.VER','Descuentos: Ver',16,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.CREAR','Descuentos: Crear',16,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.EDITAR','Descuentos: Editar',16,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.ACTIVAR','Descuentos: Activar',16,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.DESACTIVAR','Descuentos: Desactivar',16,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.ELIMINARLOGICO','Descuentos: EliminarLogico',16,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.ELIMINARPERMANENTE','Descuentos: EliminarPermanente',16,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.DUPLICAR','Descuentos: Duplicar',16,22,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=22);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.APLICAR','Descuentos: Aplicar',16,21,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=21);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'DESCUENTOS.CONSULTARHISTORIAL','Descuentos: ConsultarHistorial',16,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=16 AND `Accion`=20);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.VER','Impuestos: Ver',17,1,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=1);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.CREAR','Impuestos: Crear',17,2,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=2);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.EDITAR','Impuestos: Editar',17,3,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=3);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.ACTIVAR','Impuestos: Activar',17,8,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=8);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.DESACTIVAR','Impuestos: Desactivar',17,9,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=9);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.ELIMINARLOGICO','Impuestos: EliminarLogico',17,10,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=10);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.ELIMINARPERMANENTE','Impuestos: EliminarPermanente',17,11,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=11);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.DUPLICAR','Impuestos: Duplicar',17,22,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=22);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.APLICAR','Impuestos: Aplicar',17,21,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=21);
INSERT INTO `Permisos` (`Codigo`,`Nombre`,`Modulo`,`Accion`,`EsSistema`,`Activo`,`Eliminado`,`FechaCreacion`)
SELECT 'IMPUESTOS.CONSULTARHISTORIAL','Impuestos: ConsultarHistorial',17,20,1,1,0,UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM `Permisos` WHERE `Modulo`=17 AND `Accion`=20);

-- 3. Columnas nuevas en Usuarios y RolPermisos
ALTER TABLE `Usuarios` ADD COLUMN IF NOT EXISTS `RolId` INT NULL;
ALTER TABLE `RolPermisos` ADD COLUMN IF NOT EXISTS `RolId` INT NULL;
ALTER TABLE `RolPermisos` ADD COLUMN IF NOT EXISTS `PermisoId` INT NULL;

-- Backfill: Usuarios.RolId según el enum legado Usuarios.Rol (RolUsuario: Administrador=1, Vendedor=2)
UPDATE `Usuarios` u JOIN `Roles` r ON r.`NombreNormalizado`='administrador' SET u.`RolId`=r.`Id` WHERE u.`Rol`=1 AND u.`RolId` IS NULL;
UPDATE `Usuarios` u JOIN `Roles` r ON r.`NombreNormalizado`='vendedor' SET u.`RolId`=r.`Id` WHERE u.`Rol`=2 AND u.`RolId` IS NULL;

-- Backfill: RolPermisos.RolId según el enum legado RolPermisos.Rol
UPDATE `RolPermisos` rp JOIN `Roles` r ON r.`NombreNormalizado`='administrador' SET rp.`RolId`=r.`Id` WHERE rp.`Rol`=1 AND rp.`RolId` IS NULL;
UPDATE `RolPermisos` rp JOIN `Roles` r ON r.`NombreNormalizado`='vendedor' SET rp.`RolId`=r.`Id` WHERE rp.`Rol`=2 AND rp.`RolId` IS NULL;

-- Backfill: RolPermisos.PermisoId según Modulo+Accion
UPDATE `RolPermisos` rp
JOIN `Permisos` p ON p.`Modulo`=rp.`Modulo` AND p.`Accion`=rp.`Accion`
SET rp.`PermisoId`=p.`Id`
WHERE rp.`PermisoId` IS NULL;

-- 4. Llaves foráneas (solo si no existen ya - revisar nombres de constraint antes de aplicar)
ALTER TABLE `Usuarios` ADD CONSTRAINT `FK_Usuarios_Roles_RolId` FOREIGN KEY (`RolId`) REFERENCES `Roles`(`Id`) ON DELETE RESTRICT;
ALTER TABLE `RolPermisos` ADD CONSTRAINT `FK_RolPermisos_Roles_RolId` FOREIGN KEY (`RolId`) REFERENCES `Roles`(`Id`) ON DELETE CASCADE;
ALTER TABLE `RolPermisos` ADD CONSTRAINT `FK_RolPermisos_Permisos_PermisoId` FOREIGN KEY (`PermisoId`) REFERENCES `Permisos`(`Id`) ON DELETE RESTRICT;

-- 5. Precarga de matriz por defecto del rol Vendedor (idempotente: solo si el rol no tiene filas aún)
INSERT INTO `RolPermisos` (`Rol`,`RolId`,`Modulo`,`Accion`,`Permitido`)
SELECT 2, r.`Id`, m.modulo, m.accion, 1
FROM `Roles` r
JOIN (
  SELECT 1 AS modulo, 1 AS accion UNION ALL
  SELECT 2 AS modulo, 1 AS accion UNION ALL
  SELECT 3 AS modulo, 1 AS accion UNION ALL
  SELECT 7 AS modulo, 1 AS accion UNION ALL
  SELECT 7 AS modulo, 2 AS accion UNION ALL
  SELECT 7 AS modulo, 3 AS accion UNION ALL
  SELECT 7 AS modulo, 5 AS accion UNION ALL
  SELECT 4 AS modulo, 1 AS accion UNION ALL
  SELECT 4 AS modulo, 2 AS accion UNION ALL
  SELECT 4 AS modulo, 3 AS accion UNION ALL
  SELECT 8 AS modulo, 1 AS accion UNION ALL
  SELECT 10 AS modulo, 1 AS accion
) m
WHERE r.`NombreNormalizado`='vendedor'
AND NOT EXISTS (SELECT 1 FROM `RolPermisos` rp WHERE rp.`RolId`=r.`Id`);

COMMIT;

-- =====================================================================
-- VERIFICACIONES POST-MIGRACIÓN (ejecutar y revisar manualmente)
-- =====================================================================
SELECT COUNT(*) AS usuarios_sin_rolid FROM `Usuarios` WHERE `RolId` IS NULL;
SELECT COUNT(*) AS rolpermisos_sin_rolid FROM `RolPermisos` WHERE `RolId` IS NULL;
SELECT COUNT(*) AS rolpermisos_sin_permisoid FROM `RolPermisos` WHERE `PermisoId` IS NULL;
SELECT `Nombre`, `EsAdministrador`, `Activo` FROM `Roles`;
SELECT COUNT(*) AS total_permisos_catalogo FROM `Permisos`;
