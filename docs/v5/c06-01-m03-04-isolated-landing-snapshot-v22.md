# SOLQARYN V22 — C06-01.M03.04: snapshot de borradores aislado MySQL 8.4

Repositorio exclusivamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Responsable histórico de esta microtarea DEV03 (TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`). Riesgo R2 por persistencia y aislamiento. Acción concreta PLAN D881: **Implementar persistencia, migración reversible o adaptador aislado**. PLAN E describe el objetivo acumulado M03 (sitio público y SEO), que no se acredita ni habilita mediante esta microtarea aislada.

## A0 — alcance e impacto
`C0601IsolatedLandingMySqlSnapshotAdapter` es un lector read-only con conexión ADO.NET explícita, que consulta **solo metadatos de borrador** de `V22C0601LandingDraftIsolated`, filtrados por `TenantId` derivado/verificado previamente por servidor y `PageSlug` canónico. No acepta host ni rol del navegador como autoridad. La factoría de conexión debe suministrarla únicamente el test aislado; **no existe registro en DI de la API ni endpoint de negocio**. La tabla solo se crea dentro de un esquema UUID de MySQL efímero del runner, que se destruye al finalizar. No modifica `AppDbContext`, migraciones ni ninguna base Aiven.

La lectura usa consultas parametrizadas, transacción `REPEATABLE READ`, validación tenant+slug+revisión positiva+estado permitido, rechazo de filas duplicadas y rollback sin escritura. En caso de excepción, invalidez o proveedor ausente retorna `null` sin filtrar datos privados. Una cancelación solicitada se propaga. La respuesta siempre incluye cuatro capacidades **falsas**: publicar, leer privado/otro tenant, certificar legal/SEO o efectuar mutaciones de negocio.

## A1/A2/A3 — evidencia requerida
La prueba dirigida `C0601M0304IsolatedLandingMysqlTests` emplea **MySQL físico 8.4 efímero** en GitHub-hosted y verifica `SELECT VERSION()`, dos empresas A/B con slug igual pero revisiones distintas, lectura positiva aislada, slug desconocido, tenant ajeno, identificadores inválidos, inyección/carácter no canónico, cancelación, versión cero, estado inválido, tabla ausente y 0 mutaciones antes/después. Workflow `v22-c06-01-m03-04-isolated-mysql84.yml`: exigir exact-SHA, `verify-project-scope.mjs`, Release y TRX con total≥1, 0 FAIL y 0 NotExecuted. `Category=Integration` **no** se acredita con filtros que excluyen integración.

A2 JWT/HTTP/tenancy autenticada real **NO_APLICA a este diff test/adapter aislado no conectado al runtime**, pues no modifica autenticación, rutas ni productos. No confundir la prueba MySQL real con autorización JWT/HTTP, ni reusar este PASS como prueba de ellas. El diseño depende de un tenant ya verificado por servidor y deliberadamente no concede permisos.

## A4, A5 y limitaciones
Antes de pasar a LISTO, verificar checks requeridos GitHub Actions COMPLETED SUCCESS del SHA pertinente, Scope Lock exacto, V5 procedencia/CHANGELOG, código y test blobs, PLAN/COLA/DAG, BITACORA y CAS V17 con readback. QUEUED/IN_PROGRESS/FAIL/CANCELLED/SKIPPED requeridos NO son PASS. A5 exige revisión genuina por Task Javier distinta de la autora DEV03 después de A0–A4. Si CI falla, reparar la causa en dev y repetir exact-SHA sin desactivar checks.

Esto **no** implementa ni certifica páginas públicas productivas, SEO, dominios, contenido legal, JWT de producto, RBAC/tenant comercial vivo, migrations Aiven, API HTTP, worker, pagos ni M03.05/M03.06. R3 NO_GO, QA/main/PROD y secretos permanecen prohibidos. Rollback con `git revert` normal del cambio aislado, sin force-push y sin tocar BITACORA histórica.
