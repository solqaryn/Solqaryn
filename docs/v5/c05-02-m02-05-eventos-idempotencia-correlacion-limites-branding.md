# SOLQARYN V5 FINAL — C05-02.M02.05: eventos, idempotencia, correlación y límites de branding y presets

**Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Rama:** solqaryn/Solqaryn dev. PROJECT_SCOPE_LOCK=STRICT, 2026-11-15.

**Fuentes:** Standard V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN/COLA/DAG/BITACORA; skill canónica y AGENTS.md; C05-02.M01.01–06 y M02.01–04, revisados según estado actual de cada ID. La microtarea M02.05 está habilitada por COLA G/O=SI y DAG F=VALIDADO/G=SI al reclamarla.

**Clasificación:** sólo documento de diseño. Ninguno de los eventos o sobreescritos siguientes se emite ni se almacena. No crear webhooks, queues, dispatcher, cron, Tasks, workers, eventos server-side, API, migraciones, secretos ni cambios de datos. Las señales E01–E10 son PROPUESTAS_FUTURAS_NO_EMITIDAS.

## 1. Distinción entre capacidades actuales y diseño

- TemaVisual (backend) es una fila GLOBAL Id=1 con 15 propiedades de color, no una tabla tenant. Los endpoints de TemaVisualService consultan y modifican el tema global actual; el reset a defaults NO es rollback versionado de empresa.
- ConfigEmpresa sí está ligada a EmpresaId y contiene configuración operativa Version, pero no demuestra una entidad de versión de branding aislado.
- ThemeApplierService aplica sólo colores HEX permitidos y evita respuestas obsoletas del tema. EmpresaIdentidadService limpia identidad A al cambiar a B. La UI no concede permisos y no sustituye checks de servidor.
- Los contratos M02.01, M02.03 y M02.04 son diseños de objetos, persistencia futura y flags hipotéticos; no suponen que ya existan revisiones históricas o un sistema de eventos.

## 2. Catálogo de 10 señales FUTURAS S01–S10 (NO EMITIDAS)

| ID | Nombre lógico | Origen futuro, condicionado | Consumidor futuro y restricción |
| --- | --- | --- | --- |
| S01 | BrandDraftCreated | Servicio tenant con identidad/rol verificado | Mantener borrador privado, sin publicar ni conceder permisos |
| S02 | BrandPreviewRequested | Preview autorizada para EmpresaId | Tokens temporales sin sobrescribir versión publicada |
| S03 | BrandPreviewValidated | Validación HEX/contraste y ownership | Solo permite visualizar una preview de la empresa actual |
| S04 | BrandPresetSelected | Elección de preset autorizado | No copiar datos de otro tenant, ni cambiar tema global Id=1 |
| S05 | BrandPublicationRequested | Actor con permiso Editar/Proponer según política | CAS de versión en backend; sin grant no llega a DB |
| S06 | BrandPublished | Commit durable de nueva revisión de Empresa | Invalidación segura de cache tenant verificada |
| S07 | BrandPublicationRejected | Control CAS/permisos inválidos | Respuesta denegada sin marca de otro tenant |
| S08 | BrandRollbackRequested | Actor autorizado y snapshot propio aprobado | Comparación de versión, retención e integridad previa |
| S09 | BrandRollbackCompleted | Compensación durable versionada | Auditoría y lectura tenant, no reset global |
| S10 | BrandAssetRevoked | Revocación de logo/preset autorizado | Impedir acceso al asset propio revocado, sin borrar datos de terceros |

Estas señales no constituyen un sistema instalado. Una implementación posterior deberá elegir patrón transaccional apropiado y verificar cuotas, idempotencia, permisos, seguridad y observabilidad antes de emitir.

## 3. Sobre lógico futuro F01–F16 (sin serializar ni publicar)

| Campo | Regla futura obligatoria |
| --- | --- |
| F01 schemaVersion | Versión cerrada; rechazar esquema desconocido |
| F02 eventId | Identificador estable y único para deduplicar |
| F03 correlationId | Traza redactada sin datos personales |
| F04 operationId | Referencia a operación autorizada y durable |
| F05 empresaId | Fuente backend verificada; no la decide cliente/host |
| F06 actorId | Usuario validado, no email público en logs |
| F07 actorPermissionReference | Referencia a permiso revalidado, nunca booleano cliente |
| F08 eventKind | Tipo cerrado S01–S10 |
| F09 previousBrandVersion | Versión anterior y tenant coincidente |
| F10 expectedBrandVersion | Versión CAS; detectar carreras y stale |
| F11 resultingBrandVersion | Solo presente al confirmar persistencia |
| F12 presetVersion | Catálogo aprobado y revisión propia |
| F13 assetReference | ID opaco y propiedad del tenant, no URL arbitraria |
| F14 idempotencyKeyDigest | Huella sin exponer key/token original |
| F15 payloadHash | Hash canonicalizado; conflicto ante payload distinto |
| F16 committedAtUtc | Instante de commit real, no hora de cliente |

