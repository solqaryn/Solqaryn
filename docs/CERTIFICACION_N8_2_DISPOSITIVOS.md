# Certificación N8.2 — Compatibilidad de dispositivos

Autoridad operativa: `docs/VAEP_AUTHORITY.md`.

## Alcance

N8.2 exige validar explícitamente la experiencia en escritorio, laptop, tablet, Android e iPhone sobre `Desarrollo`, sin tocar `main`, Producción, deploy, secretos ni PR #2.

Perfiles dirigidos certificados:

- Escritorio: 1440x900.
- Laptop: 1366x768.
- Tablet: 820x1180.
- Android: 412x915.
- iPhone: 390x844.

La regresión dirigida verifica ancho de viewport, ausencia de overflow horizontal, visibilidad de header/carrito y disparador de navegación móvil cuando el ancho es <= 760 px.

## Evidencia causal

- Test dirigido: `frontend/e2e/n82-device-compatibility.spec.ts`.
- Gate causal: `.github/workflows/n8-2-device-compatibility.yml`.
- Workflow: `N8.2 - Compatibilidad de dispositivos`.
- Run: `34941980085`.
- Job: `104292533908`.
- Functional candidate gated: `1f7008f464f195aab3020a5d0102a85428c8919a`.
- Resultado: `completed/success`; `npm ci`, lint, build de producción, inicio Angular, Playwright dirigido y publicación de evidencia terminaron PASS.

## Cadena VAEP

- N8.2.D `LISTO_REAL`: `vaep/evidence/receipts/N8.2.D_LISTO_REAL_20260915T071257Z_SUP05.json`.
- N8.2.E `LISTO_REAL`: `vaep/evidence/receipts/N8.2.E_LISTO_REAL_20260915T073218Z_SUP48.json`.
- N8.2.F `LISTO_REAL`: `vaep/evidence/receipts/N8.2.F_LISTO_REAL_20260915T073344Z_SUP48.json`.
- N8.2.G `LISTO_REAL`: `vaep/evidence/receipts/N8.2.G_LISTO_REAL_20260915T073447Z_SUP48.json`.

REVIEW_FIRST final de E, F y G cerró con P0=0, P1=0 y P2=0. El P1 inicial `MISSING_N8_2_EXPLICIT_DEVICE_PROFILE_COVERAGE` fue resuelto same-run añadiendo la regresión explícita y su gate causal.

## Seguridad y arquitectura

El delta funcional de N8.2 no modifica runtime de aplicación, API, persistencia, RBAC, límites tenant, migraciones, deploy ni Producción. El workflow causal usa `permissions: contents: read`, no referencia secretos y liga el servidor de desarrollo a `127.0.0.1`. Por ello las pruebas backend/migración y seguridad runtime son N/A causales para este punto; la revisión de seguridad del changeset resultó PASS.

## Cierre documental H

Esta certificación es el paquete canónico de N8.2.H. El cierre `LISTO_REAL` de H requiere, además, reconciliación aditiva/history-preserving de `TASKS.md` y `CHANGELOG_AI.md`, REVIEW_FIRST documental sin P0/P1 y receipt/readback final. Hasta ese receipt, este documento no debe interpretarse como sustituto de `LISTO_REAL`.
