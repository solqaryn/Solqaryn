-- ERP-N0.2 — preflight antes de retirar CatalogosProducto
-- Debe ejecutarse sobre el esquema inmediatamente anterior a N0.2.
-- Criterio de salida: la última fila (Bloqueos) debe ser 0.

SELECT 'legacy_marca_sin_normalizado' AS Regla, COUNT(*) AS Total
FROM CatalogosProducto c
LEFT JOIN Marcas m ON m.Id = c.Id
WHERE c.Tipo = 'Marca' AND m.Id IS NULL
UNION ALL
SELECT 'legacy_modelo_sin_normalizado', COUNT(*)
FROM CatalogosProducto c
LEFT JOIN Modelos m ON m.Id = c.Id
WHERE c.Tipo = 'Modelo' AND m.Id IS NULL
UNION ALL
SELECT 'legacy_color_sin_normalizado', COUNT(*)
FROM CatalogosProducto c
LEFT JOIN Colores x ON x.Id = c.Id
WHERE c.Tipo = 'Color' AND x.Id IS NULL
UNION ALL
SELECT 'legacy_talla_sin_normalizado', COUNT(*)
FROM CatalogosProducto c
LEFT JOIN Tallas t ON t.Id = c.Id
WHERE c.Tipo = 'Talla' AND t.Id IS NULL
UNION ALL
SELECT 'modelo_marca_desalineada', COUNT(*)
FROM CatalogosProducto c
JOIN Modelos m ON m.Id = c.Id
WHERE c.Tipo = 'Modelo' AND NOT (m.MarcaId <=> c.CatalogoPadreId)
UNION ALL
SELECT 'producto_marca_huerfana', COUNT(*)
FROM Productos p LEFT JOIN Marcas m ON m.Id = p.MarcaId
WHERE p.MarcaId IS NOT NULL AND m.Id IS NULL
UNION ALL
SELECT 'producto_modelo_huerfano', COUNT(*)
FROM Productos p LEFT JOIN Modelos m ON m.Id = p.ModeloId
WHERE p.ModeloId IS NOT NULL AND m.Id IS NULL
UNION ALL
SELECT 'producto_color_huerfano', COUNT(*)
FROM Productos p LEFT JOIN Colores c ON c.Id = p.ColorId
WHERE p.ColorId IS NOT NULL AND c.Id IS NULL
UNION ALL
SELECT 'producto_talla_huerfana', COUNT(*)
FROM Productos p LEFT JOIN Tallas t ON t.Id = p.TallaId
WHERE p.TallaId IS NOT NULL AND t.Id IS NULL
UNION ALL
SELECT 'variante_marca_huerfana', COUNT(*)
FROM ProductoVariantes pv LEFT JOIN Marcas m ON m.Id = pv.MarcaId
WHERE pv.MarcaId IS NOT NULL AND m.Id IS NULL
UNION ALL
SELECT 'variante_modelo_huerfano', COUNT(*)
FROM ProductoVariantes pv LEFT JOIN Modelos m ON m.Id = pv.ModeloId
WHERE pv.ModeloId IS NOT NULL AND m.Id IS NULL
UNION ALL
SELECT 'variante_color_huerfano', COUNT(*)
FROM ProductoVariantes pv LEFT JOIN Colores c ON c.Id = pv.ColorId
WHERE pv.ColorId IS NOT NULL AND c.Id IS NULL
UNION ALL
SELECT 'variante_talla_huerfana', COUNT(*)
FROM ProductoVariantes pv LEFT JOIN Tallas t ON t.Id = pv.TallaId
WHERE pv.TallaId IS NOT NULL AND t.Id IS NULL;

SELECT
    (SELECT COUNT(*) FROM CatalogosProducto c LEFT JOIN Marcas m ON m.Id=c.Id WHERE c.Tipo='Marca' AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM CatalogosProducto c LEFT JOIN Modelos m ON m.Id=c.Id WHERE c.Tipo='Modelo' AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM CatalogosProducto c LEFT JOIN Colores x ON x.Id=c.Id WHERE c.Tipo='Color' AND x.Id IS NULL)
  + (SELECT COUNT(*) FROM CatalogosProducto c LEFT JOIN Tallas t ON t.Id=c.Id WHERE c.Tipo='Talla' AND t.Id IS NULL)
  + (SELECT COUNT(*) FROM CatalogosProducto c JOIN Modelos m ON m.Id=c.Id WHERE c.Tipo='Modelo' AND NOT (m.MarcaId <=> c.CatalogoPadreId))
  + (SELECT COUNT(*) FROM Productos p LEFT JOIN Marcas m ON m.Id=p.MarcaId WHERE p.MarcaId IS NOT NULL AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM Productos p LEFT JOIN Modelos m ON m.Id=p.ModeloId WHERE p.ModeloId IS NOT NULL AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM Productos p LEFT JOIN Colores c ON c.Id=p.ColorId WHERE p.ColorId IS NOT NULL AND c.Id IS NULL)
  + (SELECT COUNT(*) FROM Productos p LEFT JOIN Tallas t ON t.Id=p.TallaId WHERE p.TallaId IS NOT NULL AND t.Id IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv LEFT JOIN Marcas m ON m.Id=pv.MarcaId WHERE pv.MarcaId IS NOT NULL AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv LEFT JOIN Modelos m ON m.Id=pv.ModeloId WHERE pv.ModeloId IS NOT NULL AND m.Id IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv LEFT JOIN Colores c ON c.Id=pv.ColorId WHERE pv.ColorId IS NOT NULL AND c.Id IS NULL)
  + (SELECT COUNT(*) FROM ProductoVariantes pv LEFT JOIN Tallas t ON t.Id=pv.TallaId WHERE pv.TallaId IS NOT NULL AND t.Id IS NULL)
  AS Bloqueos;
