# C08-04.M05.03 — Integración contractual aislada de módulos sectoriales

Autor DEV06, TASK_ID 6af16bbad26881918c283abe06e1f32a. Proyecto solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT, fila PLAN 1288, V17_LOCK 2000001288. Riesgo R2 (tenant, permisos, integración).

## A0 / alcance / rollback
Se prueban juntas implementaciones **reales** de Application (C0804SectorModuleAuthorizationFence y C0804SectorModuleReadOnlyIntegrationBoundary) e Infrastructure (C0804IsolatedTenantModuleSnapshotAdapter), mediante los puertos ya existentes. No se registra DI de runtime, ni endpoint, ni proveedor externo, ni nueva entidad, ni MySQL, EF, JWT o RBAC productivos. La captura sintética jamás puede declararse fuente persistida ni audit fence; una supuesta política confiable debe seguir fallando cerrado. Cambios exclusivos: prueba xUnit, workflow dirigido, documento y CHANGELOG. Rollback: git revert normal fast-forward, sin force-push.

## A1–A3
Seis pruebas xUnit positivas de comportamiento seguro y negativas/adversariales: actor sintético aun confiable no certifica proveedor; tenant B, elevación de privilegio/dispatch, revocación de rol, revisión obsoleta y cancelación. Se verifica contador cero de invocaciones del proveedor (espía sintético), resultado opaco y ausencia de concesión de acceso. A2 acredita rechazo lógico del fence, NO JWT HTTP ni autorización real. A3 es prueba de integración entre clases reales Application/Infrastructure en .NET 10 GitHub-hosted, sin mocks del adapter o del fence. MySQL 8.4/EF y proveedor externo: NO_APLICA al diff sin persistencia ni adaptador vivo; nunca reportar como integración real de base o proveedor.

## A4–A5 y continuidad
Exigir SHA exacto, xUnit Release que ejecute tests (conteo >0) y checks Scope Lock, V5, backend, seguridad pertinentes completed/success. QUEUED, SKIPPED, CANCELLED, IN_PROGRESS no son PASS obligatorios. Mantener VALIDANDO durante CI externa con run/check/SHA y owner; LISTO únicamente con readback. A5 revisor de otra Task Javier. No QA/main/PROD ni credenciales reales.
