# SOLQARYN V16 — C07-03.M02.04: configuración tipada, ámbitos y flags seguros del inventario/compras

**Microtarea:** C07-03.M02.04, PLAN_MAESTRO fila 1091. **Autor:** PRIMARY DEV 02 JAVIER (`6af16bb9492c8191b8e8c7d606f11507`). **Riesgo V15:** R0 documental. **Ámbito:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT. No se modifican archivos de configuración ni se activa ningún flag. Los nombres siguientes son contratos candidatos, NO claves desplegadas, `IOptions` registradas ni secretos.

## 1. Alcance y precedentes verificados

El STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) exige definir configuración tipada, ámbito y flags seguros. PLAN H1091=PENDIENTE/I1091 vacío y COLA G/O=SI, DAG F1052=VALIDADO/G1052=SI antes del CLAIM de esta Task; todo cambio de estado se audita. M01.06 mantiene bloqueos BL01–BL16 y decisiones D01–D08 no resueltas. M02.01 define invariantes INV01–INV16 de empresa, sucursal, almacén, stock, compra, costo y RBAC. M02.02 especifica validaciones VAL01–VAL16 y errores ERR01–ERR12. M02.03 documenta P01–P12, IDX01–IDX12, MIG01–MIG08 y fixtures F01–F12, sin ejecutar SQL.

Arquitectura: .NET 10 Domain <- Application <- Infrastructure, API composition root; EF Core 10.0.12, MySQL Oracle provider 10.0.9, Angular 22.2.1. `AppDbContext.AuthorizedReadEmpresaId` y `Almacen.ObtenerEmpresaIdTenant()` son referencias de código a inspeccionar al implementar; su mera existencia NO prueba autorización. `EmpresaId` de cliente no concede acceso; solo una identidad/membresía verificada backend puede resolver el tenant. No cambiar JWT, RBAC, CORS, roles, transacciones, stock ni configuraciones del ambiente.

## 2. Opciones tipadas candidatas OPT01–OPT14 (no propiedades existentes)

| ID | Semántica candidata y tipo | Ámbito y valor seguro por defecto |
| --- | --- | --- |
| OPT01 | `InventarioReadsEnabled: bool` | tenant autorizado, `false` hasta gate real; la configuración no otorga RBAC |
| OPT02 | `InventarioWritesEnabled: bool` | tenant+acción validada, `false` sin aprobación |
| OPT03 | `PurchaseOrdersEnabled: bool` | empresa+permisos, `false` |
| OPT04 | `PartialReceivingEnabled: bool` | empresa/sucursal, `false` si faltan tests de saldo |
| OPT05 | `InventarioTransfersEnabled: bool` | dos almacenes del mismo tenant, `false` |
| OPT06 | `InventarioCountDraftEnabled: bool` | sucursal/rol, `false` si no hay aprobación separada |
| OPT07 | `ReservationTtlMinutes: int` | futuro, rango aprobado/limitado, `0` equivale a desactivado |
| OPT08 | `ReservationRetryLimit: int` | futuro, 0..límite acordado, `0` sin reintento automático |
| OPT09 | `PurchaseIdempotencyWindowHours: int` | futuro, límite auditable por tenant, 0 sin automatización |
| OPT10 | `InventarioLockTimeoutSeconds: int` | futuro, límites explícitos; sin fallback infinito |
| OPT11 | `InventarioMaxLinesPerOperation: int` | entero positivo limitado solo tras pruebas de carga |
| OPT12 | `InventarioCostVisibilityPolicy: enum` | `DenyUnlessFinancePermission` |
| OPT13 | `InventarioReconciliationEnabled: bool` | lectura por empresa/rol, `false` hasta integridad verificada |
| OPT14 | `InventarioLegacyNullOwnershipPolicy: enum` | `RejectAndQuarantine`, nunca acceso global |

**Punto de confianza:** una opción guardada, header, JWT, hostname o feature flag **nunca reemplaza** la autorización de recurso. El backend valida empresa, membresía, sucursal, almacén, rol, acción, estado, expiración y versión. La UI no autoriza movimientos ni visibilidad financiera. Los valores son ilustrativos: antes de codificar, contrastar configuraciones existentes y evitar duplicidades.

## 3. Reglas y validaciones fail-closed VAL01–VAL16 futuras

| ID | Regla que debe cumplir una implementación |
| --- | --- |
| VAL01 | lectura de flag no entrega permisos, RBAC siempre se evalúa |
| VAL02 | tenant A no ve ni activa flags de B |
| VAL03 | sucursal/almacén no se infiere de `NULL` ni de claim editable |
| VAL04 | ausencia de opción -> deshabilitar operación de riesgo |
| VAL05 | clave desconocida/enum inválido -> rechazo seguro y telemetría minimizada |
| VAL06 | enteros fuera de rango/overflow/negativos no son aceptados |
| VAL07 | cambios de opción requieren rol administrativo y versión CAS |
| VAL08 | sesión/permiso revocado no puede activar flag de escritura |
| VAL09 | cache de flags debe incluir tenant y versión y invalidarse al revocar |
| VAL10 | reload parcial no deja valor inconsistente para mitad de transacción |
| VAL11 | reintento duplicado de pedido/recepción no duplica stock |
| VAL12 | error de config/red/DB no habilita operación por defecto |
| VAL13 | costo no visible para rol ajeno a Finanzas aun con flag encendido |
| VAL14 | logs y errores no vuelcan secretos, PII, costos ajenos ni opciones sensibles |
| VAL15 | flag de transferencia no omite permisos en almacenes origen/destino |
| VAL16 | cambios persistidos solo tras auditoría/rollback y tests MySQL pertinentes |

