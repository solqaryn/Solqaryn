# Certificación ERP-N8.3 — Compatibilidad de navegadores

Fecha de cierre documental: 2026-09-15
Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama certificada: `Desarrollo`

## Alcance

N8.3 certifica la compatibilidad causal del storefront soportado en Chromium, Firefox y WebKit sin introducir cambios de runtime backend, persistencia, migraciones, autenticación, RBAC ni datos productivos.

## Cadena certificada

- N8.3.A PRE — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.A_LISTO_REAL_20260915T074220Z_SUP48.json`.
- N8.3.B DOMAIN — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.B_LISTO_REAL_20260915T074355Z_SUP48.json`.
- N8.3.C DB_MIG — `LISTO_REAL` N/A grounded: `vaep/evidence/receipts/N8.3.C_LISTO_REAL_20260915T074445Z_SUP48.json`.
- N8.3.D BACKEND_API — `LISTO_REAL` N/A grounded: `vaep/evidence/receipts/N8.3.D_LISTO_REAL_20260915T074535Z_SUP48.json`.
- N8.3.E FRONTEND_UX — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.E_LISTO_REAL_20260915T074915Z_SUP48.json`.
- N8.3.F SEC_AUDIT — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.F_LISTO_REAL_20260915T075410Z_SUP48.json`.
- N8.3.G TEST_CI — `LISTO_REAL`: `vaep/evidence/receipts/N8.3.G_LISTO_REAL_20260915T075520Z_SUP48.json`.

## Functional candidate y gate causal

Functional candidate: `b833e44a976f92a426fff7f969a3f3f757234f20`.

Workflow causal: `N8.3 - Compatibilidad de navegadores`, run `34943377733`, job `104296994230`, terminal `success`.

Validaciones terminales:

- `npm ci`: PASS.
- frontend lint: PASS.
- build de producción frontend: PASS.
- instalación de Chromium, Firefox y WebKit: PASS.
- arranque Angular en `127.0.0.1`: PASS.
- regresión Playwright dirigida: PASS en Chromium, Firefox y WebKit.
- publicación de evidencia del job: PASS.

## Seguridad y observabilidad

El workflow usa `permissions: contents: read`, no consume secretos, levanta el servidor local exclusivamente en `127.0.0.1`, usa datos sintéticos para la configuración pública de empresa y no toca Producción. No existe delta de autenticación/RBAC/auditoría de producto atribuible a N8.3; el stage N8.3.F quedó certificado con P0=0, P1=0 y P2=0.

## Archivos causales

- `.github/workflows/n8-3-browser-compatibility.yml`
- `frontend/e2e/n83-browser-compatibility.spec.ts`
- `frontend/playwright.n83.config.ts`

El E2E verifica storefront visible, hero, header, superficie del carrito, ausencia de overflow horizontal y ausencia de errores de página en los tres motores soportados.

## Rollback

Si fuera necesario revertir exclusivamente N8.3, retirar de `Desarrollo` los tres artifacts causales anteriores y volver al baseline anterior al functional candidate. No se requiere rollback de esquema, datos, secretos ni infraestructura porque N8.3 no introduce delta en esas superficies.

## Guardas

- `main`: intacta.
- Producción: intacta.
- deploy: no ejecutado.
- secretos: no tocados.
- PR #2: no merge, no auto-merge.
- P0 abiertos atribuibles: 0.
- P1 abiertos atribuibles: 0.
- P2 abiertos atribuibles: 0.

N8.3.H sólo pasa a `LISTO_REAL` mediante REVIEW_FIRST documental, receipt verificable y reconciliación del control-plane; este documento por sí solo no sustituye ese cierre.

## Revalidación current-standard — 2026-09-17

La revalidación posterior al cambio de arquitectura conserva el historial anterior y certifica de nuevo la superficie vigente de N8.3 sobre `Desarrollo`.

- Functional head causal: `844b3584c4159abe3b9cc854addf49033460985a`.
- Workflow causal fresco: `N8.3 - Compatibilidad de navegadores`, run `35174604643`, job `105053421253`, terminal `success`.
- Resultado dirigido: `npm ci`, lint y build de producción PASS; Chromium, Firefox y WebKit `3/3 PASS`; artifact `10478007909`, SHA-256 `c51744d40ff790ff4f87545fb8b8a028424a0074d5100f3de860d902d771973a`.
- N8.3.E revalidado: `vaep/evidence/receipts/N8.3.E_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T024817Z_SUP36.json`.
- N8.3.F revalidado N/A grounded para SEC_AUDIT: `vaep/evidence/receipts/N8.3.F_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T024949Z_SUP36.json`.
- N8.3.G revalidado: `vaep/evidence/receipts/N8.3.G_REVALIDATED_CURRENT_STANDARD_LISTO_20260917T025102Z_SUP36.json`.
- El contrato vigente está en `docs/N8_3_BROWSER_SUPPORT.md`; limita N8.3 a renderizado, navegación, interacción y regresión E2E frontend y excluye backend, API, persistencia, RBAC, tenant isolation, secretos, deploy y Producción.
- Desde el functional head hasta los receipts E/F/G sólo se añadieron evidencias VAEP; no hubo delta funcional adicional antes de esta reconciliación documental.
- P0 abiertos atribuibles a N8.3: 0. P1 abiertos atribuibles a N8.3: 0.
- `main`, Producción, deploy, secretos y PR #2 permanecen intactos.

Este apéndice actualiza la evidencia documental vigente sin borrar ni reescribir la certificación histórica del 2026-09-15. El estado operativo fresco continúa perteneciendo a `CONFIG/COLA` y sólo N8.3.H puede cerrar mediante REVIEW_FIRST, receipt verificable y write/readback del control-plane.
