# SOLQARYN V5 FINAL — C02-02.M02.06: dependencias y contrato de salida shared/dedicated

**Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT; TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. **Ámbito:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

## 1. Resultado real y límites de la entrega

Este artefacto consolida los contratos M02.01–M02.05 de C02-02 y fija criterios de salida verificables para la preparación de implementación posterior. **Es documentación contractual:** no crea router shared/dedicated, DbContext adicional, endpoints, entidades EF, migraciones MySQL, colas, workers, webhooks, secrets, asignaciones de tenant, selección de proveedor ni transferencias de datos. No representa ejecución de un backup o una restauración. No autoriza QA, main, producción, DNS, Hostinger, Cloudflare, Aiven ni cambio de cuenta.

La línea base actual preservada es ASP.NET Core 10, capas Domain/Application/Infrastructure/API, EF Core 10.0.12, proveedor MySql.EntityFrameworkCore 10.0.9, JWT/RBAC server-side, AppDbContext, StorageTenantContext y UsuarioEmpresa; Angular 22.2.1 no puede determinar la autoridad de datos. La arquitectura futura es materia de diseño y autorización separada.

## 2. Prerrequisitos inspeccionados

| ID | Documento/commit fuente | Evidencia y límite |
| --- | --- | --- |
| C02-02.M01.06 | d2ff35ff801901af12c32888f24ae744fe4b11bb | Condiciones M01 y A5 independiente registradas; no acredita migración |
| C02-02.M02.01 | 86316ea0d0470aa16dc58f1bf781de331d14c35a | TEN01–TEN14, INV01–INV16 y 8 pares P/N **futuros**; A5 J00 |
| C02-02.M02.02 | d80e102b31425530acc480e51387acefde8ec094 | API01–08, DTO, ERR01–12, VAL01–14; A5 J00; ningún endpoint ejecutable |
| C02-02.M02.03 | a3bbb0b9c2b164cdfb04ed7f11d23ab90142549b | Persistencia/índices propuestos y fixtures no ejecutados; A5 J00 |
| C02-02.M02.04 | 36b6a099ea3e6968375cf9b25873f2e6ff9de0b3 | CFG01–16 y flags futuras apagadas; A5 J00; ninguna configuración activa |
| C02-02.M02.05 | 39468082745dabaacf1348e6e7904b715945c369 | Señales SIG01–08 no emitidas, correlación e idempotencia; A5 J00 |

El DAG_TECNICO F=VALIDADO/G=SI de M02.06 y COLA G/O=SI autorizaron exclusivamente **esta preparación**, con reclamación por transición condicional del estado PENDIENTE a EN_PROGRESO y propietario único en PLAN. Un A5 documentario es evidencia del contrato, no certificación de integraciones o seguridad real.

## 3. Condiciones de salida contractual (SAL01–SAL16)

| Control | Exigencia verificable antes de autorizar implementación posterior |
| --- | --- |
| SAL01 | Mantener una autoridad backend para identificar empresa, usuario, membresía y alcance; nunca confiar en cabeceras ni parámetros del navegador |
| SAL02 | Verificar JWT válido, RBAC vigente y pertenencia a tenant para cada acción, con denegación por defecto |
| SAL03 | Modelo TEN01–TEN14 y relaciones revisados sin duplicar tablas actuales ni asumir migraciones aplicadas |
| SAL04 | Toda intención de placement se vincula a tenant, actor, revisión esperada y expediente idempotente |
| SAL05 | API/DTO futuras versionadas, con errores saneados, sin divulgar existencia de recursos de otra empresa |
| SAL06 | El destino de datos y los secretos son exclusivamente server-side; rechazar DSN o selector de infraestructura del cliente |
| SAL07 | Definir rutas SHARED/DEDICATED sin fallback automático a almacenamiento de otra empresa |
| SAL08 | Índices y FKs de diseño no se convierten en DDL sin revisión y CI MySQL efímera de aislamiento A/B |
| SAL09 | Las flags inseguras permanecen apagadas; ninguna elimina comprobaciones JWT, tenant, Scope Lock o autorización |
| SAL10 | Cutover requiere prueba futura de snapshot y restore, integridad, conteos, FKs y ventana aprobada, no evidencia textual |
| SAL11 | Dual-write, dual-read y reconciliación requieren estrategia de consistencia, límites, observabilidad y rollback probados |
| SAL12 | Eventos futuros exigen cuota, clave idempotente por tenant/operación/revisión y redacción de PII/secretos |
| SAL13 | Auditoría debe enlazar SHA, actor, intención, transición, decisión y resultado sin credenciales ni datos sensibles |
| SAL14 | Fallo parcial, timeout, cuota o pérdida de contexto obligan NO_GO sin mutación cruzada |
| SAL15 | Las regresiones .NET/Angular/Node/MySQL/seguridad pertinentes deberán ejecutarse en GitHub Actions sobre SHA aplicable |
| SAL16 | Toda publicación posterior exige DAG y COLA vigentes, owner exclusivo, A0–A4, A5 de otra Task Javier y autorización de entorno distinta si procediera |

