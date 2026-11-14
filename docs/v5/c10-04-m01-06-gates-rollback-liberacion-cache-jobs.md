# SOLQARYN V5 — C10-04.M01.06: bloqueos, reversión y liberación de preparación documental

**Unidad:** C10-04.M01.06, PLAN_MAESTRO fila 1555. **Autor:** PRIMARY DEV 06 / JAVIER_CHATGPT / TASK_ID 6af16bbad26881918c283abe06e1f32a. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA. **Repositorio:** solqaryn/Solqaryn rama dev; PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-14.

## Objetivo, alcance y preflight

La salida terminal M01.06 es un contrato **documental** de NO_GO, rollback y condiciones de liberación para C10-04 — caché, jobs, backups y exportaciones. No implementa servicios, no ejecuta worker ni webhook, no crea Tasks, no ejecuta SQL, no restaura backups y no autoriza Cloudflare R2, Hostinger, producción, QA/main o cambios de secretos.

Antes de reclamar se verificaron COLA G/O=SI, DAG F=VALIDADO/G=SI y ausencia de propietario, además de cinco antecedentes LISTO con A5 independiente y autor distinto del revisor:

| Microtarea antecedente | Contrato ya publicado | Revisión independiente acreditada |
| --- | --- | --- |
| C10-04.M01.01 | Objetivos y no-regresión de cache/jobs/backup | Autor DEV05; A5 DEV04 |
| C10-04.M01.02 | Actores, I/O y fronteras | Autor DEV04; A5 DEV05 |
| C10-04.M01.03 | Inventario de 10 fuentes, G01–G12 | Autor DEV05; A5 DEV06 |
| C10-04.M01.04 | R01–R16 potenciales, S01–S10 no verificados, D01–D10 pendientes | Autor DEV05; A5 DEV06 |
| C10-04.M01.05 | AC01–AC18, N01–N18, RNF01–RNF12 futuros | Autor DEV05; A5 DEV06 |

PLAN_MAESTRO K de las cinco filas fue leído con A5_PASS y propietario original conservado. **A5 de esta M01.06 no está aprobada por la Task autora** y debe realizarla otra Task Javier. Hasta ese momento, no liberar M02, M03 ni terminales posteriores por inferencia.

## Contrato de fronteras y dependencias

El futuro componente debe aislar claves de caché y recursos por EmpresaId y sucursal autorizada, mantener idempotencia en reintentos, proteger exportaciones frente a PII y formula injection, controlar cuotas/TTL, y establecer backups/restauración verificables con hashes. JWT/RBAC y contexto tenant se resuelven en backend; el navegador nunca define permisos vía slug, query, header, cookie o EmpresaId. Las capacidades de storefront público no habilitan inventario privado.

Cloudinary existente es transición documentada; R2/Hostinger constituye objetivo futuro, no infraestructura activada. Un documento M01 no representa un servidor, worker, dispatcher ni migración. No modificar identidad de seis Tasks, ni interpretar llamadas cruzadas entre Tasks como ejecución server-side perpetua.

## Bloqueos obligatorios B01–B14 y condiciones futuras

| Gate | NO_GO concreto | Condición para liberar en fases autorizadas |
| --- | --- | --- |
| B01 | Tenant A lee caché o exportación de tenant B | Prueba A/B negativa en CI y guard backend |
| B02 | EmpresaId de navegador controla cache key | Contexto servidor confiable y test manipulación |
| B03 | Job o worker ejecuta sin scope/rol | Capacidad mínima y auditoría de actor |
| B04 | Retry duplica operación comercial | Idempotency key y fallo/reintento verificados |
| B05 | Dispatcher crea bucles o no respeta cuota | Diseño con límites, sin implementarlo ahora |
| B06 | Exportación excede permisos o revela PII | RBAC, scope tenant y datos sintéticos |
| B07 | CSV puede ejecutar fórmula maliciosa | Escape y test negativo controlado |
| B08 | Fallo parcial publica archivo o trabajo incompleto | Reversión/compensación efectivas |
| B09 | Backup sin cifrado, cuota ni integridad | Secretos externos, checksum y autorización |
| B10 | Restore no ensayado en ambiente efímero | Recuperación y comparación de conteos/hashes |
| B11 | RPO/RTO no medidos se afirman satisfechos | Umbrales acordados y telemetría real |
| B12 | Se activa R2/Hostinger/servicio sin permiso | Gate comercial/técnico explícito |
| B13 | CI obligatoria queued, failed, skipped o cancelled | completed/success en SHA exacto |
| B14 | Se autoaprueba A5 o se altera owner de otra Task | Reviewer distinto, CAS + readback |

