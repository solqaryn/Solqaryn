# SOLQARYN V5 — C10-04.M01.03: inventario de contratos, código y evidencia para caché, jobs, backups y exportaciones

**Autor:** PRIMARY DEV 05 — JAVIER; TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; **PLAN_MAESTRO:** fila 1552; **proyecto:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK=STRICT**. Fecha: 2026-11-14.

## Alcance comprobable

La tarea M01.03 exige **leer contratos, código y evidencia disponibles sin asumir certificación**, dentro del bloque C10-04 «Caché, jobs, backups y exportaciones». La salida atómica es un inventario documental con blobs Git y brechas de evidencia; **no crea servicios, worker, respaldos, exportaciones, proveedores ni pruebas ejecutables nuevas**. Su antecedente documental C10-04.M01.01 definió NR01–NR16 y escenarios P01–P08/N01–N14 futuros. La habilitación dinámica DAG y COLA permite reclamar esta unidad, pero no demuestra que los predecesores de otras fases o A5 estén completos. Los contratos presentes en fuentes C03-04, C10-01, C10-02 y C10-03 no sustituyen las pruebas de recuperación reales.

## Inventario de diez fuentes originales y blobs inspeccionados en dev

| Ruta fuente | Git blob SHA | Hecho limitado a inspección estática |
| --- | --- | --- |
| `backend/src/Application/Services/OutboxRetryProcessor.cs` | `542c0a655a3ee0698e3d80f1854d43903b6470fa` | `IOutboxEffectDispatcher` y opciones `OutboxRetryProcessorOptions` (batch, máximo intentos, backoff, stale claim, jitter); no evidencia de un scheduler/worker genérico habilitado. |
| `backend/src/Application/Interfaces/IPublicStoreCache.cs` | `15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8` | Contrato cache del storefront; TTL por segmento identidad/tema/categorías 5 minutos, destacados 30 segundos y productos 15 segundos. |
| `backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs` | `1a102138b88bd54989eeade5acf7a5ee7bf63867` | Adaptador `IMemoryCache` por tenant, segmento, generaciones y hash de parámetros; semáforo por clave. No garantiza coherencia distribuida. |
| `backend/src/Infrastructure/Services/PublicStoreTenantKeyProvider.cs` | `78f3c4cdb24372e0cb7ebfd6bf6aa0e44040c45f` | Superficie de resolución empresarial para cache; sin suponer cobertura completa de todas las rutas ni de otros servicios. |
| `backend/src/API/Filters/PublicHttpCacheAttribute.cs` | `8c29b56fbdac3c01562a89ae4b982e4f74f6a4a7` | Filtro de caché HTTP pública GET/ETag con perfiles; no garantiza seguridad de cualquier endpoint no inspeccionado. |
| `backend/src/Application/Services/TiendaBootstrapService.cs` | `63f2497f44a4c580434675189174fca513ef4f8e` | Consumidor Application de contratos de caché y proveedor de tenant. |
| `backend/tests/Solqaryn.Tests/PublicHttpCacheAttributeTests.cs` | `e28a5cac8aaade85ebcadd762486e554130e2a5a` | Fuente xUnit con casos de caché HTTP; **no ejecutada** por esta revisión documental. |
| `backend/src/Infrastructure/Services/DevCloudinaryLegacyNormalizationHostedService.cs` | `863e035107b849fb97d9530ef85cf2af1d2311ae` | `BackgroundService` puntual DEV/opt-in, guardias de host/proveedor/MySQL/tenant; no autoriza reactivar flag ni acredita worker durable de negocio. |
| `frontend/scripts/validate-http-cache-contract.mjs` | `ddf0beb1a306f25e347a01d55d6b379186cf6b3a` | Verificador estático de contrato HTTP-cache; código presente, **no corrido** en esta microtarea. |
| `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs` | `454d588851ecc7db810259e63705852360172928` | Contrato de archivos de compra y sobrecargas tenant-aware; no implica un backup cifrado, recuperación o exportación general. |

También se cotejaron documentos del propio alcance: `docs/v5/c10-04-m01-01-alcance-no-regresion-cache-jobs-backups-exportaciones.md` (blob `3e7a76ea1edae3039c5c7545a2e10db123fa543a`) y `docs/v5/c10-03-m01-01-alcance-inventario-real-y-almacenamiento-futuro.md` (blob `ca7ccfaf423ead017a127acde393d53cf6ab181b`). Este último mantiene los archivos históricos en **INVENTARIO_DESCONOCIDO**; no está verificado que haya cero medios. No se consultaron Cloudinary, buckets, backups, bases productivas, archivos privados, secretos ni entornos ajenos.

