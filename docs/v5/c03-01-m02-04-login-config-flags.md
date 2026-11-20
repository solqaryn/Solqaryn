# C03-01.M02.04 — Configuración tipada y feature flags seguros UX_LOGIN_INTELIGENTE_V1

**SOLQARYN V5 FINAL / DEV06 JAVIER**, TASK_ID `6af16bbad26881918c283abe06e1f32a`; `solqaryn/Solqaryn@dev` y `PROJECT_SCOPE_LOCK=STRICT`. Autoridad: STANDARD V5 FINAL, PLAN_MAESTRO!D443 y anexo UX_LOGIN_INTELIGENTE_V1. PLAN E443 es objetivo *acumulativo* de la fase, no requiere certificar autenticación completa con esta unidad. **Salida atómica D443:** modelo TypeScript tipado de configuración UX y MFA, fail-closed con test adversarial; no endpoint, migración, UI conectado ni grant.

## Contrato actual e integración futura

Los contratos reales existentes son `POST /auth/login`, `POST /auth/renovar`, `GET /tenant-context/empresas` y `GET /tenant-context/{empresaId}`. El login DTO vigente no incluye selector, ni autoridad de empresa con el campo legacy `Rol`. Véanse M02.01 (12 invariantes 0/1/N) y M02.02 (12 oráculos API), cuyos archivos están en `docs/v5/`.

`frontend/src/app/core/models/login-inteligente-config.model.ts` exporta `LoginDisplayFlags` con versión literal `1`, booleanos `companyPicker`, `rememberedCompanyHint`, `recoveryLink` y `MfaLayout='default'|'compact'`. `parseLoginDisplayFlags(raw: unknown)` no tolera valores truthy, versiones extrañas ni campos de autorización injertados. `selectLoginDisplayVisibility` es una **proyección puramente visual**: necesita hechos confirmados desde backend (`ServerConfirmedLoginFacts`), sin compartir preferencias entre usuarios. Un flag no es grant de RBAC/Plataforma, no elige tenant, no crea JWT, no desactiva MFA exigida por backend.

### Oráculos positivos, negativos, límite y error

| ID | Aserción falsable |
| --- | --- |
| F01 | Valor nulo, arreglo, versión distinta o ausente => política deshabilitada |
| F02 | Strings, números y objetos truthy => false; `mfaLayout` inválido => default |
| F03 | Campos `isPlatformAdmin`, `bypassMfa`, `jwt`, `rol`, `tenantId` no se propaguen |
| F04 | Cero membresías: sin empresa ni hint |
| F05 | Una membresía: sin selector, confirmar en API |
| F06 | Dos membresías verificadas: sólo pista de UI configurable, nunca grant |
| F07 | Hint de empresa ajena/revocada: sin recordatorio |
| F08 | Sesión revocada: limpiar controles tenant |
| F09 | Sin autenticación: ningún selector empresarial |
| F10 | Conteos negativos/fraccionarios/NaN/Infinity: fail-closed |
| F11 | Desafío MFA exigido por servidor no puede suprimirse con flag falso |
| F12 | Revocación descarta desafío de sesión obsoleta |
| F13 | Enlace recuperación sólo presenta UI; no confiere identidad |

**Límites críticos:** no hay nuevo permiso de inicio de sesión, plataforma, rol ni empresa. MFA se impone por backend, `mfaLayout` sólo cambia presentación. El cliente no puede tomar `localStorage`, host, header, URL o feature flags como autorización. La revocación A/B, idempotencia de POST, emisión de JWT y anti-replay requieren pruebas de runtime en microtareas M03/M05. El DTO actual conserva compatibilidad. Ninguna prueba en este cambio certifica MySQL/HTTP/Chromium real.

## A0–A5 adaptativas y reversión

- **A0:** diff 5 archivos: modelo frontend puro, 13 pruebas Vitest, workflow dirigido, documento y CHANGELOG. Interfaces API, guards, DB, migraciones y secretos sin cambios.
- **A1:** 13 asserts ejecutables, positivos, negativos, frontera, error. Certificar sólo tras logs GitHub Actions `completed/success` en SHA exacto.
- **A2:** flags fail-closed, aislamiento conceptual 0/1/N, revocación y MFA. No fingir pruebas firmadas de JWT/RBAC/tenants A/B del backend.
- **A3:** `NO_APLICA` HTTP/Oracle MySQL/Chromium porque no se conecta runtime de login ni persistencia; pruebas frontend reales en GitHub-hosted Node 24/Vitest. Implementación posterior deberá probar integraciones reales.
- **A4:** commit FF/CAS en `dev`, checks obligatorios `verify-solqaryn-project-scope`, V5 CHANGELOG, frontend y workflow dirigido `SUCCESS` exact-SHA; PLAN/COLA/BITACORA readback. `IN_PROGRESS/SKIPPED/CANCELLED/FAIL` no es PASS de un check requerido.
- **A5:** revisión independiente genuina de Task Javier distinta del autor DEV06 (y del reparador si corresponde) después de LISTO.
- **Rollback:** `git revert` normal del commit bajo HEAD revalidado; no force-push, QA/main/PROD ni datos productivos, preservar BITACORA y DAG. Sin migración que deshacer.

**Resultado:** contrato tipado de UX y pruebas proporcionadas; NO se afirma login inteligente/MFA, HTTP JWT, tenant real, privilegios, datos ni producción desplegados.
