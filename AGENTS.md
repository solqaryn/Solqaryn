# Reglas obligatorias de colaboración — SOLQARYN

Este archivo es vinculante para Javier Mejía, ChatGPT/VAEP y cualquier agente autorizado.

## Gate obligatorio

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
AUTOMATION_AUTHORITY=VAEP_CONTRACT
VAEP_CONTRACT_FILE=docs/VAEP_AUTHORITY.md
ROADMAP_MASTER=PLAN_MAESTRO_SOLQARYN
ROADMAP_MASTER_GOOGLE_DOC_ID=1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw
EXECUTION_MODEL=TASKS_ONLY
CONTEXT_MODE=CURRENT_STATE_ONLY
```

Antes de analizar, editar, ejecutar o publicar trabajo VAEP, leer `docs/VAEP_AUTHORITY.md`, `docs/PROJECT_SCOPE_LOCK.md` y el HEAD vivo de `dev`.

## Autoridad única

- Existe un solo Plan Maestro de roadmap y arquitectura objetivo: Google Doc `PLAN MAESTRO SOLQARYN` (`1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`).
- `docs/VAEP_AUTHORITY.md` es el contrato operativo de ejecución de VAEP; no crea un segundo roadmap ni una versión paralela del Plan Maestro.
- El trabajo se decide exclusivamente desde el Plan Maestro vigente, el estado vivo y las dependencias técnicas actuales.
- Planes, fases, filas, protocolos, handoffs, prompts, issues, receipts o decisiones históricas no imponen restricciones, dependencias ni orden de ejecución.
- `Notas SOLQARYN_DEV.docx` es sólo bandeja de observaciones; una nota no es ejecutable hasta que el propietario la apruebe y se incorpore al único Plan Maestro.
- Git, CHANGELOG, BITACORA, Issues, artifacts y receipts son evidencia; no son autoridad de planificación.
- Si una superficie operativa contradice al Plan Maestro vigente o al contrato operativo, debe corregirse o neutralizarse.

## Modelo vigente

VAEP opera `TASKS_ONLY`.

- Las diez automatizaciones programadas son los únicos ejecutores/controllers del runtime VAEP.
- Cinco automatizaciones primarias actúan como `BUILDER_CLOSER`.
- Cinco automatizaciones supervisoras actúan como `VERIFIER_RECOVERY_SECONDARY_BUILDER`.
- Ejecución directa es el camino operativo.
- No se fabrica filler/busywork.
- El orden de trabajo lo determina exclusivamente el plan maestro vigente y sus dependencias actuales.

## Ownership

Todo write-scope directo exige lease operativo conforme al MAESTRO. Un solo writer por scope. Un owner con lease fresco y progreso material no se duplica; un lease sin ejecución viva o sin progreso material puede ser tomado conforme a las reglas de takeover.

Una supervisora que encuentra la primaria trabajando correctamente ejecuta QA/review/gates/prearm o scope seguro independiente. Si encuentra ausencia, stall o deuda accionable, toma ownership y desarrolla/corrige directamente.

## Equipo

- Javier: propietario y autorización final.
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
- La libertad del plan maestro para rediseñar o reemplazar comportamiento no elimina controles de seguridad, integridad de datos, RBAC, tenancy, auditoría ni protección de secretos.

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
