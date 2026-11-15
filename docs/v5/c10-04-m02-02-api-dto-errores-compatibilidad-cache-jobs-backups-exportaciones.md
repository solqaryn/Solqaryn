# SOLQARYN V5 FINAL — C10-04.M02.02: contratos API/DTO, errores y compatibilidad de caché, jobs, backup y exportaciones

**Autor:** PRIMARY DEV 05 / JAVIER_CHATGPT / TASK_ID `6adc2e55f47c81919f6374e561687076`. **Proyecto:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA institucional. **Clase:** contrato de diseño M02, no implementación ni exposición de API.

## A0 — Fuentes comprobables y límites de arquitectura

La preparación M01.05 especificó AC01–AC18, N01–N18 y RNF01–RNF12 como **oráculos futuros, NO_EJECUTADOS**. M01.06 definió B01–B14 y rollback sin activar ningún servicio. M02.01 propuso doce objetos conceptuales y OWN01–OWN18; no contiene entidades EF reales. Esta M02.02 define una frontera REST/DTO **propuesta** con adaptación futura independiente, sin romper interfaces existentes. Se contrastaron las siguientes firmas reales en `dev`:

| Fuente Git real | Blob | Compatibilidad y límite |
| --- | --- | --- |
| `backend/src/Application/Interfaces/IPublicStoreCache.cs` | `15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8` | `GetOrCreateAsync<T>(tenantKey,segment,parameters,ttl,factory,ct)`, `InvalidateAll` y `InvalidateTenant`. Es caché **pública**; no expone ni autoriza caché privada ni HTTP de administración. |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | Procesador/retry existentes con política acotada. No equivalen a scheduler universal ni permiso para crear workers o disparar eventos. |
| `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` | `454d588851ecc7db810259e63705852360172928` | Overloads tenant-aware cuyo default niega con `NotSupportedException`; `Url`/`PublicId` legacy nunca son grants de descarga. |
| `docs/v5/c10-04-m02-01-entidades-ownership-cache-jobs-backups-exportaciones.md` | `041ff979d7a3fb94003e2072be922d727eb3c2c1` | Doce entidades conceptuales y ownership; ninguna tabla ni endpoint nuevo. |
| `docs/v5/c10-04-m01-06-gates-rollback-liberacion-cache-jobs.md` | `bbf0bfaa75d46362c2e5504d6f0a1840d27bda18` | Puertas B01–B14 y requisitos de A5/revisión; no autoriza R2, Hostinger o restauración. |

La autoridad tenant procede de sesión autenticada y membresía/propiedad persistida **resueltas por servidor**, jamás del `EmpresaId`, host, header, slug, archivo o claims del cliente sin revalidación. API futura y Domain/Application/Infrastructure se mantendrán separadas; cualquier configuración, migración, operación real o despliegue requiere un ID posterior con gates propios.

## Contratos FUTUROS: operaciones propuestas (no rutas existentes ni aprobadas)

| Operación propuesta | Método/ruta ilustrativa futura | Entrada / resultado propuesto | Denegación crítica |
| --- | --- | --- | --- |
| Consultar job | `GET /api/empresa/trabajos/{opaqueJobId}` | `JobStatusDto` seguro, estado y versión, sin payload secreto | Actor sin permiso o tenant B: 404 no enumerativo |
| Solicitar trabajo permitido | `POST /api/empresa/trabajos` | `CreateJobDto` con `OperationKey` allowlisted, `IdempotencyKey`, `PayloadHash`; `202` sólo tras claim durable futuro | Operación sin rol, cuota o grant: 403/429, ningún enqueue |
| Cancelar trabajo | `POST /api/empresa/trabajos/{opaqueJobId}/cancelar` | `CancelJobDto` con `ExpectedVersion`; resultado pendiente/terminal auditable | Version mismatch: 409/412; el cliente no cancela trabajo de B |
| Solicitar exportación | `POST /api/empresa/exportaciones` | `ExportRequestDto` con `ResourceKey`, filtros allowlisted, formato, límite | Proyección A no puede incluir datos B; fórmulas CSV neutralizadas |
| Consultar exportación | `GET /api/empresa/exportaciones/{opaqueId}` | `ExportStatusDto` con `State`, `ByteCount`, `ContentSha256` saneado | Parcial/incompleta: no entrega URL |
| Descargar exportación | `GET /api/empresa/exportaciones/{opaqueId}/descarga` | Sesión/grant **revalidados en cada acceso**; respuesta privada no-store con TTL limitado | Token vencido, URL reusada, otro método o tenant: 403/404 |
| Consultar manifiesto backup | `GET /api/plataforma/respaldos/{opaqueId}` | `BackupManifestDto` restringido a administrador con MFA y razón futura | Sin reautenticación o ámbito: 403 |
| Preflight restore efímero | `POST /api/plataforma/ensayos-restauracion/preflight` | `RestoreDryRunDto` con entorno **efímero** allowlisted, manifiesto, checksum y aprobación segregada futura | Destino PROD/otro tenant o huella inválida: 403/409; **no ejecuta restore** |

