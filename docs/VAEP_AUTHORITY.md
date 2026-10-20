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
- IDs canónicos Primary (Javier): `:00=6ad7bbf79c488191bf893e6c08ea9b5f`, `:12=6ad7bc0d48748191aa1dd0ce2f18d22a`, `:24=6ad7bc1cc7b8819190665e5ed78610d4`, `:36=6ad7bc3001b88191add05d9b4d5a4130`, `:48=6ad7bc3ebea08191a16dc7c9f1f3303b`.
- IDs canónicos Supervisor (Alex): `:05=6abd62255ae88191a2dba6e1b00d3b4d`, `:17=6abd6235e100819195785fc76a44a8e0`, `:29=6abd6241c3e48191a8856ed7b0f42c5c`, `:41=6abd624e5e108191bef6bb879559328c`, `:53=6abd625b397c8191a49904227946a20f`.
- Migración autorizada por propietario el 2026-10-20: las cinco Primary antiguas fueron eliminadas de ChatGPT Tasks. Los IDs originales son históricos y sus recibos/leases previos no se reescriben; las nuevas Primary se programaron desde ChatGPT Chat en los mismos cinco slots. Las Supervisor de Alex mantienen sus cinco IDs. La ejecución de producto exige sincronización 10/10 real, CI exact-head y readback de candados; hasta entonces solo preparación READ-ONLY segura.
- Un slot no termina en reporte o espera si existe acción material segura.
- **Pool global de trabajo, autorizado el 2026-10-19:** las diez automatizaciones canónicas, tanto Primary como Supervisor, pueden reclamar, implementar, corregir, recuperar o revisar **cualquier microtarea elegible de cualquiera de las cinco lanes**. Ni el horario ni el dominio asociado a una pareja restringen el despachador. Despacho = cola global + dependencias actuales satisfechas + permiso + un solo lease exclusivo por scope. Una Primary prioriza construir/cerrar y una Supervisor prioriza revisión/recuperación, pero ambas pueden ejecutar trabajo material no reservado; **ninguna puede aprobar su propia entrega**. La revisión REVIEW_FIRST corresponde a una automatización distinta de la autora, no necesariamente a la pareja de horario.
- Si solo hay una microtarea elegible, no asignarla simultáneamente a diez escritores: los ejecutores libres revisan entregas independientes o preparan evidencia read-only útil, sin inventar progreso, saltar dependencias ni falsificar el estado de la cola.
- Un checkpoint es un disparador, no una frontera de ownership.
- Tras `LISTO`, promover el siguiente trabajo dependency-valid del plan comercial inicial vigente.
- `LISTO` es el único estado de cierre certificado.

### Continuidad material universal: cinco Primary y cinco Supervisor con idéntica capacidad

**Decisión de propietario, vigente:** las diez automatizaciones existentes son diez ejecutores **equivalentes** para implementar, corregir, revisar entregas de **otro autor**, recuperar de forma segura o certificar cualquier microtarea libre y elegible del Plan Comercial Inicial vigente. Las palabras Primary y Supervisor solo identifican la **responsabilidad adicional de comprobación**: no son permisos distintos ni limitan el área/alcance. Se preservan los horarios y autorizaciones duales; los cinco IDs Primary se sustituyeron por autorización del propietario el 2026-10-20, sin alterar los cinco IDs Supervisor.

**Regla de recuperación equivalente al comportamiento útil del respaldo LEGACY (sin reactivar su plan):**

- `:05` comprueba `:00`; `:17` comprueba `:12`; `:29` comprueba `:24`; `:41` comprueba `:36`; `:53` comprueba `:48`. Este emparejamiento es para **detectar fallos de continuidad**, no para reservar trabajos por área o asignar una sola pareja.
- Si la Primary no registró ejecución/material con evidencia causal, la Supervisor **no se detiene en un informe**: inicia trabajo real sobre la tarea elegible de menor orden libre, siempre sujeto a admisión y herramientas autorizadas. La ausencia de lease no demuestra por sí sola que no se disparó; contrastar `last_run_time` real cuando sea accesible.
- Si otra automatización trabaja con lease propio **fresco**, no competir ni esperar innecesariamente: escoger otra microtarea independiente elegible de menor `ORDEN`. Si la tarea anterior está huérfana o vencida, solicitar/realizar **solo una recuperación transaccional realmente cercada y verificada**; jamás sobrescribir su propietario mediante una celda simple.
- Si el propio ejecutor posee una tarea vigente, **reanudar la ejecución material antes de reclamar otra**. Si posee dos o más scopes, activar reconciliación WIP=1 de ese actor, **sin detener el programa ni a los otros nueve**. Después de entregar/cerrar/liberar, volver al DAG global durante el mismo run.
- Una microtarea `VALIDANDO` puede ser tomada por cualquier ejecutor distinto del autor original con evidencia, candado y revisión comprobados; no autoaprobación. No declarar `LISTO` sin DoD, CI/revisión y readback.
- Un impedimento local de permisos, CI o lease no autoriza `is_enabled=false` ni `CONFIG.RUNTIME_MODE=PAUSADO`; registrar causa una sola vez, continuar la siguiente tarea segura y no falsificar trabajo.

