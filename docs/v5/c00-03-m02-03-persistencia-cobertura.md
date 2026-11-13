# SOLQARYN V5 FINAL — C00-03.M02.03: persistencia, índices, migración y datos de prueba de cobertura

**Tipo:** 1, contrato de diseño documental, **NO** migración, SQL ni cambio de base de datos. **Autor:** SOLQARYN PRIMARY DEV 01 — JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuente ejecutable:** Google Doc V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` y Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

**Resultado exigido de la fase:** convertir cada requisito previo en mejora, contrato, prueba y criterio de cierre dentro del orden V5, con matriz de cobertura integrada sin nueva cola histórica. Esta M02.03 diseña el almacenamiento de correspondencias entre requisitos R01–R12, los 2.340 IDs existentes, contratos, pruebas, evidencias y faltantes. No declara completadas las 2.340 correspondencias ni crea tablas, jobs, cuentas, endpoints o cambios de tenant.

## 1. Anclajes de fuente auditables

| Dependencia real de dev | Blob SHA leído | Función del contrato |
| --- | --- | --- |
| `docs/v5/c00-03-m01-06-cierre-matriz-integrada.md` | `8f83554e0526089c82cf730abb144e26d0b7da66` | Matriz R01–R12, salida del lote M01 |
| `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md` | `b6b16dfa1775ef07666d108580f6b21efe9b081a` | Identidades, ownership e invariantes de cobertura |
| `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md` | `803685cc695660ce2fadee3175b5d97e40e3f3ab` | DTO y errores lógicos, no endpoints desplegados |
| `docs/v5/c00-03-m01-05-acceptance.md` | `40f78cf1b69563f21a3fb8210de3001bac3afbec` | Aceptación del diseño, no suite runtime |

En la lectura previa de PLAN, `C00-03.M01.01–06` y `C00-03.M02.01–02` estaban LISTO, `C00-03.M02.03` PENDIENTE sin owner y `COLA!G/O=SI`. El ID no tiene excepción individual de DAG: conservar gate de fase existente, sin liberar M03/M06 por el solo contrato.

**Persistencia institucional vigente:** `PLAN_MAESTRO!H` define estado y `!I` responsable, `COLA!E` deriva por fórmula (no escribir un literal), `COLA!G/O` son condiciones y no permisos, `DAG_TECNICO!A:G` documenta excepciones validadas y `BITACORA!A:Z` tiene 26 columnas con EVENT_ID e IDEMPOTENCY_KEY únicos. Los registros históricos son apéndices, nunca otra fuente de despacho. `TAREAS_PROGRAMADAS!AA:AN` registra solamente la última señal del ejecutor, no autoridad transaccional.

## 2. Modelo futuro propuesto: entidades, claves e índices (NO IMPLEMENTADO)

| Relación conceptual | Campos clave e integridad | Índice propuesto / riesgo |
| --- | --- | --- |
| SourceRequirement | `source_ref, source_version, requirement_key, scope_hash` | UNIQUE(source_ref, source_version, requirement_key); cambios de fuente no sobrescriben la versión anterior |
| WorkItemReference | `microtask_id` de PLAN, FK lógica al catálogo fijo | UNIQUE(microtask_id); no generar ID 2341 ni copiar estados H/I |
| CoverageLink | `source_ref,source_version,requirement_key,microtask_id,kind` | UNIQUE por tupla; índice por microtask_id para consulta muchos-a-muchos sin duplicar trabajo |
| ContractReference | `microtask_id, contract_path, blob_sha, status` | índice por microtask_id y huella; no afirmar que se desplegó un DTO |
| TestEvidence | `microtask_id,test_kind,fingerprint,commit_sha,job_url,verdict` | UNIQUE(microtask_id,test_kind,fingerprint,commit_sha); exigir SHA de pruebas reales aplicables |
| CoverageGap | `requirement_key,microtask_id,missing_contract,missing_test,reason` | índice por requisito y estado; un faltante permanece explícito, sin generar éxito automático |
| ReviewEvidence | `microtask_id,published_sha,author_task_id,reviewer_task_id,verdict` | UNIQUE(microtask_id,published_sha,reviewer_task_id); autor != revisor y ambos Javier autorizados |
| AuditEventMirror | `event_id,idempotency_key,timestamp_utc,source_hash` | UNIQUE(EVENT_ID), UNIQUE(IDEMPOTENCY_KEY); solo réplica futura, BITACORA A:Z sigue siendo autoridad |

Estos nombres y claves son **propuesta de diseño**, no esquemas MySQL existentes. Una futura decisión de usar EF Core/Oracle MySql.EntityFrameworkCore o un almacén alternativo exige microtarea ejecutable, migración versionada, prueba real y autorización pertinentes. No inferir que Sheets aporta un compare-and-swap atómico: lectura más `batchUpdate` no es un lock transaccional. Los cambios de owner de otra Task necesitan exclusión efectivamente demostrable o se bloquean.

## 3. Migración futura reversible (plan, NO ensayo real)

1. Preflight autorizado con encabezados y fórmulas exactos, 2.340 IDs únicos, DAG, mapeo R01–R12 y 26 columnas de BITACORA; identificar PII y excluir valores de credenciales/JWT/tenant A/B de cualquier export o log.
2. Construir snapshot versionado **sin escribir producción**; validar consistencia de claves, cobertura incompleta y diferencias con fuente canónica, conservando metadatos de procedencia.
3. Crear un adaptador únicamente en una futura microtarea aprobada: transacciones, índices, migraciones forward/reverse, aislamiento tenant A/B y tests dirigidos .NET + MySQL 8.4 real en GitHub Actions, sin presentar mocks como base viva.
4. Verificar lectura dual y checksums antes de cualquier conmutación. Ante retraso, duplicado, pérdida de evidencia o CAS no resuelto, cancelar conmutación; estado H/I sigue en Sheets.
5. Plan de reversión: deshabilitar el adaptador con autoridad explícita, retornar a fuente Sheet inalterada y conservar BITACORA/eventos, sin borrar historial, sin rollback sobre datos reales desde esta tarea.

**Datos de prueba futuros (no insertados):** fuente `R01` versionada ligada a dos IDs existentes; un contrato con SHA válido; un caso de falta de evidencia; un evento repetido con la misma clave idempotente; rechazo de ID inexistente y Task no autorizada. Sus identificadores son ficticios de diseño, no lecturas live ni ejecución de MySQL.

## 4. Oráculos documentales positivos y negativos (NO pruebas de runtime)

| Caso | Expectativa de contrato |
| --- | --- |
| P01 | Un requisito R01 versionado se vincula con un ID real del plan y un blob verificable, sin duplicar la microtarea |
| P02 | La fuente R02 se vincula con dos mejoras existentes: cardinalidad muchos-a-muchos sin crear filas extra en PLAN |
| P03 | La matriz conserva faltantes de pruebas y contratos como PENDIENTE, sin certificar cobertura completa |
| P04 | Estado y dueño se leen de PLAN H/I, COLA E deriva por fórmula y BITACORA A:Z conserva la trazabilidad |
| N01 | Se introduce el ID 2.341 o una segunda fuente mutable de estado: rechazar |
| N02 | Un EVENT_ID o IDEMPOTENCY_KEY existente se reenvía: rechazar segundo append |
| N03 | H=VALIDANDO de dueño ajeno sin CAS demostrado: rechazar segundo claim, no sobrescribir |
| N04 | Se presenta un check SKIPPED o de SHA anterior como prueba de persistencia: NO PASS |
| N05 | Se afirma ejecutar migración real/rollback live con solo este Markdown: rechazar certificación |
| N06 | Falta predecesor M02 real o DAG es NO: bloquear liberación aunque COLA O=SI |
| N07 | Se promete A5_PASS propio o de cuenta ajena: rechazar por independencia |
| N08 | Se publica token JWT, cadena de conexión o PII en evidencia: rechazar |
| N09 | Tenant A intenta leer B por UI, header o flag: backend debe denegar; no inferir aislamiento de esta documentación |
| N10 | Se añade una cola nueva de trabajo histórico o se altera QA/main/PROD sin orden: NO_GO |

La matriz sirve para probar que el **diseño** contiene condiciones falseables; no constituye pruebas HTTP, MySQL, EF migrations, Playwright ni verificación de rollback real.

## 5. Clasificación de impactos y cierre

`requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`. Cambios previstos: solo este Markdown y `CHANGELOG_AI.md` aditivo. No se modifican Domain/Application/Infrastructure/API, DTO C#, Angular, config, secretos, base, migraciones, workers ni infraestructura. Consumidores: M02.04–06 y futuros M03/M05, sujetos a sus propios gates.

A0: revisar texto, referencias SHA, diff, espacios y secretos detectables. A1: verificar P01–P04 y N01–N10 como oráculos documentales. A2: fronteras JWT/RBAC/empresa A/B, identidad autorizada, PII/secretos, A5 independiente. A3: compatibilidad con M01.06 y M02.01/02 y readback PLAN/COLA/DAG/BITACORA; pruebas .NET/MySQL/E2E no aplican a **este diff**. A4: commit serializado fast-forward `dev`, diff y blobs SHA, `verify-solqaryn-project-scope` SUCCESS en el mismo SHA, readbacks; A5_PENDIENTE de otra Task Javier antes de LISTO_DEV, A5_PASS independiente para gates previos a QA/PROD y autorización aparte. Rollback de **este contrato documental**: `git revert` posterior con HEAD y dependencias revalidados, sin reset/force-push ni borrar la BITACORA.