Todas las rutas son **artefactos de diseño, no rutas implementadas, registradas, desplegadas o habilitadas**. En particular ningún endpoint de backup/restore es una autorización administrativa presente; su implantación necesitaría decisión de seguridad y entorno de CI efímero. No hay worker/webhook/dispatcher nuevo ni ejecución cruzada entre Tasks.

## DTO y campos obligatorios futuros

- `TenantAuthorityContext` (INTERNAL-ONLY): `ActorId`, `TenantId`, `MembershipRevision`, `EffectiveGrantSet`, `ResourceOwnerTenantId`, `SourceSha`. Los campos se derivan en servidor y jamás se enlazan directamente al body público; sin contexto verificado => DENY.
- `CreateJobDto`: `OperationKey` de catálogo, `IdempotencyKey` opaca (1–128 ASCII seguros), `PayloadSha256` de 64 hex, `ExpectedVersion`, `DeadlineUtc` acotado. No permite `TenantId` autoritativo, assembly/command shell o URL arbitraria.
- `JobStatusDto`: `OpaqueId`, `State` allowlisted, `Attempt`/`MaxAttempts`, `NextAttemptUtc`, `CorrelationId` saneado, `ETag`/versión. No incluye PII, stack traces, tokens, URLs firmadas ni payload interno.
- `ExportRequestDto`: `ResourceKey`, filtros allowlisted, `Format` CSV/XLSX permitido, `MaxRows`/`MaxBytes` limitados por política **servidor**, `IdempotencyKey`. El cliente no cambia cuotas; CSV neutraliza valores iniciales `=`, `+`, `-`, `@` y caracteres de control relevantes.
- `ExportStatusDto`: `OpaqueId`, estado `SOLICITADA/GENERANDO/CONFIRMADA/RECHAZADA/REVOCADA`, `ContentSha256`, `Bytes`, `ExpiresAtUtc` y `ETag`; no publicId legacy ni ruta privada sin reautenticación.
- `BackupManifestDto` y `RestoreDryRunDto`: ambiente permitido, SHA de esquema, checksums, conteos y referencias opacas, evidencia de autorización/MFA y resultado de prueba; **NO** credenciales, material de claves ni datos productivos.

## Errores, evolución y contrato de seguridad

| Código futuro | Causa | Comportamiento obligatorio |
| --- | --- | --- |
| 400 | DTO mal formado, formato no permitido, filtro o SHA inválido | Respuesta estable sin detalles sensibles y sin side effects |
| 401 | Sin identidad backend autenticada | Ninguna consulta privada o preparación de trabajo |
| 403 | Permiso, membresía, MFA/razón requerida o cuota denegada | Sin bypass por `TenantId` de cliente |
| 404 | Recurso inexistente o ajeno | Evitar enumerar IDs de tenant B |
| 409 | Misma `IdempotencyKey` con payload diferente, conflicto de estado | Sin segundo efecto |
| 412 | Versión/ETag obsoleta | Releer estado y autorización, no reintento ciego |
| 413/429 | Tamaño o frecuencia excedidos | Rechazo con presupuesto y rate limit server-side |
| 503 | Adaptador/almacenamiento/prueba de autoridad no disponibles | Fallar cerrado, sin 2xx falsos ni URL parcial |

