# Modernización — Fase 2 Node 24 + npm 11 — 2026-10-02

Estado: en certificación exact-head.

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

## Seguridad y datos

No cambia API, DTO, migraciones, datos, RBAC, tenancy, secretos ni Aiven.
