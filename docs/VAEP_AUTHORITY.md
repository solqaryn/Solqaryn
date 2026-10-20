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
GLOBAL_DISPATCH_POOL=TEN_WORKERS_ANY_ELIGIBLE_TASK
LANE_BINDING=ADVISORY_ONLY
PAIR_BINDING=ADVISORY_ONLY
REVIEW_INDEPENDENCE=REVIEWER_DISTINCT_FROM_AUTHOR
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
SKILL_FRESHNESS_POLICY=FAIL_CLOSED
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
- **Pool global de trabajo, autorizado el 2026-10-19:** las diez automatizaciones canónicas, tanto Primary como Supervisor, pueden reclamar, implementar, corregir, recuperar o revisar **cualquier microtarea elegible de cualquiera de las cinco lanes**. Ni el horario ni el dominio asociado a una pareja restringen el despachador. Despacho = cola global + dependencias actuales satisfechas + permiso + un solo lease exclusivo por scope. Una Primary prioriza construir/cerrar y una Supervisor prioriza revisión/recuperación, pero ambas pueden ejecutar trabajo material no reservado; **ninguna puede aprobar su propia entrega**. La revisión REVIEW_FIRST corresponde a una automatización distinta de la autora, no necesariamente a la pareja de horario.
- Si solo hay una microtarea elegible, no asignarla simultáneamente a diez escritores: los ejecutores libres revisan entregas independientes o preparan evidencia read-only útil, sin inventar progreso, saltar dependencias ni falsificar el estado de la cola.
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

## 7. No espera de fases históricas y avisos de intervención

### Autonomía y límites

```text
CURRENT_PLAN_ONLY=TRUE
HISTORICAL_PHASE_DISPATCH=DENY
HISTORICAL_PHASE_DEPENDENCY=DENY
UNRELATED_HISTORICAL_CI_WAIT=DENY
AUTONOMOUS_APPROVED_DEV_RECOVERY=TRUE
PURCHASES_AND_PAID_UPGRADES=DENY
OWNER_INTERVENTION_ALERT=REQUIRED
OWNER_BLOCKER_DEDUPLICATION=REQUIRED
BLOCKED_TASK_RECHECK_WITHOUT_NEW_EVIDENCE=DENY
PASSIVE_ALERT_WATCHER_COUNTS_AS_WORKER=FALSE
```

- Ejecutar exclusivamente las microtareas activas del **SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO**. Ninguna fase anterior (SQ, N, numeración de migraciones u otro plan), ni un workflow cuyo nombre recuerde un plan histórico, puede construir un bloqueo de secuencia, prioridad o fuente de trabajo.
- Las regresiones actuales de MySQL, RBAC, tenant isolation, integridad, backup/restore y seguridad son **comprobaciones técnicas causales** cuando el changeset lo exige, aunque el workflow conserve un título viejo. Mantener las comprobaciones exigidas por las reglas protegidas de GitHub; no saltar CI de seguridad ni marcarlo ficticiamente en verde. Pruebas ajenas a la microtarea no deben convertirse en un gate adicional de negocio.
- La autorización general del propietario abarca implementación, corrección y recuperación del producto en **DEV** dentro del plan comercial. No autoriza compras, tarifas de terceros, suscripciones, upgrades, cargos, pagos ni contrataciones. No invalida la necesidad de autorización específica para `main`, QA/PROD, credenciales/secretos o infraestructura productiva.

### Protocolo anti-bucle y notificación