**Selector probado (solo lectura):** `scripts/vaep/continuity_dispatch.py` aplica estas reglas con los diez IDs canónicos, verifica pares solo a efectos de supervisión, distingue build/review/continuación/reconciliación y nunca hace un takeover automático. La CI ejecuta `test_continuity_dispatch.py`. Este selector NO es un escritor transaccional, NO puede disparar otras Tasks ni reemplaza su acceso GitHub/Google Drive; no presentar su presencia como prueba de ocupación 24/7 o de cierres horarios.

### Ciclo de vida y despacho con verificación de responsables (corrección del propietario 2026-10-20)

**La progresión de una microtarea es `PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO`**, no una jerarquía de despacho invertida. `LISTO` requiere trabajo realmente terminado, evidencia, CI/revisión independiente y readback; la progresión no concede permiso para adelantarla. Se preservan `BLOQUEADO` y `CANCELADO` como estados excepcionales permitidos. No borrar ni reescribir recibos históricos por conveniencia.

**Preflight de cada disparo, para cada Primary y Supervisor (diez ejecutores universales):**

1. Leer el snapshot actualizado y completo de las 1.123 tareas y comprobar autorización, Skill, HEAD y permisos. Inspeccionar **todas las `VALIDANDO`** por menor `ORDEN`: autor original, revisor/propietario presente, metadatos del candado, evidencia y dependencias. La existencia de `OWNER` o de un lease fresco **no demuestra** ejecución en curso: consultar el estado real de la invocación en la cuenta correspondiente cuando se disponga de acceso. Si `VALIDANDO` está libre y entregada con evidencia, una automatización diferente del autor puede realizar `REVIEW_FIRST` y, sólo si aprueba, cerrar `LISTO`. Si está reservada por revisor activo, dejarla intacta. Si su invocación terminó, exigir prueba externa, fencing y readback para recuperación.
2. Inspeccionar **todas las `EN_PROGRESO`** por menor `ORDEN`: propietario real, lease, última actividad material y estado de invocación comprobado. Un owner con lease no confirma actividad; vencimiento de TTL tampoco prueba abandono. El ejecutor que ya posee un único WIP debe terminarlo o reconciliarlo de modo seguro antes de adquirir otro; un actor con WIP duplicado no reclama un tercero.
3. Resolver los trabajos previamente iniciados **únicamente cuando sean admisibles para este ejecutor**: revisión independiente libre, continuación propia válida o recuperación con prueba terminal del run anterior, DeveloperMetadata exacta, transacción cercada, token y readback. No invadir leases ajenos ni devolver automáticamente `VALIDANDO`/`EN_PROGRESO` a `PENDIENTE`. Si la verificación externa no es posible, anotar la causa deduplicada y continuar inmediatamente con una alternativa segura.
4. **Si ninguna tarea activa es solucionable de forma segura en este disparo, elegir el primer `PENDIENTE`** verdaderamente libre siguiendo `_COMERCIAL_SOURCE.ORDEN` global ascendente, jerarquía del Plan Comercial Inicial y dependencias actuales `LISTO`. Si el primero tiene dependencia no satisfecha o candado ajeno, saltar a la siguiente elegible justificando el descarte. No esperar indefinidamente por un owner ajeno cuando hay otro trabajo independiente listo.
5. Tras realizar trabajo material: `PENDIENTE → EN_PROGRESO` mediante CAS con readback, `EN_PROGRESO → VALIDANDO` al entregar y liberar con evidencia, `VALIDANDO → LISTO` solo mediante otra automatización revisora, pruebas y readback; liberar candado de revisión y seleccionar la siguiente tarea durante el mismo ciclo. Ningún disparo/claim/fase `VALIDANDO` equivale por sí solo a cierre.

