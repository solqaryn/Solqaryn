# SOLQARYN V5 FINAL — C00-04.M02.03: persistencia, índices, migraciones y datos de prueba del gobierno de cambios

**Tipo de entrega:** diseño documental del ID existente `C00-04.M02.03`, no implementación. **Autor:** PRIMARY DEV 01 — JAVIER, `TASK_ID=6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Resultado exigido:** definir un contrato de almacenamiento y reversibilidad que distinga hechos institucionales de datos operativos del SaaS. No se crean tablas, consultas SQL, migraciones, repositorios, DTO, endpoint, worker, webhook, Task, credencial ni registros productivos. La autoridad ejecutable es el documento V5 FINAL de Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y el Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Puerta individual, fuentes y precedencia

Los ocho predecesores exactos son `C00-04.M01.01` a `C00-04.M01.06`, `C00-04.M02.01` y `C00-04.M02.02`. `PLAN_MAESTRO!H110:H117` contiene ocho `LISTO`. `K110:K114` acredita cinco revisiones A5 correctivas; `K115:K117` acredita tres revisiones A5 independientes, incluida M02.02 por Javier :50 sobre SHA `2632be79af645346ce9485bd8f9c002d3626996f`. `DAG_TECNICO!A148:J148` limita la puerta `F=VALIDADO/G=SI` al presente ID; `COLA!G118/O118=SI` es condición calculada, nunca permiso de despliegue.

Fuentes reproducibles en HEAD de preflight:
- `docs/v5/c00-04-m02-01-entidades-ownership-invariantes.md`, blob `aaf0e385f2d59440b66e923d355f77721f2917f9`: nueve entidades lógicas, diez invariantes y fuente de estados.
- `docs/v5/c00-04-m02-02-contratos-api-dto-errores-gobierno.md`, blob `94c46293e0e168f20875846f7e8b13e963a752a8`: cinco contratos propuestos, denegaciones y compatibilidad.
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`, blob `c4b32f6637f1b4426655bb0cd12af8dd9d229866`: persistencia operativa real, distinta del Sheet institucional.
- `backend/src/Application/DTOs/AuditoriaDto.cs`, blob `2bc2bc437441d0677a4f86deea7aad743d26fbf6`: auditoría de la aplicación, no sustituto del histórico de BITACORA.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`: EF Core `10.0.12` y `MySql.EntityFrameworkCore 10.0.9`; `backend/src/API/Solqaryn.API.csproj` usa `net10.0`. Estas versiones se contrastan, no se cambian aquí.

## 2. Mapa de persistencia y fuente única

| Conjunto | Fuente autoritativa actual | Identificador y regla | Prohibición de esta microtarea |
| --- | --- | --- | --- |
| Estado y owner de 2.340 microtareas | `PLAN_MAESTRO!A/H/I` | ID existente, transición auditada, como máximo un owner real | No materializar segundo estado escribible ni derivar permisos de una copia |
| Elegibilidad derivada | `COLA!E/G/O` + `DAG_TECNICO!A/F/G` | ID idéntico y gate técnico validado; fallback de fase no basta | No inventar dependencia ni editar fórmula para forzar `SI` |
| Hechos inmutables | `BITACORA!A:Z` | 26 campos, EVENT_ID e IDEMPOTENCY_KEY únicos | No UPDATE/DELETE histórico ni RUN_ID ficticio |
| Código y verificación | GitHub `dev`, commit/tree/blob y check-runs | `head_sha` exacto, conclusión `success` solo si `completed` | No declarar `SKIPPED/CANCELLED/IN_PROGRESS` como PASS |
| Operación SaaS | MySQL/EF del backend vigente | Identidad, autorización y tenant resueltos por servidor | No usar una fila de governance para conceder JWT/RBAC o acceso a Empresa B |

La tabla `RegistroAuditoria`/DTO de auditoría del producto y los eventos institucionales de BITACORA tienen ciclos y controles diferentes: **no son almacenes intercambiables**. Una consulta futura de gobierno puede construir una proyección **solo de lectura** a partir de fuentes verificadas; su cache nunca será fuente de cambios de estado, permisos, aprobación o ownership.

## 3. Modelo lógico FUTURO, índices e integridad — sin DDL

Una implementación futura y autorizada podría usar `GobiernoDecisionProyeccion` con campos conceptuales: `ScopeKey`, `MicrotareaId`, `SourceRevision`, `EstadoObservado`, `OwnerTaskId`, `CommitSha`, `FechaLecturaUtc`, `ValidezEvidencia` y `VersionEsquema`. Otra proyección opcional `IndiceEvidenciaDecision` tendría `EventId`, `IdempotencyKey`, `ScopeKey`, `MicrotareaId`, `ShaCausal`, `RevisionA5` y `FechaUtc`. **No existen como tablas ni entidades EF por este documento**. No incluir tokens, PasswordHash, payload libre de PII, secreto ni aprobaciones inferidas.

Para evaluar más adelante, un índice único lógico `(ScopeKey, MicrotareaId, SourceRevision)` impediría versiones duplicadas de una proyección y otro `(EventId)` / `(IdempotencyKey)` garantizaría unicidad dentro del ámbito institucional; los índices de consulta serían `(MicrotareaId, FechaLecturaUtc)` y `(CommitSha, FechaUtc)`. Son hipótesis de diseño, no instrucciones DDL. `ScopeKey` institucional y `EmpresaId` comercial no equivalen: nunca mapear uno al otro como autorización, y para cualquier dato por tenant exigir binding server-side verificable antes de leer.

Integridad: rechazo de evento duplicado, fuente caducada, estado incompatible o transición sin evento; comparación por versión y relectura de las fuentes antes de actuar, aislamiento transaccional en una futura implementación real. El readback de Google Sheets por sí solo no garantiza CAS transaccional; una operación sensible con concurrencia no resuelta queda en `NO_GO`.

## 4. Plan de migración reversible CONDICIONAL — no ejecutado

**Paso futuro M0:** autorización expresa del scope de implementación, inventario de consumidores y amenazas, esquema efímero, prueba de respaldo/restauración y revisión de impacto tenant. **M1:** agregar estructura opcional aditiva con migración versionada EF/MySQL, sin tocar tablas originales ni estados institucionales. **M2:** poblar una proyección desde eventos ya verificados, con marca de versión, comparador de reconciliación y prohibición de decisiones desde cache. **M3:** habilitar lectura gradual solo tras pruebas de seguridad de empresas A/B, drift, duplicación, compatibilidad y rollback. **M4:** conmutar a lectura institucional original ante cualquier discrepancia y deshabilitar la proyección sin pérdida de BITACORA.

Si se llegara a implementar M1–M4 en otra microtarea, exigir MySQL 8.4 efímero en CI, prueba de migración positiva/negativa, índices y foreign keys reales, prueba de tenant B denegado, readback, plan de downgrade y autorización de datos; ninguna de esas pruebas se ejecutó aquí. No aplicar `ALTER`, `DROP`, backfill o conexión a Aiven/PROD en esta unidad.

## 5. Fixtures simbólicos y oráculos positivos/negativos

Los siguientes son **casos de contrato sobre datos sintéticos**, no filas insertadas ni tests de runtime:

| Caso | Entrada simbólica | Resultado exigido |
| --- | --- | --- |
| P01 | Ocho predecesores LISTO+A5 y gate 148 validado | Solo M02.03 es elegible para contrato documental y readback |
| P02 | Fuente de estado, owner, DAG, BITACORA y SHA coherentes | Proyección de lectura identificable con `SourceRevision`, nunca aprobación automática |
| P03 | Evento sintético único con 26 campos y SHA exacto | Una sola representación lógica auditada y rollback documentado |
| P04 | Diseño más CHANGELOG íntegro, Scope Lock/V5 exact-SHA `completed/success` | Elegible a LISTO_DEV documental, con A5_PENDIENTE de otra Javier |
| N01 | `COLA G/O=SI` sin `DAG F=VALIDADO/G=SI` propio | NO_GO, no claim |
| N02 | Evento repetido, key idempotente repetida o 25 campos | Rechazo; no cambiar BITACORA existente |
| N03 | Tenant B en URL/header, o `ScopeKey` confundido con `EmpresaId` | Denegar lectura; no permitir escalamiento |
| N04 | M02.02 o una dependencia ya no LISTO, A5 causal fallida o SHA incorrecto | Gate deja de habilitar; no avanzar |
| N05 | `SKIPPED/CANCELLED/QUEUED/IN_PROGRESS/FAIL` para check requerido | No PASS ni cierre |
| N06 | Migración productiva, dato real, ejecución de SQL/EF, tablas nuevas o secretos | Fuera de alcance; NO_GO |
| N07 | Revisión A5 por la propia Task autora o ejecutor fuera de seis Javier | Rechazo y evidencia preservada |
| N08 | Cache/worker/webhook/Task→Task pretende ejercer autoridad o crear ejecutor | Rechazo; solo fuentes vigentes y flujo por corrida |

## 6. Auditorías, impacto y reversión

**A0:** comprobar los ocho IDs, blobs y versiones citados, Markdown, ausencia de secretos y procedencia de la sección del CHANGELOG. **A1:** inspección documental P01–P04/N01–N08; ninguna prueba HTTP, SQL o UI fue ejecutada. **A2:** límites explícitos JWT/RBAC/tenant A/B, idempotencia, PII, separación de autor y revisor. **A3:** compatibilidad lógica con M02.01, M02.02, PLAN/COLA/DAG/BITACORA y backend existente sin cambiar consumidores. **A4:** tras publicar fast-forward en dev, comprobar padre, diff, dos blobs, Scope Lock y verificador V5 exact-SHA `completed/success`, readback institucional y rollback.

**Superficies reales alteradas:** solo este Markdown y una sección completa, aditiva de `CHANGELOG_AI.md`; ningún archivo Domain/Application/Infrastructure/API, Angular, MySQL, workflow, migración ni dato del producto. Para A0–A4 documentales, .NET, Angular, MySQL, Docker y Playwright son `NO_APLICA`, no PASS sintético. **A5:** pendiente de una de las otras cinco Tasks Javier con TASK_ID distinto, sin promover QA/main/PROD.

**Rollback:** `git revert` ordinario del commit documental después de verificar dependientes y HEAD. Si hubiera error institucional, añadir evento compensatorio a BITACORA, nunca editar filas históricas ni forzar la rama. M02.04–06 y todas las implementaciones posteriores requieren DAG y pruebas propias.
