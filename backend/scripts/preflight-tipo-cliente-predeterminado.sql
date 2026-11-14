-- Preflight check para contar registros TipoCliente predeterminados activos no eliminados
SELECT COUNT(*)
FROM TipoClientes
WHERE EsPredeterminado = 1
  AND Activo = 1
  AND Eliminado = 0;
