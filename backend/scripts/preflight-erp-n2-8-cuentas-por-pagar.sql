-- ERP-N2.8.C — Preflight de persistencia Cuentas por pagar.
-- Read-only sobre datos persistentes: sólo usa una tabla temporal para fallar cerrado.
DROP TEMPORARY TABLE IF EXISTS __N28CPreflight;
CREATE TEMPORARY TABLE __N28CPreflight
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N28C_Preflight_Cero CHECK (Violaciones = 0)
);

-- Dependencias físicas obligatorias del modelo.
INSERT INTO __N28CPreflight (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('FacturasProveedor', 'Proveedores');

-- Fail-closed ante una instalación parcial o una colisión previa.
INSERT INTO __N28CPreflight (Id, Violaciones)
SELECT 2, COUNT(*)
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('CuentasPorPagar', 'AplicacionesCuentaPorPagar');

DROP TEMPORARY TABLE __N28CPreflight;

SELECT
    'N2.8.C preflight OK' AS Resultado,
    (SELECT COUNT(*) FROM FacturasProveedor) AS FacturasProveedorActuales,
    (SELECT COUNT(*) FROM Proveedores) AS ProveedoresActuales;
