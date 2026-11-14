-- ERP-N1.10.C — preflight read-only lógico de costeo.
-- Debe ejecutarse antes de aplicar 20260817204000_N1_10_CosteoPersistencia.
-- Usa sólo tablas temporales de sesión para convertir violaciones en fallo SQL.

DROP TEMPORARY TABLE IF EXISTS __N110Preflight;
CREATE TEMPORARY TABLE __N110Preflight
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N110_Preflight_Cero CHECK (Violaciones = 0)
);

-- No aceptar una instalación parcialmente materializada.
INSERT INTO __N110Preflight (Id, Violaciones)
SELECT 1, COUNT(*)
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN (
      'PoliticasCosteoInventario',
      'CostosEstandarInventario',
      'CapasCostoInventario',
      'AsignacionesCostoMovimientoInventario',
      'VariacionesCostoEstandarInventario');

-- El contexto actual es single-company: exactamente una configuración activa.
INSERT INTO __N110Preflight (Id, Violaciones)
SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
FROM `EmpresaConfiguracion`
WHERE `Activo` = 1;

-- La autoridad de costo actual no debe contener costos negativos.
INSERT INTO __N110Preflight (Id, Violaciones)
SELECT 3,
       (SELECT COUNT(*) FROM `Productos` WHERE `Costo` IS NOT NULL AND `Costo` < 0) +
       (SELECT COUNT(*) FROM `ProductoVariantes` WHERE `Costo` IS NOT NULL AND `Costo` < 0);

-- Las existencias son autoridad cuantitativa y deben ser físicamente válidas antes del cutover.
INSERT INTO __N110Preflight (Id, Violaciones)
SELECT 4, COUNT(*)
FROM `ExistenciasVariante`
WHERE `StockFisico` < 0 OR `StockReservado` < 0 OR `StockReservado` > `StockFisico`;

SELECT 'ERP-N1.10.C PRECHECK OK' AS Resultado;
DROP TEMPORARY TABLE __N110Preflight;
