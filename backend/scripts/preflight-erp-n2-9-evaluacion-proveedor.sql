-- ERP-N2.9.C — Preflight de persistencia Evaluación de proveedores.
-- Read-only sobre datos persistentes; usa tabla temporal sólo para fail-closed.
DROP TEMPORARY TABLE IF EXISTS __N29CPreflight;
CREATE TEMPORARY TABLE __N29CPreflight
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N29C_Preflight_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N29CPreflight (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('Proveedores', 'OrdenesCompra', 'RecepcionesCompra');

INSERT INTO __N29CPreflight (Id, Violaciones)
SELECT 2, COUNT(*)
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name = 'EvaluacionesProveedor';

DROP TEMPORARY TABLE __N29CPreflight;

SELECT
    'N2.9.C preflight OK' AS Resultado,
    (SELECT COUNT(*) FROM Proveedores) AS ProveedoresActuales,
    (SELECT COUNT(*) FROM OrdenesCompra) AS OrdenesCompraActuales,
    (SELECT COUNT(*) FROM RecepcionesCompra) AS RecepcionesCompraActuales;