B01–B14 son hipótesis de control, no defectos comprobados de la aplicación. No se inventan tests, datos, cuotas, resultados de restore ni políticas comerciales finales.

## Reversibilidad contractual y restauración futura

**Rollback documental propio:** revert de Git normal de este commit y de CHANGELOG si fuera necesario, manteniendo historia, sin force-push ni editar BITACORA histórica. **Rollback runtime futuro NO_EJECUTADO:** conservar estado durable/idempotente, detener solo consumidores autorizados según contrato, probar compensación sin duplicados, restaurar únicamente desde copia validada en MySQL/almacenamiento efímero y verificar aislamiento tenant, registros y hashes antes de activar nueva ruta. Ninguna operación sobre datos Aiven, recursos R2/Cloudinary o backups reales se realiza ahora.

Fallos previsibles se manejan sin filtrar secretos ni PII: caché puede fallar de forma segura, jobs no provocan tormentas de reintentos, exportaciones no publican objetos truncados. El mecanismo real y los objetivos de recuperación se decidirán en microtareas con permisos efectivos y CI dirigida.

## Condición de salida del lote M01

1. Las cinco unidades C10-04.M01.01–05 están LISTO y A5_PASS independiente (verificado en preflight).
2. M01.06 obtiene A0–A4 documental, publicación CAS fast-forward a dev y checks de Scope Lock y procedencia de CHANGELOG completed/success de SHA exacto, con readback PLAN/COLA/BITACORA. Antes de esos readbacks el estado es VALIDANDO.
3. **M01.06 requiere su propia A5 por Task distinta de DEV06.** Hasta el registro independiente en PLAN/BITACORA, la dependencia M01.06+A5 sigue NO y no se debe liberar artificialmente.
4. Solo después se reevalúan PLAN H/I, COLA G/O y DAG F/G por ID exacto. El posterior M02, si se habilita, será diseño de contratos, no permiso de implementar workers o exportaciones productivas.
5. A5 o LISTO documental nunca autoriza JWT/RBAC, secrets, migración, datos Aiven, pagos, proveedores, QA/main/PROD o cambios de reglas de ramas.

## Oráculos futuros P01–P06 / N01–N10 — todos NO_EJECUTADOS

**P01–P06:** P01 seis antecedentes con A5 leídos por fuente institucional; P02 aislar caché A/B en fixture; P03 retry duplicado idempotente; P04 exportación sintética permitida; P05 backup efímero y restore con hash verificado; P06 fallo de worker recuperable sin pérdida ni fuga.

**N01–N10:** N01 A consulta caché B; N02 cliente altera EmpresaId; N03 worker autoejecuta sin scope; N04 retry duplica operación; N05 export revela filas B; N06 CSV formula injection; N07 restore checksum errado; N08 URL privada se utiliza con otro tenant; N09 CI pendiente se contabiliza PASS; N10 autor DEV06 se autoaprueba A5. Son escenarios futuros y **NO** pruebas realizadas en esta entrega.

## Impacto, revisión A0–A5 y control de publicación

**Impacto real:** únicamente docs/v5 y CHANGELOG_AI.md. No se crean rutas, APIs, workers, queues, migraciones, bases de datos, secretos ni proveedores. **A0:** source V5, skill canónica/HEAD, identity, scope lock, cinco A5 anteriores y DAG. **A1:** comprobar B01–B14, rollback y condiciones de liberación sin falsear ejecución. **A2:** límites tenant A/B, JWT/RBAC/PII/idempotencia. **A3:** coherencia con M01.01–05 y arquitectura cache, outbox, Cloudinary actual. **A4:** diff Markdown, fast-forward CAS y checks pertinentes completed/success exact SHA con readbacks. Backend/frontend/MySQL no causales al Markdown son NO_APLICA, nunca PASS de runtime. **A5:** pendiente de revisión independiente de otra Task Javier, no autoaprobación.
