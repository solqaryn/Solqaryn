# SOLQARYN V5 — C12-04.M02.05: señales conceptuales, idempotencia y límites de cutover MySQL

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Fecha:** 2026-11-15. **Repositorio:** solqaryn/Solqaryn, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. **Clase:** contrato DOCUMENTAL e INERTE.

**NO IMPLEMENTAR EVENTOS TODAVÍA.** No se implementan emisores, consumidores, webhook, dispatcher, outbox nuevo, scheduler, Task, migraciones EF, DDL, backups, restores, API ni contratos C# ejecutables. Los eventos descritos son señales candidatas de una futura migración autorizada, no señales emitidas realmente. No se conecta Hostinger, Cloudflare, Aiven ni infraestructura externa.

**Autoridad de requisitos:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA) y Sheet PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA (1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU). Se confirmó PLAN H=PENDIENTE/I vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI para C12-04.M02.05, tras M01.01–06 LISTO con A5 independiente. M02.05 es independiente de otros M02 y no habilita terminal M02.06 ni M03.

## 1. Fuentes y fronteras verificables

| Fuente versionada | Blob de referencia | Límite real |
| --- | --- | --- |
| backend/src/Infrastructure/Persistence/AppDbContext.cs | c4b32f6637f1b4426655bb0cd12af8dd9d229866 | Existen superficies Verified* tenant para EF, no un contexto de migrador externo autorizado. |
| backend/src/Infrastructure/Solqaryn.Infrastructure.csproj | 6210d8b12abd77171d95d17e2292c1a319b89263 | net10.0, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9 versionados; no confirma versión ni topología del destino. |
| backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj | 4fb87115e0c1e38a436d777c898341068c6cc668 | Hay proyecto EF de migraciones; no acredita ejecución real de DDL. |
| docs/v5/c12-04-m01-03-inventario-codigo-evidencia-mysql-cutover.md | 4da1375d688b2b96ce09f375baf3993866f18451 | 16 fuentes y G01–G14 de evidencia faltante; inventario de código no significa inventario de datos. |
| docs/v5/c12-04-m01-06-bloqueos-rollback-liberacion-mysql-privado-cutover.md | 0e1d0699054dc5039a29b766db75bf92aa754ed9 | Gates B01–B16, 12 pares P/N y 10 rollback futuros, no implementados. |

Estas huellas son blobs de archivos, no checksums de datos ni ensayos de restore. JWT/RBAC, tenant A/B, contexto Trusted Verified*, permisos y compatibilidad deben comprobarse realmente en una fase técnica posterior.

## 2. Sobre lógico mínimo de señal FUTURA_NO_EMITIDA

| Campo conceptual | Condición fail-closed |
| --- | --- |
| EventId | Identificador opaco único generado por backend |
| SchemaVersion | Versión allowlist; consumidor futuro rechazará desconocida |
| PlanId | Identidad del plan de migración aprobado, no input libre de usuario |
| SourceEnvironment | Ambiente fuente autorizado; no inferir PROD |
| TargetEnvironment | Destino efímero permitido para ensayo, nunca QA/PROD por documento |
| TenantScope | Empresa verificada desde origen y permiso; no host/query/header |
| ActorId | Operador autenticado con autorización activa y auditada |
| Operation | Acciones predefinidas sin ejecución de código arbitrario |
| CorrelationId | ID opaco para rastreo y límites de tamaño/profundidad |
| CausationId | Referencia causal sin recursión infinita |
| IdempotencyKey | Plan + tenant + operación + version + hash de payload |
| PayloadSha256 | SHA de contenido canónico calculado desde origen confiable |
| CreatedUtc | Timestamp de servidor, no navegador |
| DeadlineUtc | Vencimiento explícito, posterior a CreatedUtc |
| Attempt/MaxAttempts | Contadores y límite finito; idempotencia por reintentos |
| ResultCode | Código seguro sin secretos, SQL, PII ni dumps |

Las propiedades son **diseño**, no payload, DTO, tabla ni outbox real. Una señal jamás sustituye un grant IAM, autorización de migración, prueba CI, A5_PASS, cambio de PLAN o orden humana para datos.

## 3. Diez señales CANDIDATAS futuras

| Señal de diseño | Posible emisor futuro autorizado | Condición para no inferir éxito |
| --- | --- | --- |
| CutoverPlanReviewed | Auditor técnico del plan | Revisión no aprueba ni ejecuta cutover |
| DataSourceSchemaVerified | Analizador EF/MySQL de fuente aprobada | Código/schema no confirma datos |
| ExportManifestProposed | Planificador con permisos | Nombre de backup no significa artefacto |
| ExportManifestValidated | Verificador de checksum real | Debe cotejar bytes, cifrado y alcance |
| EphemeralRestoreRequested | Operador de ensayo autorizado | Solo destino efímero segregado |
| EphemeralRestoreVerified | Comparador de hash/FK/conteos | Error o diferencia mantiene NO_GO |
| TenantIsolationChecked | Prueba con identidades A/B sintéticas | JWT inválido o lectura B no obtiene PASS |
| MigrationLeaseDenied | Control de migrador único | Segundo migrador nunca ejecuta DDL |
| CutoverWindowApproved | Propietario habilitado tras múltiples gates | Requiere aprobación explícita externa a este documento |
| RollbackEvidenceRecorded | Operador con prueba de reconciliación | No significa rollback real ni habilita DNS |

