-- ERP-N1.8.C — Postcheck read-only de persistencia de reservas.
-- Verifica tablas, índices, FKs y ausencia de estados físicos imposibles.

SELECT required.Nombre,
       CASE WHEN t.table_name IS NULL THEN 'FALTA' ELSE 'OK' END AS Estado
FROM (
    SELECT 'ReservasInventario' AS Nombre
    UNION ALL SELECT 'ReservaInventarioDetalles'
) required
LEFT JOIN information_schema.tables t
       ON t.table_schema = DATABASE()
      AND t.table_name = required.Nombre
ORDER BY required.Nombre;

SELECT s.table_name,
       s.index_name,
       s.non_unique,
       GROUP_CONCAT(s.column_name ORDER BY s.seq_in_index) AS Columnas
FROM information_schema.statistics s
WHERE s.table_schema = DATABASE()
  AND s.table_name IN ('ReservasInventario', 'ReservaInventarioDetalles')
  AND s.index_name IN (
      'UX_ReservasInventario_Numero',
      'IX_ReservasInventario_Estado_Expiracion',
      'IX_ReservaDetalles_ExistenciaFisica',
      'UX_ReservaDetalles_ClaveFisica')
GROUP BY s.table_name, s.index_name, s.non_unique
ORDER BY s.table_name, s.index_name;

SELECT tc.table_name,
       tc.constraint_name,
       tc.constraint_type
FROM information_schema.table_constraints tc
WHERE tc.constraint_schema = DATABASE()
  AND tc.table_name IN ('ReservasInventario', 'ReservaInventarioDetalles')
  AND tc.constraint_name IN (
      'FK_ReservasInventario_Ventas_VentaId',
      'FK_ReservaInventarioDetalles_ReservasInventario_ReservaInventarioId',
      'FK_ReservaDetalles_ProductoVariantes_ProductoVarianteId',
      'FK_ReservaDetalles_Almacenes_AlmacenId',
      'FK_ReservaDetalles_Ubicacion_MismoAlmacen')
ORDER BY tc.table_name, tc.constraint_name;

SELECT COUNT(*) AS ReservasSinDetalle
FROM ReservasInventario r
WHERE r.Estado <> 0
  AND NOT EXISTS (
      SELECT 1
      FROM ReservaInventarioDetalles d
      WHERE d.ReservaInventarioId = r.Id);

SELECT COUNT(*) AS DetallesInvalidos
FROM ReservaInventarioDetalles d
WHERE d.CantidadReservada <= 0
   OR d.CantidadConsumida < 0
   OR d.CantidadConsumida > d.CantidadReservada;

SELECT COUNT(*) AS UbicacionesFueraDeAlmacen
FROM ReservaInventarioDetalles d
JOIN UbicacionesAlmacen u ON u.Id = d.UbicacionAlmacenId
WHERE d.UbicacionAlmacenId IS NOT NULL
  AND u.AlmacenId <> d.AlmacenId;
