# SOLQARYN V5 — C10-01.M02.05: eventos conceptuales, idempotencia, correlación y límites de almacenamiento neutral

**Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-15. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN/COLA/DAG institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`). **Entorno único:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

**ALCANCE DOCUMENTAL. EVENTOS NO IMPLEMENTADOS.** No hay dispatcher, webhook, mensaje emitido, worker, cola, broker, tarea programada ni tráfico Cloudinary/R2. Los mensajes y pruebas son contratos FUTUROS_NO_EJECUTADOS. Ningún evento se interpreta como permiso o A5_PASS; una Task distinta de la autora revisará este documento.

## 1. Contexto verdadero y límites

C10-01.M01.03 (blob `1d807b2b9cba1f8b87a0e4e4e571f8e832ff0203`) identificó contratos reales `IImageStorageService`, `ICompraDocumentoStorageService`, `StorageTenantContext`, adaptadores Cloudinary y `ImagenUploadSecurity`. C10-01.M01.06 (blob `b82c2f7734f6b5b9fd582090c5110b4c64d9591e`) documentó BL01–BL14 y RB01–RB10, con M01 LISTO+A5. Los contratos de M02, si están LISTO, no son todavía implementación. El diseño futuro debe permitir imágenes/documentos/videos/exportaciones sin mezclar permisos, tenants, datos ni capas.

**Separación:** Cloudinary sigue en código actual; R2/Hostinger no se contrataron, configuraron ni probaron. `StorageTenantContext` deriva del backend y no del path/header. Un evento de objeto privado nunca entrega contenido, URL firmada, cookie, JWT ni identificadores de cliente. Ninguna integración real se autorizó por esta microtarea.

## 2. Catálogo propuesto de mensajes NO EMITIDOS

| Evento hipotético | Emisor posible, sujeto a autorización posterior | Semántica y riesgo a controlar |
| --- | --- | --- |
| `storage.upload.requested.v1` | API autenticada tras validar tenant, RBAC, MIME y cuota | Una solicitud no certifica bytes cargados ni grant |
| `storage.upload.verified.v1` | Servicio futuro tras SHA-256 de bytes reales | Hash, MIME y longitud verificados; evitar repetir publicación |
| `storage.object.published.v1` | Persistencia confirmada y versión esperada | Resultado visible sólo si hay objeto íntegro y permiso |
| `storage.object.downloaded.v1` | Backend tras autorización efectiva por lectura | Traza depurada, sin URL temporal completa ni PII |
| `storage.object.revoked.v1` | Servicio autorizado con revisión/versionado | Invalidar enlaces, preservar historia, no borrar datos de B |
| `storage.integrity.failed.v1` | Verificador de checksum autorizado | Error sin publicar objeto corrupto o permitir fallback |
| `storage.migration.checked.v1` | Validación futura en sandbox de CI | Comparar hashes/counts con manifiesto, no iniciar cutover |
| `storage.operation.rejected.v1` | Validador de seguridad | Denegación sin recurso, secreto, token ni existencia de B |

Todos son nombres de diseño, **no tipos C# existentes, mensajes enviados ni endpoints instalados**. Ningún evento desbloquea PLAN/COLA/DAG o despierta otras Tasks; la fuente auditable de estados sigue siendo PLAN y BITACORA institucional, con A5 independiente real.

## 3. Envelope propuesto y autoridad

| Campo hipotético | Tipo o procedencia | Invariante |
| --- | --- | --- |
| `schemaVersion` | versión fija soportada | Rechazar desconocida; migración explícita futura |
| `eventId` | identificador único | No confundir con RUN_ID del proveedor |
| `idempotencyKey` | digest tenant efectivo + operación + objeto + solicitud | Mismo key/cuerpo no duplica efecto; cuerpo distinto rechaza |
| `correlationRef` | referencia opaca de operación | Nunca autentica, ni concede permisos |
| `causationRef` | solicitud backend verificada | No registrar cabeceras/token del cliente |
| `actorScopeRef` | identidad segura y rol verificados | Emisor sin RBAC o rol revocado produce rechazo |
| `tenantScopeRef` | Empresa derivada del servidor | No mezclar tenant A/B por nombre de carpeta |
| `objectRef` | referencia opaca y versionada | Nunca path real, URL firmada o nombre sensible |
| `bytesSha256` | hash de bytes reales verificados | Ausente en REQUESTED; no tomar digest declarado como PASS |
| `actualMime` | detector de contenido real | Contrastar magic bytes con tipo autorizado |
| `state` | requested, verified, published, failed, revoked | Error/queued/cancelled nunca es éxito |
| `sourceSha` | commit verificable del cambio causal | No mezclar evidencia de SHA distintos |
| `occurredAtUtc` | reloj real del sistema autorizado | No inventar fecha nativa de una Task |
| `evidenceRef` | artefacto saneado de CI/operación | Sin payload, JWT, cookie, PII, secrets, URL firmada |

Este envelope no se almacena ni emite. Una implementación futura debe decidir tamaño, retención, acceso y versionado con base en política efectiva, costes y pruebas; no inventar cuotas/TTL arbitrarios.

## 4. Idempotencia, límites y fail-closed

- D01: mismo idempotencyKey + bytes/versión idénticos -> un efecto, un resultado auditable.
- D02: mismo key + contenido distinto -> conflicto sin sobrescribir ni cambiar propietario.
- D03: operación con `expectedVersion` caduca -> rechazo, no modificación parcial.
- D04: cambio tenant A→B invalida contexto, grants y resultados pendientes de A.
- D05: error de red/proveedor nunca habilita objeto incompleto ni fallback R2/Cloudinary no autorizado.
- D06: 401/403, URL vencida, rol revocado o ID ajeno no son reintentables como éxito.
- D07: replay/reordenamiento de eventos no reactivan objetos revocados ni grants expirados.
- D08: bytes de imagen incorrectos, MIME falso, tamaño/cuota invalidados bloquean publicación.
- D09: no emitir PII/JWT en payload, traza, nombre de archivo o logs de error.
- D10: eventos de migración sandbox deben tener manifiesto/hash/conteos y rollback probado, sin cutover.
- D11: cualquier retry futuro tiene presupuesto y backoff acotados; cero bucles server-side perpetuos.
- D12: el resultado de seguridad/CI `cancelled`, `skipped`, `in_progress` o `failed` nunca es PASS ni libera A5.
- D13: las seis Tasks Javier son ejecutores programados, no suscriptores de un bus de eventos.
- D14: solo PLAN/COLA/DAG y BITACORA con autorización real determinan estados y revisión independiente.

## 5. Oráculos futuros positivos/negativos (NO_EJECUTADOS)

| Caso | Positivo futuro | Negativo que deberá rechazarse |
| --- | --- | --- |
| P01/N01 | Carga idéntica con misma clave es idempotente | Replay crea duplicado o cambia hash |
| P02/N02 | SHA-256 coincide tras escribir/leer bytes reales | Hash del cliente falso publica objeto |
| P03/N03 | A autorizado recibe sólo referencia objeto A | A lee metadatos/objeto B por ID conocido |
| P04/N04 | MIME real y tamaño dentro de policy | Extensión permitida con magic bytes falso |
| P05/N05 | Revocación corta grant sin perder historia | Link vencido o rol revocado sigue activo |
| P06/N06 | Error de proveedor deja estado seguro y compensable | Objeto truncado visible o retry infinito |
| P07/N07 | Manifiesto sandbox verifica conteos y hashes | Cutover sin backup, hash o permiso |
| P08/N08 | Evento depurado referencia actor/operación autorizados | JWT, token, PII o URL firmada en traza |
| P09/N09 | Revisión A5 distinta autentica evidencia y SHA | Evento recibido se interpreta como PASS institucional |

## 6. A0–A5, dependencias y rollback

A0: sólo documento `docs/v5` más sección autocontenida en `CHANGELOG_AI.md`, sin código/secretos. A1: ocho mensajes, catorce campos del envelope, D01–D14 y nueve pares P/N escritos; no xUnit/HTTP/SDK ejecutados. A2: análisis de RBAC servidor, tenant A/B, MIME/tamaño/checksum, URL temporal y privacidad, NO prueba runtime. A3: correspondencia con interfaces y M01 existentes; integración real de broker/proveedor/DB NO_APLICA por Markdown. A4: Git fast-forward/CAS, diff/blob SHA exacto, `verify-solqaryn-project-scope` y V5 procedencia CHANGELOG `completed/success`, readback PLAN/COLA/BITACORA. A5: revisor de otra Task Javier, nunca la autora.

Roll back del documento por `git revert` normal y conservar cambios ajenos/historia de BITACORA. M02.06 y M03 conservan sus propios DAG y CI, con requerimiento M02.01–05 LISTO+A5 cuando corresponda. No instalar eventos, webhook, dispatcher, tareas ni cambios QA/main/PROD.
