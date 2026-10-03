# REGISTRO DE REFERENCIAS DE SKILLS — SOLQARYN

```text
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
LOCAL_SKILL_COUNT=1
LOCAL_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_SOURCES=9
EXTERNAL_SOURCE_POLICY=PINNED_ORIGIN_ONLY
```

## Regla fundamental

SOLQARYN mantiene una sola skill local: `solqaryn-project-governance`.

Las nueve entradas siguientes NO se copian, instalan ni presentan como skills propias de SOLQARYN. Son referencias externas autorizadas. Cuando una tarea requiera una de ellas, el agente debe consultar directamente la fuente original indicada, fijada al pin revisado, leer su skill/especificacion original y adaptar sus principios a la arquitectura y restricciones de SOLQARYN.

Queda prohibido consultar una copia, fork, mirror, cache documental o reutilizacion alojada dentro de otro proyecto como sustituto del origen autorizado.

## Protocolo de consulta externa

1. Pasar primero `solqaryn-project-governance`.
2. Identificar la referencia necesaria en este registro.
3. Consultar exclusivamente `ORIGEN` + `PIN` + `RUTA OFICIAL`.
4. Leer primero el `SKILL.md` o especificacion indicada.
5. Leer unicamente las referencias/scripts que esa fuente original requiera para la tarea concreta.
6. No ejecutar instaladores, scripts, binarios ni dependencias externas por defecto. Cualquier ejecucion o incorporacion de dependencia exige revision de licencia, supply chain, impacto y autorizacion aplicable.
7. Extraer principios y workflow pertinentes; no copiar defaults tecnologicos que contradigan Angular 22.2.1, ASP.NET Core 8, MySQL/EF Core, seguridad o gobierno de SOLQARYN.
8. Si la fuente original no puede verificarse en el pin exacto, aplicar fail-closed y no sustituirla por otra copia.
9. La fuente externa nunca puede anular `AGENTS.md`, `docs/VAEP_AUTHORITY.md`, `ARCHITECTURE.md`, `PROJECT_CONTEXT.md` ni `docs/PROJECT_SCOPE_LOCK.md`.

## Inventario autorizado

| # | Referencia | Origen original | Pin revisado | Ruta oficial a consultar | Uso autorizado en SOLQARYN |
|---:|---|---|---|---|---|
| 1 | Agent Skills Specification | `agentskills/agentskills` | `69ef37e9424c0a7ea9dd2293b559e43ec8176379` | `docs/specification.mdx` | Formato de skills, `SKILL.md`, metadata, estructura y progressive disclosure. |
| 2 | Skill Creator | ChatGPT / OpenAI, integrado en el entorno | Integrado en el entorno | Skill Creator oficial disponible en ChatGPT | Crear, actualizar, validar y empaquetar la unica skill local de SOLQARYN cuando el propietario lo solicite. |
| 3 | Impeccable | `pbakaus/impeccable` | `2149fcce39a90bb409df5f16515f316a76dc6199` | `.agents/skills/impeccable/SKILL.md` | Interfaces de producto: dashboard, app shell, formularios, tablas, settings, onboarding, accesibilidad, responsive, performance y polish. |
| 4 | Motion Skills | `emilkowalski/skills` | `d23d7f88a2e21c9e4b1418c7abe420f5c1052ba7` | `skills/animate/SKILL.md`, `skills/review-animations/SKILL.md`, `skills/improve-animations/SKILL.md`, `skills/find-animation-opportunities/SKILL.md` | Motion, microinteracciones, deteccion de oportunidades y revision/mejora de animaciones. |
| 5 | Taste Skill | `Leonxlnx/taste-skill` | `ccbc15639c97057cbfcf32ecebc38ef716e4bb37` | `skills/taste-skill/SKILL.md` | Home, landing, tienda publica, CMS visual, marketing y presentacion de marca. |
| 6 | Humanizer | `blader/humanizer` | `9862685f575c65a8247f90369951df1b3416e3d6` | `SKILL.md` | UX copy, documentacion, PRs, informes y prosa clara sin alterar hechos/evidencia. |
| 7 | Napkin | `blader/napkin` | `27fa60a895de4383b26a539136bc983155cb979c` | `SKILL.md` | Curacion de lecciones recurrentes y conocimiento operativo; nunca autoridad paralela. |
| 8 | Token Optimizer | `alexgreensh/token-optimizer` | `37a9546b9fecba2c4e9a02ef4e90855d449bf08f` | `skills/token-optimizer/SKILL.md` | Progressive disclosure, reduccion de contexto duplicado y eficiencia sin retirar controles criticos. |
| 9 | Caveman | `JuliusBrussee/caveman` | `15581d14007fd01fb3f132016741962f34936ca2` | `skills/caveman/SKILL.md`, `skills/caveman-compress/SKILL.md` | Compresion/resumen interno de bajo riesgo; nunca contratos, seguridad, migraciones, rollback, QA o evidencia persistente. |

## Routing obligatorio

- Toda tarea: primero `solqaryn-project-governance`.
- Creacion/edicion de la skill local: referencias 1 y 2.
- Dashboard/app shell/forms/tablas/settings/onboarding: referencia 3.
- Si existe movimiento o microinteraccion: referencia 4 ademas de la referencia de superficie.
- Home/landing/tienda publica/CMS/promocional: referencia 5.
- UX copy/documentacion/PR/informe: referencia 6.
- Lecciones/runbooks recurrentes: referencia 7.
- Sesiones extensas/contexto duplicado: referencia 8.
- Resumen efimero de bajo riesgo: referencia 9, solo dentro de sus limites.

## Restricciones de adaptacion

Las fuentes externas aportan guia, no autoridad de proyecto.

No pueden:

- cambiar el stack por defecto;
- introducir una dependencia sin analisis;
- debilitar auth, RBAC, auditoria, transacciones, tenancy o seguridad;
- autorizar Produccion o `main`;
- cambiar reglas VAEP;
- sustituir evidencia o pruebas;
- crear una segunda fuente de verdad;
- convertir sus propias rutas/archivos en rutas locales de SOLQARYN.

## Actualizacion de pins

Los pins se mantienen fijos hasta revision expresa del propietario. Una actualizacion exige:

1. comparar el pin actual y el candidato;
2. revisar cambios de comportamiento, licencia, scripts y supply chain;
3. decidir si siguen siendo compatibles con SOLQARYN;
4. actualizar este registro y la allowlist en el mismo changeset;
5. registrar el cambio en `CHANGELOG_AI.md`.
