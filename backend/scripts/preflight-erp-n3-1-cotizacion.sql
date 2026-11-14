-- ERP-N3.1.C — Preflight de persistencia de Cotizaciones.
-- Read-only sobre datos persistentes; la tabla temporal se usa solo para fail-closed.
DROP TEMPORARY TABLE IF EXISTS __N31CPreflight;
CREATE TEMPORARY TABLE __N31CPreflight
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N31C_Preflight_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N31CPreflight (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('Clientes', 'Productos', 'ProductoVariantes');

INSERT INTO __N31CPreflight (Id, Violaciones)
SELECT 2, COUNT(*)
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('Cotizaciones', 'CotizacionDetalles');

DROP TEMPORARY TABLE __N31CPreflight;

SELECT
    'N3.1.C preflight OK' AS Resultado,
    (SELECT COUNT(*) FROM Clientes) AS ClientesActuales,
    (SELECT COUNT(*) FROM Productos) AS ProductosActuales,
    (SELECT COUNT(*) FROM ProductoVariantes) AS VariantesActuales;
