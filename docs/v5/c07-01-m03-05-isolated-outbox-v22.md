# SOLQARYN V22 — C07-01.M03.05 cola catalogo aislada MySQL 8.4

Fuente: STANDARD V5 FINAL, PLAN_MAESTRO D1026 (implementar worker, proveedor, cola o integracion cuando aplique). Unidad R2 concreta: cola/adaptador aislado. PLAN E es meta acumulativa de fase, no certifica M03.

A0: Infrastructure añade C0701IsolatedCatalogOutbox, fabrica DbConnection sin DI productiva, tabla exclusiva de CI V22C0701CatalogOutboxIsolated. Lease y ACK acotados con TenantId, ProductId y CatalogVersion comprobados por SQL parametrizado, version de lease y worker verificados por CAS, idempotencia con PK(TenantId,ProductId). El tenant debe provenir del backend autenticado, no de un dato del cliente. No cambian Domain, Application, API, DTO, UI, Aiven, permisos, secretos ni esquemas productivos.

A1/A2/A3: xUnit Integration en MySQL 8.4 efimero verifica dos tenants A/B, lease independiente, version invalidada, clave duplicada, trabajador falso, replay/ACK duplicado, revision caduca, vencimiento y cancelacion; cleanup SQL sólo de filas sandbox. Fail-closed si proveedor falla. No implica JWT/RBAC real en worker.

A4: Workflow GitHub-hosted .NET 10 Release, MySQL 8.4 efimero, TRX ejecutado y todos pass, Scope Lock exact-SHA, V5 y pruebas backend/seguridad/autorizacion pertinentes en SUCCESS. No convertir QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL a PASS.

A5: revisión independiente por otra Task Javier distinta DEV03. Limitacion de fase: no API/producto comercial live, sin provider externo, no esquema/migracion productivo, no pago ni endpoint con JWT. Rollback normal por revert sin force-push. Solo dev, no QA/main/PROD.