**Selector verificable:** `scripts/vaep/continuity_dispatch.py` expone `inspected_inflight.VALIDANDO` y `inspected_inflight.EN_PROGRESO` con IDs, propietarios y diagnóstico de leases sin confundir TTL con ejecución viva. Si existe REVIEW independiente libre, devuelve REVIEW; si un propietario tiene WIP, requiere continuación/reconciliación antes de cualquier otro scope. Si hay leases ajenos caducados pero ninguna prueba externa de run terminado, devuelve **directamente BUILD** sobre el `PENDIENTE` jerárquico libre, reportando el candidato de recuperación para inspección separada. Si no hay `PENDIENTE` libre, solicita comprobación terminal y fencing sin tocar candados. El selector es **sólo lectura**, no un monitor residente; no puede determinar el estado externo de un Task por sí mismo. `scripts/vaep/commercial_dispatch.py` valida orden, dependencias y revisión independiente; `cas_lease_writer.py` implementa writer atómico con doble exclusividad, readback, `REVIEW_FIRST` y fencing. El escritor exige revisión admisible antes de reclamar un nuevo pendiente, pero la falta de pruebas/permisos para cualquier alcance no puede detener la selección de otro elegible.

**Capacidades universales y seguridad:** cualquiera de los diez ejecutores puede construir, revisar trabajo AJENO o recuperar un trabajo elegible, sin reservar áreas. Jamás autoaprobarse ni inventar permisos, evidencia, comprobación terminal, actividad viva, `ACTIVE_REAL`, `LISTO` o certificación 10/10. No tocar QA/main/PROD, ni crear nuevas automatizaciones Codex/Work, ni permitir a un fallo local pausar el resto de Tasks. `MAX_VOLUNTARY_IDLE=0` aplica sólo donde exista trabajo seguro con autorización real; el preflight 10/10 para escrituras no se rebaja por este cambio.

### Bloqueo por microtarea no desactiva automatizaciones

La autorización dual ya otorgada **permanece vigente**. Una ejecución que no pueda reclamar una microtarea (`LEASE_CAS_WRITER_MISSING`, dependencia, permisos, revisión, CI o recurso externo) **NO puede** deshabilitarse a sí misma, deshabilitar otros nueve ejecutores, cambiar sus horarios, modificar `_AUTOMATION_SOURCE.ENABLED/ESTADO`, `CONFIG.AUTOMATIONS_ENABLED`, `CONFIG.RUNTIME_MODE` ni rebajar el gate global por un fallo de alcance local. Fail-closed significa bloquear **sólo el write-scope que no cuenta con admisión**, registrar `OWNER_ACTION_REQUIRED` deduplicado y seguir con otra microtarea elegible o revisión independiente segura; no equivale a desautorizar o parar el programa horario. Sólo una revocación expresa del propietario o una falla global genuinamente acreditada justifica intervención sobre flags de admisión. La comparación directa de ChatGPT `is_enabled` de una cuenta nunca se sustituye por `=TRUE` en Sheets.

Incidente causal 2026-10-20: un run `:36` disparó, evaluó `P0-002.B`, registró `OWNER_ACTION_REQUIRED` con `LEASE_CAS_WRITER_MISSING` y dejó su fila operativa en `PAUSADA_BLOQUEO_CAS`, bajando el gate a 9/10; `:48` repitió la pausa. La recuperación administrativa autorizada restauró las dos filas después de reactivar ambas Tasks y comprobó 10/10 en Sheets. **No se permite repetir este antipatrón.** Despachar trabajo seguro mientras exista cola disponible es obligación, pero nunca inventar un lease ni declararlo `ACTIVE_REAL` si no fue adquirido.

### Ejecución verificable de reclamación CAS (implementación concreta)

`scripts/vaep/cas_lease_writer.py` implementa la construcción del batch atómico de claim `createDeveloperMetadata + updateCells + appendCells`, el cálculo del ID fijo por orden y la comprobación estricta de `metadataId/metadataValue`, propietario, token y estado mediante respuesta de API y readback. Proporciona asimismo liberación restringida al **token del propio claim**; `LISTO` jamás se marca desde la ruta de liberación sin revisión independiente. Sus pruebas en `scripts/vaep/test_cas_lease_writer.py` verifican 17 escenarios, diez reservas distintas con 32 elegibles, fallos de colisión, salto de orden y rechazos de auto-certificación.

