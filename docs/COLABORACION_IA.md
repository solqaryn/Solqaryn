# Colaboración IA — Solqaryn

## Objetivo

Coordinar a Javier Mejía, Codex, ChatGPT, Chat B (ChatGPT Business), Jules J1–J6 y componentes reservados con mínima pérdida de contexto, mínimo trabajo redundante, aislamiento entre proyectos y máxima trazabilidad en `dev`.

## Identidad de este proyecto

```text
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
AUTOMATION_AUTHORITY=MASTER
MASTER_FILE=docs/VAEP_AUTHORITY.md
```

Estas reglas pertenecen a Solqaryn y solo el contexto canónico de Solqaryn puede autorizar cambios aquí.

## Inicio de CADA conversación/sesión

Antes de escribir:

1. confirmar identidad del proyecto/repo/rama;
2. leer `AGENTS.md`;
3. leer `docs/VAEP_AUTHORITY.md`;
4. leer `docs/VAEP_HANDOFF_CURRENT.md`;
5. leer `PROJECT_CONTEXT.md` y `PROJECT_INDEX.md` solo en lo necesario;
6. revisar únicamente commits nuevos desde el handoff conocido;
7. abrir solo archivos objetivo/dependencias directas.

Localmente, ejecutar `scripts/iniciar-sesion-ia.ps1`. Remotamente, realizar la verificación equivalente mediante GitHub.

Si hay discrepancia entre memoria y repositorio real, prevalece el MAESTRO + estado fresco de GitHub/Plan Maestro y el agente no debe implementar sobre estado stale.

## Equipo

### Javier Mejía

- propietario del proyecto;
- define prioridades y aceptación;
- autoriza merge, Producción, migraciones productivas y cambios de reglas.

### Codex

- está fuera del flujo operativo vigente salvo orden explícita de Javier;
- si se reincorpora, trabaja únicamente en `dev` bajo `docs/VAEP_AUTHORITY.md`;
- debe leer `docs/VAEP_HANDOFF_CURRENT.md` y Git antes de actuar para no repetir diagnóstico ni scopes ya cerrados.

### ChatGPT / VAEP

- controller, arquitectura, auditoría, coordinación, REVIEW_FIRST, QA, integración, corrección, CI y certificación cuando exista conexión autorizada;
- no afirma actividad, PASS, CI ni LISTO sin evidencia real.

### Chat B (ChatGPT Business)

- colaborador full-access par de ChatGPT/VAEP para controller, REVIEW_FIRST, QA, corrección, integración, CI, certificación, rollup y failover;
- opera en `dev` y consume `docs/VAEP_AUTHORITY.md` como autoridad única;
- no es una lane Jules, no publica por fuera del flujo ni puede declarar `LISTO_REAL` sin evidencia completa del MAESTRO.

### Jules J1–J6

- seis lanes cloud canónicas;
- un write-scope material exclusivo por worker;
- patch/artifact only; sin autoridad de merge, Producción ni LISTO_REAL.

## Acceso

Acceso local reconocido: Javier Mejía y Codex. ChatGPT, Chat B y otros agentes operan remotamente solo mediante conectores autorizados, salvo ampliación explícita documentada por Javier.

## Memoria compartida

- `docs/VAEP_AUTHORITY.md` — autoridad operativa única.
- `docs/VAEP_HANDOFF_CURRENT.md` — delta reciente y qué verificar al retomar.
- `PROJECT_CONTEXT.md` — contexto técnico e identidad.
- `PROJECT_INDEX.md` — mapa de carpetas.
- `ARCHITECTURE.md` — patrones y fronteras.
- `TASKS.md` — historial/pendientes resumidos; no fuente machine-readable de estado fresco.
- `CHANGELOG_AI.md` — evidencia/handoff histórico.

## Evidencia por cambio

Cada changeset debe dejar evidencia en Git y sincronizar el Plan Maestro cuando cambie estado. Los documentos colaborativos se modifican cuando su contenido realmente cambió; el handoff actual debe decir qué cambió, qué quedó pendiente y dónde verificarlo.

## Handoff operativo actual

El snapshot compartido vigente está en `docs/VAEP_HANDOFF_CURRENT.md`. Todo colaborador debe verificar ahí el último cierre, el `CURRENT_PARENT`, la admisión y los scopes materiales antes de iniciar trabajo. Si el handoff difiere del catálogo o del Plan Maestro, el catálogo + evidencia + Plan fresco prevalecen y el handoff se corrige.

## Flujo eficiente

1. gate de proyecto;
2. leer MAESTRO + handoff;
3. localizar módulo con `PROJECT_INDEX.md`;
4. revisar solo objetivo + dependencias directas;
5. implementar mínimo cambio correcto;
6. validar proporcionalmente;
7. registrar evidencia;
8. publicar en `dev`;
9. handoff con SHA/pendiente.

## Optimización de tokens y tiempo

- No volver a recorrer todo el repositorio.
- No releer archivos ya documentados si no cambiaron.
- No repetir comandos ya confirmados por reconexión.
- Usar Git para saber qué cambió.
- Usar búsquedas dirigidas por símbolo/ruta.
- Abrir únicamente documento de fase/punto necesario.
- Finalizar al completar objetivo + validaciones.

## Git, CI y Producción

- rama única `dev`;
- no ramas adicionales;
- `main` no se toca;
- PR #2 permanece borrador;
- no auto-merge;
- Producción congelada;
- no secretos;
- no migraciones productivas sin autorización.

Las reglas completas viven en `AGENTS.md` y `docs/VAEP_AUTHORITY.md`.

## AntiG / Antigravity — RESERVED_INACTIVE

AntiG/Antigravity está fuera del flujo operativo actual y no bloquea automatizaciones, handoffs, reviews, QA ni promociones. El estado exacto lo gobierna únicamente `docs/VAEP_AUTHORITY.md`.

Flujo vigente:

```text
Jules J1–J6 -> REVIEW_FIRST VAEP -> R2 único cuando corresponda / QA_TAKEOVER -> VAEP Controller -> LISTO_REAL
```

Los componentes AntiG se conservan como capacidad técnica reservada, pero el runtime vigente no procesa handoffs y el instalador vigente no puede crear scheduler. Una reincorporación futura requiere autorización explícita de Javier y un changeset que modifique el MAESTRO.

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

Regla vinculante: este archivo solo puede interpretarse con contexto de SOLQARYN. Está prohibido consultar o usar skills, documentación, chats, repositorios, memorias o reglas fuera de SOLQARYN salvo autorización explícita del propietario para la fuente/alcance concreto o una entrada `ACTIVE` en la allowlist versionada. La disponibilidad técnica no equivale a permiso. Ante duda, aplicar fail-closed y permanecer dentro de `solqaryn/Solqaryn`. La única skill local es `solqaryn-project-governance`; las nueve referencias externas solo se consultan en su origen original, pin y ruta registrados.