**Prohibido en la carga:** JWT, refresh, password, DSN, API keys, correos completos, payload CSS libre, asset URL privado o datos de B accesibles desde A. Identificadores tenant quedan sujetos a autorización del consumidor.

## 4. Invariantes de correlación e idempotencia I01–I14

| ID | Regla | Rechazo requerido |
| --- | --- | --- |
| I01 | Idempotencia por EmpresaId+operación+clave+hash | Replay con payload distinto sin nuevos efectos |
| I02 | Evento se produce sólo después de commit durable | Prohibir BrandPublished sin revisión guardada |
| I03 | Una versión de publicación tiene un ganador CAS | Carreras entre editores no producen overwrite |
| I04 | Actor autorizado se revalida al consumir | Token revocado nunca publica o revierte |
| I05 | Tenant A/B se mantiene entre emisor/consumidor/asset | Mensaje A no modifica B por ID/host cambiado |
| I06 | Preview nunca afecta estado publicado | Salir preview no cambia versión de Empresa |
| I07 | TemaVisual global nunca se altera por evento tenant | Denegar escritura de Id=1 como fallback |
| I08 | Color/imagen deben pasar allowlist, ownership y contraste | Rechazar CSS/JS/URL arbitrarias y logos ajenos |
| I09 | Reintento tiene límite, backoff y presupuesto propuestos | Prohibir ciclos perpetuos o inundación |
| I10 | Cada respuesta conserva correlación redactada | Logs no exponen JWT/DSN/PII completa |
| I11 | Reversión sólo usa revisión del mismo tenant | Rechazar snapshot de B para A |
| I12 | Revocación elimina acceso efectivo sin borrar historia legítima | No resurrect de asset privado |
| I13 | CI requerida se verifica exact-SHA con conclusion success | CANCELLED/SKIPPED/IN_PROGRESS no PASS |
| I14 | A5 independiente y permisos de entorno separados | No autoaprobar ni activar QA/main/PROD |

## 5. Casos conceptuales P01–P04 y N01–N10 (FUTUROS_NO_EJECUTADOS)

| Caso | Tipo | Estímulo | Resultado que deberán medir futuras pruebas |
| --- | --- | --- | --- |
| P01 | positivo | Publicación A con permiso y versión CAS | Un commit, S06 único y correlación válida |
| P02 | positivo | Replay idéntico de selección preset A | Idempotencia, sin segunda escritura |
| P03 | positivo | Rollback A con snapshot y autorización | Nueva revisión auditada, B intacta |
| P04 | positivo | Revocación de logo A | Acceso A retirado con trazabilidad |
| N01 | negativo | Admin A envía perfil B | Denegación server-side y sin S06 |
| N02 | negativo | Same key con diferente payload | Conflicto lógico sin escribir |
| N03 | negativo | Dos editores publican expectedVersion igual | Un ganador CAS, el otro conflicto |
| N04 | negativo | Preview cambia versión publicada | Rechazar cambio |
| N05 | negativo | Evento contiene token/DSN/PII | Redactar y abortar entrega |
| N06 | negativo | Logo de B en revisión A | Rechazar enlace |
| N07 | negativo | Reset global se anuncia como rollback A | No certificar restauración |
| N08 | negativo | EventId repetido en consumidor | Deduplicar, no efecto duplicado |
| N09 | negativo | CI esencial queued/cancelled/skipped | Mantener VALIDANDO |
| N10 | negativo | Una Task genera webhook permanente sin permiso | Prohibir creación/ejecución; conservar seis schedules |

Ningún escenario anterior fue ejecutado: son oráculos para futuros tests HTTP, .NET, Angular, MySQL y seguridad, no PASS de la microtarea documental.

## 6. Verificación A0–A5, impacto y rollback

**A0:** fuente y alcance, dos Markdown exclusivamente (este documento y sección contigua de CHANGELOG), sin secretos/credenciales. **A1:** comprobación estructural de S01–S10, F01–F16, I01–I14 y P/N futuros con denegación especificada. **A2:** análisis documental RBAC, tenant A/B, caché stale, XSS, CAS y redacción PII. **A3:** consistencia con M01.01–06, modelos y flags de M02 en su estado real, DAG/PLAN/COLA por ID; no adelantar M02.06 o M03. **A4:** publicación mediante FF/CAS sobre dev, comprobación Scope Lock y V5 exact-SHA completed/success, readback PLAN/COLA/DAG/BITACORA; compilación o E2E runtime NO_APLICA a dos Markdown, nunca PASS. **A5:** una Task Javier distinta del autor debe revisar source diff, blob, pruebas y evidencias antes de liberar fase.

**Impacto cero en runtime:** no eventos reales, dispatcher, Task, websockets, worker, backend, frontend, migración, datos, permisos, secretos, QA/main/PROD ni servicios de pago. **Rollback de esta documentación:** git revert normal con pruebas pertinentes, BITACORA histórica inmutable. La implementación futura necesita autorización y CI causal propios.