No se desarrolla transporte o event dispatcher ni se emite ninguna de estas señales por escribirlas.

## 4. Dieciséis invariantes de seguridad y continuidad FUTURAS

- EVT01: toda operación proviene de plan y actor aprobados; ausencia de autorización = NO_GO.
- EVT02: tenant se resuelve de origen backend y recurso; JWT por sí solo o header no concede propiedad.
- EVT03: identificadores de empresa A no permiten leer ni mutar filas, hashes o exportaciones de B.
- EVT04: los tipos de señal y operaciones están en allowlist versionada, no se ejecutan instrucciones de payload.
- EVT05: la idempotencia incluye plan, ambiente, actor, tenant, operación y hash canónico; diferente carga con misma clave falla.
- EVT06: lease/version transaccional debe garantizar un solo migrador; reloj o un GET previo no equivale a CAS.
- EVT07: la revocación de rol, sesión o permiso detiene el siguiente paso y no se ignora por evento pendiente.
- EVT08: retry y deadline son finitos; una falla no crea tareas ni reintentos sin control.
- EVT09: manifiestos y comparaciones deben incluir integridad referencial, conteos, tipo, NULL y hash reproducible.
- EVT10: errores de checksum, FK, diferencias tenant o pérdida de auditoría producen NO_GO, nunca 2xx ficticio.
- EVT11: el primer restore de ensayo se limita a MySQL efímero, con credenciales ajenas a PROD.
- EVT12: cualquier resultado de operación debe evitar tokens, passwords, DSN, PII, dumps y URL firmadas en log.
- EVT13: la ventana de cambio exige freeze autorizado, reconciliación delta y salida reversible verificada.
- EVT14: un cutover futuro necesita aprobación humana explícita y pruebas reales en CI, no solo estados LISTO/A5.
- EVT15: correlación y causalidad se limitan en profundidad y retención, sin llamadas entre Tasks como motor infinito.
- EVT16: una señal conceptual nunca habilita QA/main/PROD, Hostinger, R2, Cloudflare, DNS o migraciones reales.

## 5. Diez pares de criterios FUTUROS_NO_EJECUTADOS

| Caso | Positivo futuro | Negativo que causaría NO_GO |
| --- | --- | --- |
| P01/N01 | Plan autorizado genera señal auditada | Cliente falsifica actor o plan |
| P02/N02 | Manifest válido se comprueba por hash real | Snapshot corrupto se marca restaurable |
| P03/N03 | Tenant A conserva filas propias tras restore efímero | B adquiere registros A por ID |
| P04/N04 | Migrador único reclama lease con CAS | Dos migradores ejecutan DDL |
| P05/N05 | Retry idéntico mantiene un efecto durable | Reuso de key con payload distinto duplica cambios |
| P06/N06 | JWT/RBAC revocado se deniega | Mensaje antiguo revive privilegio |
| P07/N07 | Comparación de conteos/FK concuerda | Divergencia se oculta por canonicalización |
| P08/N08 | Cutover solo tras ventana y aprobación | Estado documental habilita DNS sin humano |
| P09/N09 | Fallo produce evidencia reversible y saneada | Log revela JWT, DSN o PII |
| P10/N10 | Rollback de ensayo reconcilia delta sintético | Datos o pagos se pierden por cambio parcial |

**No se ejecutó ningún escenario.** Para construir o activar un mecanismo en otra fase deberán obtenerse autorización explícita, CI causal .NET10/MySQL8.4 efímero, pruebas negativas A/B, SHA, locks, idempotencia, límites, recuperación y revisor A5 independiente. No se instalarán webhooks ni ejecutores automáticos desde la presente documentación.

## 6. Impacto A0–A5 y reversión

**Impacto actual:** dos archivos Markdown (este docs/v5 y una sección autocontenida en CHANGELOG_AI.md) exclusivamente en dev. Domain/Application conservan lógica de propiedad y negocio, Infrastructure/EF gestiona datos con permiso; API valida JWT/RBAC y Angular no decide tenant ni credenciales. No se alteran repositorios ajenos, secretos, proveedores, endpoints, tareas programadas o datos.

**A0:** autorización, skill, Scope Lock, seis antecedentes M01 LISTO+A5, PLAN/COLA/DAG, ausencia de secretos, diff de dos Markdown. **A1:** verificar 16 campos, diez señales, 16 invariantes y diez pares P/N como contratos, no tests. **A2:** límites tenant A/B, revocación, lease, privacidad, no loops, idempotencia y rollback conceptual. **A3:** coherencia con AppDbContext, EF/MySQL y M01.01–06; CI runtime de DB/backend/Angular = NO_APLICA al diff documental, nunca PASS falso. **A4:** commit fast-forward/CAS en dev, Scope Lock y procedencia V5 exact-SHA completed/success, registrar y leer de vuelta PLAN/COLA/DAG/BITACORA. **A5:** otra Task Javier revisa independientemente el mismo SHA y fuentes, nunca autoría propia.

**Rollback documental:** git revert normal tras revalidar HEAD y conservar eventos históricos. No hay operación sobre DB, DNS, cutover o datos que revertir porque esta entrega no las ejecuta.
