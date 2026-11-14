# Modernización — Fase 2 Node 24 + npm 11 — 2026-10-02

Estado: **PASS / CERRADA**.

## Objetivo

- Node.js local y CI: `24.21.0` exacto.
- npm: `11.19.0` exacto.
- Vercel DEV: Node `24.x`, que es la granularidad soportada por Vercel.
- Angular permanece `20.3.33`.

## Cambios

- `frontend/package.json`: `engines.node=24.x`, `engines.npm=11.19.0`, `packageManager=npm@11.19.0`.
- `.nvmrc` y `.node-version` en raíz y frontend: `24.21.0`.
- `frontend/scripts/verify-toolchain.mjs`: guard exacto Node/npm.
- 36 workflows operativos con `actions/setup-node@v4` migrados a `24.21.0`.
- `.github/workflows/modernization-baseline.yml` conserva Node 20 únicamente como reproducción histórica del baseline pre-modernización.
- Workflow causal: `Modernización - Fase 2 Node 24 npm 11 DEV`.

## Certificación requerida

- npm ci.
- lint.
- npm audit productivo high+.
- build PROD.
- Angular 20.3.33 sin cambios.
- Playwright completo.
- readback Vercel DEV con Node 24.x.
- QA/main/PROD no tocados.

## Evidencia de cierre

- Run de Fase 2: [37130894429](https://github.com/solqaryn/Solqaryn/actions/runs/37130894429), HEAD exacto `1b34ec476722bc83cd4362eba6cf4391bb492f70`; los tres jobs (npm/lint/build, E2E canónico y Dictamen Fase 2) terminaron `success`.
- El job verificó `NODE=24.21.0`, `NPM=11.19.0`, `SOLQARYN_TOOLCHAIN=PASS` y las dependencias Angular Core/CLI `20.3.33` sin cambios. `npm ci`, lint/contratos y build PROD terminaron correctamente; `npm audit --omit=dev --audit-level=high` reportó cero vulnerabilidades productivas high+.
- El E2E canónico dependiente terminó exitoso sobre el mismo HEAD; el Dictamen Fase 2 también terminó `success`.
- Readback de Vercel DEV observado el 2026-10-05: el proyecto `solqaryn-dev` declara `nodeVersion=24.x` (granularidad configurable por Vercel). Esto prueba configuración, no la versión patch efectiva de un runtime ya desplegado.
- La corrida documentada es la certificación exact-head propia de Fase 2; Angular 21 y 22 se migraron después en fases separadas. En HEAD posterior `de8fda1aaa948ba0398627a8c18441e739b6c85e`, los workflows permanentes de Fase 5 y de aceptación seguían ejecutándose al iniciar esta revalidación; no se atribuye a este documento un resultado de esos runs aún no concluido.

**Dictamen: `FASE_2_NODE_NPM=PASS`.** DEV únicamente; QA/main/PROD no fueron modificados por esta fase.

## Seguridad y datos

No cambia API, DTO, migraciones, datos, RBAC, tenancy, secretos ni Aiven.
