# SOLQARYN V5 — C01-04.M03.03: autorización, tenant y denegación por defecto del expediente

**Autor:** SOLQARYN PRIMARY DEV 04, Task `6aeacb7820ac8191b7d768a6f33dfa09`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; único repositorio `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## Implementación real y frontera de confianza

`V5UniversalCaseFileAuthorizationGate` es código nuevo .NET 10 en Application que implementa un preflight fail-closed específico para expediente y seguimiento universal. Usa `IV5UniversalCaseFileVerifiedEvidenceReader` para exigir evidencia de origen servidor: SHA fuente exacto, actor, empresa, expediente y propiedad persistida, autenticación verificada, membresía y permiso actuales, no revocación, y snapshots congruentes con las reglas Domain `V5UniversalCaseFileDomainRules` (M03.01). Las solicitudes de bypass de filtros tenant, escritura institucional, ID inválido o SHA inválido se rechazan **antes** de consultar el lector. La selección de tenant desde cliente nunca otorga autoridad.

Para lectura, observaciones, decisiones y adjuntos se invocan las reglas Domain. Versionado, idempotencia, auditoría de motivo, archivado, pertenencia de sucursal, revisión independiente de decisiones y autorización de adjuntos se mantienen cerrados por defecto. Los indicadores de revisión o adjunto **solo** proceden de evidencia backend, no de flags de solicitud. Ante preflight favorable se devuelve exclusivamente `TRUSTED_CASEFILE_EXECUTION_REQUIRED`: `GrantsTimelineRead`, `GrantsCaseWrite`, `GrantsAttachment`, `AllowsTenantFilterBypass` y `GrantsInstitutionalWrite` son siempre **false**.

## Pruebas implementadas, alcance y limitaciones

La nueva suite `V5UniversalCaseFileAuthorizationGateTests` añade once escenarios xUnit reales (antes de CI, no presumidos PASS): lectura y escritura sin grant; solicitud nula/acción/ID/SHA; bypass/registro institucional; evidencia ausente o obsoleta; revocación y permisos; tenant A/B, actor y propiedad persistida; tenant de cliente y sucursal; archivado y versión; revisión y adjunto; cancelación. Se exige CI pertinente para A0–A4, Backend Release y pruebas backend, seguridad multiempresa y autorización, Scope Lock y procedencia V5 por SHA exacto o equivalencia demostrada.

**No se implementan ni prueban** autenticación HTTP/JWT de extremo a extremo, EF/MySQL real, conexión a Cloudinary, DI/API/controller, transacciones, escrituras, worker, outbox, cambio de permisos ni auditoría persistida. El lector es interfaz inerte: un adaptador runtime posterior deberá verificar por sí mismo JWT, RBAC, pertenencia actual, ownership por query tenant-safe, concurrencia e idempotencia transaccional. Ningún resultado positivo se puede usar como autorización de API o acceso a medios.

## Gobierno, dependencias, riesgo y rollback

M03.03 sigue a C01-04.M02 y M03.01/M03.02, sin liberar M03.04 ni sucesoras antes de estados A0–A5 reales. Se usa una sola Task autora y las revisiones A5 deben proceder de **otra** de las seis Javier. Impacto: nueva clase Application y xUnit; Domain sin cambios; sin migraciones ni datos; ningún cambio a frontend o integración externa. Seguridad fail-closed, tenant A/B y errores con códigos sin PII.

Rollback mediante `git revert` no destructivo del commit y revalidación de CI; conservar BITACORA append-only y SHA. Sin cambios a main/QA/PROD, infraestructura, secretos o proveedores reales.
