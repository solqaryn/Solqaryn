# VAEP — CONTRATO OPERATIVO ÚNICO

Este archivo gobierna exclusivamente cómo ejecuta VAEP. No es un segundo Plan Maestro ni un roadmap paralelo. La única autoridad ejecutable en la versión comercial inicial es el archivo `SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO` (Drive ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`). El Maestro antiguo queda como visión futura NO EJECUTABLE.

```text
PROJECT_ID=SOLQARYN
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
AUTOMATION_AUTHORITY=VAEP_CONTRACT
VAEP_CONTRACT_FILE=docs/VAEP_AUTHORITY.md
EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j
FUTURE_PLAN_EXECUTABLE=FALSE
EXECUTION_PLAN_TITLE=SOLQARYN_PLAN_COMERCIAL_INICIAL_UNIFICADO
ARCHIVED_NOTES_AUTHORITY=DENY
CONTEXT_MODE=CURRENT_STATE_ONLY
CERTIFIED_DONE_STATE=LISTO
ALTERNATE_DONE_STATES=PROHIBITED
```

```text
BEGIN_AUTOMATION_POLICY
EXECUTION_MODEL=TASKS_ONLY
DIRECT_EXECUTION_DEFAULT=TRUE
PRIMARY_DIRECT_BUILD_CLOSE=TRUE
SUPERVISOR_VERIFY_RECOVER_SECONDARY_BUILD=TRUE
TASK_SCOPE_LEASE_REQUIRED=TRUE
TASK_SCOPE_LEASE_SINGLE_WRITER=TRUE
TASK_SCOPE_LEASE_TTL_MINUTES=10
STALE_LEASE_TAKEOVER_AFTER_MINUTES=10
LEASE_FRESH_REQUIRES_LIVE_MATERIAL_PROGRESS=TRUE
ENDED_INVOCATION_RELEASES_LEASE_IMMEDIATELY=TRUE
DIRECT_NEXT_SAFE_PREARM_REQUIRED=TRUE
MAX_VOLUNTARY_IDLE=0
GLOBAL_DISPATCH_ADMISSION=OPEN_ONLY
GLOBAL_FROZEN_PROHIBITED=TRUE
CAUSAL_HOLD_SCOPE=TASK_OR_EXECUTION_ONLY
UNRELATED_WORKFLOW_BLOCKING_PROHIBITED=TRUE
REVIEW_BEFORE_EXPENSIVE_CI=TRUE
CLOSURE_CANDIDATE_HEAD_FREEZE=TRUE
PREARM_BEFORE_CAUSAL_CI=TRUE
VAEP_CHECKPOINTS=:00,:12,:24,:36,:48
VAEP_SUPERVISOR_CHECKPOINTS=:05,:17,:29,:41,:53
VAEP_ALL_ACTIVE_SLOTS=:00,:05,:12,:17,:24,:29,:36,:41,:48,:53
PRIMARY_RESPONSABLE_OPERATIVO=JAVIER_MEJIA
SUPERVISOR_RESPONSABLE_OPERATIVO=ALEX_MORALES
MANUAL_RUN_SLOT_GUARD=TRUE
MANUAL_RUN_NEAREST_DUE_SLOT_ONLY=TRUE
MANUAL_RUN_PAST_WINDOW_MINUTES=3
MANUAL_RUN_FUTURE_SLOT_PREEMPT_PROHIBITED=TRUE
MANUAL_RUN_OFF_SLOT_PROHIBITED=TRUE
DEFECT_RECOVERY_FIRST=TRUE
FIRST_DETECTOR_OWNS_RECOVERY=TRUE
NO_REJECT_QUEUE=TRUE
RECOVERY_MUST_RESOLVE_SAME_RUN=TRUE
RECOVERY_UNBLOCK_DEPENDENTS_SAME_RUN=TRUE
CURRENT_COMMERCIAL_ONLY=TRUE
PREVIOUS_PLAN_DEPENDENCY=PROHIBITED
PREVIOUS_PHASE_GATE=PROHIBITED
PREVIOUS_QUEUE_ANCHOR=PROHIBITED
END_AUTOMATION_POLICY
```

## 1. Fuente única y precedencia

1. La única autoridad ejecutable de la etapa inicial es `SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO` (Drive ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`); el Maestro anterior es sólo referencia futura.
2. `docs/VAEP_AUTHORITY.md` es el contrato operativo de las diez automatizaciones y no puede inventar objetivos, fases o prioridades fuera del Plan Comercial Inicial aprobado y sus 1123 microtareas autorizadas.
3. GitHub manda para código y evidencia técnica; el estado operativo fresco manda para ejecución; el Plan Comercial Inicial manda para trabajo de lanzamiento; este contrato manda para coordinación y controles de ejecución.
4. Las notas, colas y documentos archivados carecen de autoridad operativa; sólo cambios aprobados dentro del Plan Comercial Inicial alteran las 1123 tareas.
5. Ninguna fase, secuencia, fila, gate, excepción, protocolo o decisión histórica puede condicionar la ejecución actual.
6. El historial Git no se reescribe, pero tampoco se usa como autoridad operativa.
7. Ante contradicción, se corrige la superficie stale para quedar alineada al Plan Comercial Inicial único + este contrato operativo.

