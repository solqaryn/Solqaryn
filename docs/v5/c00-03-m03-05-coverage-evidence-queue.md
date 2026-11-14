# SOLQARYN V5 — C00-03.M03.05: integración de cola de evidencias (sin dispatcher)

Código real de backend Application .NET 10: `V5CoverageEvidenceQueue` integra los contratos existentes `V5RequirementCoverageProjectionService` y `V5RequirementCoverageRules`. Produce una colección acotada, determinista e inmutable de solicitudes de comprobación de evidencia sobre IDs del catálogo V5 existente. No ejecuta un solo trabajo de usuario ni crea filas.

El algoritmo requiere actor de allowlist Javier, scope autorizado, SHA exacto, referencias de requisitos catalogadas y snapshots de PLAN. Cada entrada mantiene el estado real y `EVIDENCE_NOT_VERIFIED`; ni siquiera un LISTO representa evidencia de CI/A5. Evita que el autor obtenga su propia tarea LISTO como revisión independiente. Rechaza SHA obsoleto, mapeos duplicados, referencias inexistentes y exceso de capacidad; contrato de pruebas positivas y negativas en `V5CoverageEvidenceQueueTests`.

Aislamiento: los conjuntos y snapshots deben proceder de fuente autenticada. No hay JWT nuevo, permisos otorgados, tenant A/B inferido desde headers, I/O externo, logs PII, MySQL, webhooks, cola durable, workers hosteados ni Scheduled Tasks invocadas. Un consumidor futuro debe implementar autorización JWT/RBAC, tenancy confiable, CAS institucional, pruebas A0–A5 y readbacks. Se implementa integración de proyección, NO event dispatcher.

Clasificación: requiere_dotnet=SI; requiere_security=SI (autor, scope, revisión independiente y negativos); requiere_scope_lock=SI. Angular/Node, MySQL, Docker y E2E=NO_APLICA al diff. Compilación y test xUnit en GitHub Actions SHA exacto antes de A4/LISTO; A5 solo otra Task Javier. Rollback normal `git revert`.