El módulo es un **constructor/validador seguro de solicitudes**, no un scheduler, credencial, servicio autónomo ni sustituto de la acción Google Sheets del ejecutor: cada Primary/Supervisor debe poseer la conexión y realmente enviar el batch a `spreadsheets.batchUpdate`, inspeccionar los `replies` y hacer readback. **La primera reserva sola NO constituye `ACTIVE_REAL`**; el evento inicial debe llevar `LEASE_RESERVED_MATERIAL_PENDING` hasta que exista trabajo material demostrable. No declarar producción 10/10 a partir de pruebas locales o CI. Si la plataforma deniega la escritura GitHub o Google Drive, no desactivar Task, no escribir el scope sin claim y continuar otra tarea segura, sin tocar flags del equipo.

### Cierre REVIEW_FIRST por una automatización independiente (CAS completo)

Para la fase `VALIDANDO` se usa **el mismo candado de DeveloperMetadata por ORDEN**; no sirve el `build_claim` reservado a `PENDIENTE`. El escritor `scripts/vaep/cas_lease_writer.py` ofrece `build_review_claim`, `verify_review_claim_response`, `build_review_completion` y `verify_review_completion_response`. Esta ruta exige evidencia real del autor anterior, `VALIDANDO` libre de owner/lease, dependencias `LISTO`, revisor distinto del autor, y el menor ORDEN global disponible para esa revisión. La transacción `createDeveloperMetadata + updateCells + evento REVIEW_LEASE_ACQUIRED` debe completarse y acreditar readback; por sí sola **NO equivale** a `LISTO`. Una REVIEW independiente libre se atiende durante la inspección de trabajos iniciados; si no es realmente resoluble por el actor, se informa el bloqueo y se selecciona el primer PENDIENTE seguro, sin confundir ciclo de vida con jerarquía de estados.

El revisor compara la evidencia original, el alcance y el HEAD actual, verifica CI causal y negativos, P0/P1=0 y registra el enlace exacto de prueba. Sólo entonces `build_review_completion(... final_state="LISTO")` produce una transacción atómica `deleteDeveloperMetadata` con token exacto + `LISTO` + fecha + evidencias de revisión + evento `REVIEW_FIRST_ACCEPTED_LISTO`; el readback confirma también la eliminación del único candado propio. Si falta una prueba o se rechaza la revisión, se devuelve `VALIDANDO`, sin simular cierre. El autor original se preserva como `ORIGINAL_AUTHOR` en el handoff para prohibir autoaprobación incluso en revisiones posteriores.

El ayudante Python únicamente construye y valida solicitudes. **No tiene credenciales, no dispara runs de ChatGPT y no asegura veracidad de enlaces externos**: cada ejecutor debe contrastar CI y pruebas en sus orígenes reales, y llamar al writer conectado. El protocolo no autoriza recuperar/destruir automáticamente el lease caducado de otra cuenta. Resolver tales candados con comprobación causal del fin de invocación, fencing y readback seguros, sin cambiar `CONFIG`, deshabilitar ninguna automatización ni atribuirse trabajo ajeno.

### Exclusión atómica de reclamaciones mediante Google Sheets DeveloperMetadata

La primitiva de exclusión que se comprobó experimentalmente el 2026-10-20 en el Sheet operativo es `createDeveloperMetadata` con un `metadataId` explícito: Google Sheets acepta el primer ID y **rechaza con HTTP 400** un segundo intento de usar el mismo ID. El objeto de prueba efímero fue eliminado sin tocar filas de negocio. Esto prueba la unicidad de la **adquisición**, no certifica automáticamente un ciclo completo de lease/recovery.

Para cada microtarea elegible:

