# SOLQARYN V5 — C13-04.M03.02: caso de uso de consistencia visual por tenant

**Autor:** PRIMARY DEV 05 Javier, TASK_ID `6adc2e55f47c81919f6374e561687076`. Repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuente: STANDARD V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional.

## A0 — Alcance, arquitectura, impacto y rollback

M03.02 implementa un caso de uso **real de Application en .NET 10**, `V5VisualConsistencyApplicationService`, que orquesta la regla Domain preexistente `V5VisualConsistencyDomainRules` de C13-04.M03.01. Sólo acepta un `VerifiedContext` entregado por un caller backend confiable y un catálogo server-side de pantallas. El `ClientRequestedTenantId` nunca concede autoridad y debe coincidir exactamente con el tenant verificado. Rechaza catálogo vacío, lote nulo, lote vacío, máximo inválido, más de 64 observaciones, y detiene el lote al primer fallo, sin exponer observaciones siguientes.

No añade controlador HTTP, DI, rutas Angular, JWT/RBAC de runtime, permisos, MySQL, migraciones, contabilidad, proveedores, workers, eventos, secretos, despliegues ni datos productivos. Domain sigue independiente de infraestructura; Application referencia solamente Domain. No presupone evidencia de WCAG, Chromium/Playwright, accesibilidad real ni una autorización de liberación. Rollback técnico: `git revert` de un commit directo en dev, con regresión y preservación de BITACORA histórica.

## A1 — Prueba funcional real en xUnit

`V5VisualConsistencyApplicationServiceTests` contiene nueve pruebas [Fact] con escenarios positivos y negativos: dos observaciones válidas dejan evidencia pendiente, autoridad nula/revocada o sin permiso denegada, claim de tenant B rechazado, catálogo nulo o vacío, lote excedido/inválido, segunda observación nula o de tenant B con cómputo de visita limitado, SHA distinto, pantalla no autorizada, teclado/feedback/contraste insuficientes y referencia insegura/PII. La prueba válida retorna `EXTERNAL_BROWSER_AND_A5_REQUIRED`, no `PASS` de navegador ni certificado WCAG.

## A2 — Seguridad y aislamiento

En todo fallo la respuesta es código de denegación sin mutación. El backend tiene obligación de derivar `VerifiedContext` desde autenticación, rol y pertenencia vigentes; **esta microtarea no crea ese adaptador ni lo sustituye**. El catálogo debe ser una allowlist de servidor, no un campo de petición. Observaciones tenant A/B, datos sensibles, pantallas arbitrarias, SHA incoherente y datos no revisables se rechazan mediante Domain. `AuthorizesRelease`, `AuthorizesTenantAccess`, `CertifiesAccessibility` y `PersistsEvidence` permanecen siempre `false`.

## A3 — Integración dirigida, no extrapolación

La integración concreta es Application -> Domain sobre métodos puros. Requiere compilación/test GitHub-hosted .NET 10 (xUnit), seguridad multiempresa y regresión backend sobre SHA publicado. Angular, MySQL, Playwright, HTTP autenticado y ensayo de navegador permanecen `NO_APLICA` a la modificación de código exclusivamente Application sin nueva UI, API o DB. Las pruebas por ejecutar se reportan sólo cuando los jobs terminen en `completed/success`.

## A4/A5 — Publicación, trazabilidad y puertas futuras

Publicación directa fast-forward/CAS a `dev` en un changeset con código, xUnit, documento y sección nueva autocontenida en `CHANGELOG_AI.md`. Después de publicar entrar VALIDANDO antes de A1-A3; verificar `verify-solqaryn-project-scope`, procedencia V5, backend y seguridad causal sobre el SHA exacto. Si un check requerido está queued/in_progress/cancelled/failed, conservar VALIDANDO y TASK_ID del autor, sin suplantar PASS ni anticipar LISTO_DEV. Sólo después de A0-A4 verificadas y readbacks institucionales se permite LISTO_DEV; A5 independiente exige otra de las cinco Tasks Javier. No QA/main/PROD.
