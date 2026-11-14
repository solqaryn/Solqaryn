# SOLQARYN V5 — C04-04.M01.03: auditoría de fuentes, contratos y evidencias de cobros

**Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER; `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Proyecto:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** M01 análisis documental, no implementación ni activación de cobros.

## 1. Objeto y cadena de autoridad

El documento Drive V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` define la tarea exacta `C04-04.M01.03`: «Leer contratos, código y evidencia disponibles sin asumir certificación». `PLAN_MAESTRO!A688:O688` y `DAG_TECNICO!A649:G649` (tipo `DOC_M01_PREP`, F=`VALIDADO`, G=`SI`) delimitan la autorización para preparar, no ejecutar, pagos por enlace. Se verificó libre y sin responsable antes del claim, con predecesores M01 del área C01/C02/C03 habilitantes. Las unidades `C04-04.M01.01` y `.02` describen objetivo, actores y flujos; no acreditan por sí mismas un proveedor económico real.

**Regla de evidencia:** código o pruebas existentes muestran únicamente lo que contienen sus blobs. Un informe anterior de certificación N7.5/N7.8 y ejecuciones de GitHub pasadas son *referencias históricas*, jamás autoridad para declarar la nueva tarea V5 en LISTO. No se importan instrucciones ni backlog legacy.

## 2. Inventario de fuentes inspeccionadas en GitHub

| ID | Fuente actual | Blob SHA | Afirmación limitada y reproducible |
| --- | --- | --- | --- |
| SRC01 | `Application/Interfaces/IPagoOnlineProvider.cs` | `50ad09788c3ad31a1a727f9ae6b154834813ee26` | Puerto provider-neutral: empresa, factura, monto, moneda, referencia e idempotencia; URI de checkout opaca |
| SRC02 | `Application/Services/PagoOnlineService.cs` | `65272808514f2995f8c0abee5316aa872a9ce7a4` | Reutiliza intento por clave, valida saldo y moneda, recoge estado del proveedor; lock mediante ConcurrentDictionary/SemaphoreSlim es local al proceso |
| SRC03 | `API/Controllers/PagosOnlineController.cs` | `99962b2957f672a6dd435895daffaba7333f8a6b` | Authorize + permiso Facturación/Aplicar + control tenant al iniciar; no prueba que enlace sea un permiso |
| SRC04 | `API/Controllers/InboundWebhooksController.cs` | `05bc86051f32c2de75b68877b79bf70b82c07078` | Ruta por tenant/proveedor, secreto de configuración, límite de 256 KiB, firma y clasificación de resultado |
| SRC05 | `Domain/Entities/PagoOnline.cs` | `5063b0dc667583f7403fbeec879b519b6ca216d2` | Relación empresa/factura, proveedor, referencia, hash de idempotencia, estado/tiempos; no demuestra settlement real |
| SRC06 | `Domain/Entities/WebhookEntrante.cs` | `90294acb096f4ab228bc974fce3be7e7eadbd9d0` | Evento externo, correlación, hash y lifecycle para ingreso verificado; no dispatcher |
| SRC07 | `Infrastructure/Persistence/Configurations/PagoOnlineConfiguration.cs` | `83a9c3676937c94d84ea3ef55017a489479bec1a` | Check constraints de empresa/factura/monto/moneda y varios índices declarativos; la existencia de una configuración no verifica migración aplicada a MySQL en esta revisión |
| SRC08 | `Infrastructure/Persistence/Configurations/WebhookEntranteConfiguration.cs` | `8c0f9d0af84bf9aea28d57fc50f54c27f56ca570` | Índice único declarativo por EmpresaId/Proveedor/EventoExternoId y borrado restrictivo; sin readback de base real |
| SRC09 | `Infrastructure/Persistence/Migrations/20260914200100_N7_8_D_PagoOnlineIdempotencia.cs` | `b1513b82bcc49349e1200174cc964e32ef34509a` | Archivo de migración versionada existente; no se ejecuta ni se certifica en este análisis |
| SRC10 | `tests/Solqaryn.Tests/N78DPagoOnlineServiceTests.cs` | `102b9c59b0b0e49737bb9e543a71aa473e2caf72` | Cuatro casos xUnit definidos: replay proveedor llamado una vez, fallo seguro, URL HTTP no segura y provider sin configurar. Usa mocks; no se han ejecutado en esta tarea |
| SRC11 | `tests/Solqaryn.Tests/API/InboundWebhookIngressTests.cs` | `9ac95c8f29df6e98d5d16ce02c26cad5a2b87933` | Tests de firma, timestamp, replay/huella, tenant cruzado y payload demasiado grande; no equivale a evidencia de integración con proveedor live |
| SRC12 | `docs/CERTIFICACION_N7_8_PAGOS_ONLINE.md` | `d8e020e8309b843f8bc450c266ce07b9963b0c62` | Relato histórico del cierre ERP-N7.8; explícitamente no autoriza main/PROD y reconoce fallos globales de suites no causales |
| SRC13 | `docs/CERTIFICACION_N7_5_WEBHOOKS.md` | `f55507292686a1886a3dcd9fb5c34c14b7b6c985` | Relato histórico N7.5 de recepción segura; no certifica motor financiero V5, QA/main/PROD ni nueva automatización |

