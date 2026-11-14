# SOLQARYN V5 — C09-02.M01.04: riesgos, supuestos y decisiones pendientes de fidelización y reputación

**Microtarea:** C09-02.M01.04 · PLAN_MAESTRO fila 1337 · **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Alcance autorizado M01:** registrar riesgos potenciales, supuestos y decisiones humanas por resolver **sin convertir incertidumbres en vulnerabilidades confirmadas, autorizaciones ni funciones ejecutables**. Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Baseline causal verificado

- M01.01 `docs/v5/c09-02-m01-01-alcance-fidelizacion-reputacion.md`, blob `1030c70296220626c5341f58d99e68e47b8a6cb2`: NR01–NR14, P01–P08/N01–N08 FUTUROS_NO_EJECUTADOS.
- M01.02 `docs/v5/c09-02-m01-02-actores-interfaces-fidelizacion.md`, blob `7de37b4ccfa35cd07245975b97cf04dcca2b59a7`: A01–A14, I01–I12, F01–F09.
- M01.03 `docs/v5/c09-02-m01-03-inventario-codigo-brechas-fidelizacion.md`, blob `b5bbb9d2c8e85814390bd12c85e5f4d6bd3bfed7`: 12 rutas Git originales y G01–G12 por verificar; no fue prueba runtime.
- Fuentes comprobadas incluyen `Cliente.cs` (`8c6d4c4453e44f8c0be9475340465e1aadb3f787`), `Venta.cs` (`64ab3cd92e81031e74137f420cd748c564e28f01`), `CreditoCliente.cs` (`651fb9f9530daded4a11611a7d58a1f1cc401183`) y `UsuarioEmpresa.cs` (`f9b0f6187fdace08d12e9e3ed8d5c5ceadaccf3c`). Una ruta existente **no acredita** ledger de puntos, consentimientos, autorización de promociones o sistema de reseñas.

## R01–R14: riesgos POTENCIALES, no hallazgos confirmados

| ID | Riesgo por hipótesis | Mitigación y prueba causal posterior | Contrato afectado |
| --- | --- | --- | --- |
| R01 | Cruce de saldo o ledger entre tenants por identificadores predecibles | Aislamiento A/B en API/EF, ownership desde backend, deny-by-default | P01/N01, G03-G04 |
| R02 | Doble acreditacion por replay de venta o retry del proveedor | Idempotency key tenant-scoped, unique index y transaccion atómica | P02/N02, G02/G05 |
| R03 | Canje concurrente crea saldo negativo | Version optimista o lock transaccional y test carrera MySQL | P03/N03, G05 |
| R04 | Puntos se confunden con credito monetario y alteran contabilidad | Separar ledger lealtad y CreditoCliente/Venta; reconciliar | NR05, G01/G10 |
| R05 | EmpresaId=NULL de legado se atribuye al tenant erróneo | Backfill solo con prueba ownership y cuarentena sin acceso | NR03, G03 |
| R06 | Promociones expiran/caducan desfasadas por reloj, cache o zona | UTC, vigencia tipada, snapshot de regla, deny on timeout | P04/N04, G06 |
| R07 | Reseñas muestran PII o HTML/XSS | Sanitizacion contextual, moderacion y redaccion | P05/N05, G07 |
| R08 | Moderador altera reseña sin rastro o sin apelacion | Motivo/version/actor y reversión auditada por permiso | P06/N06, G08 |
| R09 | Opt-out no corta campaña por retry o cache | Supresion comprobada al envio, historial por canal | P08/N08, G09 |
| R10 | Posventa permite cruzar Cliente/Venta/Ticket de otro tenant | RBAC por recurso y filtrado serverside, IDs opacos | P07/N07, G03/G10 |
| R11 | Logs y exportación revelan correo, teléfono o saldo | Redaccion PII, minimizacion, rate limit, retencion | G11, NR09 |
| R12 | Dependencia de worker/proveedor externo reintenta sin limite | Backoff, cuotas, circuit breaker; sin activar worker en M01 | NR12, G11 |
| R13 | La UI asume permisos de Angular sin verificacion de API | Backend JWT/RBAC/tenant, negativas al saltar guard | NR02/NR08, G04/G12 |
| R14 | Deshabilitar programa borra historial o rompe ventas existentes | Flags seguros, disable reversible, sin hard delete de operaciones | NR14, G10 |

Estas hipótesis deberán convertirse en fallos demostrados solamente después de observar implementación, entradas maliciosas y resultados reproducibles. **Ninguna corresponde a vulnerabilidad actual confirmada por esta tarea**. Priorización: R01/R02/R03/R05/R09/R10 son críticos para puertas de tenant, integridad y consentimiento si la funcionalidad llega a activarse; los demás no autorizan bypass.

## S01–S08: supuestos explícitos sin autoridad

