# SOLQARYN V5 — C14-04.M03.03 autorización y aislamiento de revisión DEV

**Autor:** PRIMARY DEV 05, `TASK_ID=6adc2e55f47c81919f6374e561687076`; rama `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK STRICT; fecha 2026-11-15. Origen institucional: STANDARD V5 FINAL y PLAN_MAESTRO/COLA/DAG_TECNICO.

## Implementación real delimitada

El archivo `backend/src/Application/AutomationV5/V5ReleaseReadinessAuthorizationGate.cs` agrega una frontera .NET 10 de autorización de preflight **sólo DEV** para el caso de uso ya publicado en `V5ReleaseReadinessApplicationService.cs`. Recibe un `V5ReadinessVerifiedCaller` desde `IV5ReadinessAuthenticatedCallerSource` (aún sin adaptador registrado), nunca desde un DTO del navegador; recibe `VerificationContext` y `Candidate` desde `IV5ReleaseReadinessVerifiedEvidenceSource`. No existe implementación productiva de estos proveedores en esta microtarea.

La puerta deniega por defecto ausencia de identidad autenticada, membresía revocada, falta de permiso explícito para revisión, QA/PROD, SHA inválido, tenant B con autoridad A, task fuera de allowlist, grant no concedido, revisión por el autor, referencia manipulada y CI cancelada. Reusa `V5ReleaseReadinessDomainRules.Evaluate` para SHA/manifest, checks exactos, aceptación observada, rollback y parada. Un resultado positivo únicamente requiere **revisión externa independiente autenticada**; los campos `AllowsRelease`, `AllowsDeployment`, `AllowsInstitutionalWrite`, `GrantsTenantAccess` son siempre false.

## A0–A5, pruebas y seguridad

- A0: tres archivos nuevos (Application, xUnit, documento) más `CHANGELOG_AI.md`, sin nuevas rutas HTTP, DI, migraciones, secrets, servicios o roles.
- A1: xUnit positivo de revisión requerida y negativos de identidad, revocación, tenant A/B, grants y SHA.
- A2: validación de confianza de fuente server-side, independencia Task, alcance tenant y ambiente dev; no afirma que exista un proveedor backend conectado.
- A3: composición con Domain existente y las interfaces Application previas; pruebas .NET GitHub Actions obligatorias para el SHA.
- A4: fast-forward con CAS exact HEAD, Scope Lock y procedencia V5, Backend Release, authorization gate y seguridad multiempresa pertinentes completadas SUCCESS; CANCELLED/SKIPPED/IN_PROGRESS jamás PASS obligatorio.
- A5: Task Javier distinta de la autora, con diff, blobs y resultados CI; sólo después de LISTO_DEV, no habilita QA/main/PROD.

**Amenazas y límites:** DTO cliente o tenant/query/header no constituyen membresía ni grant. Un adaptador confiable y pruebas HTTP/EF/MySQL de extremo a extremo requieren autorización y fase propias antes de cualquier uso runtime. Este cambio no despliega releases, lee datos de producción, escribe estados institucionales desde código, reabre Tasks ni implementa webhooks. Rollback mediante `git revert` normal de changeset en dev, revisión de commits concurrentes y readback institucional; nunca force push.
