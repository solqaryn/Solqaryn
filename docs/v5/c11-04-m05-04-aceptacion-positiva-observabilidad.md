# SOLQARYN V17 — C11-04.M05.04: caso positivo de aceptación de observabilidad privada

**Microtarea:** C11-04.M05.04. **Riesgo:** R2 conservador por frontera de autorización; sólo pruebas de interfaz y documentación. **Autor:** DEV04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio/rama:** solqaryn/Solqaryn@dev. **Gobierno:** PROJECT_SCOPE_LOCK=STRICT, V15 A0–A5 y candado V17 `2000001721`.

## A0 — requisitos y cambio exacto
Añadir una prueba Playwright al fichero ya existente `frontend/e2e/c11-04-m04-01-observabilidad.spec.ts`, sin modificar componentes productivos, API, DTO, routing, persistencia, autenticación, proveedores ni permisos. La aceptación positiva sólo certifica comportamiento visual read-only ante un GET sintético `/plataforma/sesion` que devuelve 200. El estado empresarial, las métricas y los exporters no deben consultarse ni generarse por esta prueba.

## A1 — escenarios capaces de detectar defectos
- **Positivo:** con sesión sintética autorizada aparecen las cuatro tarjetas privadas y el encabezado semántico; el botón de revalidación mantiene las cuatro tarjetas al recibir 200.
- **Recuento y regresión:** exactamente dos GET (carga inicial y revalidación), sin duplicación. Los tests existentes del mismo fichero ejercitan 403, 503, revocación, contenido oculto, revalidación lenta y ausencia de doble acción.
- **Frontera responsive:** en 320 y 1280 px no hay desbordamiento horizontal ni pérdida de tarjetas.
- **Error/seguridad:** capturar y prohibir peticiones a exporters, traces, alert dispatch o telemetría; las pruebas negativas anteriores permanecen. Una regresión debe fallar con una aserción reproducible.

## A2 — límites de seguridad y tenancy
El token usado en el navegador es intencionalmente **sintético y no JWT**. Se conserva `plataformaGuard`, sin sustituir autorización backend, pruebas tenant A/B reales, controles de roles, revocación server-side o auditoría durable. Ningún dato de clientes reales, secret o proveedor es utilizado. No atribuir certificación HTTP real a respuestas de `page.route`.

## A3 — integración pertinente
GitHub Actions debe ejecutar Chromium/Playwright real, lint/build Angular y Vitest en runner alojado, mediante `.github/workflows/v17-c11-04-m04-01-observabilidad.yml`. La API de sesión está simulada únicamente en los E2E. MySQL 8.4, EF, migraciones, SMTP, Cloudinary y proveedores reales: **NO_APLICA**, porque ningún camino productivo se modifica.

## A4 / A5 — publicación, readback y rollback
Publicar un único commit fast-forward/CAS en `dev`; registrar SHA causal y hashes de blobs de test, documento y CHANGELOG. Exigir Scope Lock, V5, frontend y job dirigido `C11-04.M04.01 - Vitest y Chromium de observabilidad` con `completed/success` del SHA exacto o equivalencia de blobs demostrada. Una CI `queued/in_progress/failed/cancelled/skipped` requerida **no es PASS**; dejar VALIDANDO/owner con RUN y reanudar REVIEW_FIRST. Con A0–A4 reales, LISTO_DEV; **A5 por otra Task Javier** independiente. Readback PLAN, COLA, DAG y BITACORA. Rollback con `git revert` normal, sin force-push, sin QA/main/PROD y sin borrar historia.

Los pasos descritos constituyen alcance y **criterios**, no una declaración de ejecución hasta tener CI causal.
