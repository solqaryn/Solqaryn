# SOLQARYN V5 — C10-04.M02.05: contrato conceptual de eventos, idempotencia, correlación y límites

**Entrega real:** especificación DOCUMENTAL e INERTE de M02.05, C10-04 «Caché, jobs, backups y exportaciones». Autor: PRIMARY DEV 03 — JAVIER, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3; 2026-11-15; repositorio solqaryn/Solqaryn rama dev; PROJECT_SCOPE_LOCK=STRICT.

**NO IMPLEMENTAR EVENTOS TODAVÍA.** No se crea, registra ni ejecuta publicador, consumidor, broker, outbox nuevo, queue, worker, webhook, cron, scheduler, Task, API, tabla EF/MySQL, trigger, evento real o transporte. El texto describe contratos que se podrán evaluar en otra microtarea con autorización efectiva, pruebas dirigidas y A5 independiente. No hay operaciones R2, Hostinger, Cloudinary, Aiven, restauraciones ni exportaciones reales.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL en Drive (1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), Sheet institucional (1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU) PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, Skill canónica, AGENTS.md, docs/PROJECT_SCOPE_LOCK.md. Al reclamo: PLAN H=PENDIENTE/I vacío; COLA G/O=SI; DAG F=VALIDADO/G=SI para ID exacto; C10-04.M01.01–06 LISTO+A5 independiente. El gate **no** permite cambios runtime ni adelantar terminal M02.06/M03 por inferencia.

## 1. Estado real contrastado y frontera de confianza

| Fuente existente en dev | Blob cotejado | Límite de conclusión |
| --- | --- | --- |
| backend/src/Application/Services/OutboxRetryProcessor.cs | 542c0a655a3ee0698e3d80f1854d43903b6470fa | Contiene IOutboxEffectDispatcher, OutboxRetryProcessorOptions, política de retry, stale claim y lotes; no instala un dispatcher continuo ni un proveedor externo. |
| backend/src/Application/Interfaces/IPublicStoreCache.cs | 15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8 | Contrato de cache para storefront y TTL de segmentos públicos, sin conceder permiso a datos privados. |
| backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs | 1a102138b88bd54989eeade5acf7a5ee7bf63867 | Cache de memoria con claves por tenant, generación y hash; no verifica invalidación distribuida ni eventos de empresa B. |
| backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs | 454d588851ecc7db810259e63705852360172928 | Funciones tenant-aware de almacenamiento con default fail-closed; URI, publicId o enlace no son autoridad. |
| docs/v5/c10-04-m01-03-inventario-codigo-cache-jobs-backup-exportaciones.md | da3ab0ef3606bae512038caeffa3160c427995af | G01–G12 son brechas de evidencia, no fallo demostrado ni permiso de implementar. |
| docs/v5/c10-04-m01-06-gates-rollback-liberacion-cache-jobs.md | bbf0bfaa75d46362c2e5504d6f0a1840d27bda18 | B01–B14 siguen siendo condiciones futuras; no se ejecutaron restauros ni pruebas de worker. |

Este contrato no reemplaza la autorización real de la capa API/Application/Infrastructure ni valida estados de CI mediante una señal. Una señal o mensaje firmado no equivale a un grant de tenant, a A5_PASS o a permiso de modificación del Sheet.

## 2. Esquema conceptual común de señales futuras — NO instalado

| Campo conceptual | Restricción mínima, origen confiable y finalidad |
| --- | --- |
| EventId | UUID/identificador opaco de ocurrencia irrepetible generado por backend; no derivarlo de datos personales |
| Tipo | Catálogo allowlist versionado, sin nombres ejecutables arbitrarios desde cliente |
| SchemaVersion | Entero positivo o semver revisado; consumidor rechaza versiones desconocidas |
| EmpresaId | Se resuelve en backend a partir de recurso y principal autenticado, no del navegador |
| ActorId | Actor actual o identidad de servicio autorizada; nunca el correo o JWT crudo |
| RecursoId | Identificador opaco y propiedad persistida verificada antes de cualquier efecto |
| Operacion | Operación allowlisted; READ no autoriza WRITE o RESTORE |
| CorrelationId | Correlación opaca por cadena causal y tenant; limita longitud, entropía y exposición |
| CausationId | Evento causal anterior, si existe; previene ciclos sin recursión ilimitada |
| IdempotencyKey | Scope = empresa+actor+operación+recurso+versión+payload hash; no reutilizar con contenido distinto |
| PayloadSha256 | Huella calculada sobre representación normalizada por backend, no una firma del navegador |
| CreatedUtc | Timestamp de servidor; orden relativo explícito y expiración independiente del reloj de cliente |
| DeadlineUtc | Tiempo máximo de ejecución/reintento finito, siempre posterior a CreatedUtc |
| Attempt/MaxAttempts | Enteros acotados; incremento controlado en transacción con lease CAS |
| TraceClass | Nivel de privacidad y retención; datos sensibles omitidos/redactados |
| ResultCode | Categoría segura (accepted/rejected/retry/dead-letter); nunca incluir stack traces, tokens o URLs firmadas |