## 2. Modelo de ejecución: TASKS_ONLY

VAEP opera exclusivamente con las diez automatizaciones programadas como ejecutores/controllers autónomos.

Camino canónico:

`AUTOMATION -> LEASE -> EJECUCIÓN DIRECTA -> TESTS -> REVIEW_FIRST -> GATES -> LISTO -> PROMOCIÓN`

Reglas:

- Cinco slots primarios: `:00/:12/:24/:36/:48`, responsabilidad operativa de Javier Mejía.
- Cinco slots supervisores: `:05/:17/:29/:41/:53`, responsabilidad operativa de Alex Morales.
- IDs canónicos Primary (Javier): `:00=6aa15346f5408191bdd9043fd26ff7aa`, `:12=6aa1534deee481918280def1343adcfa`, `:24=6abd70e42cb4819190b3b28916dc9dbb`, `:36=6aa1535a51508191a610e6cdb90a2a4d`, `:48=6aa1535f8cd48191b73e10f17843372a`.
- IDs canónicos Supervisor (Alex): `:05=6abd62255ae88191a2dba6e1b00d3b4d`, `:17=6abd6235e100819195785fc76a44a8e0`, `:29=6abd6241c3e48191a8856ed7b0f42c5c`, `:41=6abd624e5e108191bef6bb879559328c`, `:53=6abd625b397c8191a49904227946a20f`.
- Las tareas equivalentes antiguas en la cuenta de Javier están RETIRADAS e inactivas; no son canónicas ni deben reactivarse.
- Un slot no termina en reporte o espera si existe acción material segura.
- Un checkpoint es un disparador, no una frontera de ownership.
- Tras `LISTO`, promover el siguiente trabajo dependency-valid del plan comercial inicial vigente.
- `LISTO` es el único estado de cierre certificado.

## 3. Regla CURRENT-STATE ONLY

Todo agente o automatización debe decidir desde el estado vivo actual:

- HEAD de `dev`;
- arquitectura y contratos vigentes;
- estado actual de datos, RBAC, tenancy, CI e infraestructura autorizada;
- Plan Comercial Inicial vigente (Drive ID 1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j);
- dependencias técnicas realmente existentes.

No se reintroducen automáticamente tareas, prioridades, bloqueos, secuencias ni criterios provenientes de planes no vigentes.

El Plan Comercial Inicial puede reemplazar, refactorizar o retirar implementaciones existentes dentro del alcance aprobado cuando sea necesario para alcanzar la versión inicial, siempre preservando seguridad, integridad de datos, trazabilidad y controles de autorización.

## 4. Ownership y lease

Todo write-scope material requiere lease lógico antes de escribir.

Campos canónicos:

```text
LEASE_SCOPE
LEASE_OWNER_AUTOMATION_ID
LEASE_ACQUIRED_AT
LEASE_HEARTBEAT_AT
LEASE_BASE_HEAD
LEASE_STATUS
LEASE_TOKEN
LEASE_TTL_MINUTES=10
```

