# Contrato permanente de rendimiento — ERP SOLQARYN

Autoridad técnica vigente: `docs/PROJECT_SCOPE_LOCK.md`, `AGENTS.md` y comportamiento real del ERP. Ningún plan antiguo define automáticamente un gate.

## Qué ya está demostrado

N5.9 eliminó un hotspot causal del reporte Stock Health y preservó filtros server-side, `AsNoTracking`, `CountAsync`, paginación y orden. La API dispone además de medición de latencia P50/P95 para búsquedas seleccionadas y Angular mantiene budgets de producción.

Nada de lo anterior autoriza a inventar un porcentaje de mejora, una latencia absoluta o capacidad de carga global.

## Protocolo obligatorio para certificar cargas grandes

La certificación cuantitativa debe ejecutarse sobre un SHA exacto y un MySQL aislado/reproducible. Debe registrar versión de DB, hardware/runner, seed y cardinalidades; preparar conjuntos pequeño/medio/grande sin usar datos reales sensibles; calentar la ruta antes de medir; ejecutar suficientes muestras para reportar P50/P95/P99, errores y throughput; capturar consultas/planes para hotspots; comprobar que paginación y límites se mantienen; medir payload y bundle frontend; y publicar resultados como artifact asociado al run.

Las rutas mínimas son búsquedas de productos/clientes/proveedores/sucursales, listados de ventas/compras/inventario, Stock Health/Kardex y reportes de mayor cardinalidad. Las pruebas deben incluir concurrencia y una carga grande suficiente para exponer N+1, materialización completa o paginación falsa.

## Regla de cierre

`PERFORMANCE_CERTIFIED = TRUE` sólo cuando existe benchmark reproducible con dataset documentado y evidencia del SHA exacto. Una optimización estructural o una ejecución con base casi vacía es evidencia útil, pero **no** sustituye la prueba de carga de N8.7.

Estado actual: `STRUCTURAL_PERFORMANCE_GUARDS = PASS`; `LARGE_LOAD_BENCHMARK = PENDING_N8.7`.