Ninguna de estas propiedades es un DTO C#, entidad persistida o evento que circule hoy por añadir esta documentación.

## 3. Catálogo propuesto de diez señales, todas FUTURAS_NO_EJECUTADAS

| Señal conceptual | Emisor futuro posible | Límite de consumo y resultado |
| --- | --- | --- |
| CacheSegmentInvalidationRequested | Servicio Application con permiso sobre segmento propio | Invalidación tenant/segmento/versión; jamás caché global cross-tenant |
| CacheSegmentInvalidationCompleted | Componente cache autorizado | Evidencia de generación confirmada, no autorización de lectura |
| TenantWorkRequested | Caso de uso con usuario/servicio autenticado | Encola únicamente si permiso, recurso, cuota y idempotencia vigentes |
| TenantWorkLeaseClaimed | Adaptador transaccional futuro | Solo ganador CAS/version, sin segundo ejecutor simultáneo |
| TenantWorkRetryScheduled | Política de backoff acotado | Deadline y máximo finitos; no crea run fuera de programa |
| TenantWorkDeadLettered | Procesador después del máximo | Error redactado, evidencia durable y revisión, sin auto-reparación permisiva |
| TenantExportRequested | Servicio de exportación futuro, autorizado | Datos sintéticos/empresa exacta, cuota, privacidad, validación CSV |
| TenantExportArtifactValidated | Servicio de integridad futuro | Hash, tamaño, MIME y propiedad comprobados antes de distribuir |
| BackupManifestValidated | Subproceso futuro con permiso especial | Integridad y cifrado del artefacto; no implica que exista restore exitoso |
| RestoreDryRunCompleted | Ensayo futuro exclusivamente efímero | Resultado y métricas reales; nunca promoción a producción automática |

No autorizar flujo desde Task Javier a otra Task ni usar este catálogo para ejecutar una cascada server-side. Las seis Tasks existentes conservan horarios y son el límite de ejecutores; no se añade webhook/event dispatcher.

## 4. Dieciséis invariantes fail-closed

- EVT01: todo mensaje se origina en un caso de uso backend autenticado y su tenant se confirma desde recurso persistido.
- EVT02: el tipo, operación, actor, recurso y versión se validan contra allowlist; ningún tipo externo ejecuta código.
- EVT03: empresa A y B no comparten partición ni deduplicación; cambio de tenant invalida el mensaje.
- EVT04: el consumidor revalida membresía, RBAC, revocación, scope y pertenencia inmediatamente antes del efecto.
- EVT05: claves idempotentes se vinculan a empresa, actor, operación, recurso, versión y payload normalizado.
- EVT06: misma clave/carga repite una respuesta única; misma clave/carga distinta se rechaza sin efectos.
- EVT07: publicación y estado de negocio requerirían atomicidad duradera; jamás se considera éxito con auditoría obligatoria fallida.
- EVT08: un lease atómico con versión impide doble claim; caducidad temporal sola no acredita exclusividad.
- EVT09: retry aplica máximo de intentos, backoff, jitter, deadline y cuota finitos; no loops sin techo.
- EVT10: cada reintento revalida identidad y permiso; un permiso revocado no revive por evento viejo.
- EVT11: mensajes vencidos, versión desconocida, payload malformado o SHA inválido se deniegan de forma segura.
- EVT12: correlación y causa se limitan en profundidad, tamaño y retención; cero recursión ilimitada.
- EVT13: no incluir JWT, MFA/OTP, passwords, DSN, credenciales, PII excesiva ni URLs bearer en payload/log.
- EVT14: una exportación parcial, backup no validado o restore incompleto no produce evento de éxito.
- EVT15: indisponibilidad de MySQL, auditoría, proveedor o transporte deja estado seguro y reversible, no fallback automático a servicio nuevo.
- EVT16: un evento de aplicación nunca acredita CI SUCCESS, cambio de PLAN/OWNER, A5_PASS o autorización QA/main/PROD.

