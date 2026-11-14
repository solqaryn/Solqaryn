# Punto 20 — Suite completa de integración MySQL

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
Commit exact-head de código: `e505ebd12776db3cf579f55fb28a0dfcb9484fc5` (`fix: avoid span Contains in EF8 net10 queries`)

## Hallazgo y corrección

- Los fallos del ensayo EF8/Pomelo retargeteado temporalmente a net10 tenían una causa común: EF8 evaluaba un `Contains` capturado desde `int[]` a través de `ReadOnlySpan<int>`, ruta que falla en el intérprete de expresiones de .NET 10.
- `MovimientoInventarioRepository` ahora materializa como `List<int>` las colecciones usadas por `GetOrigenesTipadosAsync` y `ExisteMovimientoPosteriorAsync`; se mantiene el orden, distinct, semántica SQL y comportamiento de lista vacía.

## Evidencia exact-head

- Gate Fase 6 MySQL/EF DEV, run `37313721294`, HEAD `e505ebd12776db3cf579f55fb28a0dfcb9484fc5`: todos los jobs success, `FASE_6_MYSQL_EF_PROVIDER=PASS`, `P0=0`, `P1=0`, `PHASE7_EXECUTED=false`.
- Pomelo/MySQL 8.4.11: migraciones y contratos físicos completaron; categoría completa `Category=Integration`: **28/28 passed, 0 failed, 0 skipped**.
- Ensayo aislado EF8/Pomelo bajo net10: **28/28 integraciones passed, 0 failed, 0 skipped**. Los tres fallos anteriores (`ConsumoInsumoIntegrationTests`, `InventarioDocumentConcurrencyTests` y `MovimientoInventarioOrigenTipadoIntegrationTests`) ya no se reproducen.
- Lane Oracle EF10/net10: conjunto portable de integración **22/22 passed**.
- [Gate Fase 6 exact-head](https://github.com/solqaryn/Solqaryn/actions/runs/37313721294).

Los ocho fallos unitarios de `ReporteComprasServiceTests` (2380/2388 passed en net10) son independientes y permanecen abiertos exclusivamente para el punto 21.

- La adaptación compila y opera sobre una copia temporal aislada del esquema MySQL certificado. Conserva CHECKs desde `SHOW CREATE TABLE`, claves foráneas, índices y datos semilla; la inicialización concurrente se evalúa una sola vez por base temporal.
- Cuatro archivos se excluyen explícitamente de la lane Oracle por depender del replay/rollback del historial MySQL o del bridge instalado por N06: `N08MigracionesLimpiezaPreflightIntegrationTests.cs`, `N08PersistenciaLimpiezaIntegrationTests.cs`, `N110CosteoMigrationIntegrationTests.cs` y `MovimientoInventarioOrigenTipadoIntegrationTests.cs`. Permanecen cubiertos por la suite Pomelo/MySQL real; no se reportan como pruebas Oracle ejecutadas.

## Estado de Fase 6

No se cambió el TargetFramework productivo, no se ejecutó Fase 7 y no hubo despliegues.

## Resultado

**Punto 20: CERRADO** — integración MySQL completa en Pomelo 28/28 y en la copia EF8/Pomelo retargeteada a net10 28/28; Oracle EF10 22/22. Los tres fallos de integración fueron corregidos y la deuda unitaria separada sigue en el punto 21. Fase 7 no ejecutada.

MAPA_ARQUITECTURA: SIN_CAMBIO.
