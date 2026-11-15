# SOLQARYN V5 FINAL — C15-04.M03.03: autorización y aislamiento de preflight UX

**Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`, rama `dev`, Scope Lock STRICT.

## Implementación de Application .NET 10
- Se añade `V5UxAuditAuthorizedApplicationService` para exigir actor, JWT validado por servidor, membresía vigente, permiso de revisión, tenant resuelto del backend, scope dev y SHA de evidencia idéntico antes de invocar `V5UxAuditApplicationService`.
- Las superficies y criterios solicitados tienen que ser subconjuntos del catálogo autorizado del servidor. Un cliente no puede ampliar su tenant, permisos o superficies mediante request; el servicio crea un request restringido con el contexto confiable.
- Cada salida deja `GrantsAccess=false`, `AllowsInstitutionalWrite=false`, `CertifiesExternalMeasurement=false` y `AuthorizesProduction=false`; incluso una entrada estructuralmente correcta exige evidencia externa y A5 independiente.
- No se conecta JWT middleware/HTTP/EF/MySQL; `TrustedAuthority` es un contrato para datos previamente verificados, nunca confiere autoridad porque un cliente lo construya. La integración autenticada real es trabajo futuro.

## Pruebas A0–A5
- A0 inspección del diff de Application + tests + documentación + CHANGELOG, sin añadir secretos o cambios de esquema.
- A1 xUnit de happy path estructural y negativas de tenant B, SHA antiguo, catálogo no autorizado, presupuesto, contexto ausente.
- A2 xUnit de denegación sin JWT/membresía/permiso, sin certificación de aislamiento HTTP/EF/MySQL ni WCAG/Playwright real.
- A3 build y xUnit GitHub-hosted en .NET10 y seguridad multiempresa aplicables al SHA exacto.
- A4 Git dev fast-forward con expected SHA, Scope Lock y CHANGELOG V5 SUCCESS exact SHA, readback PLAN/COLA/DAG/BITACORA.
- A5 independiente de otra Task Javier; no autocertificación.

No endpoints nuevos, grants JWT/RBAC live, migraciones, DNS, API productiva, workers, eventos, webhooks, Cloudflare, QA, main o PROD. Rollback con `git revert` normal y nueva CI. La futura integración debe recuperar autoridad desde backend confiable antes de cualquier acceso.