Los nombres de SRC01–SRC13 se expresan respecto de `backend/src/`, `backend/` o raíz del repositorio, según el prefijo de cada fila; los blobs son hashes de archivos leídos. No se infiere que todos los tests existentes corrieron en la CI de **este** SHA, ni que un servicio de pagos externo esté habilitado.

## 3. Contratos observados y límites por capa

| Capa | Hecho del código | Verificación que falta antes de una implementación financiera |
| --- | --- | --- |
| Domain | `PagoOnline` y `WebhookEntrante` conservan identidad de empresa e información de correlación | Máquina financiera de estados, compensación y auditoría contable aceptadas |
| Application | Proveedor abstracto e inicio/replay; control previo de saldo y moneda | Diferenciar idempotencia de payload idéntico y conflicto cambiado; locks distribuidos/transacciones reales |
| API | Permiso de facturación, tenant y `Idempotency-Key`; ingress firmado por proveedor/tenant | E2E cross-tenant y rechazo de callback tardío bajo configuración de staging controlado |
| Infrastructure | Configuraciones EF, índices/constraints, migraciones versionadas | CI MySQL efímero y readback de esquema exacto, sin tocar Aiven productivo |
| Pruebas | xUnit documenta casos positivos y negativos, varios con mocks | Ejecución real en GitHub Actions del SHA de cambio de código correspondiente |
| Integración externa | `IPagoOnlineProvider` desacopla credenciales y URL | Proveedor comercial, límites, moneda, gestión de claves, monitoreo y autorización individual futura |
| Historial N7.5/N7.8 | Evidencia previa conservada | No sustituir plan V5, A0–A5 ni integridad institucional actual |

## 4. Matriz de discrepancias y decisiones por resolver

