# C02-04.M05.06 — regresión, rendimiento sintético y accesibilidad de consola global

Riesgo **R2** por rutas privadas, control-plane, autenticación y navegador. Autor: DEV04 `6aeacb7820ac8191b7d768a6f33dfa09`. Rama dev exclusiva, Scope Lock STRICT, PLAN fila 427, candado V17 `2000000427`. Pruebas Chromium GitHub-hosted; no modifica aplicación ni backend: sólo spec, workflow, este documento y CHANGELOG.

## A0: contrato, dependencias y rollback
Reutiliza el catálogo existente `docs/v5/c02-04-m05-01-routes.json` y la ruta Angular real `/plataforma/consola-global-v5`, con guard/interceptor/routing reales y el endpoint `/plataforma/sesion` **interceptado sintéticamente**. No se prueba JWT HTTP firmado, MFA, revocación de servidor, otorgamiento de grants, base MySQL real, REST productivo, cookies de terceros ni autorización entre empresas A/B. No se modifica Angular 22, API .NET 10, migraciones EF Oracle, secretos ni cuentas de proveedor. Revertir el commit con git revert normal, no force-push.

## A1 — cuatro pruebas fallables Chromium
1. Catálogo contractual de 15 IDs/rutas únicos; dos rutas extremas reales renderizan encabezado tras sesión 200 interceptada y emiten cero POST/PUT/PATCH/DELETE.
2. Consola en 320/375/768/1280: cuatro tarjetas, encabezado semántico, cero overflow horizontal, revalidación con objetivo vertical >=44px, Tab natural desde inicio (sin `.focus()`) alcanza botón y Enter envía exactamente otro GET; foco visible medido.
3. Contraste **computado** de color del contenedor de marca >=4.5:1 en estados institucional/alto contraste. Interacción local no crea grants ni tráfico mutante; no se afirma WCAG integral.
4. Segundo GET retenido simula lentitud real del navegador: `aria-busy=true`, botón disabled, tarjetas privadas ocultas; HTTP 503 opaco sin revelar diagnóstico, tercer GET recupera la sesión y jamás emite mutación.

## A2/A3 — fronteras
El navegador y compilación Angular son reales en runner GitHub-hosted con Node 24/npm 11/Chromium. El backend de sesión está simulado a propósito: controles multiempresa/JWT/RBAC servidor y MySQL se mantienen **NO_APLICA a diff test-only / NO_MEDIDO como verificación end-to-end de autorización**. Se conservan resultados de otras microtareas sin reutilizar PASS de SHAs distintos. El runner usa localhost y no se conecta a Aiven/Render/Vercel productivos.

## A4/A5
Validar el run dirigido `V19 C02-04.M05.06 - Chromium global catalogue regression` con cuatro pruebas completadas SUCCESS exact SHA, Scope Lock, V5, tipado/build y seguridad pertinente. Un check QUEUED/IN_PROGRESS/FAILURE/CANCELLED/SKIPPED no es PASS. Con CI externa pendiente conservar VALIDANDO, owner y SHA/RUN; A5 independiente por otra Task Javier, nunca DEV04. Sin QA/main/PROD ni liberación por observaciones de texto.
