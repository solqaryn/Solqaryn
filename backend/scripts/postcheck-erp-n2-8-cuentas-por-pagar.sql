-- ERP-N2.8.C — Postcheck de persistencia Cuentas por pagar.
-- Verifica estructura e integridad sin inventar obligaciones ni aplicaciones.
DROP TEMPORARY TABLE IF EXISTS __N28CPostcheck;
CREATE TEMPORARY TABLE __N28CPostcheck
(
    Id TINYINT NOT NULL PRIMARY KEY,
    Violaciones BIGINT NOT NULL,
    CONSTRAINT CK_N28C_Postcheck_Cero CHECK (Violaciones = 0)
);

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
  FROM information_schema.tables
 WHERE table_schema = DATABASE()
   AND table_name IN ('CuentasPorPagar', 'AplicacionesCuentaPorPagar');

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
  FROM information_schema.statistics
 WHERE table_schema = DATABASE()
   AND table_name = 'CuentasPorPagar'
   AND index_name = 'UX_CuentasPorPagar_FacturaProveedorId'
   AND non_unique = 0;

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
  FROM information_schema.statistics
 WHERE table_schema = DATABASE()
   AND table_name = 'AplicacionesCuentaPorPagar'
   AND index_name = 'UX_AplicacionesCuentaPorPagar_Cuenta_IdempotencyKey'
   AND non_unique = 0;

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 4, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
  FROM information_schema.referential_constraints
 WHERE constraint_schema = DATABASE()
   AND constraint_name IN
       ('FK_CuentasPorPagar_FacturasProveedor_FacturaProveedorId',
        'FK_CuentasPorPagar_Proveedores_ProveedorId',
        'FK_AplicacionesCuentaPorPagar_CuentasPorPagar_CuentaPorPagarId');

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 5, COUNT(*)
  FROM CuentasPorPagar c
  LEFT JOIN FacturasProveedor f ON f.Id = c.FacturaProveedorId
  LEFT JOIN Proveedores p ON p.Id = c.ProveedorId
 WHERE f.Id IS NULL OR p.Id IS NULL;

INSERT INTO __N28CPostcheck (Id, Violaciones)
SELECT 6, COUNT(*)
  FROM AplicacionesCuentaPorPagar a
  LEFT JOIN CuentasPorPagar c ON c.Id = a.CuentaPorPagarId
 WHERE c.Id IS NULL;

DROP TEMPORARY TABLE __N28CPostcheck;

SELECT
    'N2.8.C postcheck OK' AS Resultado,
    (SELECT COUNT(*) FROM CuentasPorPagar) AS CuentasPorPagar,
    (SELECT COUNT(*) FROM AplicacionesCuentaPorPagar) AS AplicacionesCuentaPorPagar;
