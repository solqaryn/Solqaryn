# C06-01.M05.02 — Contrato HTTP/DTO y errores seguros V23

Scope: SOLQARYN dev, PLAN_MAESTRO D891 «Ejecutar contrato API/DTO y validar errores seguros». E891 define la meta acumulativa de M05; no exige certificar toda la fase. Riesgo R2 por prueba HTTP y MySQL real efímero. Diff de test/workflow/documento/CHANGELOG, sin modificar API, Angular, datos, seguridad ni infraestructura viva.

## Requisitos reales

Se ejecuta ASP.NET Core 10 con MySQL 8.4 descartable inicializado mediante Oracle EF10. Se comprueban respuestas HTTP auténticas de rutas **ya implementadas**: GET /health -> 200 JSON status=ok, service=Solqaryn API; GET /health/ready -> 200 database=connected; solicitudes anónimas GET /tenant-context/empresas y /tenant-context/1 -> 401; GET /api/publico/v1/presentacion -> 404 porque ese URI descrito en M02.02 es propuesta aún sin endpoint registrado; POST /health -> 405. Después se apaga solo el servicio MySQL de CI y se prueba GET /health/ready -> 503 database=unavailable, status=not_ready, mientras liveness GET /health permanece 200. El script rechaza filtraciones de paths locales, contraseñas y claves de configuración en respuestas de error.

## Límites de certificación

No se crean rutas ni se simula su existencia. No se certifican 403/429 privados, roles JWT/RBAC tenant A/B, persistencia de leads, precios, consulta de proveedores, Core Web Vitals ni la fase M05 completa: otros IDs requieren esas pruebas. HTTP y MySQL de este job son reales en GitHub-hosted pero aislados de datos de empresas, Aiven y producción. V23 UX/Chromium NO_APLICA a este diff sin cambios de UI. No se modifica QA/main/PROD.

Referencias: governance local contrastada con Skill canónica dev, arquitectura y registro de 12 refs. SKREF-06 Humanizer leída en origen blader/humanizer@9862685f575c65a8247f90369951df1b3416e3d6 ruta SKILL.md, usada solo para precisión del documento. SKREF-03/05/10 no aplican sin cambios de UI/Angular; SKREF-04 sin motion; SKREF-11 sin tarea de modelado de amenazas; SKREF-12 sin medición de performance .NET. No se instalaron ni copiaron skills externas.

A0: diff y contrato D891, riesgos, dependencias y rollback. A1: asserts de 200/401/404/405 y 503, positivos y adversariales. A2: 401 anónimo y detección de errores con información sensible; no certifica aislamiento multitenant completo. A3: API .NET10, EF Oracle y MySQL 8.4 efímero reales, sin mocks. A4: verificar los checks dirigidos, Scope Lock, V5 y backend pertinentes completed/success exact SHA, con readback PLAN/COLA/BITACORA; SKIPPED, CANCELLED, FAIL e IN_PROGRESS no son PASS. A5: revisión independiente por otro TASK_ID Javier, distinto de DEV05 autora/reparadora. Rollback por git revert normal tras comprobar HEAD; nunca force-push.