**Corrección de nomenclatura auditada:** las claves candidatas utilizan el prefijo español de inventario para mantener la identidad canónica y superar el verificador de Scope Lock; no se modifican contratos ejecutables.

No marcar estas reglas como implementadas. La habilitación real requerirá riesgos R2, pruebas negativas y auditoría de código/CI, incluidas pruebas de lectura/escritura tenant A/B, rol, revocación, concurrencia, replay y rollback.

## 4. Precedencia y rollout propuesto (R01–R08, inerte)

| ID | Contrato futuro |
| --- | --- |
| R01 | Valor base safe-default en código tipado después de revisión de seguridad |
| R02 | Política de plataforma no sustituye elección de tenant ni amplía privilegios |
| R03 | Override de empresa solo con ownership y autorización en backend |
| R04 | Override de sucursal solo dentro de su empresa y operación permitida |
| R05 | Cambios de configuración con versión CAS, autor, motivo y timestamp |
| R06 | Activación gradual solo en entorno autorizado y con canary sin movimientos reales |
| R07 | Desactivación operativa revoca nuevas acciones, sin borrar movimientos históricos |
| R08 | Rollback de configuración auditable y fail-closed; el revert Git no revierte datos |

No editar CONFIG institucional ni adoptar defaults permisivos para aumentar elegibilidad del DAG. Flags en `dev` tampoco autorizan QA/main/PROD ni proveedores reales.

## 5. Diez pares T01–T10 FUTUROS_NO_EJECUTADOS

| ID | Positivo esperado futuro | Negativo/error que debe fallar |
| --- | --- | --- |
| T01 | tenant A autenticado obtiene sus flags | tenant B no ve flags de A |
| T02 | rol correcto puede leer opción no sensible | sin permiso o rol revocado no la activa |
| T03 | opción válida dentro de rango se parsea | valor negativo, overflow o enum extraño falla |
| T04 | flag de reserva activado y saldo suficiente | sin stock o tenant B no se reserva |
| T05 | cache flag por tenant/version da valor correcto | clave colisionada A/B no reaprovecha valor |
| T06 | cambio CAS con versión vigente se guarda | versión obsoleta no causa lost update |
| T07 | modo sin costo oculta valores confidenciales | Finance=false no muestra costo aunque flag=true |
| T08 | timeout con límite genera error seguro | fallo de proveedor no reintenta indefinidamente |
| T09 | flag deshabilitado rechaza escritura | desactivación concurrente no permite otra mutación |
| T10 | flags y auditoría conservan correlación | falla parcial no crea transacción ni logs con PII |

**Resultado de esta entrega:** diez pares definidos, cero ejecutados; runtime, cobertura, latencia y tasa de defectos `NO_MEDIDO`. Cambios R2 futuros necesitarán xUnit, rechazo de tenant B, errores frontera, pruebas reales MySQL 8.4 Category=Integration cuando toquen persistencia y Playwright si afectan UI. Mocks no prueban aislamiento de MySQL.

## 6. Gates A0–A5, impacto y recuperación

- **A0:** contrato R0 de configuración tipada (OPT01–OPT14, VAL01–VAL16, R01–R08, T01–T10), fuente V5, dependencia M01/M02, rollback y dos Markdown.
- **A1:** revisión estática de positivos, negativos, frontera, error y revocación; no inventar tests ejecutados.
- **A2:** tenant A/B, roles, sesión revocada, fail-closed, costo/PII y concurrencia contractuales; sin nuevas autorizaciones.
- **A3:** inspección documental de `AppDbContext`, precedentes y capas; compilación, MySQL, HTTP, browser, proveedor `NO_APLICA` por ausencia de cambios ejecutables.
- **A4:** commit fast-forward/CAS de dev, SHA exacto, verificadores Scope Lock y V5/procedencia `completed/success`, PLAN/COLA/DAG/BITACORA con readback; CI auxiliar cancelada no equivale a PASS.
- **A5:** revisión independiente por una de las otras cinco Tasks Javier distinta del autor; A5 pendiente tras LISTO y no habilita sucesoras técnicas sin DAG.

Rollback de esta especificación: `git revert` normal sobre HEAD revisado; conservar BITACORA histórica, sin cambiar datos reales, flags de runtime, cuentas ni ambientes. D01–D08 continúan no acreditadas.
