SET @errores :=
    (SELECT COUNT(*) FROM Productos p WHERE p.Eliminado = 0 AND NOT EXISTS (SELECT 1 FROM ProductoVariantes pv WHERE pv.ProductoId = p.Id AND pv.Eliminado = 0))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE Eliminado = 0 AND (Sku IS NULL OR TRIM(Sku) = '' OR Cantidad < 0 OR UmbralStockBajo < 0 OR (Costo IS NOT NULL AND Costo < 0) OR (Precio IS NOT NULL AND Precio < 0)))
  + (SELECT COUNT(*) FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) = '')
  + (SELECT COUNT(*) FROM ProductoVariantes pv JOIN Modelos m ON m.Id = pv.ModeloId WHERE pv.ModeloId IS NOT NULL AND (pv.MarcaId IS NULL OR pv.MarcaId <> m.MarcaId))
  + (SELECT COUNT(*) FROM ProductoImagenes pi JOIN ProductoVariantes pv ON pv.Id = pi.ProductoVarianteId WHERE pi.ProductoVarianteId IS NOT NULL AND pi.ProductoId <> pv.ProductoId)
  + (SELECT COUNT(*) FROM (SELECT UPPER(TRIM(Sku)) k FROM ProductoVariantes GROUP BY UPPER(TRIM(Sku)) HAVING COUNT(*) > 1) x)
  + (SELECT COUNT(*) FROM (SELECT TRIM(CodigoBarras) k FROM ProductoVariantes WHERE CodigoBarras IS NOT NULL GROUP BY TRIM(CodigoBarras) HAVING COUNT(*) > 1) x)
  + (SELECT IF(COUNT(*) = 6, 0, 1) FROM information_schema.table_constraints WHERE constraint_schema = DATABASE() AND table_name = 'ProductoVariantes' AND constraint_name IN ('CK_ProductoVariantes_N03_Sku','CK_ProductoVariantes_N03_Barcode','CK_ProductoVariantes_N03_Stock','CK_ProductoVariantes_N03_Importes','CK_ProductoVariantes_N03_ModeloMarca','CK_ProductoVariantes_N03_TecnicaBarcode'))
  + (SELECT IF(COUNT(*) = 2, 0, 1) FROM information_schema.referential_constraints WHERE constraint_schema = DATABASE() AND constraint_name IN ('FK_ProductoVariantes_Modelos_ModeloMarca_N03','FK_ProductoImagenes_VarianteProducto_N03'));
SELECT @errores AS ErroresN03;
