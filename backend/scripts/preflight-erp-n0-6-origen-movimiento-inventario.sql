-- ERP-N0.6 C1 — preflight histórico de origen de MovimientoInventario.
-- SOLO LECTURA. No modifica datos ni esquema.
-- Debe devolver BloqueosN06 = 0 antes de ejecutar el backfill de N0.6.C2.
-- Contrato de mapeo documental:
--   Compra / CompraAnulada -> CompraId
--   Venta / VentaAnulada -> VentaId
--   ConsumoInsumo -> ConsumoInsumoId
-- Los movimientos Tipo=Ajuste pueden conservar temporalmente un origen legacy
-- no documental (p. ej. CargaMasiva) y no participan del backfill de FKs tipadas.

SET @tipos_invalidos := (
    SELECT COUNT(*)
      FROM MovimientosInventario m
     WHERE m.ReferenciaTipo IS NULL
        OR (
             CAST(m.ReferenciaTipo AS BINARY) NOT IN (
               CAST('Compra' AS BINARY),
               CAST('CompraAnulada' AS BINARY),
               CAST('Venta' AS BINARY),
               CAST('VentaAnulada' AS BINARY),
               CAST('ConsumoInsumo' AS BINARY)
             )
             AND CAST(m.Tipo AS BINARY) <> CAST('Ajuste' AS BINARY)
           )
);

SET @ids_invalidos := (
    SELECT COUNT(*)
      FROM MovimientosInventario m
     WHERE m.ReferenciaId IS NULL OR m.ReferenciaId <= 0
);

SET @compras_huerfanas := (
    SELECT COUNT(*)
      FROM MovimientosInventario m
      LEFT JOIN Compras c ON c.Id = m.ReferenciaId
     WHERE CAST(m.ReferenciaTipo AS BINARY) IN (
             CAST('Compra' AS BINARY),
             CAST('CompraAnulada' AS BINARY)
           )
       AND c.Id IS NULL
);

SET @ventas_huerfanas := (
    SELECT COUNT(*)
      FROM MovimientosInventario m
      LEFT JOIN Ventas v ON v.Id = m.ReferenciaId
     WHERE CAST(m.ReferenciaTipo AS BINARY) IN (
             CAST('Venta' AS BINARY),
             CAST('VentaAnulada' AS BINARY)
           )
       AND v.Id IS NULL
);

SET @consumos_huerfanos := (
    SELECT COUNT(*)
      FROM MovimientosInventario m
      LEFT JOIN ConsumosInsumos c ON c.Id = m.ReferenciaId
     WHERE CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
       AND c.Id IS NULL
);

SET @violaciones := @tipos_invalidos
                  + @ids_invalidos
                  + @compras_huerfanas
                  + @ventas_huerfanas
                  + @consumos_huerfanos;

SELECT @violaciones AS BloqueosN06;
