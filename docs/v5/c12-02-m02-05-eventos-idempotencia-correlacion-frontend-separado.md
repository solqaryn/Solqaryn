# SOLQARYN V16 — C12-02.M02.05: contratos de eventos, idempotencia y correlación del frontend separado

**Microtarea:** C12-02.M02.05 · PLAN_MAESTRO fila 1776 · **autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a` · **repo/branch:** `solqaryn/Solqaryn@dev` · **Scope Lock:** STRICT · **riesgo V15:** R0 documental sin runtime, 2026-11-15.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Sheet PLAN_MAESTRO/COLA/DAG. **Resultado futuro:** frontend separado con build, CDN, dominio, headers, caché y despliegue independiente, sujeto a smoke público y rollback real de versión. **Esta unidad no lo implementa ni lo acredita.**

## 1. Contrato, dependencias y frontera

M01.01–06 son preparación previa con A5 distinto y decisiones D01–D10 **PENDIENTES** sobre proxy, caché, DNS/TLS, headers, hosts por ambiente, SEO, manifests, observabilidad, presupuestos y promoción. M02.01 documentó entidades E01–E12 e invariantes INV01–INV16 (blob `2cc6094228286a3da32569ef50476efa7429c3ec`), M02.02 operaciones OP01–OP08/DTO y errores (blob `df914632718a47eb3e7c35aea2a27c48fe80b5cb`), M02.04 configuración conceptual CFG01–CFG14 (blob `365d07567bd3fea9a783706a91afa9f01fc9bdc5`). M02.03 de persistencia se gestiona separadamente. Una fila LISTO de otro ID no autoriza su A5 ni la entrada a M03.

**Sólo artefactos permitidos:** este Markdown y sección autocontenida del CHANGELOG. No se crean workers, eventos, webhook, scheduler, pipelines de despliegue, Route Handler, cache real, CDN, DNS, TLS, header CSP/CORS, secret, build, despliegue, preview de proveedor, migración, endpoint, cola o cuenta. No se cambian `frontend/angular.json`, `frontend/vercel.json`, `app.routes.ts` ni la API .NET. Fuentes de configuración observadas previamente no prueban un entorno live. No se modifica QA/main/PROD ni se usan proveedores o pagos.

## 2. Doce contratos de evento futuros — NO IMPLEMENTADOS

| Evento conceptual | Emisor autorizado futuro | Campos permitidos y restricciones |
| --- | --- | --- |
| EVT01 — BuildRequested | Pipeline dev autorizado | BuildId, SOURCE_SHA, ambiente dev, versión de herramientas, nunca tokens |
| EVT02 — BuildSucceeded | GitHub Actions fuente verificada | BuildId, SHA, hash de bundle, resultado de test y artefacto, sin falsa certificación |
| EVT03 — BuildFailed | GitHub Actions | BuildId, etapa, error saneado, vínculo a job exacto y no a secretos |
| EVT04 — AssetManifestPrepared | Publicador dev autorizado futuro | ManifestSha, versión, hashes/SRI, paths allowlisted; no mutación live |
| EVT05 — CachePolicyValidated | Gate de seguridad futuro | Host verificado, política aprobada, controles de caché/tenant sin ID individual |
| EVT06 — PublicSmokeCompleted | Probe sintético efímero | SHA, URL de ambiente permitido, resultado real y evidencia de readback |
| EVT07 — ReleaseCandidateAccepted | Control A0–A5 autorizado | Source SHA, test SHA, logs, reviewer distinto, no equivaler a PROD |
| EVT08 — PublicationRequested | Operador humano autorizado en futuro | Versión, entorno, permisos y rollback aprobado; **no publicar en este alcance** |
| EVT09 — PublicationConfirmed | Proveedor con comprobación efectiva | Versión, cabeceras, recurso/host aprobado, evidencia live; hoy NO_MEDIDO |
| EVT10 — RollbackRequested | Autoridad futura con incidente | ReleaseId, target previo verificado, actor, motivo idempotente |
| EVT11 — RollbackVerified | Diagnóstico/Smoke futuro | Artefacto y respuesta recuperados; no declarar por texto solamente |
| EVT12 — AuditRecordAppended | Registro institucional futuro | Evento, actor/tenant autorizados, correlación, UTC y SHA sin credencial/PII |

Estos nombres son una **propuesta para microtareas futuras**; no implican EventBus, runtime, dispatch entre Tasks ni ejecución actual. **Los webhooks/event dispatcher están explícitamente prohibidos en esta fase.** Los eventos que representan despliegue, proveedor, dominio o rollback real permanecen NO_AUTORIZADOS hasta permisos separables. Ningún evento activa otro actor automáticamente.

## 3. Idempotencia y correlación futuras

| Regla | Invariante comprobable futura | Rechazo obligatorio |
| --- | --- | --- |
| ID01 | Clave (entorno dev, tipo, BuildId/ReleaseId, SOURCE_SHA, intento) | Reutilización de clave con payload distinto falla |
| ID02 | `request_id`/correlation ID saneado, longitud limitada y aleatorio | CR/LF, PII, JWT y tokens no aparecen en logs |
| ID03 | Un evento confirmado tiene llave durable y readback | Retry no duplica publicación, rollback ni auditoría |
| ID04 | SOURCE_SHA y hash de artefacto coinciden con CI causada | No sustituir SHA por HEAD ajeno ni build de otro diff |
| ID05 | Transición de estados es monótona por versión y CAS | Un publish obsoleto no pisa versión nueva |
| ID06 | Publicación condicionada a A5 distinta y permisos de entorno | LISTO_DEV no equivale a autorización QA/PROD |
| ID07 | Fallo a mitad de despliegue preserva release anterior sano | No marcar SUCCESS sin smoke/respuesta verificada |
| ID08 | Cache keys por entorno, host y versión para públicos; privadas jamás compartidas | Tenant B o sesión caducada no recibe contenido privado A |
| ID09 | Retries con límite, backoff y DLQ sólo si luego se autorizan | Tormenta de reintentos no causa cargos ni reentradas infinitas |
| ID10 | Eventos secretos/sensibles se minimizan y redactan | Cookie, Authorization, query PII y SQL nunca son atributos |
| ID11 | Evento registra procedencia y reviewer/owner separados | Autor no puede autoaprobar A5 ni fingir RUN_ID |
| ID12 | Telemetría de éxito tiene fuente, timestamp, numerador/denominador | Ausencia de prueba = NO_MEDIDO, no PASS |

## 4. Diez oráculos positivos/negativos futuros — 0 ejecutados

| Caso | Positivo requerido en implementación futura | Negativo que debe fallar con defecto |
| --- | --- | --- |
| T01 | Compilar Angular con artifact hash reproducible | Bundle inválido o SHA distinto se rechaza |
| T02 | Registrar resultado real de build y pruebas en CI SHA | QUEUED/SKIPPED/CANCELLED/FAIL no cuenta PASS |
| T03 | Correlacionar publicación y manifest allowlisted | Falsificación host/tenant rompe gate seguro |
| T04 | Respetar CSP/CORS y headers aprobados al responder | Headers relajados o wildcard inseguro bloquean |
| T05 | Cache público separado de privado/auth | CDN mezcla sesión A y B o token en caché |
| T06 | Smoke de release con HTTP y URL de ambiente autorizado | Build verde pero smoke 404/5xx no certifica |
| T07 | Retry idéntico se deduplica en estado durable | Mismo key con payload distinto no publica |
| T08 | Rollback recupera versión previa y smoke | Deploy parcial o versión no recuperable no PASS |
| T09 | UI ligera, accesible y responsive con pruebas reales | Foco perdido, content layout shift y 320px roto NO_GO |
| T10 | Auditoría registra idempotencia, owner/reviewer y blobs | PII, credenciales o cambio no autorizado no persisten |

T01–T10 **FUTUROS_NO_EJECUTADOS**. No se atribuyen builds npm, Playwright/Chromium, pruebas API/HTTP ni estado live de Vercel, Hostinger o Cloudflare a este documento. El contrato de smoke público y rollback de versión es una condición para otra fase, no una prueba actualmente realizada.

## 5. Límites, decisiones y rollback

D01–D10 continúan PENDIENTES. No especificar una URL live, secreto, cuota, TTL, budget, host ni proveedor como aprobado si su valor no consta. No usar un token del navegador para seleccionar tenant o rol; enforcement pertenece a backend JWT/RBAC y aislamiento tenant A/B. Cache/session en edge, CDN y revalidación deben ser probados con controles positivos y negativos antes de cualquier activación de ámbito R2/R3.

Reversión **de esta microtarea R0**: `git revert` normal de su commit en `dev` tras comprobar SHA y ausencia de escritura competidora, sin reset/force-push; agregar evento idempotente compensatorio en BITACORA y releer PLAN/COLA/DAG. No borrar evidencia previa. La reversión de infraestructura o artefactos publicados exige plan y permisos diferenciados; aquí no hay despliegue que revertir.

## 6. Calidad A0–A5 y entrega

- **A0:** alcance exacto M02.05, dependencias M01/M02, ruta documental + sección CHANGELOG, 12 contratos de eventos futuros, ID01–ID12 y diez casos T con negativos, sin creación de infraestructura.
- **A1:** pares falsables T01–T10 definidos, eventos EVT01–EVT12 y reglas ID01–ID12 auditables; **0/10 tests runtime ejecutados**, no simular PASS.
- **A2:** reglas JWT/RBAC/tenant A/B, host trusted, CSP/CORS, cookies, IDOR, sesiones, PII, revocación y fail-closed especificadas estáticamente, ningún enforcement cambiado.
- **A3:** consistencia de fuentes M02.01/.02/.04 y arquitectura frontend/backend/caché; pruebas .NET/Angular/npm/Playwright, MySQL y proveedores NO_APLICA al diff sólo Markdown.
- **A4:** publicar commit fast-forward/CAS en dev y verificar `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success en SHA exacto; readbacks PLAN/COLA/DAG/BITACORA, SHA/blobs y owner correctos.
- **A5:** revisión independiente por una de las otras cinco Tasks Javier, autor diferente, con posibilidad de FAIL y reparación; **no se libera M03, QA/main/PROD por documentación**.

Resultado final: documento contractual sin ejecución de dispatcher, webhook, eventos, workers, API, build, CDN, DNS, datos, migraciones, cuentas, gastos ni servicios externos.