| ID | Supuesto | Estatus | Cómo acreditarlo |
| --- | --- | --- | --- |
| S01 | Las 12 rutas inspeccionadas representan solo una muestra; NO hay inventario exhaustivo de cada módulo. | NO_VERIFICADO | Busqueda de símbolos y tests en implementación M03 |
| S02 | El negocio podría optar por puntos NO monetarios y sin conversion a efectivo. | NO_DECIDIDO | Decisión económica/legal explícita previa a M02 |
| S03 | Una misma persona puede poseer cuentas de tienda en varias empresas sin compartir saldo. | NO_VERIFICADO | Fixtures multiempresa y contrato de identidad |
| S04 | Las ventas anuladas podrán distinguirse por estado confiable, sin confiar en UI. | NO_VERIFICADO | Trazabilidad venta/confirmación/cancelación |
| S05 | Se dispondrá de un reloj UTC y políticas de expiración versionadas. | NO_VERIFICADO | Contrato tipado y prueba límite temporal |
| S06 | La moderación puede exigir política de apelación y auditoria propia. | NO_DECIDIDO | Aprobación de owner producto/jurídico |
| S07 | Consentimiento granular y opt-out por canal requerirán fuente trazable. | NO_VERIFICADO | Plan de datos, retención y prueba reenvío |
| S08 | La operación actual no necesita proveedor externo para documentar riesgos. | VERIFICABLE_M01 | No introducir secrets/eventos/worker ni gasto |

Ningún supuesto `NO_VERIFICADO` se usa para liberar migraciones, permisos, servidores, DNS, facturación, campañas, A5 o producción. Los datos con `EmpresaId=NULL` continúan sin pertenencia acreditada y deben tratarse como bloqueados para acceso multiempresa hasta reconciliación.

## D01–D08: decisiones que requieren autoridad específica antes de implementación

| ID | Decisión pendiente | Responsable propuesto (no asignación de permisos) | Momento de gate |
| --- | --- | --- | --- |
| D01 | Determinar si puntos tienen valor económico o son simbólicos | Propietario producto/finanzas | Antes de contratos monetarios, nunca por inferencia |
| D02 | Fijar política de ownership Cliente/Venta legacy EmpresaId=NULL | Responsables datos/seguridad | Antes de API y backfill |
| D03 | Aprobar reglas de expiración, reversión y redondeo de incentivos | Producto/finanzas | Antes de persistencia e implementación |
| D04 | Seleccionar permisos granulares de canje, ajuste y moderación | Seguridad/RBAC | Antes de API/DTO y roles |
| D05 | Definir consentimiento, retención y supresión por canal | Privacidad/producto | Antes de campañas o envío |
| D06 | Aprobar moderación, apelación y manejo de contenido sensible | Producto/seguridad | Antes de reseñas públicas |
| D07 | Elegir ledger/versionado, integridad transaccional y rollback | Arquitectura/datos | Antes de MySQL/EF migraciones |
| D08 | Establecer fixtures y métricas de no regresión/reversibilidad | QA/seguridad | Antes de A1-A3 runtime y cualquier release |

Este registro **no solicita ni concede ahora** permisos para alterar JWT/RBAC, Cloudinary, MySQL, precios, mensajes o producción. Una futura autorización debe estar documentada, explícita y verificarse contra el estado vigente del repositorio y Sheet; no inferirse de estas propuestas de responsable.

## Matriz de aceptación FUTUROS_NO_EJECUTADOS

| Caso | Futuro positivo | Futuro negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Cliente A observa sólo sus puntos | A prueba ID cliente B y no obtiene saldo |
| P02/N02 | Venta confirmada acredita una vez | Replay/doble submit no duplican evento |
| P03/N03 | Canje atómico registra saldo | Concurrencia o saldo insuficiente rechaza |
| P04/N04 | Promoción vigente con límites aplica en A | Promoción B/expirada o parámetro no permitido falla |
| P05/N05 | Reseña pública saneada conserva anonimato necesario | Script/PII o suplantación se bloquean |
| P06/N06 | Moderación autorizada con recurso y motivo | Rol lector o empresa B no modifica reseña |
| P07/N07 | Ticket posventa A conserva venta A | Referencia a B jamás expone datos |
| P08/N08 | Opt-out revoca canal y queda auditado | Retry/caché posterior no reactiva publicidad |

**No se ejecutaron aquí** APIs, tests xUnit, MySQL/EF, Playwright, npm, envíos, colas o proveedores. La publicación de este documento sólo exige validación de integridad y checks documentales pertinentes; `A5_PENDIENTE` hasta Task Javier independiente.

## Impacto, dependencias y reversión

**Componente:** documentación del dominio futuro fidelización/reputación (C09-02). **Código y servicios modificados:** ninguno. No hay interfaces nuevas activas, migraciones, transacciones, secretos, jobs, eventos o UI. **Seguridad obligatoria futura:** tenant A/B, JWT válido, RBAC a nivel API, aislamiento de cache/archivos, protección PII, prevención replay y fallo cerrado. **Observabilidad futura:** correlación mínima y auditoría sin datos sensibles.

**Bloqueos de implementación:** no habilitar M02/M03 sensible, wallet, promociones monetarias, moderación o envío comercial basándose en esta matriz; exigir contrato final, decisión autorizada y pruebas positivas/negativas reales con MySQL efímero y API cuando aplique. **Rollback presente:** revertir commit ordinario en dev tras verificar HEAD, preservando cambios ajenos; no tocar QA/main/PROD ni BITACORA histórica. **A0–A4:** lectura, consistencia documental, seguridad de límites, revisión de diff y Scope Lock/V5 exact-SHA. **A5:** revisión por un TASK_ID diferente dentro de las seis Javier.
