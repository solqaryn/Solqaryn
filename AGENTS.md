# Reglas obligatorias de colaboración — SOLQARYN

Este archivo es vinculante para Javier Mejía, ChatGPT/VAEP y cualquier agente autorizado.

## Gate obligatorio

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
AUTOMATION_AUTHORITY=VAEP_CONTRACT
VAEP_CONTRACT_FILE=docs/VAEP_AUTHORITY.md
EXECUTION_PLAN=SOLQARYN_PLAN_COMERCIAL_INICIAL_UNIFICADO
EXECUTION_PLAN_DRIVE_FILE_ID=1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j
FUTURE_PLAN_EXECUTABLE=FALSE
EXECUTION_MODEL=TASKS_ONLY
SKILL_FRESHNESS_POLICY=FAIL_CLOSED
CONTEXT_MODE=CURRENT_STATE_ONLY
```

Antes de analizar, editar, ejecutar o publicar trabajo VAEP, leer `docs/VAEP_AUTHORITY.md`, `docs/PROJECT_SCOPE_LOCK.md` y el HEAD vivo de `dev`.

## Gate de Skill vigente y uso por automatización

Cada VAEP Primary/Supervisor aplica primero `.agents/skills/solqaryn-project-governance/SKILL.md` en el HEAD actual de `dev`. No basta que ChatGPT cargue una versión instalada: comparar con la canónica y confirmar `node scripts/verify-solqaryn-skills.mjs --self-test` o el check exact-head en CI. Discrepancia de versión o de fuente externa => `SKILL_VERSION_MISMATCH` y `NO_GO`. Las doce referencias externas sólo se consultan cuando corresponda exactamente a la microtarea, desde el origen original + SHA pin + ruta oficial del registro. Cada Primary y Supervisor, antes de cada tarea, clasifica componente y riesgo: SKREF-10 para Angular 22, SKREF-11 únicamente para modelado explícito de amenazas/trust boundaries A/B/JWT/RBAC/HMAC, SKREF-12 para rendimiento .NET 10 ante evidencia de problemas de performance. Registrar origen, pin, ruta y por qué fue necesaria; no consultar indiscriminadamente, descargar/copiar skills externas o ejecutar instaladores/scripts ajenos. Playwright CLI permanece fuera de la allowlist: usar Playwright existente. No copiar ni instalar referencias como skills locales; no activar las automatizaciones hasta verificación independiente 10/10.

### Ciclo independiente de revisión con candado compartido

El despacho no está terminado hasta que la automatización revisora distinta del autor pueda adquirir el candado CAS sobre `VALIDANDO`, verificar evidencias, cerrar `LISTO` y liberar el mismo metadataId de la tarea con readback exacto. Emplear `build_review_claim / build_review_completion` de `scripts/vaep/cas_lease_writer.py`. Los métodos de claim de implementación no reemplazan los de REVIEW_FIRST; un lease sin evidencia no cierra nada. En error local bloquear sólo el scope y no auto-desactivar las diez automatizaciones.

### Las diez Task tienen la misma capacidad de ejecución; supervisoras hacen failover material

Aplicar primero `scripts/vaep/continuity_dispatch.py` (selector read-only, sin roles exclusivos) o su contrato determinista a cada invocación. Primary :00/:12/:24/:36/:48 y Supervisor :05/:17/:29/:41/:53 pueden implementar, corregir, recuperar y revisar trabajo de otro autor de cualquier lane, sin diferencias de facultad. Las parejas :05→:00, :17→:12, :29→:24, :41→:36 y :53→:48 son únicamente liveness/continuidad: si el precedente no produce evidencia, la supervisora **ejecuta** sobre la menor ORDEN libre, no termina en informe. Si el precedente conserva lease fresco, trabaja otra libre sin interferir. Sin pruebas de fin o fencing no usurpa su lease vencido. WIP=1 por ejecutor: con ownership vigente reanudar, con owner duplicado reconciliar primero; jamás adjudicar un segundo scope al mismo actor. Una entrega VALIDANDO solo la revisa actor distinto del autor y con evidencia. Fallo local no pausa la Task ni las otras nueve. Este selector no dispara cron, no reemplaza escritor CAS y no acredita un cierre por sí mismo.

### Despacho global y selección universal de referencias por tarea

Las cinco Primary y las cinco Supervisor pueden implementar o recuperar **cualquier microtarea elegible de cualquier área**, sin exclusividad de dominio, horario o pareja; una revisión siempre corresponde a **otro** ejecutor. En **cada toma de tarea**, evaluar explícitamente las **12/12** referencias del inventario registrado (SKREF-01..SKREF-12), clasificar `APLICA`/`NO_APLICA`, y seleccionar/consultar únicamente las pertinentes por componente, riesgo, requisito y pin original. Conservar evidencia `SKILL_EVAL=12/12`, `SKILL_SELECTED`, justificación y orígenes/pins usados, unida al mismo `TASK_ID`/`RUN_ID`; sin tareas extra. Si ninguna aplica, usar solo la Skill local. El gate de frescura de la Skill, permisos, leasing exclusivo, revisión por actor independiente, dependencias, CI y prohibición de acciones productivas siguen vigentes. El catálogo no es filtro por lane ni autorización para instalar todas las referencias. Detalle vinculante: sección 10 de `docs/VAEP_AUTHORITY.md`.

### Antiapagado ante NO_GO local y CAS de microtareas

Ningún ejecutor puede auto-desactivar su Task, desactivar otra de las diez, escribir `_AUTOMATION_SOURCE.ENABLED/ESTADO` ni rebajar `CONFIG.RUNTIME_MODE` por no poder reclamar **una** microtarea (incluido `LEASE_CAS_WRITER_MISSING`). Mantener el disparador habilitado; anotar una alerta causal deduplicada y continuar sobre otra tarea elegible, revisión ajena o preparación read-only segura. Únicamente el scope sin lease queda sin escritura. En 2026-10-20 una auto-pausa `:36` y otra `:48` bloquearon el gate global: este modo de falla está prohibido.

El procedimiento nativo de lease documentado en `docs/VAEP_AUTHORITY.md` emplea en **un mismo batch atómico** un `createDeveloperMetadata` con ID fijo `1600000000+ORDEN`, escritura de owner/lease y evento; Google Sheets rechaza IDs duplicados. El readback de ownership es obligatorio. No se permite reclamar si hay lock ajeno, ni borrar candados stale sin pruebas de fencing. El test aislado de unicidad aprobó, **pero la recuperación completa y el rendimiento 10/10 todavía exigen evidencia de ciclos reales**. Esto no crea automatizaciones adicionales ni permite falsificar LISTO. Si otra tarea anterior está VALIDANDO o tiene dependencia, se puede trabajar una posterior verdaderamente libre sin violar el orden de despacho.

### Writer CAS reproducible y sin apagado global

`scripts/vaep/cas_lease_writer.py` genera/valida el batch atómico nativo de Google Sheets: DeveloperMetadata de ID fijo por ORDEN, actualización del runtime y evento en la misma transacción. El evento de claim registra `LEASE_RESERVED_MATERIAL_PENDING`, NO `ACTIVE_REAL`; sólo la evidencia material y revisión independiente permiten progresar al cierre. Diecisiete pruebas forman parte de CI VAEP. Los ejecutores necesitan realmente invocar la API y demostrar readback; no basta leer el código ni la instrucción. Ningún error local de escritor CAS o rechazo de escritura GitHub autoriza a auto-deshabilitar automatizaciones, cambiar la fuente ENABLED o pausar CONFIG.

### Selección antisalto y no ocio con trabajo elegible

**Todas las diez automatizaciones** deben consultar la cola global completa en cada ejecución. Elegir **la primera microtarea `PENDIENTE` con `DEPENDENCIAS_ACTUALES` satisfechas y sin dueño/lease activo**, ordenada por **`_COMERCIAL_SOURCE.ORDEN` ascendente**. Si la primera ya fue adquirida legítimamente por otro actor, releer la fuente y tomar la siguiente libre, de cualquier lane. No fijar `NEXT_TASK_ID` antiguo ni esperar una tarea ajena mientras exista otra elegible. Registrar motivo de exclusión de candidatas de menor orden; no saltar ninguna que esté verdaderamente libre. Revisión siempre independiente del implementador.

`scripts/vaep/commercial_dispatch.py` es el validador de lectura sin efectos secundarios, con casos negativos en CI. **No reclama leases**: el escritor runtime autorizado debe efectuar una reserva atómica real (no basta un `batchUpdate` de Sheets), validar su ownership y readback y recomputar ante colisión. Un trigger programado NO prueba `ACTIVE_REAL`; un prompt no constituye un proceso permanente 24/7. Los diez conservan sus slots horarios, Skill local vigente, evaluación SKREF-01..12 por tarea y controles de seguridad/DEV. Por sustitución autorizada el 2026-10-20, los cinco IDs Primary cambiaron a ChatGPT Chat; las cinco Supervisor conservan sus IDs. Antes de cualquier escritura comprobar actualización exacta de docs/VAEP_AUTHORITY.md y _AUTOMATION_SOURCE, parejas 10/10 y CI exact-head, sin alterar evidencias/leases históricos ni delegar a Codex/Work.

## 2026-10-19 — DECISIÓN DEL PROPIETARIO: VERSIÓN COMERCIAL INICIAL PRIMERO

La única fuente **ejecutable en esta etapa** es [SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO](https://drive.google.com/file/d/1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j/view), ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`. Su descomposición operativa de 492 fichas a 1123 microtareas está en el [control VAEP comercial](https://docs.google.com/spreadsheets/d/1gcVyCoyhLU0jFMwRtf0s5_x8FSnfBs38ojml1QF7Xwk/edit), pestaña `PLAN_COMERCIAL`. La visión arquitectónica futura permanece archivada como **no ejecutable**, sin borrar su historia. Esta decisión expresa del propietario reemplaza referencias antiguas de autoridad. Toda automatización debe comprobar que sus pares, su prompt, el contrato y la cola coinciden antes de activarse; mientras falte ese readback debe permanecer inactiva. No afecta controles de seguridad, CI, aislamiento A/B, backups ni prohibición de promover PROD sin autorización nueva.

## Autoridad única

- Durante la versión comercial inicial, sólo `SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO` (Drive ID `1T41ppLvC3x-p6sORCtpaPvzLWPP9_Z7j`) autoriza trabajo ejecutable. La visión futura NO alimenta la cola.
- `docs/VAEP_AUTHORITY.md` es el contrato operativo de ejecución de VAEP; no crea un segundo roadmap ni una versión paralela del Plan Comercial Inicial.
- El trabajo se decide exclusivamente desde el Plan Comercial Inicial vigente, el estado vivo, las dependencias verificadas y los gates de seguridad existentes.
- Planes, fases, filas, protocolos, handoffs, prompts, issues, receipts o decisiones históricas no imponen restricciones, dependencias ni orden de ejecución.
- Los archivos históricos de notas, planes y colas están archivados, no conceden autoridad y nunca pueden inyectar tareas sin aprobación explícita del propietario.
- Git, CHANGELOG, BITACORA, Issues, artifacts y receipts son evidencia; no son autoridad de planificación.
- Si una superficie operativa contradice al Plan Comercial Inicial vigente o al contrato operativo, debe corregirse o neutralizarse; Maestro futuro nunca opera como fallback.

## Modelo vigente

VAEP opera `TASKS_ONLY`.

- Las diez automatizaciones programadas son los únicos ejecutores/controllers del runtime VAEP.
- Cinco automatizaciones primarias actúan como `BUILDER_CLOSER`.
- Cinco automatizaciones supervisoras actúan como `VERIFIER_RECOVERY_SECONDARY_BUILDER`.
- Ejecución directa es el camino operativo.
- No se fabrica filler/busywork.
- El orden de trabajo lo determinan las 1123 microtareas del Plan Comercial Inicial y sus dependencias verificadas en la hoja operativa.

## Ownership

Todo write-scope directo exige lease operativo conforme al Plan Comercial Inicial. Un solo writer por scope. Un owner con lease fresco y progreso material no se duplica; un lease sin ejecución viva o sin progreso material puede ser tomado conforme a las reglas de takeover.

Una supervisora que encuentra la primaria trabajando correctamente ejecuta QA/review/gates/prearm o scope seguro independiente. Si encuentra ausencia, stall o deuda accionable, toma ownership y desarrolla/corrige directamente.

## Equipo

- Javier Mejía: propietario, autorización final y responsable operativo de las cinco tareas Primary `:00/:12/:24/:36/:48`.
- Alex Morales: responsable operativo de las cinco tareas Supervisor `:05/:17/:29/:41/:53`.
- Javier y Alex son los dos miembros humanos del workspace ChatGPT Business `SOLQARYN`.
- Las conexiones externas utilizadas por VAEP deben apuntar a recursos corporativos SOLQARYN; no se usan cuentas personales como fuente de datos o infraestructura.
- ChatGPT/VAEP: controller, developer directo, QA, REVIEW_FIRST, integración, CI, certificación, rollup y failover.
- Otros agentes: sólo dentro del scope y permisos vigentes de SOLQARYN.

## Trabajo comercial sin herencia de fases y escalamiento al propietario

- La **única planificación ejecutable** sigue siendo SOLQARYN PLAN COMERCIAL INICIAL UNIFICADO, con las 1123 microtareas de su cola actual. Una fase histórica, N*, SQ*, PR antiguo, ticket cerrado o nombre viejo de workflow **no otorga prioridad, dependencia, veto ni autorización para reactivar trabajo**. Las pruebas de regresión MySQL, seguridad y CI pueden conservar nombres históricos, pero su significado se limita a comprobar el código presente cuando resultan causales; nunca importan la secuencia de aquel plan.
- El propietario autoriza el trabajo técnico autónomo **dentro del ámbito del Plan Comercial Inicial y DEV**, con reparaciones y sustituciones necesarias, siempre sujeto a control de alcance, protección de datos, permisos existentes, lease único y supervisión independiente. **Se prohíben compras, suscripciones, upgrades, cargos, pagos y contrataciones**, incluso cuando parezcan necesarios para resolver una tarea. Las operaciones sobre `main`, QA/PROD, bases productivas, certificados, DNS o secretos requieren la autorización específica prevista en las reglas vigentes; una aprobación genérica no la sustituye.
- Defecto técnico con solución autorizada: la automatización debe intentar una corrección causal **acotada** y revisable, no devolver al usuario trabajo rutinario ni repetir indefinidamente las mismas comprobaciones. Bloqueo por acceso ausente, decisión humana indispensable, ausencia de credenciales autorizadas, recurso que exige pago o cambio fuera del alcance: **detener sólo esa microtarea**, anotar un evento `OWNER_ACTION_REQUIRED` con identificador/fingerprint estable, causa y acción concreta, avisar al propietario mediante el canal de notificaciones de automatizaciones y continuar únicamente con otra tarea **independiente** y elegible. No volver a anunciar el mismo fingerprint mientras no cambie materialmente el bloqueo.
- El monitor de avisos, si se configura, es un **observador de solo lectura** y no constituye una undécima automatización ejecutora VAEP; no reclama lease ni declara `LISTO`. Los cinco Primary y los cinco Supervisor siguen siendo los diez únicos ejecutores del control-plane. Queda prohibido activar tareas incompletas o suplantar una supervisora desde otra cuenta.

## Ejecución y recovery

Regla obligatoria: `DEFECT_RECOVERY_FIRST + FIRST_DETECTOR_OWNS_RECOVERY + NO_REJECT_QUEUE`.

1. Cada tarea activa relee HEAD, leases, dependencias y deuda fresca.
2. Respeta un owner directo fresco; si no existe, adquiere/toma lease y ejecuta el gap material más corto al cierre.
3. REVIEW_FIRST, tests, corrección, exact-head gates y P0/P1=0 son obligatorios para `LISTO`.
4. Un defecto interno reparable se resuelve same-run cuando exista acción segura.
5. Tras recovery, desbloquear dependencias y continuar cierre/promoción same-run.

## ACTIVE_REAL y LISTO

`ACTIVE_REAL` exige run identificable + lease fresco exclusivo + actividad técnica útil/material reciente.

Un trigger, workflow, planner, lease sin progreso o declaración no es `ACTIVE_REAL`.

`LISTO` sólo lo declara VAEP/controller con REVIEW_FIRST + DoD + tests/gates causales aplicables + P0=0/P1=0 + evidencia exact-head/readback verificable.

## Git, entornos y producción

- El trabajo ordinario se realiza en `dev`.
- `main`, PROD, datos productivos, dominios, certificados, secretos e infraestructura productiva requieren autorización vigente y explícita del propietario.
- No force-push, reset destructivo ni reescritura de historia compartida.
- Revalidar HEAD antes de publicar y preservar trabajo concurrente.
- La autoridad del Plan Comercial Inicial para evolucionar comportamiento no elimina controles de seguridad, integridad de datos, RBAC, tenancy, auditoría ni protección de secretos.

## Bloqueo estricto de alcance del proyecto

```text
PROJECT_SCOPE_LOCK=STRICT
EXTERNAL_PROJECT_CONTEXT=DENY_BY_DEFAULT
PROJECT_SCOPE_POLICY=docs/PROJECT_SCOPE_LOCK.md
EXTERNAL_CONTEXT_ALLOWLIST=docs/PROJECT_EXTERNAL_CONTEXT_ALLOWLIST.md
PROJECT_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_REGISTRY=docs/REGISTRO_REFERENCIAS_SKILLS_SOLQARYN.md
LOCAL_SKILL_COUNT=1
```

Regla vinculante: este archivo sólo puede interpretarse con contexto de SOLQARYN. Está prohibido usar contexto de otros proyectos salvo autorización explícita del propietario o una entrada ACTIVE en la allowlist versionada. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`.
