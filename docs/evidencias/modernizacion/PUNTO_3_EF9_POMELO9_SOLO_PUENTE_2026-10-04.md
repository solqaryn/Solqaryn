# Punto 3 — EF9 + Pomelo 9 es sólo un puente

Fecha: 2026-10-04  
Repositorio: `solqaryn/Solqaryn`  
Rama: `dev`

## Dictamen

**Estado histórico: PASS en el HEAD de la sonda citada; no certifica el HEAD actual. Ver revalidación de vigencia al final.** El candidato EF Core 9 + Pomelo 9 se limita a una sonda transitoria de CI; no es la arquitectura final ni se adoptó en los proyectos productivos. El provider objetivo certificado para el cierre de Fase 6 es Oracle `MySql.EntityFrameworkCore 10.0.9` con EF Core `10.0.12` en una copia aislada `net10.0`.

## Evidencia del puente

- El workflow [Modernización - Probe EF9 Pomelo9 DEV](../../../.github/workflows/modernization-ef9-pomelo9-probe.yml) prueba EF Core `9.0.20` + Pomelo `9.0.0` + MySqlConnector `2.6.2` en `net8.0` y luego en una copia temporal `net10.0`.
- El run final `37216002726`, sobre `005d516a74d3ebc52a5799b43ba8a3b062ba45e0`, terminó `success`; su job `EF9 Pomelo9 net8 y net10` y los pasos de ambas lanes, migraciones, `has-pending-model-changes`, suites unitarias/integración, audit tenant y comparación de columnas/índices terminaron correctamente. [Run del probe](https://github.com/solqaryn/Solqaryn/actions/runs/37216002726).
- Hubo dos intentos anteriores fallidos en la etapa de certificación `net8` (`37215447812`, `37215532912`); no se toman como evidencia de aprobación. La conclusión se basa en el run final exitoso.
- En la fecha de aquella revisión, el HEAD `9d2dad3da651d604bd392aef49e2580dbd3d30fa` no tenía cambios relevantes respecto al run. Esa conclusión era temporal y dejó de ser válida tras cambios posteriores descritos abajo.

## Auditoría de vigencia — 2026-10-05

La comparación anterior quedó obsoleta y no se usa para declarar el punto cerrado:

- El workflow cambió después del run `37216002726`: ahora ambos lanes ejecutan `priority3_tenant_isolation_audit.py --require-certified`, por lo que el run histórico no certifica esta exigencia actual.
- Cambió `TipoClienteConcurrencyTests.cs` y se reforzó `UnitOfWorkRetryTests.cs`; son tests que ejecuta la matriz candidata. El run histórico no contiene esas revisiones.
- En el HEAD `bf744ccd63bb4ce0509efd80c2f1e59fdd4fee64`, esos inputs ya están presentes, pero el workflow sólo se disparaba en push cuando cambiaba su propio archivo. No había automatización por cambios en código backend.
- El disparador de push cubre `backend/**`, la sonda de provider y el auditor tenant para que cambios en sus inputs reejecuten el probe.
- La revalidación exact-head [37277207917](https://github.com/solqaryn/Solqaryn/actions/runs/37277207917), HEAD `248c63b75c74c28738d79c59567d1831c04b56e6`, encontró un defecto del harness, no del provider: net8 unitarios pasaron `2388/2388`, pero integración quedó `27/28`. El test nuevo `SecuenciaDocumentoConcurrencyIntegrationTests` no estaba en la lista que acorta nombres de bases efímeras; MySQL rechazó `__test_sequence_concurrency_<guid>_EFMigrationsLock` por exceder 64 caracteres.
- Se incorporó ese archivo a la adaptación temporal de nombres del probe. El rerun corregido [37278090999](https://github.com/solqaryn/Solqaryn/actions/runs/37278090999), HEAD de código `dc063110fca28eb7e715322ab3aff214ab211cc9`, terminó `success`: `EF9_SHORT_TEST_DATABASE_NAMES=27`; net8 y net10 reportaron unitarios `2388/2388` e integración `28/28`; `PRIORITY3_MULTITENANT_CERTIFICATION=PASS` con ocho contratos tenant certificados en ambas lanes; migraciones y `has-pending-model-changes` completaron; la comparación de columnas e índices terminó `EF9_POMELO9_NET8_NET10=PASS`.

**Dictamen histórico (2026-10-05): CERRADO para el HEAD de la sonda.** El fallo se resolvió en el harness efímero sin modificar paquetes ni proyectos productivos. La vigencia actual queda pendiente según la revalidación fechada al final.

## No adopción / arquitectura vigente

- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` permanece en `net8.0`, EF Core `8.0.2`, Pomelo `8.0.2` y MySqlConnector `2.3.7`.
- `backend/src/API/Solqaryn.API.csproj` permanece en `net8.0`; no hay retarget productivo a `net10.0`.
- El gate definitivo de Fase 6 en el HEAD vigente terminó `success` en el run `37228972369`; selecciona Oracle EF10, no EF9/Pomelo9. [Run exact-head de Fase 6](https://github.com/solqaryn/Solqaryn/actions/runs/37228972369).
- Las modificaciones EF9/Pomelo9 y el retarget a net10 viven sólo dentro del runner/copia efímera del probe; no se guardan en los proyectos productivos.
- `PHASE7_EXECUTED=false`.

## Criterio de cierre

En el HEAD certificado históricamente, el probe EF9/Pomelo9 fue temporal; la ruta elegida fue EF10 con Oracle Connector/NET. La revalidación actual del probe está pendiente según el dictamen fechado al final. EF9/Pomelo9 no se convierte en dependencia productiva y Fase 7 no se ejecuta.

MAPA_ARQUITECTURA: SIN_CAMBIO.


## Revalidación de vigencia — 2026-10-06

**Estado actual: PASS — revalidado exact-head.** La sonda finalizó correctamente sobre `dev` SHA `186c6e31754bd10c09218d2eac2102dff06db491` (run [37414509893](https://github.com/solqaryn/Solqaryn/actions/runs/37414509893), evento `push`).

- La sonda anterior `37278090999` sobre `dc063110fca28eb7e715322ab3aff214ab211cc9` era histórica y no se usó para cerrar el estado actual.
- Después de ese commit cambiaron dos archivos de backend que forman parte de la superficie probada: `backend/src/Infrastructure/Repositories/MovimientoInventarioRepository.cs` y `backend/src/Infrastructure/Services/ReporteComprasService.cs`. Por tanto, el run histórico no prueba esos cambios posteriores.
- En el HEAD auditado `3ff3ed02084030e7b91e759fedb75ae5ad8144c4`, los proyectos productivos siguen en `net8.0`, EF Core `8.0.2` y Pomelo `8.0.2`; la sonda EF9/Pomelo9 sigue siendo una modificación temporal del runner, no una dependencia productiva.
- La certificación de Fase 6 de ese HEAD terminó con éxito en [run 37412576773](https://github.com/solqaryn/Solqaryn/actions/runs/37412576773), pero valida la ruta Oracle EF10 y no sustituye la sonda EF9/Pomelo9.
- El workflow también se dispara cuando se modifica esta acta. Esta actualización documental provocará un nuevo run al integrarse; el dictamen permanece sujeto a que esa ejecución exact-head del nuevo HEAD termine en éxito.
- Evidencia del run exact-head: net8 y net10 aprobaron cada uno 2,388/2,388 pruebas unitarias y 28/28 pruebas de integración; `PRIORITY3_MULTITENANT_CERTIFICATION=PASS` en ambas lanes; migraciones y `has-pending-model-changes` pasaron; `EF9_POMELO9_NET8_NET10=PASS` tras comparar columnas e índices.
- `PHASE7_EXECUTED=false`; no se cambia de fase.
