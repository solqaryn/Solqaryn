# SOLQARYN V17 — C02-04.M04.06 densidad, progreso y ligereza de consola global

**Autor:** PRIMARY DEV 02 JAVIER; TASK_ID=6af16bb9492c8191b8e8c7d606f11507. **PLAN:** fila 421. **Candado:** 2000000421. Rama dev, Scope Lock STRICT, riesgo R2 conservador por consola global privada.

## A0 / alcance
Añade únicamente un spec de Playwright real, selección explícita en workflow SHA-aislado ya existente y documentación. El componente/ruta guardados, API, JWT/RBAC/tenant, grants, MFA, MySQL, datos, Scheduler y configuraciones de producción no cambian. Revertir con git revert normal, sin reescribir BITACORA.

## A1–A3 proporcionales
Tres nuevas pruebas Chromium con sesión **sintética**: (1) 320/375/768/1280, grid de una/dos columnas y distribución permitida de dos marcas, sin overflow, formularios ni falsos resultados, NO_MEDIDO conservado; (2) revalidación GET retardada, aria-busy=true, mensaje status, botón inhabilitado, tarjetas ocultas hasta resolver, exactamente dos GET; (3) HTTP 503 sintético, error opaco sin detalles, aria-busy=false, denegación de tarjetas y recuperación explícita con nuevo GET positivo. Los asserts fallan ante controles aparentes mal protegidos, diseño denso, spinner perpetuo o mensaje interno filtrado.

La integración pertinente es Chromium/Angular en GitHub-hosted; no es evidencia de MFA, JWT HTTP real, aislamiento de empresas server-side, MySQL 8.4, auditoría durable, SLO, Lighthouse, latencia ni proveedor productivo. No presentar datos simulados como resultados institucionales.

## A4 / A5
CI exact-SHA con Vitest y Chromium, Angular lint/build, Scope Lock STRICT, V5 y checks requeridos SUCCESS. El workflow conserva concurrency por github.sha y cancel-in-progress=false. Pasar VALIDANDO con CI externa QUEUED/IN_PROGRESS no equivale LISTO; A5 solamente revisor Javier de distinta Task ID, con readback PLAN/COLA/DAG/BITACORA. No QA/main/PROD.
