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

Cada VAEP Primary/Supervisor aplica primero `.agents/skills/solqaryn-project-governance/SKILL.md` en el HEAD actual de `dev`. No basta que ChatGPT cargue una versión instalada: comparar con la canónica y confirmar `node scripts/verify-solqaryn-skills.mjs --self-test` o el check exact-head en CI. Discrepancia de versión o de fuente externa => `SKILL_VERSION_MISMATCH` y `NO_GO`. Las referencias externas sólo se consultan cuando corresponda al lane y desde el origen + pin del registro. No copiar ni instalar referencias como skills locales; no activar las automatizaciones hasta verificación independiente 10/10.

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