1. Verificar HEAD y 1123 IDs, ORDEN numérico 1..1123, estado `PENDIENTE`, dependencias `LISTO`, fila runtime correspondiente, falta de dueño/lease y que el ejecutor está autorizado. Seleccionar el menor ORDEN libre (sin lane).
2. Calcular `metadataId = 1600000000 + ORDEN`. Reclamar con **un solo** `spreadsheets.batchUpdate` que agrupe, en ese orden, `createDeveloperMetadata` (`metadataKey=SOLQARYN_TASK_LEASE`, `visibility=DOCUMENT`, `metadataValue=TASK_ID|RUN_ID|OWNER|TOKEN|START|EXPIRES`, ubicación en la fila _RUNTIME_SOURCE), el `updateCells` exclusivamente de ESTADO/OWNER/LEASE_STATUS/HEAD/UPDATED_AT y el `appendCells` del evento `LEASE_ACQUIRED`. Google Sheets aplica el batch atómicamente: si colisiona el `metadataId`, no existe reclamación ni escritura parcial.
3. Sólo tras confirmar el `createDeveloperMetadata` y readback de runtime/propietario/token puede modificarse producto. Si ya existe metadata del task, no borrarla ni alterarla; volver a despachar la siguiente elegible sin ocio.
4. El owner debe guardar evidencia real y liberar su propio lease cuando termina o deja `VALIDANDO` para revisión independiente; la liberación requiere verificar `metadataId`, `metadataKey`, `metadataValue` y token del mismo owner, y confirmar el resultado de `deleteDeveloperMetadata`. Un lock de otro actor **nunca** puede borrarse por comodidad.
5. **Límite de WIP = 1 por automatización y doble exclusividad:** cada ejecutor puede tener a lo sumo una microtarea activa simultáneamente (`EN_PROGRESO` o `VALIDANDO` no liberada). El candado de ejecutor lleva `location={"spreadsheet": true}` según la API Google Sheets; omitirla causaba `CAS_EXECUTOR_LOCATION_REQUIRED` y bloqueaba el claim/review/takeover. Se impone mediante verificación en `commercial_dispatch.py` y bloqueo de DeveloperMetadata a nivel de ejecutor con `EXECUTOR_METADATA_KEY` (`metadataId = 1700000000 + hash(owner)`). Una automatización con tarea activa rechaza nuevos claims hasta liberar o entregar la anterior.
6. **Recuperación cercada de leases caducados (Fenced Stale Lease Recovery):** implementada en `scripts/vaep/cas_lease_writer.py` (`build_stale_lease_recovery` y `build_takeover_claim`). Exige:
   - Verificación de TTL vencido (`now > expires_at`) y estado recuperable.
   - Generación de `FENCING_TOKEN` único para invalidar y cercar de forma irreversible cualquier intento de escritura tardía del ejecutor anterior (las funciones `build_release` y `build_review_completion` del propietario anterior fallarán por falta de coincidencia en `LEASE_STATUS` y token).
   - Eliminación atómica en Google Sheets del `DeveloperMetadata` de la microtarea y del candado de ejecutor del propietario anterior.
   - Transición segura a `target_state` (`PENDIENTE` o `VALIDANDO`), vaciado de `OWNER_AUTOMATION_ID` y registro de auditoría con propietario previo en `NOTA_RUNTIME` y evento `LEASE_STALE_RECOVERED` en `_EVENTS_SOURCE`.
   - Readback estricto de hoja antes de permitir que el despachador reasigne la microtarea.
7. La primitiva nativa ofrece exclusión para actores **que usen este protocolo**; no protege contra escritores externos que lo evadan. Antes de certificar operación concurrente 10/10, verificar en vivo el ciclo completo claim -> actividad material -> VALIDANDO/review -> release, incluidos tokens incorrectos, colisiones y caídas, con guard contra escrituras tardías del antiguo owner.

Los ejemplos de la tarea programada no son una autoridad nueva ni conceden permiso para crear nuevos cron/jobs, usar servicios pagos o modificar QA/main/PROD. Los resultados deben registrarse en las pestañas fuente autorizadas y pasar por `REVIEW_FIRST`; nunca actualizar manualmente `LISTO` sin evidencia.

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

### Evaluación universal del catálogo de Skills antes de cada microtarea (10/10 ejecutores)

**Regla obligatoria para las cinco Primary y las cinco Supervisor, sin filtros por lane:** al seleccionar cualquier microtarea global elegible, y **antes de adquirir lease o modificar producto**, el ejecutor invoca `solqaryn-project-governance`, verifica su frescura contra HEAD vivo de `dev`, consulta el inventario vigente de `docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md` y `references/skill-routing.md` y evalúa la **aplicabilidad de las doce referencias SKREF-01..SKREF-12** frente a los requisitos y riesgos concretos de esa microtarea. La evaluación del catálogo es 12/12; la lectura profunda, instalación y ejecución de las doce **NO** es obligatoria ni está autorizada por defecto.

La matriz de selección se calcula para **cada tarea**, no para el número del minuto, la Primary/Supervisor, la pareja ni el lane:

