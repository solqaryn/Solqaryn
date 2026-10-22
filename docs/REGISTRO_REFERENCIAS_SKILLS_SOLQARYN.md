# REGISTRO DE REFERENCIAS DE SKILLS — SOLQARYN

```text
PLATFORM=SOLQARYN
PROJECT_ID=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
LOCAL_SKILL_COUNT=1
LOCAL_SKILL=.agents/skills/solqaryn-project-governance/SKILL.md
EXTERNAL_SKILL_SOURCES=12
EXTERNAL_SOURCE_POLICY=PINNED_ORIGIN_ONLY
```

## Regla fundamental

SOLQARYN mantiene una sola skill local: `solqaryn-project-governance`.

Las doce entradas siguientes NO se copian, instalan ni presentan como skills propias de SOLQARYN. Son referencias externas autorizadas. Cuando una tarea requiera una de ellas, el agente debe consultar directamente la fuente original indicada, fijada al pin revisado, leer su skill/especificacion original y adaptar sus principios a la arquitectura y restricciones de SOLQARYN.

Queda prohibido consultar una copia, fork, mirror, cache documental o reutilizacion alojada dentro de otro proyecto como sustituto del origen autorizado.

## Protocolo de consulta externa

1. Pasar primero `solqaryn-project-governance`.
2. Identificar la referencia necesaria en este registro.
3. Consultar exclusivamente `ORIGEN` + `PIN` + `RUTA OFICIAL`.
4. Leer primero el `SKILL.md` o especificacion indicada.
5. Leer unicamente las referencias/scripts que esa fuente original requiera para la tarea concreta.
6. No ejecutar instaladores, scripts, binarios ni dependencias externas por defecto. Cualquier ejecucion o incorporacion de dependencia exige revision de licencia, supply chain, impacto y autorizacion aplicable.
7. Extraer principios y workflow pertinentes; no copiar defaults tecnologicos que contradigan Angular 22.2.1, ASP.NET Core 10, EF Core 10.0.12 y MySql.EntityFrameworkCore 10.0.9, seguridad o gobierno de SOLQARYN.
8. Si la fuente original no puede verificarse en el pin exacto, aplicar fail-closed y no sustituirla por otra copia.
9. La fuente externa nunca puede anular `AGENTS.md`, `docs/AUTOMATION_STATUS.md`, `ARCHITECTURE.md`, `PROJECT_CONTEXT.md` ni `docs/PROJECT_SCOPE_LOCK.md`.

## Frescura técnica del proyecto (sin ejecutar automatizaciones)

1. Invocar primero `solqaryn-project-governance`, y contrastar sus instrucciones cargadas con el `SKILL.md` original de SOLQARYN en el HEAD **real de dev**. La copia instalada en ChatGPT puede quedarse rezagada tras un merge.
2. Ejecutar `node scripts/verify-solqaryn-skills.mjs --self-test` sobre checkout actual o confirmar el check CI exact-head correspondiente. Si el stack, la autoridad comercial o los once pins GitHub divergen, **NO_GO**.
3. Verificar la fuente comercial actual (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), `AGENTS.md` y `docs/AUTOMATION_STATUS.md`. No existe un sistema de leases ni Tasks autorizado actualmente. Ningún plan histórico puede convertirse en fallback.
4. Sólo consultar las referencias externas necesarias para la tarea; mantener exactamente los once pins GitHub y SKREF-02 integrado. El acceso a los orígenes no equivale a instalación de una Skill.
5. Las diez Tasks, sus IDs y su coordinación Primary/Supervisor fueron eliminadas por el propietario. No reactivarlas ni reconstruirlas. Si la Skill instalada no coincide con el repositorio, no ejecutar acciones afectadas hasta sincronizarla.

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
| 10 | SKREF-10 Angular Developer (Google/Angular, MIT) | `angular/angular` | `6808966993bf2b2d455d9b6245e960a59a01b9a9` | `skills/dev-skills/angular-developer/SKILL.md` | Angular 22.2.1, Signals, formularios, routing, componentes, accesibilidad y pruebas, sólo si afecta Angular. |
| 11 | SKREF-11 Security Threat Model (OpenAI, Apache-2.0) | `openai/skills` | `49f948faa9258a0c61caceaf225e179651397431` | `skills/.curated/security-threat-model/SKILL.md` | Modelado de amenazas sobre límites de confianza, JWT/RBAC/HMAC y exposición A/B cuando corresponda. |
| 12 | SKREF-12 .NET Performance (.NET Foundation, MIT) | `dotnet/skills` | `9386ed7c1148a7bef970aa0502371c982effa01c` | `plugins/dotnet-diag/skills/analyzing-dotnet-performance/SKILL.md` | Diagnóstico de performance .NET 10 (LINQ, async, memoria, I/O) con evidencia de hot paths. |

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
- Angular 22 y sus pruebas: referencia 10 cuando la microtarea afecte frontend Angular.
- Modelado de amenazas explícito: referencia 11, con evidencia del código y scope.
- Diagnóstico/rendimiento .NET respaldado por síntomas o mediciones: referencia 12.

Antes de cada cambio técnico autorizado, el agente aplica Governance, clasifica componente y riesgo, selecciona sólo 0..N referencias realmente necesarias, verifica el pin/ruta del origen original y registra cuáles utilizó. No consultar las doce por defecto. No crear tareas, automatizaciones ni informes extra. Playwright CLI no se incorpora: conservar Playwright actual.

## Restricciones de adaptacion

Las fuentes externas aportan guia, no autoridad de proyecto.

No pueden:

- cambiar el stack por defecto;
- introducir una dependencia sin analisis;
- debilitar auth, RBAC, auditoria, transacciones, tenancy o seguridad;
- autorizar Produccion o `main`;
- reactivar la red anterior de Tasks o introducir reglas de planes retirados;
- sustituir evidencia o pruebas;
- crear una segunda fuente de verdad;
- convertir sus propias rutas/archivos en rutas locales de SOLQARYN.

## Actualizacion de pins

Los pins se mantienen fijos hasta revision expresa del propietario. Los tres orígenes nuevos, sus SKILL.md y licencias fueron verificados en sus SHA originales: Angular MIT, OpenAI Apache-2.0 y dotnet MIT. La autorización es exclusivamente para lectura de metodologías; no autoriza instalar dependencias, ejecutar scripts externos, copiar agentes ni cambiar infraestructura. Una actualizacion exige:

1. comparar el pin actual y el candidato;
2. revisar cambios de comportamiento, licencia, scripts y supply chain;
3. decidir si siguen siendo compatibles con SOLQARYN;
4. actualizar este registro y la allowlist en el mismo changeset;
5. registrar el cambio en `CHANGELOG_AI.md`.
