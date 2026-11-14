# SOLQARYN V5 — C10-04.M01.05: criterios de aceptación observables de caché, jobs, backups y exportaciones

**Unidad:** C10-04.M01.05, PLAN_MAESTRO fila 1554, M01 preparación, **no implementación**. **Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Plan:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`; Scope Lock STRICT. Fecha 2026-11-14.

## Alcance de esta entrega

El resultado exigido de C10-04 es aislamiento, idempotencia, límites y restauración; su evidencia mínima futura consiste en **pruebas de tenant cruzado, fallo de worker y restauración**. Esta unidad define criterios positivos y negativos observables y no declara ninguno ejecutado. C10-04.M01.01 fija NR01–NR16; M01.03 inventaría diez fuentes y G01–G12; M01.04 clasifica dieciséis riesgos potenciales, diez supuestos no verificados y diez decisiones pendientes. M01.02 corresponde a un autor independiente y se respeta su estado real; ningún texto aquí lo aprueba automáticamente.

Línea base verificada en el árbol dev de esta corrida: `IPublicStoreCache.cs` blob `15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8`, `PublicStoreMemoryCache.cs` blob `1a102138b88bd54989eeade5acf7a5ee7bf63867`, `OutboxRetryProcessor.cs` blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`, `PublicHttpCacheAttribute.cs` blob `8c29b56fbdac3c01562a89ae4b982e4f74f6a4a7`, `DevCloudinaryLegacyNormalizationHostedService.cs` blob `863e035107b849fb97d9530ef85cf2af1d2311ae`, `PublicHttpCacheAttributeTests.cs` blob `e28a5cac8aaade85ebcadd762486e554130e2a5a`. Son **fuentes existentes**, no pruebas .NET ejecutadas ni permisos para activar workers.

## Criterios de aceptación AC01–AC18, aún pendientes de prueba causal

| ID | Escenario positivo esperado | Evidencia necesaria para futuro PASS |
| --- | --- | --- |
| AC01 | Cache GET público de empresa A sirve dato público autorizado de A | Test HTTP A y trazas de llave tenant derivada de backend, sin JWT/PII. |
| AC02 | Misma clave lógica en empresa B produce otra entrada sin datos de A | Test A/B simultáneo y comparación negativa de payload/etag. |
| AC03 | Mutation A invalida sólo segmentos pertinentes y no rompe caché B | Test EF write + nueva lectura A y lectura de B estable. |
| AC04 | Cache expirado recompone el dato sin servir valor privado obsoleto | Reloj controlado, expiración observable y replay HTTP autorizado. |
| AC05 | Error de cache mantiene validación JWT/RBAC, recurso y pertenencia | Test fallo del adaptador, autorización positiva/negativa. |
| AC06 | Outbox reintenta un fallo temporal según límite, backoff y correlación | Test con fake transport y tiempo controlado; 1 operación final, no loop. |
| AC07 | Mismo evento/clave idempotente duplicado produce a lo sumo un efecto | Test 2+ consumidores en persistencia compartida/CI real. |
| AC08 | Crash y lease vencido permiten recuperación sin doble facturación | Integración real con CAS, estado durable y resultado idempotente. |
| AC09 | Evento de tenant B jamás se ejecuta con credenciales o recurso de A | Servicio de seguridad tenant A/B y trazas sanitizadas. |
| AC10 | Máximo reintento manda estado terminal auditable, no éxito ficticio | Test límite exacto y canal de revisión o dead-letter equivalente. |
| AC11 | Backup DEV autorizado cifra material y protege custodia/retención | Fixture efímero, inspección de permiso de acceso y metadata de cifrado. |
| AC12 | Restore efímero autorizado reconstruye esquema/PK/FK y conteos | Base MySQL temporal, migraciones SHA, checksum y diff antes/después. |
| AC13 | RPO/RTO se miden en un ensayo reproducible y se comparan con objetivo | Tiempos registrados con fuente, ambiente y límites de exactitud. |
| AC14 | Export de A autorizado incluye únicamente registros propios | Test HTTP/DB positivo A y negativo cross-tenant B. |
| AC15 | CSV/XLSX seguro neutraliza fórmulas y limita tamaño/paginación | Fichero descargado y test de payloads de spreadsheet injection. |
| AC16 | URL privada temporal tiene recurso/método/TTL, revocación y auditoría | Pruebas de expiración, reuso, método erróneo y actor no autorizado. |
| AC17 | Logs/telemetría de jobs y backup registran duración/resultados sin secretos | Test sobre captura de logs y negativa a JWT, clave, PII, URL firmada. |
| AC18 | Rollback de fallo de proveedor preserva estado y no cambia QA/main/PROD | Fixture aislado, compensación, readback y revisión independiente A5. |

**Ninguno de los AC01–AC18 constituye resultado PASS de runtime en esta microtarea.** No se asume disponibilidad de proveedores, workers o infraestructura futura.

## Negativos N01–N18 — oráculos futuros NO_EJECUTADOS

