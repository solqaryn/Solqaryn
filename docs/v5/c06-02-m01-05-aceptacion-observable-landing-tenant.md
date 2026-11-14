# SOLQARYN V5 — C06-02.M01.05: aceptación observable de landing por tenant

**ID:** C06-02.M01.05, PLAN_MAESTRO fila 906. **Autor:** PRIMARY DEV 04, `6aeacb7820ac8191b7d768a6f33dfa09`. **Proyecto:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-14.

## Objetivo, límites y fuentes

Establecer criterios de aceptación **futuros y verificables** para una landing administrable por tenant con host/slug, branding, preview no indexable, publicación y rollback. Este contrato **no implementa** los criterios ni ejecuta pruebas runtime. No habilita JWT/RBAC, API, DNS/TLS, dominio, CDN, R2, MySQL, frontend, workers, pagos o producción. A5 independiente todavía se requiere tras A0–A4 documental.

Antecedentes de la misma fase, verificados en `dev`:
- M01.01, `docs/v5/c06-02-m01-01-objetivo-no-alcance-landing-tenant.md`, blob `d749c986ad03bf20bded35a3afc87d2b05d4e9fd`: NR01–NR12 y exclusiones.
- M01.02, `docs/v5/c06-02-m01-02-actores-interfaces-landing-tenant.md`, blob `050ef36f55d80e367e097f4d4699718663c91d9d`: A01–A12, I01–I12, B01–B08; publicado por esta Task.
- M01.03, `docs/v5/c06-02-m01-03-inventario-fuentes-landing-tenant.md`, blob `3fd8977ff3a001e01178e81e08b206e8a46511e1`: S01–S14 blobs y G01–G10 brechas.
- M01.04, `docs/v5/c06-02-m01-04-riesgos-supuestos-decisiones-landing-tenant.md`, blob `0001b11cfca6a136eec8627a338134c01f7e0fae`: R01–R12 riesgos posibles, S01–S08 supuestos no probados, D01–D08 decisiones **PENDIENTES**.
- Código existente: `frontend/src/app/app.routes.ts` blob `2f560c14693455b79ac4546e9333fb74586ef234`, `frontend/src/app/core/interceptors/auth.interceptor.ts` blob `4bdf19135ed1932a99fd09ace1f900bcccc5b25b`, `frontend/server/seo-utils.js` blob `b0886e30cc85efa6f800e558d5284412d8c573fc`. Storefront actual **no equivale** a landing por tenant publicada ni certficación de host binding.

## AC01–AC14 — criterios POSITIVOS FUTUROS NO_EJECUTADOS

| ID | Condición de aceptación futura | Oráculo verificable cuando exista runtime |
| --- | --- | --- |
| AC01 | Un host aprobado puede resolver solo su tenant | GET host A aprobado entrega página A, tenant backend A; registrar host canónico |
| AC02 | Un alias no canónico redirige al canonical autorizado | Normalización de host y slug sin cambiar owner; no aceptar homógrafos |
| AC03 | El contenido publicado es versión inmutable aprobada | Lectura repetida devuelve versión/ETag consistente |
| AC04 | Branding del tenant se toma solo del scope autorizado | Marca, logo, copy y moneda de A corresponden a tenant A |
| AC05 | El anónimo solo ve recursos públicos publicados | Sin JWT puede consultar página A publicada sin acceso a ediciones |
| AC06 | Editor de A puede guardar borrador con permiso validado | JWT, membresía y RBAC verificados por backend, audit trail |
| AC07 | Publicador con permiso específico puede publicar una revisión | CAS y auditoría explícitos; editor no recibe publish automáticamente |
| AC08 | Preview autorizado es efímero y no indexable | No-store/noindex, TTL y vínculo a draft exacto |
| AC09 | Sitemap y canonical incluyen solo páginas públicas | Rutas publicadas y host aprobado; sin draft/PII |
| AC10 | Cache conserva separación de contenido A/B | Clave host+tenant+versión, purge coherente |
| AC11 | Recursos externos publicados respetan ownership | Assets de A con MIME validado y permisos explícitos |
| AC12 | Página pública cumple accesibilidad básica | Teclado, focus, aria-live, responsive 320px y reduced-motion |
| AC13 | Errores de proveedores fallan con identidad neutra | No muestra PII ni branding de otra empresa |
| AC14 | Rollback retorna a versión publicada anterior | Sin borrar drafts, pedidos ni historial de auditoría |

## N01–N14 — criterios NEGATIVOS FUTUROS NO_EJECUTADOS

| ID | Denegación o fallo seguro que habrá que demostrar |
| --- | --- |
| N01 | Host no verificado denegado sin revelar tenants |
| N02 | x-forwarded-host manipulado no otorga propiedad ni contenido |
| N03 | X-Empresa-Id falso no altera tenant publicado |
| N04 | Slug traversal/homógrafo o alias colisionado rechazado |
| N05 | Usuario de tenant B no edita preview/borrador de A |
| N06 | Preview token expirado, anónimo o reusado denegado |
| N07 | Editor sin permiso de publicar no publica |
| N08 | CAS de versión desactualizada no sobrescribe publicación vigente |
| N09 | HTML/JavaScript/SQL arbitrario no se ejecuta por builder |
| N10 | Draft no figura en sitemap ni es indexable por robots/canonical |
| N11 | Caché A no se sirve al host B aunque URLs coincidan |
| N12 | CDN/Cloudinary fallido no revela archivos privados o claves |
| N13 | URL externa/no aprobada no autoriza DNS/TLS o despliegue |
| N14 | Rollback no borra historial, facturas, pedidos ni borradores |

## Plan de verificación causal futura (no ejecutado hoy)

- **A1:** pruebas xUnit/.NET 10 de normalización de host/slug, ownership, versionado/CAS, reglas de publicación y errores. Deben incluir éxito de A y rechazo para B.
- **A2:** JWT/RBAC y aislamiento A/B en backend (no `X-Empresa-Id` ni host del cliente como autoridad), código malicioso/HTML, expiración de preview, PII y caché multiempresa.
- **A3:** integración Angular 22.2.1/Playwright + API/MySQL efímero cuando se implemente; CMS, SEO, robots, sitemap, CDN/proxy confiable, rollback y accesibilidad.
- **A4:** registrar commits exact-SHA, CI obligatoria `completed/success`, diffs y readback PLAN/COLA/DAG/BITACORA. `SKIPPED`, `CANCELLED`, `queued`, `in_progress` y `failure` **no equivalen a PASS** de checks pertinentes.
- **A5:** revisión independiente de autoría distinta antes de promover. QA/main/PROD, DNS/TLS y proveedores reales requieren orden adicional.

**Estado actual:** todos AC/N descritos son oráculos FUTUROS NO_EJECUTADOS, no evidencia de tests automáticos, E2E, HTTP, tenant real, disponibilidad, dominio ni indexación. Las decisiones D01–D08 permanecen PENDIENTES. La CI general de runtime puede iniciarse por commits documentales pero es `NO_APLICA` a este diff, sin mentir sobre su estado.

## Alcance del changeset y reversión

Solo se crean este documento y una sección autocontenida de `CHANGELOG_AI.md`. A0: cotejo de fuentes y límites; A1–A3: revisión documental de los 14 positivos y 14 negativos sin runtime; A4: verificación Scope Lock STRICT y control V5 del CHANGELOG en SHA exacto, diff Markdown y readback institucional. Rollback por `git revert` normal sin borrar BITACORA ni reescribir historia. No activar webhooks, dispatcher ni otras Tasks.
