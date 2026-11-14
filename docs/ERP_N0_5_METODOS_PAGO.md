# ERP-N0.5 — Métodos de Pago Relacionales

## Dictamen

**Estado final:** LISTO / CERRADO.

ERP-N0.5 sustituye la autoridad legacy de métodos de pago por un catálogo relacional administrable, auditable y consumido por transacciones, facturación, reportes y frontend. La migración histórica es fail-closed: no normaliza ni reinterpreta silenciosamente valores desconocidos y preserva el significado histórico mediante relaciones y snapshots.

**SHA funcional recertificado:** `1bbccd9cccdcc181ab8c1e842ea0ff8343831197`.

## Contrato funcional canónico

Los códigos históricos estables son:

- `Efectivo`
- `Transferencia`
- `Tarjeta`
- `Otro`

El `Id` autoincremental del catálogo **no equivale** al enum histórico `1..4`. La equivalencia histórica se resuelve por código funcional estable. Para `FacturaPago`, donde el enum legacy persistía como entero, la conversión `1..4` se realiza explícitamente hacia esos códigos y nunca contra el nuevo Id del catálogo.

Las nuevas operaciones consumen `MetodoPagoId` y sólo pueden seleccionar métodos elegibles según estado y reglas del catálogo. Los registros históricos conservan su interpretación aunque el catálogo cambie posteriormente.

## Persistencia y migraciones

Migraciones N0.5 relevantes:

- `20260812022343_N0_5_MetodoPagoRelacionalBase.cs`
- `20260812023600_N0_5_BackfillMetodoPagoHistorico.cs`
- `20260812190253_ERP_N05_BancoNormalizadoCanonical.cs`
- `20260812201608_ERP_N05_PermiteCambioAuditable.cs`
- `20260813014500_ERP_N05_FacturaMetodoPagoSnapshots.cs`
- `AppDbContextModelSnapshot.cs`

El backfill valida previamente los valores legacy admitidos, siembra el catálogo de forma idempotente, materializa las relaciones y ejecuta postchecks de correspondencia y preservación 1:1.

Durante N0.5.14 se endureció la compatibilidad con MySQL administrado/Aiven (`sql_require_primary_key=ON`). Los snapshots temporales de N0.5 se cambiaron de `CREATE TEMPORARY TABLE ... AS SELECT` a tablas explícitas con PK y tipos históricos exactos. La misma recertificación detectó y corrigió el patrón equivalente en `20260812083000_N0_6_OrigenTipadoMovimientoInventario.cs`, porque impedía que M13 reconstruyera el historial completo bajo MySQL estricto. Este segundo ajuste es transversal y no cambia el alcance funcional de N0.6.

## Backend, API y reglas operativas

ERP-N0.5 deja cubiertos:

- CRUD administrable del catálogo.
- Activar/desactivar y eliminación lógica.
- Ordenamiento y metadata.
- `RequiereReferencia`.
- `RequiereBanco` con catálogo Banco normalizado.
- `PermiteCambio` auditable.
- Resolución relacional para Ventas, FacturaPagos y MovimientosFinancieros.
- RBAC relacional para mantenimiento de métodos de pago.
- Auditoría transaccional de mutaciones.
- Fail-closed frente a códigos legacy desconocidos o inconsistentes.

La resolución de `MetodoPago`/`Banco` usada durante el registro de pagos mantiene tracking EF cuando participa en la misma unidad de trabajo, evitando inserciones duplicadas o conflictos de identidad de entidades ya cargadas.

## Frontend y experiencia operativa

El mantenimiento Angular y los selectores dinámicos consumen el catálogo relacional. Los métodos inactivos quedan excluidos de nuevas transacciones, pero la información histórica continúa visible y consistente.

La localización `es-HN` necesaria para importes HNL quedó registrada explícitamente en el bootstrap Angular para evitar fallos runtime de `CurrencyPipe` en facturación/pagos.

## Históricos, facturas, reportes y PDFs

N0.5.11 retiró dependencias de texto/enum como autoridad en reportes y comprobantes. Facturas y pagos incorporan snapshots de código/nombre de método de pago para que una edición posterior del catálogo no reescriba semánticamente documentos históricos.

## Trazabilidad de cierre N0.5.09–N0.5.14

- **N0.5.09 — Frontend/selectores:** cierre `7da9cc73f75598dedbf7630f8b131d7dc5f72af8`; ERP-N0.5 `31662728534`, Desarrollo `31662728587` y M10 `31662728555` en SUCCESS.
- **N0.5.10 — RBAC/auditoría:** `fe669fd0f3138193b04bcbbad96934d4e93b8ccb`; ERP-N0.5 `31671574303` y Desarrollo `31671574330` en SUCCESS.
- **N0.5.11 — reportes/facturas/PDFs:** `fd841429d04d4663278cf0605be54b13d5b0178b`; ERP-N0.5 `31737978596` y Desarrollo `31737978473` en SUCCESS.
- **N0.5.12 — regresión integral:** `eaa52c4b92c6932b33afa8eb2b334ed8dec3593f`; ERP-N0.5 `31745717643`, build `31745717778`, aceptación integral `31745717860` y Fase 8 `31745717633` en SUCCESS.
- **N0.5.13 — workflow dedicado:** `.github/workflows/erp-n0-5-ci.yml` reconciliado; no se creó un duplicado. Run `31745717643` SUCCESS.
- **N0.5.14 — recertificación M13 / MySQL estricto:** `1bbccd9cccdcc181ab8c1e842ea0ff8343831197`.

## Certificación final de N0.5.14

Sobre `1bbccd9cccdcc181ab8c1e842ea0ff8343831197` quedaron verificadas las siguientes evidencias:

- ERP-N0.5 dedicado `31753406161` — **SUCCESS**.
- Recuperación de migración MySQL parcial/Aiven-like `31753406119` — **SUCCESS**.
- M11 backup/restore `31753406267` — **SUCCESS**.
- Desarrollo compilación y pruebas `31753406190` — **SUCCESS**.
- Desarrollo aceptación funcional integral `31753406328` — **SUCCESS**.
- M13 auditoría integral y certificación final `31753406059`, attempt 2 — **SUCCESS**.

M13 verificó backend con warnings como error, unitarias, historial completo desde cero con `sql_require_primary_key=ON`, integración MySQL, SQL forward idempotente, upgrade representativo con preservación histórica, frontend TypeScript/lint/build, seguridad HTTP, Playwright integral, SMTP/PDF, auditoría de dependencias, Docker y vigencia del drill M11.

## Riesgos residuales y operación

No queda un riesgo funcional conocido abierto de ERP-N0.5 después de la recertificación indicada. Como observación de eficiencia de CI, M11 y M13 se disparan en paralelo: cuando un commit modifica migraciones, el primer intento del gate de vigencia M11 de M13 puede adelantarse al nuevo drill. En esta certificación el gate se reejecutó después de M11 y el dictamen final quedó verde. Esta condición no altera datos ni funcionalidad, pero conviene eliminarla en una mejora transversal de orquestación CI para evitar reruns innecesarios.

## Cierre

ERP-N0.5 queda formalmente cerrado sobre evidencia reproducible de GitHub y el tablero VAEP. No se modificó `main`, Producción, merge/auto-merge del PR #2, secretos, infraestructura productiva ni se crearon ramas nuevas.