1. Clasificar bloqueo por `EXTERNAL_PERMISSION`, `OWNER_DECISION`, `PAID_RESOURCE`, `OUT_OF_SCOPE`, `SECURITY_AUTHORIZATION` o `INTERNAL_DEFECT`. Para `INTERNAL_DEFECT`, ejecutar hasta **dos intentos causales distintos con nuevas evidencias** si es seguro; no repetir el mismo comando o solución sin cambio de hipótesis. Si persiste y no se dispone de alternativa autorizada, escalar como `OWNER_DECISION` con evidencia. El límite de intentos no permite saltar pruebas, autorizaciones o seguridad.
2. Si se requiere realmente al propietario, conservar estado `BLOQUEADO` sólo para la unidad correspondiente. Registrar en `_EVENTS_SOURCE` un evento `OWNER_ACTION_REQUIRED` con `TASK_ID`, `RUN_ID` como fingerprint estable, causa, ámbito, acción exacta que debe realizar el humano, enlace pertinente, impacto y condiciones para reanudar; nunca incluir contraseñas, tokens o datos sensibles.
3. Una alerta autónoma separada puede consultar `_EVENTS_SOURCE` y avisar **únicamente** por un fingerprint nuevo que no conste ya como `OWNER_NOTIFICATION_SENT`. El observador no altera código, runtime de ejecución, leases ni estados, y no cuenta entre las diez automatizaciones VAEP. Como única escritura autorizada puede anotar en `_EVENTS_SOURCE` un acuse idempotente `OWNER_NOTIFICATION_QUEUED` para reservar el fingerprint antes de solicitar la entrega de la notificación. No afirmar `OWNER_NOTIFICATION_SENT` sin confirmación real del canal. Los fingerprints con `QUEUED` o `SENT` no se alertan de nuevo salvo que aparezca evidencia material de bloqueo nuevo. Si no hay mecanismo de reserva idempotente o falla su readback, no emitir avisos reiterados.
4. No hacer polling de un permiso denegado ni reintentar una compra/recurso de pago. Cuando aparezca evidencia de resolución o nueva autorización, registrar `OWNER_ACTION_RESOLVED` y reevaluar sólo la tarea bloqueada. Si otra microtarea está lista y no comparte scope/lease/dependencias, continuar de forma segura sin esperar al propietario.
5. Al usuario hay que notificarle **qué debe hacer, por qué, qué microtarea se detuvo y cómo confirmar**; no enviar alertas vagas ni una cada hora por el mismo bloqueo. Las cinco supervisoras de Alex pendientes de sincronización forman parte del preflight ya conocido, no se reenviarán reiteradamente.
6. Ninguna de estas reglas implica autoactivación: hasta readback 10/10, Skill distribuida vigente y aprobación de admisión, las primarias y supervisoras continúan deshabilitadas y no ejecutan cambios.

## 8. Git, CI y seguridad

- Trabajo ordinario: `dev`.
- `main` y cualquier acción sobre PROD requieren autorización explícita y vigente del propietario.
- No force-push, reset destructivo ni reescritura de historia compartida.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.
- CI causal debe corresponder al functional head o a equivalencia demostrada.
- No degradar RBAC, tenancy, auditoría, protección de secretos, integridad transaccional ni seguridad para acelerar ejecución.
- Un cambio controlado del Plan Comercial Inicial puede cambiar producto y arquitectura dentro del alcance aprobado, pero debe hacerlo mediante cambios controlados, verificables y recuperables.

## 9. Estados visibles

Las superficies operativas deben usar exclusivamente:

`PENDIENTE`, `EN_PROGRESO`, `VALIDANDO`, `LISTO`, `BLOQUEADO`, `CANCELADO`.

Las vistas derivadas se calculan desde estado fuente vivo. No usar snapshots manuales como autoridad de ejecución.

El Google Sheet operativo canónico es `SOLQARYN - PLAN COMERCIAL INICIAL - AUTOMATIZACIONES` (ID `1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk`). Las pestañas visibles son vistas derivadas por fórmulas, incluida `PLAN_COMERCIAL` con 1123 microtareas; quedan protegidas contra edición operativa directa. Las escrituras de runtime pertenecen únicamente a las fuentes técnicas ocultas `_COMERCIAL_SOURCE`, `_RUNTIME_SOURCE`, `_AUTOMATION_SOURCE` y `_EVENTS_SOURCE`. Las pestañas `COLA`, `PLAN_COMERCIAL` y `BITACORA` son vistas protegidas; ninguna vista reutiliza filas históricas.

## 10. Gate de frescura y routing de Skills por microtarea

Todas las primarias y supervisoras **deben invocar primero** la Skill local de SOLQARYN; después leer el fichero canónico `.agents/skills/solqaryn-project-governance/SKILL.md` y `references/architecture.md` en HEAD vivo de `dev`. Contrastar con la versión instalada por ChatGPT y los paquetes del código actual. El check `node scripts/verify-solqaryn-skills.mjs --self-test` debe constar SUCCESS en CI exact-head o ejecutarse en checkout válido. Contradicción, origen no disponible o Skill no sincronizada => `SKILL_VERSION_MISMATCH/NO_GO`, sin reclamar lease ni escribir producto o runtime.

