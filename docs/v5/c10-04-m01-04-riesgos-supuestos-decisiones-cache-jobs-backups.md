# SOLQARYN V5 — C10-04.M01.04: riesgos, supuestos y decisiones pendientes de caché, jobs, backups y exportaciones

**Autor:** PRIMARY DEV 05 — JAVIER · TASK_ID `6adc2e55f47c81919f6374e561687076`. **Unidad:** PLAN_MAESTRO fila 1553, C10-04.M01.04 (M01 documental). **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Rama:** `solqaryn/Solqaryn@dev`. **Scope Lock:** STRICT. **Fecha:** 2026-11-14.

## Objetivo, verdad observable y no-alcance

La microtarea registra **riesgos potenciales, supuestos verificables y decisiones pendientes** para el bloque C10-04 (caché, jobs, backups y exportaciones), donde el resultado futuro previsto es aislamiento, idempotencia, límites de retries y restauración. Ningún elemento de las matrices siguientes constituye una falla ya reproducida o vulnerabilidad confirmada. La preparación no instala cron, workers, dispatcher de Tasks, proveedores, servicios de pago, snapshots, migraciones ni reportes ejecutables; no consulta datos reales.

Fuentes y límites ya constatados:
- `backend/src/Application/Interfaces/IPublicStoreCache.cs` blob `15aeb1c7f4afa6c3b8422b3bd0700d345446f3f8` y `backend/src/Infrastructure/Services/PublicStoreMemoryCache.cs` blob `1a102138b88bd54989eeade5acf7a5ee7bf63867`: caché de storefront in-process tenant-bound; **no certifican** coherencia multiinstancia.
- `backend/src/Application/Services/OutboxRetryProcessor.cs` blob `542c0a655a3ee0698e3d80f1854d43903b6470fa`: opciones de procesamiento y contrato dispatcher; no prueba workers genéricos activos.
- `backend/src/Infrastructure/Services/DevCloudinaryLegacyNormalizationHostedService.cs` blob `863e035107b849fb97d9530ef85cf2af1d2311ae`: worker DEV opt-in estrictamente acotado; no habilitarlo desde esta fase.
- `backend/src/API/Filters/PublicHttpCacheAttribute.cs` blob `8c29b56fbdac3c01562a89ae4b982e4f74f6a4a7`: filtro de GET público, no significa que sea apto para respuestas privadas.
- `docs/v5/c10-04-m01-01-alcance-no-regresion-cache-jobs-backups-exportaciones.md` y `docs/v5/c10-04-m01-03-inventario-codigo-cache-jobs-backup-exportaciones.md` son preparaciones M01 previas; no realizan pruebas de funcionalidad. C10-04.M01.02 pertenece a otro autor y no se modifica.
- C10-03 mantiene **inventario histórico NO_VERIFICADO**, nunca asumir cero. R2/Hostinger son topología objetivo; Vercel/Render/Aiven/Cloudinary permanecen vigentes hasta autorización separada.

## Matriz de riesgos R01–R16 — hipótesis, no defectos acreditados

| ID | Riesgo POTENCIAL | Mitigación o evidencia exigible posteriormente |
| --- | --- | --- |
| R01 | Reutilización de clave de caché entre tenant A y B | Validar namespace server-side, partición por empresa y pruebas A/B. |
| R02 | Caché pública conserva token o respuesta privada | Allowlist de endpoints GET anónimos y negativa a cachear JWT/PII/checkout. |
| R03 | Invalidación en nodo A no llega a nodo B | Medir coherencia real, invalidación generacional o adaptación explícita. |
| R04 | TTL expirado o errores retornan información obsoleta y no autorizada | Pruebas de caducidad, fallo y comportamiento fail-closed. |
| R05 | Clave de idempotencia colisiona por tenant o tipo de evento | Scope por empresa, versión, operación y payload canonicalizado. |
| R06 | Reintentos duplican pago, correo o mutación comercial | CAS, dispatcher idempotente y pruebas de doble consumidor. |
| R07 | Reintentos infinitos agotan presupuesto y bloquean cola | Límites, backoff/jitter, timeout, dead-letter y métricas. |
| R08 | Worker reutiliza autorización antigua o tenant del cliente | Revalidación de membership y ownership en ejecución. |
| R09 | Timeout cancela sin saber si efecto externo ocurrió | Correlación, reconciliación de resultado e idempotency key de proveedor. |
| R10 | Backup sin cifrado/retención expone información empresarial | Autorización y custodia explícitas, cifrado y pruebas de acceso denegado. |
| R11 | Restore mezcla tenants o sobrescribe datos productivos | Restauración efímera segregada, hash, permisos y rollback ensayado. |
| R12 | RPO/RTO se afirman sin cronometraje real | Objetivo formal + medición de escenarios de recuperación. |
| R13 | Inventario de archivos históricos se asume vacío | Reconciliar metadatos DB/Cloudinary; desconocido no equivale a cero. |
| R14 | Export de tenant A expone registros B o PII excesiva | RBAC server-side, filtrado, redacción y pruebas cruzadas. |
| R15 | CSV inyecta fórmulas o URL de descarga tiene replay | Sanitización, límites MIME/size, TTL/revocación y tests negativos. |
| R16 | Logs o BITACORA filtran claves y payloads sensibles | Minimización, pseudonimización, exclusión explícita de secretos. |

## Supuestos S01–S10 — todos pendientes de acreditación

