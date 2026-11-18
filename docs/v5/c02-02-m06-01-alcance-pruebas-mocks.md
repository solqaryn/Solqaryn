# C02-02.M06.01 — Auditoría de pruebas: integración real y dobles sintéticos

**Fecha:** 2026-11-18. **Repositorio:** solqaryn/Solqaryn, rama dev. **PROJECT_SCOPE_LOCK=STRICT**. **Plan:** fila 356. **Riesgo de la auditoría documental:** R0, con materia examinada R2 (tenancy, JWT/RBAC, placement, EF/MySQL). Objetivo textual: “Revisar que las pruebas correspondan al alcance real y no a mocks insuficientes”.

## A0. Superficies, consumidores y límites

Esta entrega agrega solo el presente informe y una entrada de CHANGELOG_AI.md. No modifica Domain, Application, Infrastructure, API, MySQL/EF, frontend, DI, JWT, RBAC, secretos ni despliegues. Consumidores futuros: adaptador autenticado server-side de placement shared/dedicated, autorizaciones de tenant y control plane. No autoriza conexión DSN, migración, cutover, SQL sobre clientes, QA/main/PROD ni acciones R3. Rollback: revert normal del commit con revalidación HEAD y preservación de bitácora.

## A1. Matriz causal de las seis entregas M05

| Unidad | Fuente documental en dev | Evidencia realmente ejercitada | Límite: NO acreditado |
| --- | --- | --- | --- |
| M05.01 | `docs/C02-02.M05.01-PLACEMENT-DOMAIN-BOUNDARY-REGRESSIONS.md` | Pruebas xUnit Domain de casos tenant A/B, alias, revocación y fronteras 128/129; resultado NO AUTORIZANTE | MySQL, JWT firmado, API HTTP o conexión |
| M05.02 | `docs/C02-02.M05.02-PLACEMENT-REQUEST-CONTRACT-ERROR-SAFETY.md` | Contrato Application/DTO y lector doble sintético; denegaciones, cancelación y sanitización | Proveedor vivo, middleware auth y DI de placement |
| M05.03 | `docs/v5/c02-02-m05-03-oracle-mysql-placement-integration.md` | **Oracle EF y MySQL 8.4 efímero reales**: dos Empresa persistidas en contextos nuevos, rollback y restricciones localhost | Filtro row-level, IDOR/BOLA HTTP, JWT/RBAC real, router/DSN, Aiven, migración productiva |
| M05.04 | `docs/v5/c02-02-m05-04-positive-placement-acceptance.md` | Matriz positiva Application/Domain en memoria, siempre respuesta inerte AUTHENTICATED_STORAGE_ADAPTER_REQUIRED | Permiso de conexión/migración, proveedor real |
| M05.05 | `docs/v5/c02-02-m05-05-negative-security-placement.md` | Casos adversariales xUnit con evidencia sintética: DSN hostil, tenant B, revocación y error seguro del lector | JWT firmado vía HTTP, RBAC relacional, persistencia |
| M05.06 | `docs/v5/c02-02-m05-06-regression-budget.md` | Regresión .NET 10 de decisiones Domain: 128 tenants sintéticos, 4.096 pares A/B, 12.288 variantes, smoke 15 s | p95/p99 real, DB/HTTP, SLA productivo o accesibilidad UI |

### SHA y logs inspeccionados (2026-11-18)

- **M05.03:** código `backend/tests/Solqaryn.Tests/C0202M0503MySqlPlacementEvidenceIntegrationTests.cs`, blob `a22109a1203f7ec449d3c1906d20ecd26e8b36bb`; source SHA `e3688574566e84e4def5d98fda8ec852d3158a1e`, [job 135157483625](https://github.com/solqaryn/Solqaryn/actions/runs/44404923031/job/135157483625) completed/success, **3/3 tests PASS, 0 FAIL** con filtro `FullyQualifiedName~C0202M0503MySqlPlacementEvidenceIntegrationTests&Category=Integration`. Logs muestran contenedor mysql:8.4.13 y Oracle EF sobre DbContext real, rollback transaccional y dos negativos de host/puerto. Los checks Scope Lock, V5, backend y seguridad pertinentes del SHA completaron SUCCESS. La comparación A/B contra IDs persistidos se hace en reglas Domain; no prueba filtrado tenant automático.
- **M05.06:** código `backend/tests/Solqaryn.Tests/Domain/C0202M0506PlacementRegressionBudgetTests.cs`, blob `f7d3e2d46930ef6bd0e08139298e818c2ff5b2d9`; SHA `2abd96b9f243eb2e499d5eb11b01b72a9cd38bf6`, [job 135166621371](https://github.com/solqaryn/Solqaryn/actions/runs/44407530043/job/135166621371) completed/success, **2/2 tests PASS, 0 FAIL** sobre Domain puro. Scope Lock/V5/backend/seguridad completaron SUCCESS en SHA exacto. Que exista un job MySQL distinto aprobado en ese SHA no transforma las dos pruebas Domain en pruebas MySQL. Retries auxiliares SKIPPED no son PASS.

**Diagnóstico:** cinco M05 tienen cobertura de dominio/aplicación in-memory o sintética; únicamente M05.03 ejecuta EF/MySQL efímero. No existe evidencia aquí de *seis integraciones reales*. Los fixtures `JwtValidatedByBackend=true` no prueban un JWT firmado validado por middleware. El ensayo de 15 s no mide latencia real ni SLO de negocio.

## A2. Gaps de seguridad y condiciones para runtime posterior

Antes de un router, integración storage o migración, exigir en scope propio: identidad y tenant resueltos por backend, JWT firmado, RBAC relacional, ownership, revocación, pruebas A/B e IDOR/BOLA por API real, consultas parametrizadas/filtros del repositorio, fuente confiable de placement y DSN solo servidor; pruebas MySQL 8.4 efímero de lectura/escritura bajo autoridad real; no usar señales de UI para conceder permisos. Falta evidencia R2 de esas operaciones, **no** es una explotación productiva demostrada. El riesgo R3 (cutover, datos reales, despliegue, secretos) sigue NO_GO sin permiso nuevo.

## A3. Integración y no-aplicabilidad

La auditoría M06.01 **no ejecuta nuevas pruebas runtime**; MySQL/HTTP/Chromium/EF de esta diferencia documental son NO_APLICA motivado. Los dos jobs M05 citados son pruebas externas verificadas de entregas previas. No promoverlas a certificación de A3 para un futuro router. Registrar SLO p95/p99 = NO_MEDIDO. La matriz PL-01–PL-07 de M06.03 es evidencia de riesgos, no autorización ni sustituto de tests.

## A4. Puertas de cierre y A5 pendiente

Cierre documental solo tras commit fast-forward en dev, SHA exacto con `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success`, contraste de fuentes arriba, CAS V17/bitácora idempotente y readback PLAN/COLA/DAG. Mientras cualquier check pertinente esté QUEUED/IN_PROGRESS o falle, conservar VALIDANDO. A5 siempre independiente por otra Task Javier distinta de DEV04, con revisión genuina. El cierre técnico documental no concede QA/main/PROD, no modifica fórmulas G/O ni convierte mocks en pruebas productivas.
