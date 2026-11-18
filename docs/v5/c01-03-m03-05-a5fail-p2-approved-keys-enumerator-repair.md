# C01-03.M03.05 — Reparación causal P2 de catálogo backend (A5_FAIL histórico)

**Repositorio:** solqaryn/Solqaryn@dev. **Scope Lock:** STRICT. **Fila institucional:** PLAN_MAESTRO 234. **Owner y autor de la microtarea original:** DEV06 (6af16bbad26881918c283abe06e1f32a), sin transferencia. **Asistencia técnica correctiva:** DEV03 (6adc2e4bd8a48191ac7d8641ed95a1a3). **Riesgo:** R2 de manejo de evidencia multiempresa. No hay autorización de producción.

## Defecto y reparación

La revisión independiente A5 de DEV04 registró un defecto P2: la consulta autenticada `_catalog.ReadAsync` estaba dentro de un bloque de captura, pero la posterior enumeración de `CatalogEvidence.ApprovedKeys` mediante `Any` podía lanzar `InvalidOperationException` sin sanitizar. Un `IReadOnlySet<string>` de proveedor con enumerador defectuoso escapaba de `ProjectAsync` en lugar de devolver `CONFIG_BATCH_CATALOG_SOURCE_UNAVAILABLE`. No hay prueba de explotación HTTP ni exfiltración real.

El cambio conserva todas las comprobaciones anteriores de identidad, membresía vigente, tenant, SourceSha, versión, pertenencia y clave canónica; captura las excepciones ordinarias durante la enumeración y niega el resultado sin metadatos. `OperationCanceledException` se propaga y `OutOfMemoryException` no se oculta. Una clave no aprobada continúa rechazándose con `CONFIG_BATCH_TENANT_CATALOG_REJECTED`.

## Pruebas y alcance

- xUnit adversarial de `IReadOnlySet<string>.MoveNext` que arroja una excepción con mensaje privado, verifica el código sanitizado, cero descriptores y llamada de catálogo restringida al tenant original.
- xUnit negativo para un conjunto sin la clave solicitada; preserva las pruebas existentes positivas, tenant B, revocación, cancelación y errores del proveedor.
- No hay cambio de esquema, endpoints, DTO, JWT, RBAC, migraciones, jobs activos ni integración externa. MySQL/HTTP/Chromium NO_APLICA para el diff, no se atribuye un PASS de runtime no ejecutado.
- A4 exige CI GitHub-hosted de backend .NET, seguridad, Scope Lock y V5 `completed/success` sobre SHA correctivo exacto. Cualquier job `queued/in_progress/skipped/failed` requerido NO es PASS.
- Conserva `PLAN H234=LISTO`, `I234=DEV06`, A5_FAIL anterior y BITACORA histórica. No implica A5_PASS; un revisor Javier distinto de DEV03 y del autor del código debe revisar independientemente con SHA válido.
- **Rollback:** `git revert` del SHA de reparación, previa verificación de HEAD y escritores; nunca reset/force-push. No QA/main/PROD ni datos productivos.
