# SOLQARYN V5 — C01-04.M03.02: preflight de aplicación para expediente y seguimiento

**Owner:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance implementado

Se introduce **código real en Application (.NET 10)** mediante `V5UniversalCaseFileApplicationService`, con suite xUnit propia. Es un preflight de caso de uso limitado y de solo lectura que **compone** la política real `V5UniversalCaseFileDomainRules` de la microtarea M03.01. Su entrada contiene SHA esperado/observado, referencia de evidencia no sensible, snapshot de autoridad server-side, propiedad persistida declarada y comando. Rechaza incongruencia de SHA, ausencia de evidencia, revocación, empresa ajena, versión obsoleta, falta de idempotencia y adjuntos sin verificación.

No se interpreta el snapshot provisto como autorización autenticada. Aunque el preflight sea aceptable, el resultado `AUTHENTICATED_CASEFILE_ADAPTER_REQUIRED` exige posteriormente un adaptador externo capaz de comprobar JWT, RBAC actual, membresía, pertenencia del expediente leída de base, control CAS, transacción, historial y auditoría. `AllowsPersistence`, `AllowsExternalMediaAccess` e `IsFinalAuthorization` permanecen **false** para todos los resultados. No se introducen endpoints ni cambios a base de datos.

## Fuentes y relaciones

- `backend/src/Domain/AutomationV5/V5UniversalCaseFileDomainRules.cs`: blob `19c51673071ad58b86aeff138e1a137f63660689`; contrato de dominio y controles tenant/branch/versión/idempotencia, probado previamente.
- `backend/tests/Solqaryn.Tests/Domain/V5UniversalCaseFileDomainRulesTests.cs`: blob `ab7a21cf2067dee98e23800398f98a8a52ec9499`.
- `backend/src/Application/AutomationV5/V5OperationalAuditApplicationService.cs`: patrón existente, blob `ede09e1997febe719e4aaf9d2e6e4f95de9d1e13`.
- PLAN_MAESTRO fila 267: previo PENDIENTE sin owner; COLA G/O=SI; DAG F=VALIDADO G=SI al reclamar. Predecesores `C01-04.M02.01–06+A5` y `C01-04.M03.01+A5` verificados por gate calculado; no se libera M03.03 sin sus requisitos.

## Pruebas y oráculos

La suite xUnit `V5UniversalCaseFileApplicationServiceTests` cubre aceptación limitada de lectura y escritura-intención sin permisos efectivos, nulidad, SHA desigual, referencia de evidencia insegura, membresía revocada, aislamiento tenant A/B, contextos no verificados, versión antigua, clave idempotente ausente, archivado, adjuntos y revisión independiente. **La existencia del código no equivale a ejecución de las pruebas:** A1–A4 requieren GitHub Actions reales sobre SHA causal, `completed/success` y revisión del diff. No se atribuyen a estas pruebas llamadas reales HTTP, MySQL, EF Core, JWT, E2E, Cloudinary ni observaciones de producción.

## Impacto, seguridad y reversión

- Domain se **reutiliza sin editar**; Application añade un servicio pasivo, sin registro en DI ni consumidores productivos.
- API/DTO HTTP, EF/migraciones, infraestructura, frontend, JWT/RBAC runtime, tenancy y secretos permanecen sin cambios.
- Reversión: `git revert` normal del commit si falla regresión; preservar la historia, sin force-push.
- A0: capa, diff, límites y secretos; A1: resultados positivos y negativos xUnit; A2: tenant A/B, revocación y acceso a adjuntos; A3: compilación y pruebas .NET10 pertinentes; A4: Scope Lock, procedencia V5, SHA exacto y readbacks. Hasta CI completa: `VALIDANDO`, **NO LISTO**.
- A5 requiere otra Task Javier distinta del autor, con diff/CI/seguridad; QA/main/PROD y nuevos servicios permanecen no autorizados.