Contrato:

1. Un solo writer autoritativo por scope.
2. Antes de adquirir lease, releer `dev` HEAD y estado operativo fresco.
3. La frescura exige invocación viva y progreso material verificable.
4. Si la invocación propietaria terminó, liberar el lease inmediatamente.
5. >=10 minutos sin progreso material permiten takeover seguro tras reread/readback.
6. Finalizado el scope, liberar o cerrar el lease con evidencia exacta.
7. Está prohibido dejar leases fantasmas.

## 5. ACTIVE_REAL y LISTO

`ACTIVE_REAL` exige ejecución identificable + lease exclusivo fresco + actividad técnica útil/material reciente.

`LISTO` exige:

- REVIEW_FIRST;
- DoD material completo;
- tests/gates/CI aplicables y causales;
- P0=0 y P1=0;
- exact-head o equivalencia demostrada;
- evidencia verificable;
- write + immediate readback del estado final.

Nunca fingir actividad, PASS, CI, evidencia o `LISTO`.

## 6. Recovery

Regla: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

- La tarea que detecta un defecto interno accionable lo resuelve same-run cuando sea seguro.
- Si el owner terminó, está stale o no produce progreso material, la siguiente automatización realiza takeover seguro.
- Recovery no crea colas administrativas innecesarias.
- Tras recovery, desbloquear dependencias actuales y continuar cierre/promoción same-run.

## 7. Git, CI y seguridad

- Trabajo ordinario: `dev`.
- `main` y cualquier acción sobre PROD requieren autorización explícita y vigente del propietario.
- No force-push, reset destructivo ni reescritura de historia compartida.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.
- CI causal debe corresponder al functional head o a equivalencia demostrada.
- No degradar RBAC, tenancy, auditoría, protección de secretos, integridad transaccional ni seguridad para acelerar ejecución.
- Un cambio controlado del Plan Comercial Inicial puede cambiar producto y arquitectura dentro del alcance aprobado, pero debe hacerlo mediante cambios controlados, verificables y recuperables.

## 8. Estados visibles

Las superficies operativas deben usar exclusivamente:

`PENDIENTE`, `EN_PROGRESO`, `VALIDANDO`, `LISTO`, `BLOQUEADO`, `CANCELADO`.

Las vistas derivadas se calculan desde estado fuente vivo. No usar snapshots manuales como autoridad de ejecución.

El Google Sheet operativo canónico es `SOLQARYN - PLAN COMERCIAL INICIAL - AUTOMATIZACIONES` (ID `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`). Las pestañas visibles son vistas derivadas por fórmulas, incluida `PLAN_COMERCIAL` con 1123 microtareas; quedan protegidas contra edición operativa directa. Las escrituras de runtime pertenecen únicamente a las fuentes técnicas ocultas `_COMERCIAL_SOURCE`, `_RUNTIME_SOURCE`, `_AUTOMATION_SOURCE` y `_EVENTS_SOURCE`. Las pestañas `COLA`, `PLAN_COMERCIAL` y `BITACORA` son vistas protegidas; ninguna vista reutiliza filas históricas.

## 9. Gate de admisión comercial

Cualquier ejecutor debe comprobar **en el HEAD fusionado de dev** `EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`, 1123 IDs coincidentes y únicos en hoja fuente/runtime, cero duplicados, dependencias vigentes, y que las diez tareas reales (cinco primarias y cinco supervisoras) existen, están alineadas y tienen lectura independiente. Las supervisoras nunca pueden ser suplantadas por una tarea duplicada de la primaria. **Sin readback 10/10 y owner autorizado la admisión es NO_GO**, incluso si todas las fórmulas dicen LISTO.

Nunca reactivar por iniciativa de un agente. Un cambio en el Word canónico requiere revisión del diff y recarga atomizada de la cola con respaldo previo. Si falta cualquier prerrequisito, ninguna tarea toma lease, ejecuta código ni escribe estado.

## 10. Cierre

Una tarea sólo queda cerrada cuando el estado vivo demuestra que el objetivo vigente fue cumplido. La existencia de evidencia previa no sustituye validación actual.