Solo si un task necesita ayuda de una referencia externa, leer `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md` y validar origen original+pin+ruta bajo la allowlist. Reglas condicionales por lane de cada microtarea real: Seguridad -> governance, SKREF-11 sólo para modelado de amenazas explícito; Comercio -> SKREF-10 si edita Angular, Impeccable/Taste si afecta UI, SKREF-11 para riesgo de nuevas fronteras de confianza, SKREF-12 si se mide una lentitud .NET; UX -> SKREF-10 Angular, Impeccable/Taste/Motion/Humanizer según componente; Rutas -> governance y pruebas, SKREF-11 para amenazas HMAC/RBAC, SKREF-10 para Angular Router; QA -> pruebas/runbooks, SKREF-12 para rendimiento .NET cuando aplique. Cada Primary/Supervisor clasifica tarea, selecciona 0..N fuentes, verifica origen/pin/ruta y registra uso antes de actuar. No consultar las doce automáticamente, instalar copias, lanzar scripts ajenos ni añadir microtareas o ejecutores. No ejecutar scripts de terceros ni cambiar pins sin aprobación.

`SKILL_FRESHNESS` es requisito distinto de `COMMERCIAL_SYNC_OK`: la hoja puede estar íntegra con una Skill obsoleta. No tratar esos checks como equivalentes. La instalación de una nueva versión en la librería ChatGPT requiere distribución/instalación específica: un PR de GitHub no actualiza esa copia automáticamente.

## 11. Supervisión por cuenta independiente: alineación obligatoria

Las cinco Supervisor existentes pertenecen a la cuenta operativa de **Alex Morales**, tienen IDs y horarios canónicos que no se reemplazan ni duplican. Su función preferente es revisión independiente, recuperación y construcción secundaria, **sin reserva de dominios, tareas ni parejas**. Pueden ejecutar cualquier microtarea global realmente elegible que no tenga otro propietario con lease fresco. Las parejas indicadas a continuación son **referencias de coordinación y horarios, nunca filtros de despacho ni autorizaciones para auto-revisar trabajo**. En cada ejecución aplicar Skill vigente, VAEP, el Plan Comercial Inicial y evidencia de CI/seguridad según la microtarea; conservar REVIEW_FIRST por una automatización distinta de la autora. Sin compra ni cambios QA/main/PROD. Coordinación de referencia:

| Horario | Supervisor real | Primary asignada | Dominio |
| --- | --- | --- | --- |
| :05 | 6abd62255ae88191a2dba6e1b00d3b4d | :00 | 01_SEGURIDAD |
| :17 | 6abd6235e100819195785fc76a44a8e0 | :12 | 02_COMERCIO |
| :29 | 6abd6241c3e48191a8856ed7b0f42c5c | :24 | 03_UX |
| :41 | 6abd624e5e108191bef6bb879559328c | :36 | 04_RUTAS |
| :53 | 6abd625b397c8191a49904227946a20f | :48 | 05_QA_OPERACION |

Solo la cuenta propietaria de Alex puede actualizar y certificar directamente los prompts, estados reales (`is_enabled` tal como los reporte ChatGPT) y horarios de sus cinco Supervisor; las autorizaciones de activación ya otorgadas no exigen devolverlas a `false`. La reconciliación `_AUTOMATION_SOURCE` requiere evidencia auténtica de readback por cuenta, nunca inferencia desde una fórmula `=TRUE` o `CONFIG.ACTIVO`. No crear cinco tareas sustitutas en la cuenta de Javier ni suplantar su revisión independiente. Las cinco Primary de Javier pueden adoptar inmediatamente el despacho global desde su cuenta, sin alterar IDs/horarios. Si una cuenta no es accesible, declarar esa porción pendiente de readback, no fingir 10/10 trabajando.

## 12. Gate de admisión comercial

Cualquier ejecutor debe comprobar **en el HEAD fusionado de dev** `EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`, 1123 IDs coincidentes y únicos en hoja fuente/runtime, cero duplicados, dependencias vigentes, y que las diez tareas reales (cinco primarias y cinco supervisoras) existen, están alineadas y tienen lectura independiente. Las supervisoras nunca pueden ser suplantadas por una tarea duplicada de la primaria. **Sin readback 10/10 y owner autorizado la admisión es NO_GO**, incluso si todas las fórmulas dicen LISTO.

Nunca reactivar por iniciativa de un agente. Un cambio en el Word canónico requiere revisión del diff y recarga atomizada de la cola con respaldo previo. Si falta cualquier prerrequisito, ninguna tarea toma lease, ejecuta código ni escribe estado.

## 13. Cierre

Una tarea sólo queda cerrada cuando el estado vivo demuestra que el objetivo vigente fue cumplido. La existencia de evidencia previa no sustituye validación actual.
