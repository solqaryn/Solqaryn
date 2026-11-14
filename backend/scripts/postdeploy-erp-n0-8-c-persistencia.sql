-- ERP-N0.8.C — Postcheck de persistencia transicional
-- Solo lectura. Certifica esquema/backfill sin retirar compatibilidad histórica.
SET @schema_name := DATABASE();

SELECT
    'N0.8.C_SCHEMA' AS check_id,
    (SELECT COUNT(*) FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras' AND COLUMN_NAME='MetodoPagoId') AS compras_metodo_pago_id,
    (SELECT COUNT(*) FROM information_schema.STATISTICS
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras' AND INDEX_NAME='IX_Compras_MetodoPagoId') AS compras_metodo_pago_index,
    (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras'
        AND CONSTRAINT_NAME='FK_Compras_MetodosPago_MetodoPagoId'
        AND COLUMN_NAME='MetodoPagoId' AND REFERENCED_TABLE_NAME='MetodosPago') AS compras_metodo_pago_fk;

SELECT
    'N0.8.C_COMPRA_BACKFILL' AS check_id,
    COUNT(*) AS violaciones
FROM Compras c
LEFT JOIN MetodosPago mp ON mp.Id = c.MetodoPagoId
WHERE c.MetodoPagoId IS NULL
   OR mp.Id IS NULL
   OR LOWER(TRIM(mp.Codigo)) <> LOWER(TRIM(c.MetodoPago));

SELECT
    'N0.8.C_ORIGENES_TIPADOS' AS check_id,
    SUM(CASE WHEN COLUMN_NAME IN ('CompraId','VentaId','ConsumoInsumoId','AjusteInventarioId') THEN 1 ELSE 0 END) AS columnas_tipadas
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA=@schema_name
  AND TABLE_NAME='MovimientosInventario';

SELECT
    'N0.8.C_ORIGEN_EXCLUSIVO' AS check_id,
    COUNT(*) AS violaciones
FROM MovimientosInventario
WHERE (CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) > 1;

SELECT
    'N0.8.C_COMPATIBILIDAD_PRESERVADA' AS check_id,
    (SELECT COUNT(*) FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras' AND COLUMN_NAME='MetodoPago') AS compra_metodo_pago_legacy,
    (SELECT COUNT(*) FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='MovimientosInventario' AND COLUMN_NAME='ReferenciaTipo') AS referencia_tipo,
    (SELECT COUNT(*) FROM information_schema.COLUMNS
      WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='MovimientosInventario' AND COLUMN_NAME='ReferenciaId') AS referencia_id;

SELECT
    'N0.8.C_RESULT' AS check_id,
    CASE WHEN
        (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras' AND COLUMN_NAME='MetodoPagoId') = 1
        AND (SELECT COUNT(*) FROM information_schema.KEY_COLUMN_USAGE
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras'
            AND CONSTRAINT_NAME='FK_Compras_MetodosPago_MetodoPagoId'
            AND COLUMN_NAME='MetodoPagoId' AND REFERENCED_TABLE_NAME='MetodosPago') = 1
        AND (SELECT COUNT(*) FROM Compras c LEFT JOIN MetodosPago mp ON mp.Id=c.MetodoPagoId
          WHERE c.MetodoPagoId IS NULL OR mp.Id IS NULL
             OR LOWER(TRIM(mp.Codigo)) <> LOWER(TRIM(c.MetodoPago))) = 0
        AND (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='MovimientosInventario'
            AND COLUMN_NAME IN ('CompraId','VentaId','ConsumoInsumoId','AjusteInventarioId')) = 4
        AND (SELECT COUNT(*) FROM MovimientosInventario
          WHERE (CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) > 1) = 0
        AND (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='Compras' AND COLUMN_NAME='MetodoPago') = 1
        AND (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='MovimientosInventario' AND COLUMN_NAME='ReferenciaTipo') = 1
        AND (SELECT COUNT(*) FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA=@schema_name AND TABLE_NAME='MovimientosInventario' AND COLUMN_NAME='ReferenciaId') = 1
    THEN 'PASS' ELSE 'FAIL' END AS result;
