# SOLQARYN V16 — C07-01.M02.06: dependencias tecnicas y contrato de salida M02 del catalogo universal

**ID:** C07-01.M02.06 · **Owner:** PRIMARY DEV 03 JAVIER, Task `6adc2e4bd8a48191ac7d8641ed95a1a3` · **Rama:** `solqaryn/Solqaryn@dev` · **Scope:** `PROJECT_SCOPE_LOCK=STRICT` · **Riesgo V15:** R0 documental · 2026-11-15.

**Autoridad:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`); PLAN/COLA/DAG institucional en Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Cierre de contrato **M02**, no certificacion de catalogo universal en runtime ni permiso para ampliar alcance.

## 1. Alcance, entradas y precedencias verificables

Esta unidad consolida criterios de salida para productos, servicios, categorias, variantes, atributos, impuestos, precios, stock y coexistencia con ERP. **Solo se anade este documento y una seccion CHANGELOG**: no hay cambio de .NET, Angular, DTO compilable, API, SQL/EF, secretos, flags, jobs, eventos, worker, webhook, proveedor ni datos.

| Lote | Documento de referencia | Limite |
| --- | --- | --- |
| M01.06 | `docs/v5/c07-01-m01-06-gates-rollback-liberacion-catalogo-universal.md` | Gates K01-K14 y rollback de preparacion; no sustituye A5 de M02 |
| M02.01 | `docs/v5/c07-01-m02-01-entidades-ownership-invariantes-catalogo-universal.md` | Modelo/ownership e invariantes conceptuales |
| M02.02 | `docs/v5/c07-01-m02-02-api-dto-errores-validaciones-compatibilidad.md` | Operaciones/DTO y errores propuestos, sin endpoints |
| M02.03 | `docs/v5/c07-01-m02-03-persistencia-indices-fixtures-catalogo-universal.md` | Indices, migraciones y fixtures futuros, sin DDL |
| M02.04 | `docs/v5/c07-01-m02-04-configuracion-tipada-ambitos-flags-catalogo-universal.md` | Flags tipados inertes y validaciones |
| M02.05 | `docs/v5/c07-01-m02-05-eventos-idempotencia-correlacion-limites-catalogo-universal.md` | Eventos conceptuales, sin dispatcher ni worker |

**Preclaim verificado:** PLAN H1021=PENDIENTE, I1021 vacio; COLA G1021=SI/O1021=SI; DAG F982=VALIDADO/G982=SI para C07-01.M02.06. Cada sucesora debe releer su propio DAG y A5 del predecesor; `VALIDANDO` o el mero texto de M02.06 no autoriza desarrollo M03 ni una migracion de datos.

## 2. Dieciseis gates G01-G16 para una futura liberacion (NO EJECUTADOS)

| Gate | Categoria | NO_GO si |
| --- | --- | --- |
| G01 | Identidad tenant | Nunca confiar en tenant_id, slug, URL, cabecera o cookie cliente sin ownership server-side |
| G02 | Aislamiento A/B | Lectura/edicion de recursos, variantes o categorias del tenant B desde A implica NO_GO |
| G03 | RBAC y revocacion | Rol ausente, permiso revocado o JWT vencido nunca autoriza mutaciones |
| G04 | Precios vigentes | Moneda, vigencia, impuesto y redondeo no comprobados impiden publicar precio |
| G05 | ERP legacy | Identidad de producto, servicio, variante o categoria sin mapeo reversible impide migrar |
| G06 | Stock concurrente | Sin transaccion, control de sobreventa, compensacion y auditoria no tocar inventario |
| G07 | Claves de persistencia | Sin FK/indice tenant-aware o backfill probado no ejecutar migracion EF/MySQL |
| G08 | Contratos API | DTO con mass assignment, SQL/HTML arbitrario o campos ocultos sin allowlist implica rechazo |
| G09 | Errores seguros | No exponer PII, stack, claves, IDs de otro tenant ni detalles de proveedores |
| G10 | Idempotencia | Evento/operacion sin clave por tenant, version, replay controlado y cuota no se libera |
| G11 | Cache compartida | No permitir cache cross-tenant de precios, stock, identidad o HTML privado |
| G12 | Historia comercial | Facturas y pedidos existentes conservan snapshot de impuestos/precio/variante |
| G13 | Prueba DB real | Cambios EF requieren MySQL 8.4 efimero Category=Integration ejecutada, 0 FAIL |
| G14 | Prueba UI/API | Cambios Angular necesitan build/Chromium; HTTP exige 401/403 y A/B negativos |
| G15 | CI causal | SKIPPED/CANCELLED/QUEUED/IN_PROGRESS de check requerido nunca es PASS |
| G16 | Autoridad de liberacion | Sin A0-A4, Scope Lock, V5, A5 independiente y DAG real no iniciar M03 ni QA/PROD |

Cada gate es un **oraculo de aceptacion futuro**, no una falla observada del ERP. Preservar los controles K01-K14 de M01.06, no forzar formulas G/O ni tratar mocks/builds como evidencia MySQL, HTTP, tenant A/B o proveedores.

## 3. Rollback condicionado RB01-RB08 (FUTURO_NO_EJECUTADO)

| Paso | Evidencia/procedimiento requerido |
| --- | --- |
| RB01 | Capturar SHA, arbol, owners, schema/version, flags, scopes y permiso del ambiente antes de tocar runtime |
| RB02 | Crear backups/fixtures sinteticos aislados por A/B y snapshot de pedidos/facturas; verificar restauracion |
| RB03 | Aplicar expand-contract aditivo y ensayar rollback de constraints y backfill con MySQL efimero |
| RB04 | Preservar compatibilidad de DTO, clientes ERP y storefront mediante versionado y pruebas |
| RB05 | Desactivar flags tipados con readback sin deshabilitar autorizaciones ni perder historial |
| RB06 | Drenar y conciliar eventuales eventos idempotentes antes de repetir operacion, sin workers ahora |
| RB07 | Ejecutar regresion stock/precio/impuesto, 401/403, revocacion, tenant B y snapshots en entorno aislado |
| RB08 | Registrar evidencias/autor/revisor/sha/defecto y revertir solo mediante git revert o rollback aprobado |

**Rollback de ESTA entrega documental:** un `git revert` regular del commit publicado con readback de PLAN y BITACORA. No hay cambios de esquema, inventario, DNS, QA, main, PROD o servicios reales que revertir. Los RB01-RB08 deben ensayarse solo cuando se autorice implementacion tecnica.

## 4. Diez pares de pruebas futuras T01-T10 — NO EJECUTADAS

| ID | Assert positivo futuro | Assert negativo/error capaz de fallar |
| --- | --- | --- |
| T01 | Tenant A ve solo sus datos | A intenta SKU o variante B y debe fallar sin revelar existencia |
| T02 | Precio con moneda/vigencia aplicable | Precio sin vigencia o impuesto incompatible se rechaza |
| T03 | Factura conserva precio historico | Cambiar lista no reescribe snapshots ya facturados |
| T04 | Stock se actualiza atomicamente | Dos reservas concurrentes no producen negativos ni doble consumo |
| T05 | Revocacion invalida acceso | JWT/rol anterior no concede lectura ni escritura |
| T06 | DTO permite solo campos definidos | Mass assignment, HTML y SQL no se aceptan |
| T07 | Idempotencia evita duplicados | Replay entre tenants o clave colisionada se rechaza |
| T08 | Indices preservan FK tenant | Join sin tenant o FK de B no permite fuga/corrupcion |
| T09 | Migracion reversible con MySQL 8.4 | Fallo entre expand/backfill obliga rollback sin perdida |
| T10 | Cliente actual conserva compatibilidad | Rutas/UI/DTO antiguos no silencian 401/403 ni rompen pedidos |

**Medicion actual:** 10/10 pares especificados; 0/10 ejecutados sobre API, DB, .NET o browser en esta unidad R0. Si una M03 toca EF/MySQL, requerira CI con MySQL 8.4 efimero y Category=Integration realmente ejecutada; si toca UI, npm/build y Chromium; si toca JWT/RBAC/tenant, pruebas de rechazo A/B/revocacion/IDOR; ninguna se sustituye por este Markdown.

## 5. A0-A5, estado, prueba causal y continuidad

- **A0:** fuente unica, alcance de M02.06, matriz de 6 antecedentes, diff de 2 Markdown y rollback declarados. R0; sin efectos ejecutables.
- **A1:** G01-G16, RB01-RB08 y T01-T10 definidos; afirmaciones de runtime NO_APLICA. Pruebas de comportamiento futuras, no PASS.
- **A2:** invariantes documentales de JWT/RBAC, tenancy A/B, PII, historicos ERP, revocacion, precios, stock, CAS e idempotencia fail-closed; ninguna autorizacion real modificada.
- **A3:** comparacion documental de M01.06 y M02.01-05; API/.NET/Angular/Playwright/EF/MySQL/proveedores **NO_APLICA** por no existir diff de codigo.
- **A4:** publicar por commit FF/CAS sobre HEAD verificado; exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA publicado o equivalencia auditada; bitacora de 26 columnas, PLAN/COLA/DAG y readbacks.
- **A5:** revision independiente por un TASK_ID Javier diferente del autor; no se declara A5_PASS desde esta entrega. Reviewer puede rechazar por requisitos o pruebas ausentes.

**Gate de salida:** `LISTO_DEV` solo tras A0-A4 verificados. M02.06 no crea M03, no autoriza migrar catalogo/ERP, no activa eventos, no despliega, no concede QA/main/PROD, ni altera datos, roles, secrets o cuentas. Un P0/P1 pendiente o CI requerida no exitosa mantiene `VALIDANDO` hasta reparacion causal. Las sucesoras requieren elegibilidad `PLAN H=PENDIENTE/I vacio + COLA G/O=SI + DAG F=VALIDADO/G=SI` y sus propias pruebas/autoridad.
