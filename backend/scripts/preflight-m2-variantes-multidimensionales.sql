-- M2 preflight fail-closed: no modifica datos.
-- Debe devolver cero filas en todas las consultas antes de aplicar la migración M2.

-- Colores de variantes sin maestro normalizado.
SELECT pv.Id AS VarianteId, pv.ColorId
FROM ProductoVariantes pv
LEFT JOIN Colores c ON c.Id = pv.ColorId
WHERE pv.ColorId IS NOT NULL AND c.Id IS NULL;

-- Dimensiones legacy de producto sin maestro normalizado.
SELECT p.Id AS ProductoId, p.MarcaId, p.ModeloId, p.ColorId, p.TallaId
FROM Productos p
LEFT JOIN Marcas ma ON ma.Id = p.MarcaId
LEFT JOIN Modelos mo ON mo.Id = p.ModeloId
LEFT JOIN Colores co ON co.Id = p.ColorId
LEFT JOIN Tallas ta ON ta.Id = p.TallaId
WHERE (p.MarcaId IS NOT NULL AND ma.Id IS NULL)
   OR (p.ModeloId IS NOT NULL AND mo.Id IS NULL)
   OR (p.ColorId IS NOT NULL AND co.Id IS NULL)
   OR (p.TallaId IS NOT NULL AND ta.Id IS NULL);

-- Modelo legacy que no pertenece a la marca indicada.
SELECT p.Id AS ProductoId, p.MarcaId, p.ModeloId, mo.MarcaId AS MarcaRealModelo
FROM Productos p
JOIN Modelos mo ON mo.Id = p.ModeloId
WHERE p.ModeloId IS NOT NULL
  AND (p.MarcaId IS NULL OR mo.MarcaId <> p.MarcaId);

-- SKU no vacío duplicado (la restricción actual debería impedirlo).
SELECT UPPER(TRIM(Sku)) AS SkuNormalizado, COUNT(*) AS Repeticiones
FROM ProductoVariantes
WHERE Sku IS NOT NULL AND TRIM(Sku) <> ''
GROUP BY UPPER(TRIM(Sku))
HAVING COUNT(*) > 1;

-- Código de barras no vacío duplicado.
SELECT TRIM(CodigoBarras) AS CodigoBarrasNormalizado, COUNT(*) AS Repeticiones
FROM ProductoVariantes
WHERE CodigoBarras IS NOT NULL AND TRIM(CodigoBarras) <> ''
GROUP BY TRIM(CodigoBarras)
HAVING COUNT(*) > 1;

-- La unicidad Producto+Color vigente actual garantiza que el backfill inicial
-- Marca/Modelo/Talla desde Producto no colisiona entre variantes comerciales.
SELECT ProductoId, ColorId, COUNT(*) AS Repeticiones
FROM ProductoVariantes
WHERE Eliminado = 0 AND EsTecnica = 0
GROUP BY ProductoId, ColorId
HAVING COUNT(*) > 1;
