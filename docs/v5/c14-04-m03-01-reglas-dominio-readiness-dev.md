# SOLQARYN V5 — C14-04.M03.01: reglas de dominio de evidencia de release DEV

**Owner:** PRIMARY DEV 02 — JAVIER_CHATGPT (`6af16bb9492c8191b8e8c7d606f11507`). **Repositorio:** `solqaryn/Solqaryn@dev`. **Implementación real:** `backend/src/Domain/AutomationV5/V5ReleaseReadinessDomainRules.cs` y pruebas xUnit `backend/tests/Solqaryn.Tests/Domain/V5ReleaseReadinessDomainRulesTests.cs`.

## Propósito, precedentes y límites

Se incorpora una validación **pura de dominio en .NET 10**, delimitada a comprobar coherencia de un paquete de evidencias DEV suministrado por código de aplicación autenticado. Contratos antecedentes: `C14-04.M02.01–06` LISTO y revisiones A5 independientes, leídos del PLAN/COLA/DAG. No se inventa autoridad sobre los 2.340 IDs ni se consulta GitHub desde Domain.

**No se implementan** endpoints, DTO HTTP, configuraciones de entorno, persistencia EF/MySQL, migraciones, disparadores, workers, webhooks, despliegues, DNS/TLS, restore ni acceso a QA/main/PROD. La invocación desde Application/API es una tarea futura: la pureza y la entrada de evidencia ya modelada no certifican una liberación real.

## Comportamiento implementado

El contrato exige fuente de autoridad versionada, SHA de Git completo de 40 hex, tenant de sesión autenticada, grant DEV y catálogo de tareas/checks obtenido de autoridad confiable. El candidato suministra release, SHA, manifest SHA, digest SHA256 tipado, entorno `dev`, scope tenant, autor/revisor allowlisted distintos, rollback, condición de detención, checks requeridos completos con `completed/success` en **ese SHA exacto** y casos de aceptación realmente ejecutados con referencia de evidencia.

Cada falta devuelve código tipado sin excepción de permiso: `UNVERIFIED_AUTHORITY`, `DEV_GRANT_REQUIRED`, `RELEASE_MANIFEST_MISMATCH`, `TENANT_SCOPE_MISMATCH`, `INDEPENDENT_REVIEW_REQUIRED`, `ROLLBACK_AND_STOP_CONDITION_REQUIRED`, `DUPLICATE_OR_INVALID_CHECK`, `CHECK_NOT_SUCCESS_EXACT_SHA`, `REQUIRED_CHECK_MISSING`, `OBSERVED_ACCEPTANCE_REQUIRED` o `ACCEPTANCE_NOT_OBSERVED`. El único resultado positivo es `READY_FOR_EXTERNAL_EVIDENCE_REVIEW`, **no** A5_PASS, LISTO, go-live ni seguridad operativa certificada.

La implementación compara tenant A/B mediante un scope **resuelto fuera del dominio** por autoridad backend; un tenant entregado por navegador no constituye autoridad. Los checks CANCELLED/SKIPPED/FAIL/queued/in_progress nunca pasan. Los casos escritos sin ejecución son rechazados. La autoridad `dev` es explícita y cerrada.

## Validación A0–A5 y reversión

- A0: código C# localizado en Domain, sin dependencia a Infrastructure/API, sin secretos ni estructuras de producción.
- A1: xUnit real de aceptación DEV y denegación de grant/entorno/manifest/digest/autoría/casos no ejecutados.
- A2: xUnit negativo de tenant B vs A, autoaprobación y ejecución sin CI completed/success.
- A3: integración de compilación Domain en solución backend y suite xUnit GitHub Actions, sin servicios externos reales.
- A4: publicación fast-forward CAS en `dev`, SHA exacto, CI backend/seguridad/ScopeLock/procedencia V5 y lectura posterior PLAN/COLA/DAG/BITACORA. Mientras CI requerida esté en progreso, conservar `VALIDANDO`, no cerrar.
- A5: otra Task Javier distinta del autor revisa diff, tests y SHA; está pendiente hasta evidencia independiente.

**Rollback:** revert ordinario del changeset sobre nuevo HEAD verificado, sin reset, force-push ni cambios en BITACORA histórica. Las fases M03.02–06 permanecen bajo sus DAG y revisiones propias.
