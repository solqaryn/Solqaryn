-- ERP-N0.5 preflight MetodoPago: SOLO LECTURA.
-- Precondición: 20260812022343_N0_5_MetodoPagoRelacionalBase ya aplicada.
-- Debe devolver BloqueosN05 = 0 antes del backfill histórico.

SET @base_faltante := IF(
    EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
         WHERE MigrationId = '20260812022343_N0_5_MetodoPagoRelacionalBase'
    ), 0, 1
);

SET @backfill_ya_aplicado := IF(
    EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
         WHERE MigrationId = '20260812023600_N0_5_BackfillMetodoPagoHistorico'
    ), 1, 0
);

SET @ventas_invalidas := (
    SELECT COUNT(*) FROM Ventas
     WHERE MetodoPago IS NULL
        OR CAST(MetodoPago AS BINARY) NOT IN
           (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY))
);

SET @pagos_invalidos := (
    SELECT COUNT(*) FROM FacturaPagos
     WHERE MetodoPago NOT IN (1, 2, 3, 4)
);

SET @movimientos_invalidos := (
    SELECT COUNT(*) FROM MovimientosFinancieros
     WHERE MetodoPago IS NOT NULL
       AND CAST(MetodoPago AS BINARY) NOT IN
           (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY))
);

-- Compra aún conserva contrato legacy en este punto; se audita para impedir que
-- exista un quinto significado incompatible aunque su FK no forme parte de N0.5.
SET @compras_invalidas := (
    SELECT COUNT(*) FROM Compras
     WHERE MetodoPago IS NULL
        OR CAST(MetodoPago AS BINARY) NOT IN
           (CAST('Efectivo' AS BINARY), CAST('Transferencia' AS BINARY), CAST('Tarjeta' AS BINARY), CAST('Otro' AS BINARY))
);

SET @fks_preexistentes :=
      (SELECT COUNT(*) FROM Ventas WHERE MetodoPagoId IS NOT NULL)
    + (SELECT COUNT(*) FROM FacturaPagos WHERE MetodoPagoId IS NOT NULL)
    + (SELECT COUNT(*) FROM MovimientosFinancieros WHERE MetodoPagoId IS NOT NULL);

SET @catalogo_conflictivo := (
    SELECT COUNT(*)
      FROM MetodosPago mp
     WHERE LOWER(TRIM(mp.Codigo)) IN ('efectivo', 'transferencia', 'tarjeta', 'otro')
       AND (
           mp.Activo <> 1
           OR mp.Eliminado <> 0
           OR (LOWER(TRIM(mp.Codigo)) = 'efectivo' AND
               (CAST(mp.Codigo AS BINARY) <> CAST('Efectivo' AS BINARY)
                OR CAST(mp.Nombre AS BINARY) <> CAST('Efectivo' AS BINARY)
                OR CAST(mp.Tipo AS BINARY) <> CAST('Efectivo' AS BINARY)))
           OR (LOWER(TRIM(mp.Codigo)) = 'transferencia' AND
               (CAST(mp.Codigo AS BINARY) <> CAST('Transferencia' AS BINARY)
                OR CAST(mp.Nombre AS BINARY) <> CAST('Transferencia' AS BINARY)
                OR CAST(mp.Tipo AS BINARY) <> CAST('Transferencia' AS BINARY)))
           OR (LOWER(TRIM(mp.Codigo)) = 'tarjeta' AND
               (CAST(mp.Codigo AS BINARY) <> CAST('Tarjeta' AS BINARY)
                OR CAST(mp.Nombre AS BINARY) <> CAST('Tarjeta' AS BINARY)
                OR CAST(mp.Tipo AS BINARY) <> CAST('Tarjeta' AS BINARY)))
           OR (LOWER(TRIM(mp.Codigo)) = 'otro' AND
               (CAST(mp.Codigo AS BINARY) <> CAST('Otro' AS BINARY)
                OR CAST(mp.Nombre AS BINARY) <> CAST('Otro' AS BINARY)
                OR CAST(mp.Tipo AS BINARY) <> CAST('Otro' AS BINARY)))
       )
);

SET @violaciones := @base_faltante
                  + @backfill_ya_aplicado
                  + @ventas_invalidas
                  + @pagos_invalidos
                  + @movimientos_invalidos
                  + @compras_invalidas
                  + @fks_preexistentes
                  + @catalogo_conflictivo;

SELECT @violaciones AS BloqueosN05;
