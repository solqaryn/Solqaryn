# C05-04.M05.02 — Contrato API/DTO del directorio ligero
Autor: PRIMARY DEV 06 Javier, Task `6af16bbad26881918c283abe06e1f32a`. Rama exclusiva `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK STRICT. Riesgo R2: validación de respuesta de API y superficie de control global.

## A0 y ámbito
Ajuste localizado del cliente Angular 22.2.1 `PlataformaEmpresasService` que ya consume `GET /plataforma/empresas`. El contrato existente distingue `success=true`, `items`, `pagina`, `tamano`, `total`, `puedeCrear` y metadatos minimizados; no modifica controlador, endpoints, DTO servidor, JWT, RBAC, tenant o persistencia. Se rechaza, antes de mostrar un directorio, página no entera/menor que 1, tamaño fuera de 1..50, más elementos que el tamaño, total inferior a elementos, IDs duplicados y nombres vacíos. El campo `puedeCrear` sigue reflejando solo la respuesta ya autorizada por el servidor y nunca concede operaciones.

## A1, A2 y A3 dirigidos
Vitest/Angular con `HttpClientTestingModule`/`HttpTestingController`: GET válido página 2/tamaño 50, vacío 200, diez DTO malformados que deben fallar y HTTP 403/503 que no pueden emitir listado positivo. A2 es validación de cliente, no seguridad de servidor: no afirma autoridad JWT/rol/tenant A/B real ni ausencia de fuga HTTP. A3 es integración Angular con cliente HTTP de prueba real; MySQL/EF/servidor en vivo NO_APLICA porque no se modifica backend. Playwright NO_APLICA porque no se cambia DOM, rutas ni interacciones. No hay PII, datos productivos ni secretos.

## A4, A5 y reversión
GitHub-hosted ejecuta el test dirigido, lint y build; exigir SUCCESS exact-SHA de Scope Lock, V5, frontend, seguridad y checks realmente requeridos. Test escrito no equivale a PASS. Si CI QUEUED/IN_PROGRESS, mantener VALIDANDO con SHA, RUN y owner hasta su finalización, sin falsa certificación. A5 por otra Task Javier distinta de DEV06 con revisión del diff, código y pruebas. Reversión mediante `git revert` normal fast-forward, nunca force-push. No QA/main/PROD.
