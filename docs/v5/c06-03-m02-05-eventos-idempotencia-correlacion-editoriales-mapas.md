# SOLQARYN V5 — C06-03.M02.05: eventos conceptuales, idempotencia, correlación y límites editoriales

**Alcance:** contrato DOCUMENTAL M02. No se crean eventos ejecutables, dispatcher, colas, suscripciones, worker, Task, webhook, API, tablas ni proveedores. **Autor:** PRIMARY DEV 01, JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

## 1. Autoridad, fuente y límites de entrada

Única autoridad comercial: Standard de Implementación de Automatizaciones V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Preflight por ID: `C06-03.M02.05`, PLAN_MAESTRO fila 948 PENDIENTE/sin dueño antes del CAS, COLA G/O=SI y DAG_TECNICO fila 909 F=VALIDADO/G=SI. La operación condicional sobre H948 obtuvo exactamente un cambio y I948 fue asignada al TASK_ID propio con lectura posterior. El único predecesor DAG autorizado es el conjunto `C06-03.M01.01–06+A5`; no se infieren gates adicionales.

Referencias ya publicadas: M01.06, SHA `0dc07b4509e3f74c9b867fc450b3b5abda63075b`, blob `e51e254064bcaf53449ea66efeac7b5612966121` (G01–G16 y RB01–RB08); M02.01 blob `77d4e7a6f9a6327f6a4d3998bcfc56ccc80db013` (entidades y ownership); M02.02 blob `ba46a9f4ad3a54a3db7606d8dcd9e4ef8bd9b0f0` (API/DTO); M02.03 blob `838504982fe29aa65b170e03717d212056f75519` (persistencia hipotética). M02.04 corresponde a una Task diferente y su diseño de flags no se presume concluido. Este documento es independiente, no cierra la M02.06 ni habilita M03.

Un host, slug, cabecera reenviada o campo cliente no otorga membresía, permiso ni tenant. El backend futuro verificará la identidad, JWT, RBAC, ownership, revisión y consentimiento; los borradores nunca salen en caché pública, SEO o sitemap. Mapas son opcionales y no deben cargar terceros ni solicitar ubicación privada del visitante sin autorización.

## 2. Doce intenciones de eventos FUTUROS — NO EMITIDOS

| ID | Intención lógica futura | Condición de emisión a acreditar posteriormente |
| --- | --- | --- |
| EV01 | EditorialDraftChanged | Editor vigente y CAS sobre versión de A confirmada |
| EV02 | EditorialModerationRequested | Draft propio revisado y moderador permitido, sin autoaprobación |
| EV03 | EditorialModerationDecisionRecorded | Decisión auditada de moderador distinto, versión vigente |
| EV04 | EditorialPreviewGrantIssued | Token resumido, destinatario, revisión y caducidad verificables |
| EV05 | EditorialPreviewGrantRevoked | Revocación idempotente y denegación de preview previa |
| EV06 | EditorialPublicationRequested | Host/tenant vinculados y versión apta para aprobación |
| EV07 | EditorialPublicationApproved | Publicador separado, auditoría confirmada y CAS exitoso |
| EV08 | EditorialPublicationRolledBack | Snapshot aprobado anterior y autorización de reversión |
| EV09 | EditorialPublicContactChanged | Contacto de sucursal pública validado, sin PII interna |
| EV10 | EditorialPublicHoursChanged | Horario verificado, timezone y excepciones coherentes |
| EV11 | EditorialPublicLocationApproved | Dirección pública autorizada y consentimiento válido |
| EV12 | EditorialMapProjectionChanged | Opt-in, proveedor/cuota aprobados; fallback textual si no |

Los nombres EV son un catálogo de intención, no tipos implementados, tópicos de mensajería ni promesa de entrega. El futuro diseño debe determinar si corresponde emitirlos, su finalidad y retención, sin activar eventos por esta entrega.

## 3. Doce invariantes propuestos de idempotencia, orden y correlación

| ID | Invariante de diseño futuro | Fallo que debe impedir |
| --- | --- |
| ID01 | Clave de deduplicación server-side ligada a tenant, aggregateId, intención y versión de contrato | Reutilización A/B de una clave cliente |
| ID02 | Fingerprint estable de payload permitido y revisión esperada; discrepancia devuelve conflicto neutro | Reutilizar clave con contenido distinto |
| ID03 | Confirmar estado y registro de intención en la misma transacción cuando exista persistencia autorizada | Evento de éxito con escritura fallida |
| ID04 | Orden lógico por agregado/versión; no usar timestamp como autorización | Reordenamiento que publique versión vieja |
| ID05 | CorrelationId opaco generado o validado por servidor, sin JWT, correo o PII | Filtración en logs o encabezados |
| ID06 | CausationId apunta a acción auditada autorizada, no a cabecera arbitraria | Falsa cadena de causalidad entre tenants |
| ID07 | Reintentos con backoff acotado y presupuesto explícito solo en diseño futuro aprobado | Reintentos infinitos o costos inesperados |
| ID08 | Duplicado compatible es no-op verificable, no segunda publicación ni doble moderación | Doble efecto externo o escritura |
| ID09 | Grant de preview revocado/expirado invalida cualquier proyección diferida | Preview antigua visible tras revocación |
| ID10 | Audit trail minimizado y particionado por tenant; acceso controlado por roles backend | Exposición A/B por tracing o caché |
| ID11 | Mapas y proveedores se degradan a dirección textual aprobada si falta opt-in | Activación de API pagada o tracking |
| ID12 | Versionar contratos y rechazar consumidores antiguos sin RBAC/tenant explícitos | Elevación por defaults o payload incompleto |

