-- ERP-N3.1.C — Postcheck de persistencia de Cotizaciones.
-- Es una migración aditiva: no existe backfill histórico porque las tablas nacen vacías.
DROP TEMPORARY TABLE IF EXISTS __N31CPostcheck;
CREATE TEMPORARY TABLE __N31CPostcheck
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N31C_Postcheck_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N31CPostcheck (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('Cotizaciones', 'CotizacionDetalles');

INSERT INTO __N31CPostcheck (Id, Violaciones)
SELECT 2, CASE WHEN COUNT(*) = 4 THEN 0 ELSE 1 END
  FROM information_schema.referential_constraints
 WHERE constraint_schema = DATABASE()
   AND constraint_name IN
       ('FK_Cotizaciones_Clientes_ClienteId',
        'FK_CotizacionDetalles_Cotizaciones_CotizacionId',
        'FK_CotizacionDetalles_Productos_ProductoId',
        'FK_CotizacionDetalles_ProductoVariantes_ProductoVarianteId');

INSERT INTO __N31CPostcheck (Id, Violaciones)
SELECT 3, COUNT(*)
  FROM Cotizaciones c
  LEFT JOIN Clientes cl ON cl.Id = c.ClienteId
 WHERE cl.Id IS NULL;

INSERT INTO __N31CPostcheck (Id, Violaciones)
SELECT 4, COUNT(*)
  FROM CotizacionDetalles d
  LEFT JOIN Cotizaciones c ON c.Id = d.CotizacionId
  LEFT JOIN Productos p ON p.Id = d.ProductoId
  LEFT JOIN ProductoVariantes pv ON pv.Id = d.ProductoVarianteId
 WHERE c.Id IS NULL OR p.Id IS NULL OR (d.ProductoVarianteId IS NOT NULL AND pv.Id IS NULL);

DROP TEMPORARY TABLE __N31CPostcheck;

SELECT
    'N3.1.C postcheck OK' AS Resultado,
    (SELECT COUNT(*) FROM Cotizaciones) AS Cotizaciones,
    (SELECT COUNT(*) FROM CotizacionDetalles) AS Detalles;
