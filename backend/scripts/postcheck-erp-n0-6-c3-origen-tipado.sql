-- ERP-N0.6 C3 — postcheck final de integridad histórica del origen tipado.
-- Solo lectura. Debe devolver ErroresN06C3 = 0.
-- Los movimientos documentales mapeables conservan exactamente una FK;
-- los ajustes no documentales conservan cero FKs tipadas durante la transición.

SET @errores :=
    (SELECT COUNT(*)
       FROM MovimientosInventario m
      WHERE (m.CompraId IS NOT NULL) + (m.VentaId IS NOT NULL) + (m.ConsumoInsumoId IS NOT NULL) > 1)
  + (SELECT COUNT(*)
       FROM MovimientosInventario m
      WHERE CAST(m.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
        AND (m.CompraId IS NULL OR m.CompraId <> m.ReferenciaId OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
  + (SELECT COUNT(*)
       FROM MovimientosInventario m
      WHERE CAST(m.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
        AND (m.VentaId IS NULL OR m.VentaId <> m.ReferenciaId OR m.CompraId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
  + (SELECT COUNT(*)
       FROM MovimientosInventario m
      WHERE CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
        AND (m.ConsumoInsumoId IS NULL OR m.ConsumoInsumoId <> m.ReferenciaId OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL))
  + (SELECT COUNT(*)
       FROM MovimientosInventario m
      WHERE CAST(m.ReferenciaTipo AS BINARY) NOT IN (
                CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                CAST('ConsumoInsumo' AS BINARY))
        AND (CAST(m.Tipo AS BINARY) <> CAST('Ajuste' AS BINARY)
             OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
  + (SELECT IF(COUNT(*) = 1, 0, 1)
       FROM information_schema.table_constraints
      WHERE constraint_schema = DATABASE()
        AND table_name = 'MovimientosInventario'
        AND constraint_type = 'CHECK'
        AND constraint_name = 'CK_MovimientosInventario_OrigenTipado_Exclusivo_N06')
  + (SELECT IF(COUNT(*) = 2, 0, 1)
       FROM information_schema.columns
      WHERE table_schema = DATABASE()
        AND table_name = 'MovimientosInventario'
        AND column_name IN ('ReferenciaTipo','ReferenciaId'))
  + (SELECT IF(COUNT(*) = 3, 0, 1)
       FROM information_schema.referential_constraints
      WHERE constraint_schema = DATABASE()
        AND constraint_name IN (
            'FK_MovimientosInventario_Compras_CompraId_N06',
            'FK_MovimientosInventario_Ventas_VentaId_N06',
            'FK_MovimientosInventario_ConsumosInsumos_ConsumoInsumoId_N06'))
  + (SELECT IF(COUNT(*) = 2, 0, 1)
       FROM information_schema.triggers
      WHERE trigger_schema = DATABASE()
        AND event_object_table = 'MovimientosInventario'
        AND trigger_name IN (
            'TR_MovimientosInventario_N06_OrigenTipado_BI',
            'TR_MovimientosInventario_N06_OrigenTipado_BU'));

SELECT @errores AS ErroresN06C3;
