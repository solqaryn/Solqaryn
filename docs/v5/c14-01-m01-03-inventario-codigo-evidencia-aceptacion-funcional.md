# SOLQARYN V5 FINAL — C14-01.M01.03: inventario de código y brechas de aceptación funcional

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 04 JAVIER, `6aeacb7820ac8191b7d768a6f33dfa09`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** auditoría estática de preparación M01. Esta entrega **no ejecuta ni certifica E2E**, no cambia runtime y no autoriza QA/main/PROD.

**Autoridad:** [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y Skill canónica. Antes de reclamar: PLAN fila 2020 PENDIENTE sin dueño, COLA G/O=SI, DAG `F=VALIDADO/G=SI`, luego CAS exclusivo y readback. Las dependencias técnicas dinámicas se revalidan en cada trabajo posterior; esta documentación no libera `M01.06` ni un control de publicación.

## 1. Alcance y método reproducible

El objetivo de esta microtarea es **identificar evidencia de código ya existente, huecos y límites** para aceptación de flujos públicos, autenticados, administrativos, multiempresa, financieros, comerciales, sensibles y sectoriales. La referencia de dominio `C14-01.M01.01`, blob `12d813a5b3f2838c2b19b8731efacb0e1d5856c6`, define NR01–NR14 y una matriz inicial P01/N01–P08/N08. Se usa como contrato documental, no como certificación. `C14-01.M01.02` se encontraba reclamado por **PRIMARY DEV 06**, por lo que este inventario no reescribe su alcance ni afirma completada una prueba que aquella Task no haya publicado.

Los blobs siguientes fueron verificados directamente en Git sobre `dev` (base de lectura `51384804bc6500dcb3f90cb48359c7fcf1a866ac`). Los SHA corresponden a **objetos blob**, no a executions de Actions. Cuando un commit concurrente modifique los archivos, actualizar la matriz antes de usarla para aceptar una prueba de código.

## 2. Inventario versionado F01–F10

| Ref | Archivo real y blob Git | Evidencia estática observable | Qué NO demuestra |
| --- | --- | --- | --- |
| F01 | `frontend/e2e/storefront-fase12.spec.ts` — `0dfe0dcd25ee1c49299ecf4b93301d66e334fd7b` | Fuente Playwright de productos/tienda con fixtures sintéticos | Checkout real, pago cobrado, endpoint autorizado o suite completada |
| F02 | `frontend/e2e/phase7-user-isolation.spec.ts` — `5e207298ea4f8cccb6e88b5ec46e20a7163437a4` | Escenarios de cuenta y aislamiento con variables de entorno de pruebas | Test A/B positivo o negativo ejecutado, sin artefactos y entorno verificables |
| F03 | `frontend/e2e/v5-plataforma-browser-guards.spec.ts` — `72e10a1cc76141bc6d84618e532fbbf2148d2b9e` | Browser guards mockeados y respuestas sintéticas 200/403 | Permisos JWT, RBAC o tenancy reales del backend |
| F04 | `frontend/e2e/responsive-global.spec.ts` — `aef296d8d2766f652b1a9f47800e19411867fb07` | Catálogo de rutas y viewport con Playwright | Cobertura de todos los tamaños, foco, accesibilidad o build PASS |
| F05 | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` — `ce3d50c53b5de8447fa9eba09208160657415fa3` | Fuente de pruebas teclado, foco y UX | Certificación WCAG/lector de pantalla, capturas o ejecución de navegador |
| F06 | `frontend/e2e/tenant-aware-test.ts` — `9c196b0591253b247c2a6a126e98cfa4a7abfb20` | Helper que distingue tokens sintéticos mockeados de JWT reales | Validación real de identidad/tenant/propiedad en APIs |
| F07 | `frontend/src/app/app.routes.ts` — `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas públicas/privadas y lazy imports declarados | Navegabilidad de rutas, consistencia de datos o defensa backend |
| F08 | `frontend/src/app/core/guards/permiso.guard.ts` — `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Consultas de capacidades UI y contexto de tenant declarado | Denegación server-side; un guard del cliente jamás sustituye RBAC |
| F09 | `backend/tests/Solqaryn.Tests/API/C00M0306SafeErrorsTests.cs` — `f5ad59a34743ceb205400e841fbe4872c040f1cf` | Tests fuente de respuesta segura sin detalles privados | Ejecución .NET 10, cobertura API general o regresión multiempresa |
| F10 | `scripts/verify-project-scope.mjs` — `5d51fe92566cfd0e51b7265b844af5432bac57c3` | Script de Scope Lock/identidad/repositorio | Revisión A5, funcionalidad E2E, rollback validado ni permiso de despliegue |

**Limitaciones transversales:** hay fixtures/cuentas estrictamente de prueba en las fuentes, que no son credenciales productivas ni autorización para acceder a un servicio. Este inventario deliberadamente no copia tokens ni contraseñas. La existencia de `.spec` o de un helper mockeado no acredita resultados de pruebas HTTP reales, MySQL efímero ni browser.

## 3. Brechas de evidencia B01–B14 (preguntas falsables)

| Brecha | Hecho pendiente y prueba causal futura |
| --- | --- |
| B01 — rutas | Comparar rutas reales y catálogo E2E para detectar journeys críticos no cubiertos |
| B02 — identidad | Probar sesión válida, expirada, inválida y retorno seguro con JWT reales de fixture sintético |
| B03 — RBAC | Probar acción permitida/rechazada por servidor, no por menús/guards cliente |
| B04 — tenant A/B | Rechazar acceso cruzado a recursos y caché de B desde contexto A, con logs sanitizados |
| B05 — plataforma | Separar admin global de tenant; evidencia de auditoría de acciones elevadas |
| B06 — inventario | Ensayar reservas concurrentes y no negatividad de existencias en DB efímera |
| B07 — pedidos/pagos | Verificar idempotencia y consistencia de estados sin operar dinero real |
| B08 — documentos | Correlacionar factura, pago y transacción con rollback y trazabilidad |
| B09 — responsive | Ejecutar rutas críticas con viewports 320/390/768/1280 y revisar CTA/foco/scroll |
| B10 — accesibilidad | Verificar teclado, rol/nombre, error, focus/diálogo y preferencia reduced-motion |
| B11 — sectoriales | Identificar flows y roles de sector, datos propios y denegación de acceso ajeno |
| B12 — evidencias | Exigir en cada resultado run, SHA, URL de check, status, conclusion, fixture y artefacto |
| B13 — CI | Separar checks requeridos de auxiliares; `queued/in_progress/skipped/cancelled/failed` nunca PASS |
| B14 — recuperación | Plan de `git revert`, reset de fixtures efímeros sin pérdida de auditoría histórica ni datos |

**Estado:** todas B01–B14 son brechas de certificación o validación por resolver; no se presentan como defectos confirmados ni se introducen gates falsos sobre fases documentales.

## 4. Matriz de ocho oráculos positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro esperado | Negativo futuro bloqueante | Herramienta/evidencia necesaria |
| --- | --- | --- | --- |
| P01/N01 | Ruta pública muestra únicamente catálogo abierto | Usuario anónimo accede a pedido privado | Browser + respuesta API y SHA |
| P02/N02 | Login autenticado carga empresa autorizada | JWT inválido o sesión A consulta B | HTTP y fixture multiempresa A/B |
| P03/N03 | Usuario autorizado ve dato/acción por RBAC | Guard permite API sin permiso | API .NET y log sanitizado de 403 |
| P04/N04 | Reserva reduce stock de manera consistente | Carrera permite sobreventa | Tests .NET/EF con MySQL efímero |
| P05/N05 | Checkout idempotente evita doble operación | Retry duplica documento o cobro | Integración sintética, sin transacciones reales |
| P06/N06 | UI a 320px conserva foco/CTA/error claro | Control crítico queda invisible | Playwright y árbol accesible |
| P07/N07 | Error seguro omite PII/secretos y muestra recuperación | Respuesta contiene detalle SQL/token | Tests HTTP y escaneo de logs |
| P08/N08 | CI pertinente termina SUCCESS en commit exacto | RUN skipped/failed/queued se cuenta aprobado | URL run, checks y `head_sha` exacto |

**Los ocho pares son propuestas, no pruebas ejecutadas**. Cada fase de implementación posterior debe adjuntar argumentos reproducibles, fixture sintético, URL del job, SHA, antes/después, condición negativa y rollback.

## 5. Integración con las cuatro capas y seguridad

El navegador Angular 22.2.1/Signals/Material expone navegación y estados, pero `Domain <- Application <- Infrastructure` con API .NET 10 y MySQL/EF Core 10 decide identidad, empresa, membresía, privilegios y propiedad. Los contratos JWT, BCrypt, RBAC relacional, auditoría, CORS, límites de tasa y secretos fuera de Git permanecen vigentes. Playwright de browser mockeado puede servir para UX, pero **no certifica autorizaciones ni persistencia**. Cambios financieros, inventario y documentos requieren pruebas de transacción/aislamiento, evidencia causal y rollback. No se modifica API, DTO, schema, servicio, test, fixture, dependencia, Docker o migración.

**Riesgo de artefactos:** videos, screenshots, trazas y logs futuros deben redactar tokens y PII; rechazar mezclas de tenants, no utilizar datos productivos ni acceder a servicios pagados. Ningún documento M01 autoriza release.

## 6. Cierre documental, auditorías y rollback

- **Diff permitido:** exclusivamente este documento nuevo bajo `docs/v5/` más una sección contigua de `CHANGELOG_AI.md`. Sin cambios de frontend, backend, infra, scripts de test ni identidad.
- **A0:** validar 10 rutas/blobs, 14 brechas, 8 P/N, lenguaje de no-certificación, ausencia de secretos, dos Markdown y CHANGELOG sin encabezado vacío.
- **A1 documental:** inventario funcional contra NR01–NR14 de M01.01; los positivos/negativos son especificación, no ejecución.
- **A2 documental:** preservar JWT/RBAC, tenant A/B, PII, idempotencia, integridad comercial; no atribuir `HTTP`/MySQL PASS.
- **A3 documental:** inspección de blobs Git y coherencia de fuentes contra el plan, sin confundir mocks/test source con checks runtime.
- **A4:** publicar por CAS fast-forward en `dev`, verificar diff y SHA exacto; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` deben ser `completed/success` en ese SHA. CI compilación/E2E no causal de cambios solo Markdown = `NO_APLICA` motivado, nunca PASS.
- **A5:** revisión posterior independiente por una Task Javier distinta de la autora `6aeacb7820ac8191b7d768a6f33dfa09`; no puede autopasarse.
- **Rollback:** `git revert` normal del commit documental y nuevo evento compensatorio en BITACORA, sin reset/force-push ni modificación histórica.

**Resultado atómico M01.03:** inventario verificable y brechas de certificación que preparan pruebas futuras, no aceptación funcional ejecutada, no aprobación de M01.06 ni promoción a QA/main/PROD.