| ID | Supuesto a verificar | Estado actual y evidencia necesaria |
| --- | --- | --- |
| S01 | Solo contenido público es elegible para caché HTTP compartida | PENDIENTE: verificar ruta, auth y respuesta real. |
| S02 | `IPublicStoreCache` cubre todas las rutas de la superficie prevista | NO_VERIFICADO: inventariar consumidores y futuros privados. |
| S03 | Despliegue podría tener más de una instancia de API | NO_VERIFICADO: topología/instancias reales y coherencia. |
| S04 | Outbox dispone de persistencia y límites configurados según ambiente | NO_VERIFICADO: integración EF/MySQL y pruebas reales. |
| S05 | Todo efecto externo admite idempotencia o compensación | NO_VERIFICADO: contrato particular por proveedor. |
| S06 | Es posible custodiar llaves de backups sin nuevos secretos en Git | NO_VERIFICADO: autorización/proveedor/custodia. |
| S07 | RPO/RTO se pueden cumplir con recursos aprobados | NO_VERIFICADO: objetivos, presupuesto y ejercicios. |
| S08 | Archivos históricos son cero o requieren migración | **INVENTARIO_DESCONOCIDO**: conteos firmados de fuentes reales. |
| S09 | CSV/ZIP/exportes pueden operar dentro de cuotas pactadas | NO_VERIFICADO: tamaños, permisos y regresión. |
| S10 | Un sandbox MySQL efímero representa el esquema de DEV | NO_VERIFICADO: migraciones exactas, fixtures y reproducibilidad. |

Un supuesto NO_VERIFICADO impide usarlo para declarar un sistema desplegado, seguro o certificado. No se interpreta como permiso para acceder a credenciales.

## Decisiones D01–D10 — requieren autoridad técnica/comercial posterior

| ID | Decisión abierta | Condición de aprobación futura |
| --- | --- | --- |
| D01 | Caché solo in-process o distribuida tenant-aware | Evidencia de carga, instancias, costo y seguridad cross-tenant. |
| D02 | TTL/invalidation por tipo de dato y consistencia | Clasificación público/privado y pruebas medibles. |
| D03 | Contrato outbox/idempotencia de proveedores externos | Diseño del efecto, replay, compensación y deduplicación. |
| D04 | Hosting/dispatch real de jobs y triggers permitidos | Nuevo alcance autorizado, sin llamadas perpetuas entre Tasks. |
| D05 | Límite de concurrencia, retry, backoff y dead-letter | Medición de saturación y control de cuotas. |
| D06 | Formato, destino y custodia de backups | Autorizaciones, cifrado, retención y restore efímero. |
| D07 | Objetivos RPO/RTO y frecuencia de drills | Aprobación de responsables, medición y reporte. |
| D08 | Modelo de exportaciones y enlace temporal | RBAC, cuota, auditoría, PII, expiración y eliminación. |
| D09 | Manejo de archivos históricos si inventario es positivo | Conciliación real, manifiesto, checksum y plan rollback. |
| D10 | Gate de promoción a QA/main/PROD | Orden expresa, pruebas causales y A5 independiente. |

**Ninguna D01–D10 queda aprobada** por este documento. No asignar fecha de implantación, proveedores, credenciales, entornos ni acciones humanas implícitas.

## Escenarios futuros de aceptación, sin ejecución

Positivos propuestos P01–P08: caché A aislada; caché B independiente; invalidación tras escritura; job idempotente repetido; retry con timeout acotado; restauración con FK/checksum en MySQL efímero; export autorizado y sanitizado; auditoría sin secretos.

Negativos propuestos N01–N12: cross-tenant A/B; EmpresaId forjado; dato privado en GET cacheado; TTL inválido; doble efecto; job sin pertenencia; retry infinito; backup sin cifrado; restore hacia producción; CSV con fórmula; URL de export vencida/replay; fuga de secreto a logs.

**Todos los P/N anteriores son FUTUROS_NO_EJECUTADOS**: el inventario no certifica comportamiento JWT/RBAC/DB/backend, no detectó bug confirmado y no constituye resultado de test. No confundir `SKIPPED`, `CANCELLED`, `QUEUED` o `IN_PROGRESS` con `PASS`.

## Impacto, revisiones y rollback

Cambios previstos en esta microtarea: exclusivamente Markdown de M01.04 y sección autocontenida en `CHANGELOG_AI.md`. No modifica API, DTO, frontend, Domain, Application, Infrastructure, workers, credenciales, variables, base de datos ni rutas.

**A0:** cotejar fuentes Git y antecedentes de diseño C10-04.M01.01/.02/.03. **A1:** revisar R01–R16/S01–S10/D01–D10 y escenarios futuro P/N con positivos/negativos documentales, no ejecutables. **A2:** controles JWT/RBAC/tenant A/B, PII, cifrado y autorización de backups/exports sin certificar runtime. **A3:** compatibilidad documental con C03-04, C10-01/02/03 y contratos inspeccionados. **A4:** diff de solo Markdown y CHANGELOG, fast-forward CAS dev, blob SHA y checks Scope Lock/V5 `completed/success` SHA exacto. Compilaciones no causales para Markdown se marcan NO_APLICA motivado, nunca PASS inventado. **A5:** revisión independiente por Task Javier distinta, pendiente.

Rollback documental: nuevo commit de `git revert` sin force-push y con HEAD actual preservado. Ningún gate terminal M01.06/M06.06 ni acción de QA/main/PROD se libera por este registro.