| ID | Riesgo real o incertidumbre | Tratamiento de esta M01.03 | Gate de etapa futura |
| --- | --- | --- | --- |
| GAP01 | El lock de idempotencia en memoria no coordina múltiples procesos | Registrar explícitamente límite, sin llamarlo CAS distribuido | Unicidad DB, concurrencia multi-instancia y colisión/replay demostradas |
| GAP02 | La configuración de índice EF no acredita esquema realmente desplegado | Solo registrar el índice | Migración reversible probada en MySQL efímero y revisión exact-SHA |
| GAP03 | URL de proveedor opaca podría confundirse con autorización SaaS | Mantener RBAC/tenant en backend separadamente | Tests positivos/negativos con actor A y recurso B |
| GAP04 | Timeout del proveedor puede implicar estado ambiguo | No marcar pagado ni fallido definitivo por inferencia | Conciliación contra fuente aprobada e idempotencia durable |
| GAP05 | Una firma correcta no implica asiento contable autorizado | Distinguir recepción de webhook de ledger | Motor reconciliador futuro auditado, sin dispatcher implementado aquí |
| GAP06 | Documentos antiguos contienen términos "certificación" | Tratar como historia limitada | READBACK nuevo PLAN/COLA/BITACORA + checks causales |
| GAP07 | Pruebas basadas en mocks no conectan pasarela live | Declarar NO_APLICA a integración de provider en M01 | Credenciales y servicios aprobados por propietario en entorno permitido |
| GAP08 | Secretos o tarjetas en logs representarían exposición | Prohibición explícita de PAN/CVV y secret | Escaneo de secretos, logs seguros y pruebas de error sin filtraciones |
| GAP09 | Cobro y suscripción pueden divergir | Ningún estado automático derivado solo de un callback | Reglas ledger/suscripción/entitlements aprobadas y probadas |
| GAP10 | Rollback podría borrar historial y facturas | Solo revert de documentación; prohibir operaciones destructivas | Plan de compensación financiera y migraciones reversibles |
| GAP11 | Un evento firmado por tenant B podría apuntar a referencia A | Mantener correspondencia empresa/proveedor/evento/factura | Negativo cross-tenant con persistencia y auditoría, no solo mocks |
| GAP12 | Reintentos y callbacks fuera de orden podrían duplicar efectos | No afirmar exactamente-una-vez por existencia de clave | Concurrencia transaccional, índices, reintentos y readback DB |

## 5. Oráculos documentales auditables (no tests ejecutados)

**Positivos documentales P01–P05:** P01 el contrato identifica proveedor y URI opaca; P02 la ruta exige permiso/tenant; P03 el servicio rechaza saldo o moneda incoherentes; P04 existen pruebas xUnit de replay y firma; P05 el modelo registra identificador externo y huella, separando evento y pago.

**Negativos documentales N01–N12:** N01 ningún documento afirma un pago live; N02 ningún secreto se copia aquí; N03 ningún índice EF se declara migración ejecutada; N04 no se confunde lock local con distribuido; N05 no se confunde URL con RBAC; N06 no se confunde firma con settlement; N07 no se atribuye ejecución de los xUnit a esta documentación; N08 no se habilita dispatcher; N09 no se modifica plan financiero; N10 no se mezcla otro tenant; N11 no se usa archivo N7 como backlog ejecutable; N12 no se autoriza QA/main/PROD.

Estos oráculos verifican **integridad semántica del análisis**, no pruebas de backend/API/MySQL/Playwright ni transacciones reales. Quedan como entradas de diseño y prueba a futuras microtareas elegibles.

## 6. Impacto, A0–A5 y rollback

Esta microtarea edita exclusivamente un archivo nuevo `docs/v5/c04-04-m01-03-auditoria-fuentes-contratos-cobros.md` y una sección autocontenida de `CHANGELOG_AI.md`. No cambia código, API, configuración, proveedor, migraciones, arquitectura, secretos ni datos de usuario; no crea PR, rama, Task ni servicio.

**A0:** alcance V5, DAG, identidad, fuentes y blobs comprobados. **A1:** lectura documental de 13 fuentes y cinco oráculos positivos. **A2:** doce negativos y riesgos multiempresa/financieros explícitos. **A3:** cruces entre Domain/Application/API/EF/tests y documentación histórica, sin elevar esas pruebas a CI del SHA nuevo. **A4 posterior:** publicar por fast-forward con HEAD CAS, verificar diff, Scope Lock y procedencia CHANGELOG `completed/success` sobre SHA exacto y leer PLAN/COLA/BITACORA. Si falla cualquier gate requerido, conservar VALIDANDO y no publicar LISTO ficticio. Las suites runtime no se invocan ni cuentan PASS en diff solo Markdown (`NO_APLICA` motivado).

**A5_PENDIENTE:** revisión independiente por otra de las cinco Tasks Javier, distinta de la autora, previa a cualquier promoción. **Rollback:** `git revert` normal sobre dev (si es necesario), sin force-push y sin alterar BITACORA histórica. No se habilita transacción ni QA/main/PROD.
