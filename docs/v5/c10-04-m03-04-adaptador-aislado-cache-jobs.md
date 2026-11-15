# SOLQARYN V17/V15 — C10-04.M03.04: adaptador aislado de snapshots para caché, jobs y exportaciones

**Microtarea:** C10-04.M03.04, PLAN_MAESTRO fila 1565. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Entorno:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT. **Riesgo:** R2 (seguridad, tenant, jobs y concurrencia). **Candado de CLAIM:** V17 `SQV17_C10_04_M03_04`, `2000001565`. A5 requiere otra de las cinco Tasks Javier.

## Implementación real y alcance no-autorizante

Se entrega código .NET 10 nuevo `backend/src/Infrastructure/AutomationV5/C1004CacheJobsIsolatedSnapshotAdapter.cs`. Es un **adaptador de snapshot aislado/read-only** de Infrastructure para ejercitar, sin conexiones a sistemas reales, los controles ya existentes del `C1004CacheJobsAuthorizationGate` de Application y `C1004CacheJobsDomainRules` de Domain (M03.01–03).

La captura copia con comparador Ordinal la lista permitida de operaciones: un productor no puede modificar su `HashSet` después de la captura para expandir permisos. Se rechazan IDs, tenant, actor o versión observada que no coincidan con la captura, así como contradicciones internas de identidad, propietario persistido declarado y versión. El resultado nominal **únicamente** exige CAS externo futuro; todas las capacidades de queue, escritura, lease, provider, lectura privada y bypass son permanentemente falsas. La evidencia snapshot que recibe el constructor no autentica JWT ni sustituye permisos, negocio, tenant o ownership recuperados del backend real.

La implementación **NO** agrega tablas, migraciones, entidades EF, índices, conexiones MySQL, cliente R2, caches reales, workers, cron, webhooks, backup/restore, endpoints, DI de API, recursos productivos ni grants. No accede a Aiven/Cloudinary/Cloudflare ni lee inventarios externos.

## A0–A4 por riesgo

**A0:** una unidad R2: adaptador Infrastructure, 12 pruebas xUnit, este documento y sección autocontenida CHANGELOG_AI.md. Las puertas PLAN/COLA/DAG se revalidan por ID, con owner exclusivo y named-range V17. Dependencias previas M02.01–06 y M03.01–03 se conservan, sin sobrescribir código ajeno.

**A1:** `backend/tests/Solqaryn.Tests/Infrastructure/C1004CacheJobsIsolatedSnapshotAdapterTests.cs` contiene doce `[Fact]` reales sobre el adaptador y el gate. La prueba positiva termina en `PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED`, sin efectos. Negativas para request/ID, tenant A/B, actor, versión, snapshot adulterado, permiso o membresía revocada, service grant, mutación maliciosa del allowlist tras captura, hint del cliente, payload SHA, deadline, reintentos y cancelación. El número de pruebas **no acredita PASS** hasta CI real.

**A2:** reutilización de validadores existentes de autoridad, operación allowlisted, propietario, grant y rol; controles multiempresa/security/authorization por SHA son obligatorios. Ningún snapshot concede acceso incluso si un proveedor malicioso declara identidad positiva; no se atribuye JWT HTTP real, EF o control plane live.

**A3:** compilar backend .NET10 Release y ejecutar realmente xUnit con el proyecto Infrastructure enlazado a Application/Domain. `Category=Integration` MySQL 8.4, navegador y proveedores reales son **NO_APLICA al diff que no usa EF, HTTP, API, DB ni browser**; no son sustituidos por mocks para una certificación productiva.

**A4:** publicación por fast-forward CAS a dev, Scope Lock y procedencia V5 `completed/success` exact-SHA, build y pruebas backend, seguridad/authorization, diff/blobs íntegros, PLAN/COLA/DAG/BITACORA readbacks. Cualquier FAIL o queued/in_progress deja VALIDANDO bajo owner y bloquea LISTO. A5 independiente por TASK_ID distinto, sin autoprobación.

## Limitaciones, defectos y reversión

Aprobación del adaptador nunca certifica autenticación JWT de servicios, ownership real de archivos, concurrencia transaccional de MySQL, un lease CAS real, cifrado R2, restauración, seguridad de URLs o colas productivas. Estos riesgos de integración requieren tests `Category=Integration` reales en MySQL efímero y/o proveedor cuando se desarrolle el componente autorizado; no se declara sin incidentes. Si el proyecto exige autorización a proveedor, falla cerrada hasta nueva acción específica y revisión independiente.

Rollback mediante `git revert` normal del único commit después de cotejar HEAD, BITACORA append-only, sin force-push ni manipular información histórica. Sin escritura QA/main/PROD, secrets o datos externos.