## Doce brechas de evidencia (NO BUGS confirmados)

| Brecha | Lo que falta acreditar mediante pruebas y preflight autorizado posteriores |
| --- | --- |
| G01 | Namespace de caché privada por tenant y ambiente fuera del storefront público. |
| G02 | Invalidación consistente entre réplicas/nodos, si se incorpora caché distribuida. |
| G03 | Cache miss/falla y TTL expirado sin fugas cross-tenant en HTTP autenticado. |
| G04 | Correlación entre `OutboxRetryProcessor`, dispatcher y transporte externo real con idempotency key. |
| G05 | Semántica de claim/lease CAS y concurrencia real entre dos consumidores. |
| G06 | Backoff, deadline, dead-letter, cancelación y recuperación de crashes comprobados en integración. |
| G07 | Superficie real de backup cifrado, rotación de llaves, retención y control RBAC por ambiente. |
| G08 | Restauración efímera de MySQL con FK/schema íntegros y medición real RPO/RTO. |
| G09 | Inventario reconciliado de archivos y objetos por tenant, sin inferir cero histórico. |
| G10 | Generación de exportaciones con autorización server-side, cuotas, paginación, expiración y CSV injection. |
| G11 | Trazabilidad de jobs, export y restore con redacción de secretos, JWT, PII y URL firmada. |
| G12 | Pruebas causales .NET 10, MySQL efímero y aislamiento tenant A/B sobre el SHA de futuras implementaciones. |

La búsqueda por nombres `IExport`, `ExportService` y `BackupService` no aportó resultados concretos al inventario acotado; **no se concluye ausencia total** de funcionalidades de exportación o copias en el repositorio. Ni los scripts ni los xUnit enumerados se ejecutaron aquí. La observación de un `BackgroundService` DEV no implica plataforma de jobs habilitada ni permite instalar un dispatcher de Tasks.

## Matriz de aceptación FUTURA, no ejecutada

**Positivos P01–P08:** P01 caché A aislada; P02 caché B separada; P03 invalidación tras cambio; P04 idempotencia de dispatch; P05 retry acotado con backoff; P06 restore efímero con integridad; P07 export A autorizado y limitado; P08 logs mínimos con correlación.

**Negativos N01–N12:** N01 B lee caché A; N02 EmpresaId inyectado; N03 JWT/PII en caché pública; N04 TTL privado stale; N05 doble ejecución muta dos veces; N06 worker sin tenant verificado; N07 dead-letter ausente tras máximo intentos; N08 respaldo sin cifrado; N09 restore toca PROD; N10 CSV formula injection; N11 exposición de token en logs; N12 `SKIPPED` o `IN_PROGRESS` declarado como prueba exitosa.

**Todos son FUTUROS_NO_EJECUTADOS.** No se declara PASS de funcionalidad, integración, aislamiento HTTP/MySQL real ni vulnerabilidad confirmada. Para M03/M05 se requerirán builds/tests .NET 10, MySQL efímero, regresión tenant A/B, casos positivos/negativos, Gate Scope Lock exact SHA y revisión independiente según contrato. M01.06/M06.06, JWT/RBAC, migraciones, dispatcher y QA/main/PROD permanecen condicionados.

## Impacto, auditorías y rollback

**Impacto actual:** sólo este Markdown y una sección nueva autocontenida de `CHANGELOG_AI.md`; no cambio de API, DTO, UI, MySQL, infraestructura, secretos, backfills ni cron. **Capas a considerar después:** Domain y Application para idempotencia/ownership; Infrastructure para outbox/cache/backup; API para auth/capabilities; frontend para representación de estados; observabilidad y pruebas con datos efímeros.

**A0:** inventario 10 fuentes con blob verificable y contraste de prerequisitos. **A1:** matriz de fuentes, G01–G12 y P/N futuros; no PASS de test no ejecutado. **A2:** revisar requisitos tenant A/B, JWT/RBAC, aislamiento de objetos, secretos y PII sin certificación productiva. **A3:** contrastar caché, outbox, Cloudinary y el plan de backup con las fuentes, sin realizar restore. **A4:** comprobar diff únicamente Markdown/CHANGELOG, publicación fast-forward contra HEAD y Scope Lock/procedencia V5 `completed/success` en SHA exacto; CI runtime no relacionada = NO_APLICA motivado, no PASS. **A5:** otra Task Javier de TASK_ID distinto.

**Rollback:** nuevo commit de revert no destructivo sobre HEAD reconciliado, sin borrar historial ni modificar proveedores. Ningún gate de implementación o publicación se libera por este inventario.