## 5. Matriz causal positiva/negativa FUTURA — NO pruebas ejecutadas

| Caso | Esperado positivo en futura implementación | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Mensaje tenant A validado se consume en A | Actor B o cabecera falsificada intenta consumir A |
| P02/N02 | Clave igual/carga igual produce un efecto | Clave igual/carga diferente produce dos efectos |
| P03/N03 | Lease CAS elige un único consumidor | Dos consumidores ganan el mismo lease |
| P04/N04 | Retry autorizado usa backoff acotado | Reintentos exceden MaxAttempts o deadline |
| P05/N05 | Revocación bloquea siguiente entrega | Evento viejo recupera permiso suspendido |
| P06/N06 | Exportación sintética A se valida y publica | CSV formula, PII o recurso B sale publicado |
| P07/N07 | Backup futuro valida hash antes de emitirse | Snapshot corrupto se anuncia como restaurado |
| P08/N08 | CausationId enlaza trazas mínimas | Causación circular crea trabajo ilimitado |
| P09/N09 | Error del proveedor deja dead-letter auditable | Caída provoca 2xx falso o fallback no autorizado |
| P10/N10 | Evolución schema versionada rechaza incompatibles | Payload sin version o con secretos se despacha |

**Estos 20 escenarios se redactan como ORÁCULOS FUTUROS_NO_EJECUTADOS.** No se realizaron tests xUnit, Playwright, MySQL 8.4 efímero, workers, requests HTTP, webhooks, backups, dumps, restore ni métricas reales. Cualquier futura implementación debe probar seguridad multiempresa A/B, revocación, idempotencia concurrente, CAS, rollback, versión y cuota con CI exact SHA. El check Scope Lock de esta entrega solo certificará el cambio documental, no un runtime nuevo.

## 6. Contrato de límites, compatibilidad y reversión

**Límites propuestos para revisión futura:** rechazo de tamaño de payload excesivo, reintentos ilimitados, profundidad de correlación, cuotas sin actor/tenant, retención sin política, deadlines no positivos y URIs portadoras de credenciales. No se fijan cifras comerciales artificiales; los límites existentes de OutboxRetryProcessorOptions deben conservar su semántica hasta que una tarea autorizada los modifique. El sistema no cambiará el contrato IOutboxEffectDispatcher ni permitirá eventos como atajo a IAM/JWT/RBAC o a permisos de archivos.

**A0:** confirmar fuentes, predecesores LISTO+A5, owner exclusivo, ausencia de secretos y alcance solo docs/v5/CHANGELOG. **A1:** cotejar 16 campos, diez señales y diez pares positivos/negativos como especificaciones futuras. **A2:** demostrar en texto los rechazos de A/B, permiso revocado, replay, PII, loop y cambio de versión, sin atribuir seguridad runtime. **A3:** verificar compatibilidad conceptual con OutboxRetryProcessor y los contratos de caché/archivos actuales, sin instalar transporte; pruebas backend/MySQL/Angular = NO_APLICA para diff Markdown. **A4:** commit fast-forward CAS sobre HEAD vigente; Scope Lock y check V5 de procedencia completed/success SHA exacto, diffs/blobs y readback PLAN/COLA/DAG/BITACORA. **A5:** revisión independiente por otra Task Javier con SHA y evidencia, nunca la autora.

**Rollback real de este documento:** git revert normal del commit tras cotejar HEAD; BITACORA histórica permanece aditiva. Ninguna ejecución de evento se debe revertir porque aquí no se genera ninguno. Sin QA/main/PROD, recursos pagados, secretos, DNS, migración, IAM nuevo, endpoints, buckets, webhooks ni Tasks adicionales.
