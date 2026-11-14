# N8.15.A — Baseline PRE

Estado: `LISTO_REAL`

## Alcance

Este registro congela el punto de partida de inventario para la intervención N8.15–N8.24. Es evidencia de PRE; no modifica producto, no renombra ni elimina assets y no ejecuta DDL.

## Autoridad y baseline

- Rama única: `Desarrollo`.
- Autoridad única releída antes de actuar: `docs/VAEP_AUTHORITY.md`.
- Especificación causal: `docs/matrices-evaluacion/00_GOBERNANZA/ESPECIFICACION_EJECUCION_N8_15_N8_24.md`.
- HEAD congelado para iniciar el inventario: `e03dbfe786bd3ba323414e8af1f3e53f91872a98`.
- Tree SHA del HEAD: `b325f5c63776d8396957517bac2b82940ae009ce`.
- Posición de control verificada en Sheet: `N8.3.A` precede a `N8.15.A`; la intervención ocupa `N8.15.A`→`N8.24.H`; la reentrada histórica ocurre en `N8.3.B` sólo después de `N8.24.H`.

## Roots físicos observados

El tree raíz del HEAD contiene como roots de directorio: `.agents/`, `.githooks/`, `.github/`, `.jules/`, `backend/`, `docs/`, `frontend/`, `scripts/` y `vaep/`.

Archivos raíz de gobierno/build/config observados y que deben permanecer dentro del inventario: `.dockerignore`, `.gitignore`, `AGENTS.md`, `ARCHITECTURE.md`, `ARCHITECTURE_CHANGELOG.md`, `CHANGELOG_AI.md`, `CONTRIBUTING.md`, `Dockerfile`, `PLAN_EJECUCION_AUTONOMA.md`, `PROJECT_CONTEXT.md`, `PROJECT_INDEX.md`, `README.md`, `TASKS.md`, `implementation_plan.md` y `render.yaml`.

El subtree `docs/` contiene, entre otros assets de gobierno de la intervención, `matrices-evaluacion/00_GOBERNANZA/ARQUITECTURA_PADRE_HIJO.md`, `ESPECIFICACION_EJECUCION_N8_15_N8_24.md`, `PLANTILLA_MATRIZ_UI.md`, `PLAN_INTERVENCION_N8_15_N8_24.md` y `README.md`; también conserva evidencia e historia previa que no se debe tratar como autoridad autosuficiente.

## Método reproducible de inventario y conteo

1. Fijar un único commit SHA de `Desarrollo` como baseline de cada pase y registrar su tree SHA.
2. Enumerar el Git tree por SHA, no por estimación visual ni por documentación histórica.
3. Recorrer por separado `backend/`, `frontend/`, `docs/`, infraestructura/configuración (`.github/`, `.githooks/`, `.jules/`, Docker/Render) y tooling (`scripts/`, `vaep/`, `.agents/`).
4. Registrar cada asset por `path + type + blob/tree SHA`; los conteos definitivos se derivan del conjunto enumerado y sólo se publican cuando el conjunto está cerrado.
5. Para código, enriquecer el inventario con referencias semánticas de dominio/API/UI/DB/seguridad, pero nunca sustituir la enumeración física por búsquedas parciales.
6. Para candidatos de duplicidad/orphan/remoción, clasificar primero como `KEEP | CONSOLIDATE | DEPRECATE | REMOVE_SAFE | UNKNOWN`. `UNKNOWN` impide eliminación.
7. En esta etapa A está prohibido borrar, renombrar, mover o ejecutar DDL; cualquier hallazgo se difiere a las etapas causales posteriores.
8. Antes de publicar cada etapa se releen HEAD y control-plane. Si HEAD cambia fuera del writer, se rehace el delta desde el baseline y no se mezclan snapshots.

## REVIEW_FIRST

- P0: 0.
- P1: 0.
- La evidencia se limita a congelar método, roots y baseline verificables.
- No se declara conteo exhaustivo total en A; los conteos exactos pertenecen al cierre de N8.15.H una vez inventariadas todas las capas.
- No se observó una justificación para eliminar o renombrar assets durante PRE.

## Validación causal

- HEAD fue releído inmediatamente antes de publicar y permaneció en el baseline congelado.
- El control-plane fue releído y `N8.15.A` estaba dependency-valid por `N8.3.A`.
- Lease exclusivo adquirido con read-before-write y readback antes de la escritura.
- Las diez automatizaciones canónicas de Solqaryn estaban habilitadas; no se rearmó ninguna por `last_run_time`/`next_run_time`.
- No se tocaron `main`, Producción, deploy, secretos ni PR #2.

## Resultado

`N8.15.A = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.B — DOMAIN`.
