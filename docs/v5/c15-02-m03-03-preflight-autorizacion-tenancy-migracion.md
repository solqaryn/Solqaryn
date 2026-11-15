# SOLQARYN V5 — C15-02.M03.03: preflight de autorización y aislamiento para migración

**Responsable:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-15. **Repositorio:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. **Tipo:** código real .NET 10 en Application con xUnit, limitado a prevalidación de seguridad fail-closed; no es autenticación de producción.

## Alcance e impacto exacto

Se incorpora `V5ArchitectureMigrationAuthorizedPreflight`, un servicio Application read-only que comprueba un `ClaimsPrincipal` suministrado por una futura capa backend autenticada, identidad y tenant cotejados con valores que **deberán** verificarse por servidor, membresía y ownership, permiso efectivo `architecture.migration.review`, Scope Lock dev y SHA exacto. Sólo cuando esos datos son coherentes invoca el servicio `V5ArchitectureMigrationApplicationService.Prepare` de C15-02.M03.02, el cual usa las reglas Domain de M03.01. En ausencia de cualquier precondición, se deniega.

**Límite crítico:** este servicio no valida una firma JWT real, no consulta Aiven, no obtiene permisos desde MySQL, ni autentica por sí mismo al `ClaimsPrincipal`. En particular, un llamador hostil podría construir datos sintéticos; por eso el resultado positivo se llama `AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED` y **no autoriza escritura, despliegue, migración, DNS/secretos, QA/PROD ni A5**. Sólo un adaptador autenticado con JWT/membresía/RBAC/tenancy validados efectivamente podrá dar continuidad en una fase autorizada, junto con CI, rollback, permisos y evidencias específicas. No se crean controladores, tokens, rutas, cookies, headers de privilegio ni permisos persistidos.

## Precondiciones y negativas verificables

| Condición | Respuesta fail-closed |
| --- | --- |
| Identidad nula o no autenticada, membresía/ownership no comprobados | `BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED` |
| ID de sujeto o tenant claim no corresponde al contexto backend esperado | `TENANT_OR_SUBJECT_MISMATCH` |
| Falta permission efectiva de revisión de migración | `VERIFIED_RBAC_REVIEW_PERMISSION_REQUIRED` |
| Scope Lock dev falso, SHA no hexadecimal de 40 caracteres o distinto | `DEV_SCOPE_AND_SHA_REQUIRED` |
| Contexto tenant/SHA distinto del `TrustedBoundary` de migración | `TRUSTED_BOUNDARY_MISMATCH` |
| Environment=qa/prod, proveedor no autorizado, solicitud de DNS/secretos reales, migración destructiva | Rechazos de reglas Domain existentes |
| Consistencia estructural sin evidencia autenticada externa | `AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED`, sin privilegios |

Los flags, claims y catálogos son **precondiciones de una futura integración backend**, no garantías de verificación real. El nombre de permission y del claim `tenant_id` son contratos internos de la prevalidación, aún no cableados a la autenticación de la aplicación. Se deben reconciliar con el backend auténtico antes de instalar cualquier API o ampliar alcance.

## Pruebas y evidencia

`backend/tests/Solqaryn.Tests/Application/V5ArchitectureMigrationAuthorizedPreflightTests.cs` añade xUnit de coherencia positiva sólo estructural, principal no autenticado, membresía/ownership ausentes, sujeto/tenant cruzados, falta de permiso, SHA obsoleto, Scope Lock ausente, QA/PROD rechazados, proveedores fuera de catálogo, DNS/secretos y evidencia de restore incompleta. Los tests son realmente código ejecutable pero su PASS sólo se acredita cuando GitHub Actions compile y termine `completed/success` sobre el SHA publicado. Ninguna prueba con claims sintéticos certifica JWT real, pertenencia MySQL, tenant A/B runtime, backup o restore.

**Predecesores:** C15-02.M03.01 reglas Domain; C15-02.M03.02 servicio Application [blob `471aed271e5e689c2b83db15451cdb3e4cbeee6b`, test `a826d8cd7f1b791cacc0170e51a885295f74622c`] LISTO+A5 independiente de DEV05 sobre autor DEV03. PLAN, COLA y DAG confirmaron ID M03.03 sin owner, G/O=SI y F=VALIDADO/G=SI al reclamar. Estos gates no son permiso para QA/main/PROD.

## Validación A0–A5, seguridad y rollback

- **A0:** diff limitado a servicio Application, xUnit, este documento y CHANGELOG con encabezado/viñetas contiguos; sin tocar archivos de autenticación existentes, API, EF/MySQL, migraciones, proveedor, frontend ni base de datos.
- **A1:** pruebas xUnit positivas/negativas reales en GitHub-hosted .NET 10 y compilación backend; queued/cancelled/skipped/failed no PASS.
- **A2:** denegación por defecto, comprobar tenant A/B sintético, RBAC y SHA; no confundir tests unitarios con firma JWT real ni autorización de infraestructura.
- **A3:** comprobar invocación causal de M03.02 y Domain M03.01, sin endpoint, operaciones de lectura/escritura externa, credenciales o migraciones. Integraciones MySQL/HTTP reales NO_EJECUTADAS porque ninguna se instaló.
- **A4:** publicar sólo fast-forward/CAS `dev` contra HEAD leído; verificar exactamente cuatro archivos, blobs y Scope Lock/V5 y CI backend/seguridad pertinentes `completed/success` de SHA exacto, más PLAN/COLA/BITACORA readback.
- **A5:** revisión independiente obligatoria por Task Javier distinta de DEV05; no autoaprobación ni elevación de entorno.

**Rollback:** `git revert` no destructivo, sin force-push, readback de SHA/CI y evento aditivo BITACORA; preservar cambios concurrentes. No desplegar, ejecutar eventos/webhooks, modificar cuentas/programación, trabajar en QA/main/PROD ni operar proveedores.
