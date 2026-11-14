-- ERP-N2.9.C — Postcheck de persistencia Evaluación de proveedores.
-- Verifica estructura, FKs e integridad sin inventar evaluaciones históricas.
DROP TEMPORARY TABLE IF EXISTS __N29CPostcheck;
CREATE TEMPORARY TABLE __N29CPostcheck
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N29C_Postcheck_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N29CPostcheck (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name = 'EvaluacionesProveedor';

INSERT INTO __N29CPostcheck (Id, Violaciones)
SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 3 THEN 0 ELSE 1 END
  FROM information_schema.statistics
 WHERE table_schema = DATABASE()
   AND table_name = 'EvaluacionesProveedor'
   AND index_name IN
       ('IX_EvaluacionesProveedor_RecepcionCompra',
        'IX_EvaluacionesProveedor_OrdenCompra',
        'IX_EvaluacionesProveedor_Proveedor_FechaRecepcion');

INSERT INTO __N29CPostcheck (Id, Violaciones)
SELECT 3, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
  FROM information_schema.referential_constraints
 WHERE constraint_schema = DATABASE()
   AND constraint_name IN
       ('FK_EvaluacionesProveedor_Proveedores_ProveedorId',
        'FK_EvaluacionesProveedor_OrdenesCompra_OrdenCompraId',
        'FK_EvaluacionesProveedor_RecepcionesCompra_RecepcionCompraId');

INSERT INTO __N29CPostcheck (Id, Violaciones)
SELECT 4, COUNT(*)
  FROM EvaluacionesProveedor e
  LEFT JOIN Proveedores p ON p.Id = e.ProveedorId
  LEFT JOIN OrdenesCompra o ON o.Id = e.OrdenCompraId
  LEFT JOIN RecepcionesCompra r ON r.Id = e.RecepcionCompraId
 WHERE p.Id IS NULL OR o.Id IS NULL OR r.Id IS NULL;

DROP TEMPORARY TABLE __N29CPostcheck;

SELECT
    'N2.9.C postcheck OK' AS Resultado,
    (SELECT COUNT(*) FROM EvaluacionesProveedor) AS EvaluacionesProveedor;