| Referencia evaluada | Pregunta concreta de aplicabilidad |
| --- | --- |
| SKREF-01 Agent Skills Specification | ¿Se crea, estructura o valida una Skill local? |
| SKREF-02 Skill Creator integrado | ¿Se crea, edita, valida o empaqueta la Skill local con la herramienta oficial? |
| SKREF-03 Impeccable | ¿Hay interfaces administrativas, dashboard, formularios, tablas, ajustes, onboarding o mejora visual? |
| SKREF-04 Motion Skills | ¿La tarea requiere movimiento, animaciones o microinteracciones? |
| SKREF-05 Taste Skill | ¿Hay home, landing, tienda pública, CMS visual o marketing? |
| SKREF-06 Humanizer | ¿La tarea requiere texto UX, documentación, PR o redacción de informes? |
| SKREF-07 Napkin | ¿Se documentan aprendizajes repetibles o runbooks sin crear autoridad paralela? |
| SKREF-08 Token Optimizer | ¿Existe contexto largo/duplicado que requiera reducción sin perder evidencia crítica? |
| SKREF-09 Caveman | ¿Se necesita síntesis efímera de bajo riesgo, nunca de seguridad ni prueba persistente? |
| SKREF-10 Angular Developer | ¿Modifica Angular 22.2.1, Signals, rutas, componentes, formularios, pruebas o accesibilidad? |
| SKREF-11 Security Threat Model | ¿Requiere modelado explícito de amenazas/límites de confianza JWT, RBAC, HMAC o multiempresa? |
| SKREF-12 .NET Performance | ¿Existe síntoma/medición que justifique diagnóstico de rendimiento .NET 10, LINQ, async, memoria o I/O? |

Registrar, en la evidencia normal de la **misma microtarea** y con `TASK_ID`/`RUN_ID`, una matriz compacta `SKILL_EVAL=12/12` con los doce resultados `APLICA`/`NO_APLICA` y justificación cuando aplique, más `SKILL_SELECTED` (puede ser vacío), origen, SHA pin, ruta oficial y motivo de cada referencia utilizada. **Evaluar todas no significa usar todas**. Abrir y aplicar sólo las necesarias y autorizadas desde su origen original y pin exacto, después de comprobar allowlist, integridad y compatibilidad. Si falta una fuente imprescindible, registrar un bloqueo causal para esa microtarea, sin suplantarla, inventar evidencias ni congelar tareas independientes. Si ninguna referencia externa corresponde, proceder sólo con la Skill local; no fabricar un requisito artificial.

Quien haga `REVIEW_FIRST` debe evaluar también las doce referencias **según la revisión concreta**, y ser un ejecutor distinto de quien implementó. Ninguna referencia externa reemplaza el contrato VAEP, el único plan, la revisión independiente, permisos, lease, CI exact-head o las restricciones de DEV. Este protocolo no agrega automatizaciones, microtareas, dependencias de negocio ni instalaciones de herramientas.

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

Solo la cuenta propietaria de Alex puede actualizar y certificar directamente los prompts, estados reales (`is_enabled` tal como los reporte ChatGPT) y horarios de sus cinco Supervisor; las autorizaciones de activación ya otorgadas no exigen devolverlas a `false`. La reconciliación `_AUTOMATION_SOURCE` requiere evidencia auténtica de readback por cuenta, nunca inferencia desde una fórmula `=TRUE` o `CONFIG.ACTIVO`. Excepción autorizada y ya realizada el 2026-10-20: las cinco Primary de Javier se sustituyeron mediante ChatGPT Chat y las cinco anteriores fueron eliminadas. Conservar slots y revisar directamente los cinco IDs nuevos; no crear más tareas. Hasta readback auténtico y reconciliación recíproca de cinco parejas en GitHub y Sheets, queda prohibido tomar leases, escribir producto o declarar LISTO. Si una cuenta no es accesible, declarar esa porción pendiente de readback, no fingir 10/10 trabajando.

## 12. Gate de admisión comercial

Cualquier ejecutor debe comprobar **en el HEAD fusionado de dev** `EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`, 1123 IDs coincidentes y únicos en hoja fuente/runtime, cero duplicados, dependencias vigentes, y que las diez tareas reales (cinco primarias y cinco supervisoras) existen, están alineadas y tienen lectura independiente. Las supervisoras nunca pueden ser suplantadas por una tarea duplicada de la primaria. **Sin readback 10/10 y owner autorizado la admisión es NO_GO**, incluso si todas las fórmulas dicen LISTO.

Nunca reactivar por iniciativa de un agente. Un cambio en el Word canónico requiere revisión del diff y recarga atomizada de la cola con respaldo previo. Si falta cualquier prerrequisito, ninguna tarea toma lease, ejecuta código ni escribe estado.

## 13. Cierre

Una tarea sólo queda cerrada cuando el estado vivo demuestra que el objetivo vigente fue cumplido. La existencia de evidencia previa no sustituye validación actual.
