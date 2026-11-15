# SOLQARYN V5 FINAL — C03-03.M02.03: persistencia, índices, migración reversible y fixtures de auditoría

**Tipo:** contrato documental M02.03; NO crea tablas ni ejecuta SQL. **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Ámbito:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fuentes:** Standard de Implementación de Automatizaciones V5 FINAL, PLAN/COLA/DAG, M01.01–06 y M02.02. **Objetivo futuro:** auditoría de autorizaciones, denegaciones, cambios críticos y accesos excepcionales, sin secretos y aislada por empresa.

## 1. Baseline y límites del diseño

`RegistroAuditoria`, `AuditoriaService`, `AuditoriaRepository` y `AppDbContext` son componentes existentes; `AuditoriaService.RegistrarAsync` es best-effort y `RegistrarEstrictoAsync` propaga fallo. El repositorio `GetFilteredAsync` inventariado en M01.03 no enseña por sí solo filtro explícito `EmpresaId`, por lo que deben verificarse filtros globales EF y HTTP completos, no declarar una fuga supuesta ni certificar aislamiento sin pruebas. `EmpresaId` nullable exige tratamiento explícito de eventos de plataforma. La arquitectura vigente usa ASP.NET Core 10, EF Core 10.0.12 y `MySql.EntityFrameworkCore` 10.0.9, con MySQL 8.4 en la verificación pertinente. Ningún proveedor se cambia por este documento.

**Prohibición del scope:** no modificar esquema, EF migrations, SQL, rows productivas, retención ni permisos IAM; los modelos siguientes son propuestas de contrato para M03/M05 y sólo se materializan con autorización, respaldo, regresión y rollback real.

## 2. Agregados y ownership propuestos

| Elemento lógico | Clave/propietario propuesto | Invariante fail-closed |
| --- | --- | --- |
| AUD01 AuditEvent | ID opaco server-side, `TenantId` acreditado o ámbito explícito plataforma | No unir global a tenant por `NULL`; no publicar registro B a A |
| AUD02 AuditActorRef | Referencia minimizada actor + source auth backend | No almacenar JWT/contraseña/cookie/PII no imprescindible |
| AUD03 AuditTarget | tipo y hash opaco de recurso, tenant y sucursal verificadas | BOLA/IDOR y sucursal ajena denegadas |
| AUD04 AuditDecision | acción, outcome, policy version, UTC servidor | Una decisión grabada nunca autoriza otro request |
| AUD05 AuditCorrelation | ID ASCII saneado ≤64, request/server trace | No equivale a idempotency key, permiso ni identidad |
| AUD06 AuditChangeDigest | digest/redacción before/after y clasificación | Sin serialización arbitraria de payloads privados |
| AUD07 StrictWriteEnvelope | ID operación, regla crítica, marcador transaccional | Sin COMMIT de efecto crítico cuando auditoría exigida falla |
| AUD08 AuditRetentionPolicy | scope, clase, vigencia, versión aprobada | Ausencia de política no autoriza purga o export |
| AUD09 ExceptionalAccessGrant | actor, tenant exacto, aprobador distinto, razón, issued/expires UTC | TTL acotado, revocación y MFA/reauth verificables |
| AUD10 ReadScope | tenant, membership y `Auditoria.Ver` comprobados | Filtro de ámbito antes de paginar o agregar |
| AUD11 RedactionRuleVersion | catálogo de campos sensibles y versión | Nunca permitir salida de credencial por fallback |
| AUD12 AuditFailureSignal | correlación saneada, outcome, clase de fallo | No filtrar stack, conexión, claves o payloads en error |

No se afirma que estas doce entidades/tipos sean clases EF actualmente presentes. Si se usan tablas o columnas, los FK y constraints se diseñarán sobre el esquema real con índices compatibles y pruebas de migración efímeras.

## 3. Índices y consultas propuestas (sin DDL)

| Índice/consulta futura | Beneficio esperado | Restricción y verificación |
| --- | --- | --- |
| IX01 `(TenantId,OccurredAtUtc,EventId)` | Paginación descendente empresarial | Tenant efectivo de servidor, orden estable y cursor tenant-bound |
| IX02 `(TenantId,ActorRef,OccurredAtUtc)` | Buscar actor en un ámbito | Proyección pseudonimizada, no revelar existencia de B |
| IX03 `(TenantId,ActionCode,OccurredAtUtc)` | Consultar decisión/acción | Clave de catálogo allowlisted, sin wildcard masivo |
| IX04 `(TenantId,Outcome,OccurredAtUtc)` | Denegaciones y errores acotados | Límites de cardinalidad/paginación, no scans globales |
| IX05 `(TenantId,ResourceType,ResourceRef,OccurredAtUtc)` | Evidencia por recurso | ID opaco y ownership A/B antes de lectura |
| IX06 `(TenantId,CorrelationId,OccurredAtUtc)` | Traza restringida | CorrelationId no autoriza ni deduplica efectos |
| IX07 `(ScopeClass,RetentionClass,OccurredAtUtc)` | Selección para retención aprobada | Nunca borrar por `NULL TenantId` sin clasificación explícita |
| IX08 clave lógica de operación estricta | Detectar reintentos definidos por servidor | No confundir `X-Correlation-ID` con clave única |

