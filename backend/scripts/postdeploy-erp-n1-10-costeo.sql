-- ERP-N1.10.C — postcheck de persistencia/cutover de costeo.
-- Ejecutar después de aplicar migraciones en un ambiente dev/CI.

DROP TEMPORARY TABLE IF EXISTS __N110Postcheck;
CREATE TEMPORARY TABLE __N110Postcheck
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N110_Postcheck_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N110Postcheck (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN (
      'PoliticasCosteoInventario',
      'CostosEstandarInventario',
      'CapasCostoInventario',
      'AsignacionesCostoMovimientoInventario',
      'VariacionesCostoEstandarInventario');

-- El cutover inicial debe conservar exactamente una política vigente Promedio Ponderado.
INSERT INTO __N110Postcheck (Id, Violaciones)
SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
FROM `PoliticasCosteoInventario`
WHERE `VigenteHastaUtc` IS NULL AND `Metodo` = 1;

-- C no inventa capas FIFO ni costos estándar históricos.
INSERT INTO __N110Postcheck (Id, Violaciones)
SELECT 3,
       (SELECT COUNT(*) FROM `CapasCostoInventario`) +
       (SELECT COUNT(*) FROM `CostosEstandarInventario`) +
       (SELECT COUNT(*) FROM `AsignacionesCostoMovimientoInventario`) +
       (SELECT COUNT(*) FROM `VariacionesCostoEstandarInventario`);

-- La política vigente debe pertenecer a la configuración empresarial activa real.
INSERT INTO __N110Postcheck (Id, Violaciones)
SELECT 4, COUNT(*)
FROM `PoliticasCosteoInventario` p
LEFT JOIN `EmpresaConfiguracion` e ON e.`Id` = p.`EmpresaConfiguracionId`
WHERE p.`VigenteHastaUtc` IS NULL
  AND (e.`Id` IS NULL OR e.`Activo` <> 1);

-- Verificar los índices/constraints de unicidad indispensables para cutover seguro.
INSERT INTO __N110Postcheck (Id, Violaciones)
SELECT 5, CASE WHEN COUNT(*) >= 2 THEN 0 ELSE 1 END
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND (index_name = 'UX_PoliticasCosteo_Empresa_Vigente'
       OR index_name = 'UX_CostosEstandar_Variante_Vigente');

SELECT 'ERP-N1.10.C POSTCHECK OK' AS Resultado;
DROP TEMPORARY TABLE __N110Postcheck;
