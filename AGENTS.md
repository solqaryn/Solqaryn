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

### VAEP — única autoridad operativa vigente y corte pendiente (21/10/2026)

**Estado real, no histórico:** `vaep/control/logical-lease-candidate.json` está en `mode=PRE_GO_CONTRACT_CERTIFIED`, `activation_permission=OWNER_APPROVED_PRE_GO_CONTRACT_ONLY`, `commercial_writes_allowed=false` y epoch `VAEP_LOGICAL_20261021_PILOT` congelado. El Google Sheet oficial registra 10/10 `WRITE_POLICY=LOGICAL_SINGLE_OWNER_TASK_WRITE_VERIFIED` y 10/10 `LAST_HEAD` alineados, pero `CONFIG.RUNTIME_MODE=PAUSADO` por orden posterior del propietario. Los diez IDs y horarios canónicos se conservan. Habilitación del trigger no equivale a permiso para trabajo comercial; está prohibido reclamar, recuperar o cerrar scopes comerciales mientras falte `ACTIVE_CERTIFIED` y el GO conjunto.

**Escritor CAS dual retirado del código vigente:** `scripts/vaep/cas_lease_writer.py` y sus pruebas ejecutables se eliminan como motor de `createDeveloperMetadata/deleteDeveloperMetadata`. Ese procedimiento se considera evidencia histórica, **no** writer alternativo, rollback operativo ni instrucción vigente. Nunca restaurar mediante import, generación de requests, intento CAS o reintento tras `TOOL_SAFETY_BLOCK`. El borrado del archivo no prueba que los registros `DeveloperMetadata` históricos hayan desaparecido del Google Sheet; no eliminarlos ni ignorarlos sin consulta exacta, fencing y autorización seguros.

**Contrato lógico en preparación:** `scripts/vaep/logical_lease_dispatch.py` prepara reclamación, handoff, rechazo/rework, `REVIEW_FIRST`, heartbeat y recovery con guard persistente `addNamedRange` único en el mismo `spreadsheets.batchUpdate` atómico que el cambio de celdas y la auditoría. No usa CAS dual de `DeveloperMetadata`. El constructor no ejecuta por sí solo llamadas externas ni autoriza una Scheduled Task. Todo write-scope material sigue requiriendo HEAD y CI exactos, permisos efectivos por Task, estado terminal comprobado de la invocación previa, deps `LISTO`, WIP=1 por actor, exclusividad de un propietario por scope, readback `OWNER/RUN_ID/TOKEN/HEAD/TTL` y rechazo de transición antigua por fencing. `REVIEW_FIRST` solo puede hacerlo un actor diferente del autor con evidencia y pruebas aprobadas.

**Discrepancia arquitectónica aún no resuelta:** el manifiesto solo acepta pruebas administrativas GitHub para Primary `:00` (`6ad7bbf79c488191bf893e6c08ea9b5f`) y Supervisor `:29` (`6abd6241c3e48191a8856ed7b0f42c5c`). Las otras ocho Tasks siguen sin acceso a escritura comercial en el epoch congelado y no pueden asumirlo por tener `ENABLED=TRUE` ni por una política de Sheets. El objetivo de diez actores que puedan ejecutar y revisar trabajo no está implementado/certificado; NO atribuir al pool actual equivalencia con las diez automatizaciones operativas de un respaldo histórico. La aceptación del propietario de estas dos pruebas es definitiva y no exige repetir diagnósticos GitHub; no convertirlas en pruebas de diez permisos independientes ni de runs terminales.

**Readbacks previos al GO:** cinco Primary de Javier y cinco Supervisor de Alex constan con prompts PRE-GO conciliados; verificar los prompts reales desde sus respectivas cuentas al efectuar un nuevo epoch. El manifiesto continúa desactualizado respecto del `LAST_HEAD` fusionado y la migración 10/10 de Sheets. Para `ACTIVE_CERTIFIED` hacen falta reconcile del SHA, autoría/epoch y cohorte realmente operativa, verificación física de candados históricos, auténtico estado terminal de ejecuciones previas, readback por identidad, seguridad, CI exact-head y autorización GO del propietario. Nunca elevar solo el campo `mode`, crear Tasks ni reabrir el protocolo antiguo para superar un bloqueo.

**Despacho y prioridad sin falsa actividad:** las 1.123 microtareas del Plan Comercial Inicial son el único DAG. Priorizar revisión ajena `VALIDANDO`, trabajo propio recuperable y luego primer `PENDIENTE` con dependencias satisfechas **únicamente cuando exista GO y lease lógico real**. Durante PRE-GO, solo lectura y evidencia sin nuevos claims. Los ocho IDs históricos y las fases antiguas no constituyen autorización para alterar estados actuales; releer el Sheet vivo. Si una microtarea no es elegible, impedir únicamente su escritura; no deshabilitar otras Tasks, no pausar ni activar globalmente por un problema local. Respetar las restricciones de gobernanza, secretos, QA/main/PROD y de herramientas.

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
