-- ERP-N0.5 postcheck MetodoPago: certifica seed y backfill histórico.
-- Debe devolver BloqueosN05 = 0 después de aplicar N0.5.

SET @migracion_faltante := IF(
    EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
         WHERE MigrationId = '20260812023600_N0_5_BackfillMetodoPagoHistorico'
    ), 0, 1
);

SET @catalogo_historico_incompleto := IF(
    (SELECT COUNT(*)
       FROM MetodosPago mp
      WHERE mp.Activo = 1
        AND mp.Eliminado = 0
        AND (
             (CAST(mp.Codigo AS BINARY) = CAST('Efectivo' AS BINARY)
              AND CAST(mp.Nombre AS BINARY) = CAST('Efectivo' AS BINARY)
              AND CAST(mp.Tipo AS BINARY) = CAST('Efectivo' AS BINARY))
          OR (CAST(mp.Codigo AS BINARY) = CAST('Transferencia' AS BINARY)
              AND CAST(mp.Nombre AS BINARY) = CAST('Transferencia' AS BINARY)
              AND CAST(mp.Tipo AS BINARY) = CAST('Transferencia' AS BINARY))
          OR (CAST(mp.Codigo AS BINARY) = CAST('Tarjeta' AS BINARY)
              AND CAST(mp.Nombre AS BINARY) = CAST('Tarjeta' AS BINARY)
              AND CAST(mp.Tipo AS BINARY) = CAST('Tarjeta' AS BINARY))
          OR (CAST(mp.Codigo AS BINARY) = CAST('Otro' AS BINARY)
              AND CAST(mp.Nombre AS BINARY) = CAST('Otro' AS BINARY)
              AND CAST(mp.Tipo AS BINARY) = CAST('Otro' AS BINARY))
        )) = 4, 0, 1
);

SET @ventas_sin_mapa := (
    SELECT COUNT(*)
      FROM Ventas v
     WHERE v.MetodoPagoId IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM MetodosPago mp
             WHERE mp.Id = v.MetodoPagoId
               AND CAST(mp.Codigo AS BINARY) = CAST(v.MetodoPago AS BINARY))
);

SET @pagos_sin_mapa := (
    SELECT COUNT(*)
      FROM FacturaPagos fp
     WHERE fp.MetodoPagoId IS NULL
        OR NOT EXISTS (
            SELECT 1 FROM MetodosPago mp
             WHERE mp.Id = fp.MetodoPagoId
               AND CAST(mp.Codigo AS BINARY) = CAST(
                   CASE fp.MetodoPago
                       WHEN 1 THEN 'Efectivo'
                       WHEN 2 THEN 'Transferencia'
                       WHEN 3 THEN 'Tarjeta'
                       WHEN 4 THEN 'Otro'
                   END AS BINARY))
);

SET @movimientos_sin_mapa := (
    SELECT COUNT(*)
      FROM MovimientosFinancieros mf
     WHERE (mf.MetodoPago IS NULL AND mf.MetodoPagoId IS NOT NULL)
        OR (mf.MetodoPago IS NOT NULL AND
            (mf.MetodoPagoId IS NULL OR NOT EXISTS (
                SELECT 1 FROM MetodosPago mp
                 WHERE mp.Id = mf.MetodoPagoId
                   AND CAST(mp.Codigo AS BINARY) = CAST(mf.MetodoPago AS BINARY))))
);

SET @legacy_fuera_contrato :=
      (SELECT COUNT(*) FROM Ventas
        WHERE MetodoPago IS NULL
           OR CAST(MetodoPago AS BINARY) NOT IN
              (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)))
    + (SELECT COUNT(*) FROM FacturaPagos WHERE MetodoPago NOT IN (1, 2, 3, 4))
    + (SELECT COUNT(*) FROM MovimientosFinancieros
        WHERE MetodoPago IS NOT NULL
          AND CAST(MetodoPago AS BINARY) NOT IN
              (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)))
    + (SELECT COUNT(*) FROM Compras
        WHERE MetodoPago IS NULL
           OR CAST(MetodoPago AS BINARY) NOT IN
              (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY)));

SET @fk_venta_faltante := IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'Ventas'
           AND COLUMN_NAME = 'MetodoPagoId'
           AND REFERENCED_TABLE_NAME = 'MetodosPago'
           AND REFERENCED_COLUMN_NAME = 'Id'
    ), 0, 1
);

SET @fk_pago_faltante := IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'FacturaPagos'
           AND COLUMN_NAME = 'MetodoPagoId'
           AND REFERENCED_TABLE_NAME = 'MetodosPago'
           AND REFERENCED_COLUMN_NAME = 'Id'
    ), 0, 1
);

SET @fk_movimiento_faltante := IF(
    EXISTS (
        SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'MovimientosFinancieros'
           AND COLUMN_NAME = 'MetodoPagoId'
           AND REFERENCED_TABLE_NAME = 'MetodosPago'
           AND REFERENCED_COLUMN_NAME = 'Id'
    ), 0, 1
);

SET @violaciones := @migracion_faltante
                  + @catalogo_historico_incompleto
                  + @ventas_sin_mapa
                  + @pagos_sin_mapa
                  + @movimientos_sin_mapa
                  + @legacy_fuera_contrato
                  + @fk_venta_faltante
                  + @fk_pago_faltante
                  + @fk_movimiento_faltante;

SELECT @violaciones AS BloqueosN05;
