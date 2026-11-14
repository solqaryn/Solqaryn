-- ERP-N1.9.C — Preflight read-only para lotes, series y vencimientos.
-- No altera datos ni esquema. Debe ejecutarse antes de aplicar 20260817100000_N1_9_TrazabilidadLotesSeries.

SELECT 'ProductoVariantes existe' AS CheckName,
       COUNT(*) = 1 AS Ok
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name = 'ProductoVariantes';

SELECT 'Tablas N1.9 aún no materializadas' AS CheckName,
       COUNT(*) = 0 AS Ok,
       GROUP_CONCAT(table_name ORDER BY table_name) AS Encontradas
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name IN ('LotesInventario', 'SeriesInventario');

SELECT 'Columnas N1.9 aún no materializadas' AS CheckName,
       COUNT(*) = 0 AS Ok,
       GROUP_CONCAT(column_name ORDER BY column_name) AS Encontradas
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'ProductoVariantes'
  AND column_name IN (
      'ControlaLote',
      'ControlaNumeroSerie',
      'ControlaFechaVencimiento',
      'DiasAlertaVencimiento'
  );

SELECT 'Variantes activas baseline' AS CheckName,
       COUNT(*) AS TotalVariantes,
       SUM(CASE WHEN Eliminado = 0 THEN 1 ELSE 0 END) AS VariantesNoEliminadas
FROM ProductoVariantes;

SELECT 'Historial de migración disponible' AS CheckName,
       COUNT(*) = 1 AS Ok
FROM information_schema.tables
WHERE table_schema = DATABASE()
  AND table_name = '__EFMigrationsHistory';
