# Punto 20 — Suite completa de integración MySQL

Fecha: 2026-10-05 (UTC)  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`  
Commit exact-head de código: `6cb63f4918` (`test(mysql): serialize Oracle test schema bootstrap`)

## Evidencia

- Gate Fase 6 MySQL/EF DEV, run `37258284249`, job Pomelo `111600074524`: migraciones, contratos físicos y semántica MySQL 8.4 completaron correctamente; toda la categoría `Category=Integration`: **28/28 passed, 0 failed, 0 skipped**.
- Lane Oracle EF10/net10, run `37258284262`, job `111599898836`: el conjunto portable de integración fue adaptado al provider candidato y terminó **22/22 passed**.
- La adaptación compila y opera sobre una copia temporal aislada del esquema MySQL certificado. Conserva CHECKs desde `SHOW CREATE TABLE`, claves foráneas, índices y datos semilla; la inicialización concurrente se evalúa una sola vez por base temporal.
- Cuatro archivos se excluyen explícitamente de la lane Oracle por depender del replay/rollback del historial MySQL o del bridge instalado por N06: `N08MigracionesLimpiezaPreflightIntegrationTests.cs`, `N08PersistenciaLimpiezaIntegrationTests.cs`, `N110CosteoMigrationIntegrationTests.cs` y `MovimientoInventarioOrigenTipadoIntegrationTests.cs`. Permanecen cubiertos por la suite Pomelo/MySQL real; no se reportan como pruebas Oracle ejecutadas.

## Estado de Fase 6

El punto 20 mide la suite MySQL y queda cubierto por el resultado 28/28 anterior. **Fase 6 global continúa en STOP**: en la ejecución compuesta exact-head `37258284249`, el job separado de certificación Oracle EF10 registró 21/22, con fallo en `TipoClienteConcurrencyTests.Concurrency_MarcarPredeterminado_ConDosContextosIndependientes_SoloUnGanador` (`Assert.NotNull`); la lane Oracle aislada del mismo HEAD sí pasó 22/22. Esta diferencia queda visible y pendiente de resolver en su punto correspondiente; no se usa para invalidar ni para ocultar el resultado Pomelo/MySQL.

No se cambió el TargetFramework productivo, no se ejecutó Fase 7 y no hubo despliegues.

## Resultado

**Punto 20: CERRADO** — categoría completa de integración MySQL: 28/28 en MySQL 8.4 real de CI. La certificación total de Fase 6 permanece bloqueada por la discrepancia de concurrencia Oracle indicada arriba.

MAPA_ARQUITECTURA: SIN_CAMBIO.
