# SOLQARYN V16 — C03-04.M03.03: puerta Application de autorización de incidentes

**Autor:** PRIMARY DEV 02 — JAVIER (Task `6af16bb9492c8191b8e8c7d606f11507`). **Repo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación V15:** R2 seguridad/JWT/RBAC/tenant. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL en Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y DAG/PLAN/COLA vigentes.

## A0 — entrega e impacto

- Código nuevo `V5ContinuityIncidentAuthorizationGate` y contrato `IV5ContinuityIncidentVerifiedEvidenceReader` en Application. Componen `V5ContinuityIncidentDomainRules` (M03.01) y `V5ContinuityIncidentApplicationService` (M03.02), sin modificar Domain, API, Infrastructure ni persistencia. Test xUnit nuevo en Solqaryn.Tests.
- La fuente de evidencia futura debe resolver sesión JWT ya autenticada, actor, empresa, owner persistido, permisos/roles/revocación, auditoría e incidente desde backend. El lector de interfaz y los flags de prueba **no son** prueba de JWT firmado, servicios conectados o DB real: no registrar la clase en DI ni conceder acceso real con datos suministrados por cliente.
- Se rechazan SHA inválidos o stale, tenant override, rol cliente, exportación, solicitud de restauración, id/actor/tenant no coincidentes, permisos actuales ausentes, revocación, fallo del lector y condiciones de Domain (sucursal, versión, idempotencia).
- **No se otorga evidencia, escritura, revocación, restauración, tokens, proveedor ni acceso cross-tenant.** Incluso la ruta positiva produce exclusivamente `AUTHENTICATED_INCIDENT_RUNTIME_ADAPTER_REQUIRED`, y cinco propiedades explícitas de la decisión nunca otorgan privilegios.
- No migraciones EF/MySQL, endpoints HTTP, Cloudinary, workers, backups/restores reales, cambios UI, secretos, QA/main/PROD. Rollback seguro: `git revert` del commit causal en dev, preservar BITACORA y estado institucional.

## A1–A3 — pruebas y límites

- A1 xUnit: positivo de preflight sin grants y negativos para solicitud nula, ID malformado/frontera 129, SHA no canónico, lectura fuente indisponible, SHA distinto y comando nulo. Tests deben fallar si el gate permite un grant accidental.
- A2 xUnit: aislamiento A/B con owner persistido, actor discrepante, tenant hint/override, revocación JWT/membresía/permiso, rama ajena, evidencia de auditoría faltante y exportación/restauración prohibidas. No confundir mocks con JWT firmado live.
- A3 integration: se compone realmente la lógica Application/Domain bajo tests xUnit en GitHub Actions para .NET 10 y se exige build Release y seguridad multiempresa/autorización pertinente. MySQL Category=Integration **NO_APLICA**: no se toca EF, DDL, repositorio ni DB; HTTP y Playwright **NO_APLICA** por ausencia de API/frontend modificados. No atribuir validación live de tokens, servicio, SMTP o backup.
- La fuente externa futura debe revalidar autoridad y propiedad desde runtime, transacciones idempotentes/compare-and-swap y evidencias; todo uso con cambio de persistencia o integración exige nuevo alcance R2 y pruebas reales.

## A4–A5 y aceptación

Publicar en un único commit fast-forward/CAS en dev con cuatro archivos (código, pruebas, documento y CHANGELOG autocontenido). Certificar `verify-solqaryn-project-scope` y V5 CHANGELOG, compilación .NET 10, xUnit, backend release y jobs de seguridad/autorización necesarios `completed/success` sobre SHA exacto o equivalencia auditada completa. CI en progreso no es PASS y obliga dejar VALIDANDO con owner, SOURCE_SHA y RUN. No existe PASS de JWT live/HTTP/MySQL por este scope. Al acreditar A0–A4, LISTO DEV y readback BITACORA; solicitar A5 independiente de una de las otras cinco Tasks Javier (autor distinto) sin autoprobación.

**Rollback:** git revert ordinario, sin force-push ni borrar historia; A5 distinto si cambia SHA/artefacto. Ningún permiso de QA/main/PROD.