## 4. Dependencias e hitos posteriores

La secuencia M03.01–M03.06 **requiere código real** por alcance: Domain, Application, autorización/tenancy, adaptadores, integraciones cuando apliquen, auditoría y errores. Sus pruebas deberán cubrir casos positivos y negativos, revocación, tenant A/B, intentos repetidos, revisión/versión alterada y manejo fail-closed. No sustituir implementación por matrices Markdown.

El DAG de C02-02.M03.01 exige C02-02.M02.01–06 **LISTO y A5**, más C01-01.M06.06 con A5; por tanto, publicar este documento o dejarlo VALIDANDO **no libera M03.01**. Las sucesoras con prerrequisitos adicionales permanecen NO hasta que las fórmulas verificadas los acrediten. No alterar manualmente COLA G/O ni DAG G.

## 5. Pruebas positivas/negativas previstas, NO EJECUTADAS

| Caso | Positivo futuro | Negativo futuro que debe negarse |
| --- | --- | --- |
| PN01 | Tenant A solicita sólo su propio estado | Tenant B consulta o infiere estado de A |
| PN02 | JWT válido y membresía con permiso verifican propuesta | Token ausente, revocado o rol insuficiente |
| PN03 | Binding resuelto desde backend autorizado | DSN, host o tenant arbitrario en payload cliente |
| PN04 | Solicitud con revisión y clave idempotente coincide | Clave duplicada con payload/revisión divergente |
| PN05 | Target previo verificado habilita solicitud de rollback | Git revert documental usado como prueba de restore |
| PN06 | Snapshot y restore efímero pasan integridad y FK | Snapshot ausente, checksum incorrecto o destino cruzado |
| PN07 | Flags seguras mantienen validación y registro | Flag abre bypass de permisos o escritura dual insegura |
| PN08 | Error saneado y auditoría sin secretos | Exposición de DSN, token, PII o existencia cross-tenant |
| PN09 | Cambio de modo con prueba de compatibilidad versionada | Cliente legado alterado sin ventana ni reversión |
| PN10 | CI dirigida exact-SHA positiva/negativa en entorno efímero | CANCELLED, SKIPPED, queued o failure presentados como PASS |

Estos casos **no son tests xUnit, Playwright, HTTP, MySQL, restore ni pentesting realizados**; son criterios pendientes para implementación y CI futura.

## 6. Riesgos abiertos y decisiones no delegadas

- **R01, consistencia:** sin transacciones/compensación verificadas no aprobar dual-write o cutover.
- **R02, aislamiento:** toda lectura y escritura deben probar tenant A/B y ausencia de fallback cruzado.
- **R03, IAM:** cambios de JWT/RBAC, control plane y claves requieren alcance y autorización específicos, no este contrato.
- **R04, regresión:** cambios de proveedor o configuración pueden romper contratos vigentes; exigir suite dirigida y rollback causal.
- **R05, restauración:** git revert sólo revierte archivos Git; los datos requieren copia, restauración y verificación independiente.
- **R06, cuota/PII:** no registrar tokens ni DSN ni publicar eventos sin dispatcher autorizado, idempotencia y límites.
- **R07, simultaneidad:** una Task no puede asumir owner de otra con lease activo; no simular CAS con lectura y escritura.
- **R08, promoción:** ninguna evidencia DEV habilita QA, main o PROD sin autorización separada.

## 7. A0–A5, evidencia y rollback

**A0:** comprobar cambio limitado a este Markdown más una sección nueva de CHANGELOG_AI.md, sin encabezados vacíos ni viñetas intercaladas. **A1:** revisar SAL01–16, PN01–10 y relaciones de M02.01–05; oráculos futuros explícitos. **A2:** inspección documental JWT, RBAC, tenant A/B, permisos, confidencialidad y fail-closed, sin declarar test real. **A3:** consistencia con M01.06, plan V5 y arquitectura actual, sin modificar APIs, bases ni consumidores. **A4:** commit fast-forward/CAS a dev sobre HEAD vigente; diferenciar CI causal Scope Lock y procedencia V5 SUCCESS exact-SHA de compilaciones .NET/UI/MySQL no causales a este diff. Pasar por VALIDANDO y registrar/readback PLAN/COLA/DAG/BITACORA antes de LISTO_DEV. **A5:** otra Task Javier distinta de autora debe revisar contenido, SHA, blobs, checks y dependencias; no autoaprobación.

**Rollback documental:** git revert normal del commit en dev, sin force-push y preservando cambios ajenos. Este rollback no restaura MySQL ni autoriza cambios de proveedor, DNS, secretos, datos de empresa o producción.