| ID | Acción que debe rechazarse / comportamiento prohibido |
| --- | --- |
| N01 | Tenant B solicita entrada de caché tenant A y obtiene datos de A. |
| N02 | Parámetro/slug/header `EmpresaId` falsificado cambia la autoridad del servidor. |
| N03 | Respuesta privada, JWT o PII se almacena en caché compartida. |
| N04 | Cache TTL vencido devuelve credenciales/dato privado obsoleto. |
| N05 | Error de caché omite RBAC, membresía o autorización de recurso. |
| N06 | Dos consumidores del mismo evento hacen dos cobros o mutaciones. |
| N07 | Retry no tiene tope y crea un bucle indefinido. |
| N08 | Worker consume un evento ajeno sin revalidación de pertenencia. |
| N09 | Cancelled/Skipped/Queued/In progress se contabiliza como PASS. |
| N10 | Efecto externo ambiguo tras timeout se repite sin idempotency key. |
| N11 | Backup sin cifrado o archivo legible por actor no autorizado. |
| N12 | Restore apuntando a PROD o mezclando objetos A y B. |
| N13 | Restore borra datos al fallar sin snapshot ni reversión. |
| N14 | Inferir inventario de archivos cero por ausencia de registros visibles. |
| N15 | Export A incluye un registro, agregado o adjunto de B. |
| N16 | Valor CSV `=HYPERLINK(...)` o fórmula similar se ejecuta en cliente. |
| N17 | Enlace expirado/revocado se reutiliza para descargar documento privado. |
| N18 | JWT, secreto, PII o URL firmada aparece en logs/BITACORA. |

## No funcionales RNF01–RNF12 — metas a fijar y verificar

| ID | Requisito medible futuro |
| --- | --- |
| RNF01 | Cada caché tiene TTL explícito, volumen limitado y política de invalidación. |
| RNF02 | Las claves privadas están segregadas por empresa/ambiente/tipo/recurso. |
| RNF03 | Retry tiene límite, backoff y deadline con estado final inequívoco. |
| RNF04 | Idempotencia soporta al menos dos consumidores simultáneos con prueba de integración. |
| RNF05 | Logs no contienen material secreto y admiten correlación de fallo. |
| RNF06 | Jobs tienen ACL, cancelación y control de presupuesto por tenant. |
| RNF07 | Backup/restauración usa cifrado, retención y acceso mínimo. |
| RNF08 | RPO/RTO tienen objetivo aprobado y medición de ensayo. |
| RNF09 | Exportes usan chunk/paginación y cuota de tamaño por tenant. |
| RNF10 | Enlaces temporales revocan correctamente y resisten replay. |
| RNF11 | UI futura debe ser ligera, accesible, con progresos y errores claros; Playwright cuando exista UI. |
| RNF12 | Releases exigen SHA exacto, Scope Lock, rollback y A5 independiente antes de QA/main/PROD. |

Estos requisitos no fijan números ficticios de latencia, RPO, RTO o capacidad: sus umbrales requieren decisión D01–D10 aprobada y baseline medible.

## Matriz de ejecución futura por capa

| Capa/superficie | Herramienta y evidencia exigida cuando haya cambios causales |
| --- | --- |
| Cache HTTP Angular/API | xUnit/Node/Playwright cuando corresponda, positivo y negativo, exact-SHA. |
| Outbox/worker .NET 10 | Build/test xUnit + integración de concurrencia real, reintentos y tenant A/B. |
| MySQL/EF, backup/restore | MySQL efímero, migraciones controladas, checksum y rollback sin tocar PROD. |
| Export, URL temporal y seguridad | HTTP/DB con permisos, CSV injection, expiración/replay y logs redacted. |
| Gobernanza | GitHub check-runs `completed/success` del SHA causal; A0–A4 y A5 de otro TASK_ID. |

**No habilitar M01.06/M06.06, JWT/RBAC nuevo, MySQL real, worker nuevo, dispatcher/eventos, R2, Hostinger, proveedores, QA/main/PROD** con la mera existencia de este documento. A5 debe conservar independencia del autor.

## Auditoría de esta microtarea y rollback

La unidad actual sólo agrega este Markdown y sección autocontenida del `CHANGELOG_AI.md`; no modifica API, infraestructura, frontend, DB, secretos, DNS, Cloudinary, backups, programaciones ni cuentas.

**A0:** revisar antecedentes M01.01–04, fuentes y límites, Scope Lock y DAG. **A1:** comprobar cobertura de 18 positivos, 18 negativos y 12 RNF como **futuros**, no ejecutados. **A2:** verificar que los criterios exigen aislamiento tenant A/B, JWT/RBAC server-side, secreto/PII y fail-closed. **A3:** coherencia con contratos de cache, outbox, storage y plan sin inventar restauraciones. **A4:** diff de 2 archivos Markdown, Git FF CAS, blobs y checks Scope Lock/procedencia V5 en `completed/success` sobre SHA propio, pruebas runtime NO_APLICA si el diff es sólo texto. **A5:** revisión posterior por otra Task Javier con TASK_ID distinto; QA/main/PROD requieren orden separada.

**Rollback:** revert aditivo no destructivo del commit documental tras revalidar HEAD. Las decisiones D01–D10, objetivos RPO/RTO y toda integración viva permanecen pendientes.
