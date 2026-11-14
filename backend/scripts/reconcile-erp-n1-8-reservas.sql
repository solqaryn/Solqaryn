-- ERP-N1.8.C — Reconciliación read-only posterior a la migración.
-- N1.8.C no posee fuente legacy de reservas: no existe backfill de documentos.
-- Este control demuestra que la nueva estructura nace sin alterar la autoridad
-- física ExistenciaVariante; la mutación de StockReservado comienza en N1.8.D.

SELECT COUNT(*) AS ReservasIniciales
FROM ReservasInventario;

SELECT COUNT(*) AS DetallesIniciales
FROM ReservaInventarioDetalles;

SELECT COUNT(*) AS ExistenciasInvalidas
FROM ExistenciasVariante
WHERE StockFisico < 0
   OR StockReservado < 0
   OR StockReservado > StockFisico
   OR StockDisponible <> StockFisico - StockReservado;

SELECT COUNT(*) AS ReservasSinDetalleFueraDeBorrador
FROM ReservasInventario r
WHERE r.Estado <> 0
  AND NOT EXISTS (
      SELECT 1
      FROM ReservaInventarioDetalles d
      WHERE d.ReservaInventarioId = r.Id);

SELECT COUNT(*) AS ClavesFisicasDuplicadas
FROM (
    SELECT d.ReservaInventarioId,
           d.ProductoVarianteId,
           d.AlmacenId,
           COALESCE(d.UbicacionAlmacenId, 0) AS UbicacionNormalizada,
           COUNT(*) AS Cantidad
    FROM ReservaInventarioDetalles d
    GROUP BY d.ReservaInventarioId,
             d.ProductoVarianteId,
             d.AlmacenId,
             COALESCE(d.UbicacionAlmacenId, 0)
    HAVING COUNT(*) > 1
) duplicadas;
