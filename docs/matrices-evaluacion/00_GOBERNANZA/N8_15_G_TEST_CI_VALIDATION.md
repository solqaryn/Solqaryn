# N8.15.G — TEST_CI validation

Estado: `LISTO_REAL`

Baseline documental de entrada: `0d202daa8836851dd18f0dbc987718ea0f4df8b5`.

## Validación causal del inventario

N8.15 es una fase de inventario/documentación: A–F no alteraron código funcional, rutas, esquema, dependencias ni runtime. Por tanto se validó el último estado funcional relevante por capa y se contrastó con referencias estáticas actuales; no se forzó un workflow sobre commits `docs/**` que el propio `desarrollo-ci.yml` excluye mediante path filters.

### Frontend / baseline integral

El último cambio físico observado en `frontend/**` antes del baseline documental fue `7d736f4b5c55efd14fe3802cc7a45ab4e0eaddcb` (`fix(whatsapp): make factura handoff audit UX truthful`). Sobre ese SHA, el workflow canónico `Desarrollo - Compilación y pruebas` (`.github/workflows/desarrollo-ci.yml`) ejecutó run `35022980732`, run number `7154`, con `conclusion=success`.

Jobs del run y resultado:

- `Higiene del repositorio`: SUCCESS.
- `Docker y aislamiento de entornos`: SUCCESS, incluyendo `docker build` del backend.
- `Frontend producción`: SUCCESS, incluyendo `npm ci`, `npm run lint` y `npm run build:prod`.
- `Backend Release y pruebas`: SUCCESS, incluyendo restore, build Release y tests no-integration.
- `Migraciones EF, variantes y cargas masivas en MySQL 8.4`: SUCCESS, incluyendo historial/migraciones, pruebas MySQL y SQL forward.

### Backend más reciente que ese gate integral

El cambio físico posterior observado en `backend/**` es `bab40c06bf162718c44444cfc112d7b776b1797b`, correspondiente al trabajo de observabilidad N8.12.D. Ese cambio no quedó sin evidencia: el receipt canónico `vaep/evidence/receipts/N8.12.D_LISTO_REAL_20260915T221525Z_SUP48.json` identifica `functionalCandidate=bab40c06...`, REVIEW_FIRST `P0=0/P1=0/P2=0` y dos gates dirigidos SUCCESS del `RequestObservabilityTests` en run `35029570267`; el intento final checkout-equivalent quedó SUCCESS en job `104585480989`.

Así, para el estado funcional cubierto por el inventario:

- la capa frontend más reciente tiene lint/build de producción verde;
- el baseline integral previo tiene backend build/tests + Docker + MySQL/migrations verde;
- el delta backend posterior al baseline integral tiene su gate causal dirigido, REVIEW_FIRST y receipt LISTO_REAL verificables;
- A–F sólo agregaron evidencia documental y no invalidaron esos gates funcionales.

## Búsquedas/referencias estáticas dirigidas

Se contrastaron físicamente, sobre `Desarrollo`:

- `frontend/src/app/app.routes.ts` -> features/guards/permission metadata;
- `frontend/src/app/features/**` -> bounded UI existentes;
- `backend/src/API/Controllers/**` -> familias HTTP;
- `backend/src/Application/**` -> DTOs/interfaces/services/validators;
- `backend/src/Infrastructure/Repositories/**` -> persistence adapters;
- `backend/src/Infrastructure/Persistence/AppDbContext.cs` -> DbSets/invariantes;
- `backend/src/Infrastructure/Persistence/Configurations/**` -> mapping/constraints;
- `backend/src/Infrastructure/Migrations/**` y `Persistence/Migrations/**` -> historial EF;
- `Program.cs`, guards y middleware -> authn/authz/tenant/audit/observability/config.

Las referencias halladas sustentan A–F. No se usó ausencia en una sola búsqueda como prueba de orphan.

## Duplicados / orphans antes de remoción

- `REMOVE_SAFE = 0` en N8.15.G.
- Candidatos de alias/layout como los dos roots de migraciones o taxonomía de catálogos permanecen `KEEP`/`CONSOLIDATE` documental hasta intervención causal posterior.
- Componentes, services, repos, controllers, scripts o fixtures sin trazabilidad exhaustiva permanecen `UNKNOWN`; no se borran por similitud nominal.
- La falla de workflows no causales/legacy/PR no se convierte en blocker de N8.15; sólo se consideran gates causales del scope y se preserva PR #2 sin tocar.

## REVIEW_FIRST

P0=0, P1=0. No se encontró contradicción causal que invalide el inventario A–F. La ausencia de un nuevo run integral sobre commits exclusivamente documentales es esperada por los path filters y no se presentó como PASS ficticio.

## Resultado

`N8.15.G = LISTO_REAL`.

Siguiente dependency-valid: `N8.15.H — DOC_CERT`.