Índices **candidatos**, no comprobados mediante EXPLAIN ni benchmark; debe revisarse coste, longitud de clave/collation, cardinalidad, bloqueo y plan de consulta en MySQL 8.4 efímero. Las lecturas de alcance plataforma requieren permiso específico separado, jamás `TenantId IS NULL` como fallback global.

## 4. Plan reversible futuro (NO ejecutado)

1. Inventariar versión exacta de AppDbContext, filtros globales, migraciones existentes, volumen y retención aprobada; obtener respaldo y punto de restauración verificables en ambiente efímero.
2. Crear migración **aditiva** con columnas/tabla nuevas solo si un requisito no se satisface con modelo actual; mantener compatibilidad hacia atrás y no habilitar nueva escritura automática antes de pruebas.
3. Poblar fixtures sintéticos A/B y plataforma, con actores, timestamps, denegaciones, cambios críticos y canary de secreto ficticio para demostrar redacción; jamás copiar producción.
4. Validar filtro de pertenencia en HTTP + SQL real, roles, 401/403/404, keyset pagination y casos de `EmpresaId=null`; analizar índices con EXPLAIN, no inferir rendimiento.
5. Simular caída DB/timeout durante auditoría strict y comprobar rollback de operación crítica; best-effort puede fallar sin afirmar durabilidad.
6. Comprobar reintentos, unicidad real, transacciones, auditoría inmutable, límites de lectura y reversión en MySQL efímero con esquema versionado.
7. Ejercitar rollback por migración compensatoria o restauración previamente aprobada; prohibido `DROP` automático de datos legítimos o alteración de base productiva.

**Condición de NO_GO:** pérdida de datos, lectura B por A, retención sin política, audit strict con efecto parcial, cifrado/PII comprometidos, schema drift sin rollback o pruebas requeridas canceladas/fallidas.

## 5. Fixtures y escenarios causales FUTUROS_NO_EJECUTADOS

| Caso | Signo | Dato/estimulo sintético | Oráculo futuro |
| --- | --- | --- | --- |
| P01 | + | Admin A permiso Auditoria.Ver, eventos A | SELECT/HTTP devuelve solo A ordenado |
| N01 | - | JWT A + filtro/ID B | No filas, conteo, PII ni correlaciones de B |
| P02 | + | Admin B equivalente | Solo registros B, sin mezcla de caché |
| N02 | - | Membresía A revocada | 403/404 seguro sin lectura |
| P03 | + | Cambio crítico + DB operativa | Efecto y auditoría strict consistentes en commit |
| N03 | - | DB inaccesible en auditoría strict | Efecto revertido/fail-closed |
| P04 | + | CorrelationId saneado | Registro enlaza request sin autorizar |
| N04 | - | Cabecera CRLF/longitud >64 | No log injection ni persistencia maliciosa |
| P05 | + | Consulta por cursor A | Resultado de paginación estable, sin duplicaciones |
| N05 | - | Reutilizar cursor A con B | Rechazado/no datos A |
| P06 | + | Retención de clase aprobada | Sólo scope y plazo autorizados |
| N06 | - | Purga sin política o export sin grant | No DELETE/export |
| P07 | + | Acceso excepcional con aprobador ajeno, MFA y TTL | Evento restringido y durable cuando requerido |
| N07 | - | Autoaprobación/grant vencido | Denegar y registrar sin datos sensibles |
| P08 | + | Reintento con clave legítima del servidor | Una sola operación crítica |
| N08 | - | Reutilizar solo correlación como deduplicador | No conceder idempotencia ni eliminar evidencia |
| P09 | + | Evento de denegación con reasonCode | Resultado minimizado auditado |
| N09 | - | Password/JWT de prueba en before/after | Nunca aparece en DB/log/export |
| P10 | + | Restauración fixture efímero | Esquema e integridad recuperados |
| N10 | - | Migración incompatible/rollback sin backup | Bloqueo, no deploy |

Son **20 oráculos futuros**, no pruebas HTTP/SQL ejecutadas en M02.03. Deben ejecutarse sobre SHA de implementación e integrar constraints y filtrado real, no sólo mocks o SQLite in-memory.

## 6. A0–A4 documental y revisión posterior

- **A0:** preflight M01.01–06 LISTO+A5, DAG validado, diff documental limitado a este archivo y CHANGELOG; identificar transacción, seguridad, datos, consumidores, reversibilidad.
- **A1:** revisión estructural de 12 elementos, ocho estrategias de índice y diez pares de oráculos P/N, marcados FUTUROS_NO_EJECUTADOS.
- **A2:** tenant A/B, BOLA, RBAC, política plataforma distinta, no leakage de PII/JWT, auditoría estricta e índices tenant-bound.
- **A3:** consistencia entre contratos API M02.02, modelo de auditoría vivo y arquitectura Domain/Application/Infrastructure; pruebas dotnet/EF/MySQL/Angular/Playwright **NO_APLICA al diff Markdown**, no son PASS.
- **A4:** commit fast-forward/CAS en dev, blobs/diff, GitHub checks Scope Lock y procedencia V5 `completed/success` del SHA exacto, readback PLAN/COLA/DAG/BITACORA.
- **A5:** revisión independiente de otra Task Javier (autor diferente) para cualquier desbloqueo; no auto-A5.

**Rollback de ESTE documento:** `git revert` normal y evento compensatorio, sin borrar historia. **No** migraciones, DDL, backfill, export, cambios JWT, datos reales ni QA/main/PROD.
