# N9.5 — Certificación current-standard del smoke test

Fecha de certificación: 2026-09-18 UTC  
Rama: `Desarrollo`  
Parent: `N9.5 — Smoke test`

## Alcance certificado

La revalidación current-standard cubre el alcance funcional exigido por N9.5: autenticación/login, productos, inventario, compras, ventas, facturación/PDF/correo, pagos, permisos/RBAC, reportes, aislamiento de tenant y navegación protegida. El cierre se apoya en revisión fresca de implementación, recuperación causal de los gaps detectados y gates ejecutados sobre el functional head exacto.

## Functional head y recuperación

Functional/test head certificado: `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`.

La cadena de recovery preservada para N9.5.G corrigió únicamente defects causales encontrados durante la revalidación en `Desarrollo`, incluidos rutas/tenant context, estabilización del lifecycle de transferencias, overflow responsive de Configuración y fixture E2E de suscripción SaaS activa. No se modificaron `main`, Producción, PR #2, secretos, DNS ni certificados.

Los commits posteriores `54eff50ccafa6f22f118aa3f9633b17baf6f4a17` y `c57c9a1af01e377d64eb1776d17002b584a6a3b4` son exclusivamente evidencia REVIEW_FIRST/receipt y no alteran producto, tests ni runtime.

## REVIEW_FIRST y gates causales

- REVIEW_FIRST N9.5.G: `vaep/evidence/reviews/N9.5.G_REVIEW_FIRST_20260918T070531Z_SUP24.json`.
- Receipt N9.5.G: `vaep/evidence/receipts/N9.5.G_RECEIPT_20260918T070609Z_SUP24.json`.
- `P0=0`, `P1=0` para el scope certificado.
- Admission exact-head: GitHub Actions run `35316302522` — `SUCCESS`, head `1ac95f51176d8b9240741b2e33ab2bd4c2605dc0`.
- Aceptación funcional integral exact-head: run `35316302603`, job `105508629454` — `SUCCESS`.
- Playwright integral: `100/100` tests `PASS`.
- Validación SMTP + PDF: `SUCCESS`.
- Artifact: `10535867520`, digest `sha256:5b073d44217948a3cafd2938d6a1ed24f3c36e5b3234450ec1e1ab09085fd85a`.
- CI DEV de soporte previo: run `35312147429` sobre `a42d84292f3981187a87217a61accbae6d6abd34`, con jobs de migraciones/integración, Docker isolation, frontend production build/lint, hygiene y backend Release/tests en `SUCCESS`; el delta posterior fue acotado y re-gateado en el exact head anterior.

## Persistencia, contratos y documentación

El gate exact-head arrancó MySQL 8.4 descartable, aplicó las migraciones vigentes y comprobó disponibilidad antes de ejecutar la aceptación. N9.5 no introduce un nuevo contrato de dominio, modelo de persistencia, OpenAPI, ADR o ERD: esos artefactos son `N/A` para este cierre porque el recovery no añadió ni cambió contratos/schema/arquitectura; sólo corrigió defectos causales del smoke/revalidación.

## Rollback / runbook

El rollback de este recovery en `Desarrollo` consiste en revertir únicamente los commits causales de N9.5 si una regresión posterior es atribuible a ellos, preservando el historial y ejecutando nuevamente los gates causales. No existe autorización en este scope para rollback, deploy o mutación en Producción. Ante cualquier fallo de revalidación, el parent permanece abierto y se aplica `FIRST_DETECTOR_OWNS_RECOVERY`.

## Estado de cierre documental

N9.5.G está certificado `LISTO` current-standard. N9.5.H sólo puede pasar a `LISTO` después de completar su propio REVIEW_FIRST, registrar evidencia/receipt, actualizar de forma estrictamente aditiva y byte-safe `TASKS.md` y `CHANGELOG_AI.md`, realizar readback y sincronizar COLA/CONFIG/PLAN_MAESTRO. Este documento por sí solo no cierra N9.5.H ni el parent N9.5.