Las duraciones TTL, tamaño de cola, cuotas y costes no están aprobados: no asignar cifras arbitrarias. Outbox, workers, broker y despacho son opciones de arquitectura a evaluar en tareas futuras autorizadas, no artefactos de este commit.

## 4. Doce pares positivos/negativos FUTUROS_NO_EJECUTADOS

| Par | Positivo futuro que deberá probarse | Negativo futuro que deberá rechazarse |
| --- | --- | --- |
| PN01 | Editor A registra un draft A y una intención una sola vez | Actor B o revocado intenta el mismo aggregateId A |
| PN02 | Retransmisión idéntica devuelve mismo resultado neutro | Misma clave con payload modificado no repite mutación |
| PN03 | Moderador distinto decide versión vigente | Editor se autoaprueba o usa expectedVersion antigua |
| PN04 | Publicador autorizado confirma publicación y auditoría | Rol visual o Host forjado intenta publicar B |
| PN05 | Preview activo solo sirve versión autorizada noindex | Grant revocado, expirado o sin actor revela draft |
| PN06 | Contacto público A actualiza proyección de A | Campo privado o contacto B no llega a caché pública |
| PN07 | Horario con TZ y sucursal aprobada se proyecta | Falta TZ o sucursal B no genera datos inventados |
| PN08 | Dirección aprobada funciona sin SDK externo | Ubicación privada del visitante no se exige |
| PN09 | Mapa opt-in autorizado ofrece enlace seguro | Sin consentimiento, clave o cuota no carga proveedor |
| PN10 | Publicación A actualiza índice/caché particionada | Draft/B/host desconocido nunca entra en SEO público |
| PN11 | Rollback autorizado restaura versión aprobada auditada | Snapshot ausente o rol inválido no cambia estado |
| PN12 | Correlación opaca permite observabilidad mínima de A | JWT, PII, clave de terceros o trazas B no aparecen |

PN01–PN12 son oráculos de aceptación futura, **no pruebas xUnit, Angular, HTTP, MySQL, Playwright, eventos ni broker ejecutadas**. La revisión presente comprueba coherencia documental; la implementación futura exige tests reproducibles de tenant A/B, JWT/RBAC, orden, idempotencia y rollback en CI causal.

## 5. Seguridad, compatibilidad, pruebas y rollback

**Domain futuro:** agregados, estado y revisión inmutables, tenant-bound. **Application futuro:** autorización de Editor/Moderador/Publicador, membresía vigente, token/grant de preview y decisión de publicación. **Infrastructure futuro:** transacción y registros idempotentes con MySQL 8.4/EF Core 10 solo mediante CI efímera aprobada. **API futura:** error neutro, correlationId limpio, Host/proxy sin autoridad; ningún endpoint añadido aquí. **Angular futuro:** mostrar sólo proyección autorizada, teclado/foco/320px y mapa con fallback textual; ningún componente o script instalado.

**A0 estática:** verificar Skill canónica/arquitectura, PROJECT_SCOPE_LOCK, PLAN/COLA/DAG por ID y CAS exclusivo, fuente M01.06+A5, cuatro blobs M02 y ausencia de archivos ajenos. **A1 documental:** revisar 12 EV, 12 ID y 12 pares PN contra requerimiento conceptual, sin fingir test runtime. **A2 estática:** comprobar no-cross-tenant, JWT/RBAC, autoría separada, CAS, preview noindex, PII/trazas, Host no confiable, mapas opt-in y fallos fail-closed. **A3 documental:** cotejar Domain/Application/Infrastructure/API/Angular y contratos M02.01–03, sin invocar MySQL ni herramientas externas. **A4:** publicar un commit fast-forward con sólo este archivo y sección autocontenida CHANGELOG, verificar Scope Lock y procedencia V5 `completed/success` en SHA exacto y releer PLAN/COLA/DAG/BITACORA. **A5:** requiere otra Task Javier distinta del autor, revisión real de SHA, blobs, diff, alcance y evidencias antes de promover sucesoras dependientes.

Los checks .NET/Angular/seguridad de runtime son NO_APLICA a diff Markdown cuando no se altera código; queued/in_progress/cancelled/skipped/failed nunca se contabilizan como PASS de una prueba requerida. La M02.06 debe revalidar LISTO+A5 de todos los prerequisitos técnicos, no inferirlo de esta preparación independiente. Sin pagos, proveedores mapas reales, geocoding, transacciones, migraciones, DNS/TLS, R2, QA/main/PROD, Tasks ni dispatchers.

**Rollback de esta documentación:** `git revert` normal del commit publicado con relectura de HEAD para conservar trabajo concurrente; BITACORA aditiva y estados institucionales no se reescriben.
