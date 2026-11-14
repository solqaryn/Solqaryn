-- ERP-N1.9.C — Postcheck read-only para lotes, series y vencimientos.
-- Debe ejecutarse después de aplicar 20260817100000_N1_9_TrazabilidadLotesSeries.

SELECT 'Tablas N1.9 materializadas' AS CheckName,
       COUNT(*) = 2 AS Ok
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('LotesInventario', 'SeriesInventario');

SELECT 'Columnas opt-in materializadas' AS CheckName,
       COUNT(*) = 4 AS Ok
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'ProductoVariantes'
  AND column_name IN (
      'ControlaLote',
      'ControlaNumeroSerie',
      'ControlaFechaVencimiento',
      'DiasAlertaVencimiento'
  );

SELECT 'Índices únicos N1.9 presentes' AS CheckName,
       SUM(index_name = 'UX_LotesInventario_Variante_Codigo' AND non_unique = 0) >= 1
       AND SUM(index_name = 'UX_SeriesInventario_NumeroSerie' AND non_unique = 0) >= 1 AS Ok
FROM information_schema.statistics
WHERE table_schema = DATABASE()
  AND table_name IN ('LotesInventario', 'SeriesInventario');

SELECT 'FKs restrictivas N1.9 presentes' AS CheckName,
       COUNT(*) = 3
       AND SUM(delete_rule IN ('RESTRICT', 'NO ACTION')) = 3 AS Ok
FROM information_schema.referential_constraints
WHERE constraint_schema = DATABASE()
  AND constraint_name IN (
      'FK_LotesInventario_ProductoVariantes_ProductoVarianteId',
      'FK_SeriesInventario_ProductoVariantes_ProductoVarianteId',
      'FK_SeriesInventario_LotesInventario_Variante_Lote'
  );

SELECT 'Serie y lote comparten variante por FK compuesta' AS CheckName,
       COUNT(*) = 2
       AND SUM(ordinal_position = 1
               AND column_name = 'ProductoVarianteId'
               AND referenced_column_name = 'ProductoVarianteId') = 1
       AND SUM(ordinal_position = 2
               AND column_name = 'LoteInventarioId'
               AND referenced_column_name = 'Id') = 1 AS Ok
FROM information_schema.key_column_usage
WHERE constraint_schema = DATABASE()
  AND table_name = 'SeriesInventario'
  AND constraint_name = 'FK_SeriesInventario_LotesInventario_Variante_Lote'
  AND referenced_table_name = 'LotesInventario';

SELECT 'Baseline legacy conservado' AS CheckName,
       COALESCE(SUM(CASE WHEN ControlaLote = 0
                          AND ControlaNumeroSerie = 0
                          AND ControlaFechaVencimiento = 0
                          AND DiasAlertaVencimiento IS NULL THEN 1 ELSE 0 END), 0) = COUNT(*) AS Ok
FROM ProductoVariantes;
