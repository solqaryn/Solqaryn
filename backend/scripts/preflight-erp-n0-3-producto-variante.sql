SET @violaciones :=
    (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND ModeloId IS NOT NULL AND MarcaId IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.Eliminado = 0 AND pv.ModeloId IS NOT NULL AND pv.MarcaId <> m.MarcaId)
  + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
  + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes WHERE Sku IS NOT NULL AND TRIM(Sku) <> '' GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) <> '' GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT ProductoId FROM ProductoVariantes WHERE Eliminado = 0 GROUP BY ProductoId HAVING SUM(EsTecnica = 1) > 0 AND SUM(EsTecnica = 0) > 0) x)
  + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND p.ModeloId IS NOT NULL AND (p.MarcaId IS NULL OR NOT EXISTS (SELECT 1 FROM Modelos m WHERE m.Id = p.ModeloId AND m.MarcaId = p.MarcaId)))
  + (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0) AND EXISTS (SELECT 1 FROM ProductoVariantes z WHERE UPPER(TRIM(z.Sku)) = UPPER(CONCAT('TEC-', LPAD(p.Id, 10, '0')))));
SELECT @violaciones AS BloqueosN03;
