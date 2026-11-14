-- ERP-N1.7.C — Verificación read-only de invariantes físicas de conteos.
-- Debe ejecutarse después de migración/postcheck. Toda fila VIOLACION bloquea certificación.

SELECT 'VIOLACION_NUMERO_DUPLICADO' AS Hallazgo, Numero, COUNT(*) AS Repeticiones
  FROM ConteosInventario
 GROUP BY Numero
HAVING COUNT(*) > 1;

SELECT 'VIOLACION_DETALLE_OTRO_ALMACEN' AS Hallazgo, d.Id
  FROM ConteoInventarioDetalles d
  JOIN ConteosInventario c ON c.Id = d.ConteoInventarioId
 WHERE d.AlmacenId <> c.AlmacenId;

SELECT 'VIOLACION_UBICACION_CABECERA' AS Hallazgo, c.Id
  FROM ConteosInventario c
  JOIN UbicacionesAlmacen u ON u.Id = c.UbicacionAlmacenId
 WHERE c.UbicacionAlmacenId IS NOT NULL
   AND u.AlmacenId <> c.AlmacenId;

SELECT 'VIOLACION_UBICACION_DETALLE' AS Hallazgo, d.Id
  FROM ConteoInventarioDetalles d
  JOIN UbicacionesAlmacen u ON u.Id = d.UbicacionAlmacenId
 WHERE d.UbicacionAlmacenId IS NOT NULL
   AND u.AlmacenId <> d.AlmacenId;

SELECT 'VIOLACION_CLAVE_FISICA_DUPLICADA' AS Hallazgo,
       ConteoInventarioId,
       ProductoVarianteId,
       AlmacenId,
       COALESCE(UbicacionAlmacenId, 0) AS UbicacionNormalizada,
       COUNT(*) AS Repeticiones
  FROM ConteoInventarioDetalles
 GROUP BY ConteoInventarioId, ProductoVarianteId, AlmacenId, COALESCE(UbicacionAlmacenId, 0)
HAVING COUNT(*) > 1;

SELECT 'VIOLACION_CANTIDAD_NEGATIVA' AS Hallazgo, Id
  FROM ConteoInventarioDetalles
 WHERE StockEsperadoSnapshot < 0
    OR (CantidadContada IS NOT NULL AND CantidadContada < 0);

SELECT 'VIOLACION_DIFERENCIA' AS Hallazgo, Id
  FROM ConteoInventarioDetalles
 WHERE CantidadContada IS NOT NULL
   AND Diferencia <> CantidadContada - StockEsperadoSnapshot;

SELECT 'VIOLACION_AJUSTE_SIN_DIFERENCIA' AS Hallazgo, Id
  FROM ConteoInventarioDetalles
 WHERE AjusteInventarioId IS NOT NULL
   AND COALESCE(Diferencia, 0) = 0;

SELECT 'RESUMEN' AS Tipo,
       COUNT(*) AS Conteos,
       SUM(Estado = 0) AS Borradores,
       SUM(Estado = 1) AS EnProceso,
       SUM(Estado = 2) AS Cerrados,
       SUM(Estado = 3) AS Aprobados,
       SUM(Estado = 4) AS Cancelados
  FROM ConteosInventario;