Compatibilidad: versionar DTO de forma aditiva; rechazar propiedades desconocidas que pretenden delegar permisos; conservar semántica de `IPublicStoreCache` pública y `OutboxRetryProcessor`; no reinterpretar métodos legacy de storage como contrato autorizado; no activar R2/Hostinger ni nuevo proveedor por flags documentados. El estado de job se obtiene de lectura durable futura, no inferencia por un scheduler ChatGPT. Uso de claves opacas/versiones protege correlación A/B; errores de API no exponen nombres ni información de otra empresa.

## Oráculos P01–P12 / N01–N12 futuros, NO_EJECUTADOS

| Par | Escenario positivo FUTURO | Rechazo negativo FUTURO |
| --- | --- | --- |
| P01/N01 | A consulta status de su job con grant vigente | B con opaque ID de A no obtiene datos |
| P02/N02 | Creación idempotente idéntica responde una sola operación | Misma clave y payload distinto no duplica trabajo |
| P03/N03 | Retry transitorio respeta backoff, tope y lease | Dos consumidores no hacen doble efecto |
| P04/N04 | Cancelación con ETag vigente es auditable | Cancelación de versión obsoleta no modifica job |
| P05/N05 | Quota permitida crea solicitud acotada | Request masivo no genera bucle ni sobrecuota |
| P06/N06 | Export A usa filtros y RBAC de servidor | Body/header tenant B no cambia ownership |
| P07/N07 | CSV/XLSX neutraliza fórmula peligrosa | `=HYPERLINK(...)` no queda ejecutable |
| P08/N08 | Archivo íntegro confirmado conserva hash | Parcial/corrupto no publica descarga |
| P09/N09 | URL temporal autorizada expira y revoca | Enlace vencido/ajeno o replay es rechazado |
| P10/N10 | Manifiesto futuro registra checksum/retención | Backup sin cifrado o actor autorizado no accede |
| P11/N11 | Restore se prueba en MySQL efímero con diff | Ruta a PROD y checksum incorrecto: NO_GO |
| P12/N12 | Telemetría sanearía duración/resultado de pruebas | JWT/PII/URL firmada jamás aparece en log |

Ningún P/N precedente se ejecutó aquí; nombres de código HTTP/DTO y capacidades futuras **no equivalen a aplicación existente**.

## A0–A5 y rollback

**A0:** identidad Javier, skill/HEAD, Scope Lock, PLAN/COLA/DAG, prerrequisitos M01 y separación de M02.01 cuyo owner DEV03 se respeta. **A1 documental:** matriz ocho operaciones propuestas, DTO, ocho clases de errores y doce pares positivos/negativos verificables como texto, no como funciones. **A2 documental:** tenant A/B, grants, idempotencia, versión, cuotas, protección de PII y backups efímeros sin permisos implícitos. **A3:** cotejo firmas y blobs del caché público, Outbox y tenant-aware storage; no se agrega código, endpoint, EF/MySQL, SDK, router, flags o worker. **A4:** publicar exclusivamente este Markdown y sección CHANGELOG autocontenida con fast-forward/CAS, revisar SHA, blobs y checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` para el SHA causal; backend/Angular/MySQL/Playwright son **NO_APLICA** sólo por diff documental y no se cuentan como PASS. Registrar transiciones y eventos idempotentes con readback PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente por otra de las cinco Tasks Javier, distinta del autor, antes de liberar sucesoras técnicas; M02.06 exige además los otros cuatro M02 LISTO+A5. No QA/main/PROD.

**Rollback propio:** `git revert` convencional del changeset documental tras verificar HEAD y dependencias; nunca force-push ni modificación retroactiva de BITACORA. No R2, DNS, datos Aiven, secretos, servicios de pago, migraciones, workers ni REST real.
