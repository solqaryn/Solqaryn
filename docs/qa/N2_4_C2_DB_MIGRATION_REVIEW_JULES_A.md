# N2.4.C.2 — Review DB/migración de FacturaProveedor

## Alcance y base revisada

- Proyecto: Solqaryn / ERP-N2.4 FacturaProveedor.
- Rama autoritativa: `Desarrollo`.
- Base lógica revisada: `158161812e74fd9d6b91c9cdfba51f62671de5c4` y descendientes de control-plane hasta `c5b5153e88d49c58f774190855c95d4e6cd8f66c`.
- Scope: revisión de configuración EF, claves/FK, índices, precisiones, check constraints y estrategia de migración. No incluye DDL/DML sobre ambientes reales ni cambios de runtime fuera de N2.4.C.

## Resultado ejecutivo

**REVIEW RESULT: APPROVED_WITH_REQUIRED_MIGRATION_GATES**

El modelo persistente actual es coherente con el dominio de FacturaProveedor y con la separación `OrdenCompra → RecepcionCompra → FacturaProveedor`. No se detecta una colisión de claves ni un delete behavior peligroso. Antes del rollup de N2.4.C, la migración ejecutable debe materializar exactamente estas invariantes y pasar preflight/postcheck, snapshot y recovery MySQL.

## Hallazgos verificados

### Cabecera `FacturasProveedor`

- `ProveedorId > 0` y `OrdenCompraId > 0` están protegidos por `CK_FacturasProveedor_IdsValidos`.
- `Estado` se persiste como entero y se limita a `IN (1, 2, 3)`, alineado con `Borrador / Registrada / Anulada`.
- `Moneda` tiene longitud máxima 3 y el check exige exactamente tres caracteres no vacíos después de `TRIM`.
- `FechaVencimientoUtc` no puede ser anterior a `FechaEmisionUtc`.
- La identidad documental usa índice único compuesto `ProveedorId + NumeroFactura`; esta es la frontera correcta para impedir duplicación de una factura del mismo proveedor sin imponer unicidad global artificial.
- `OrdenCompraId` tiene índice dedicado; `Estado + FechaEmisionUtc` y `FechaVencimientoUtc` cubren consultas operativas previsibles.
- FK a `Proveedor` y `OrdenCompra` usan `DeleteBehavior.Restrict`, evitando borrado en cascada de documentos empresariales.
- La relación cabecera→detalle usa `Cascade`, apropiada para el agregado documental cuando se elimina una cabecera todavía administrable por la capa de dominio/servicio.

### Detalle `FacturaProveedorDetalles`

- IDs físicos/documentales válidos: `OrdenCompraDetalleId > 0`, `ProductoId > 0`, `ProductoVarianteId` nulo o positivo.
- `CantidadFacturada`, `PrecioUnitarioSnapshot`, `DescuentoSnapshot` e `ImpuestoSnapshot` usan precisión `decimal(18,4)`.
- Importes negativos están prohibidos; la cantidad debe ser estrictamente positiva.
- `DescuentoSnapshot <= CantidadFacturada * PrecioUnitarioSnapshot` evita descuentos superiores al bruto de línea.
- Índice único `FacturaProveedorId + OrdenCompraDetalleId` impide duplicar la misma línea de orden dentro de una factura, consistente con `ValidarDocumento()` del dominio.
- Índice `ProductoId + ProductoVarianteId` facilita consultas de trazabilidad documental.
- FKs hacia `OrdenCompraDetalle`, `Producto` y `ProductoVariante` usan `Restrict`; no se permite que la eliminación de maestros destruya historial facturado.

## Riesgos y gates obligatorios antes de LISTO

1. **Migración canónica única.** Debe existir una sola migración N2.4.C que cree/actualice `FacturasProveedor` y `FacturaProveedorDetalles`; no duplicar DDL en otra raíz de migraciones.
2. **Snapshot EF sincronizado.** `AppDbContextModelSnapshot` debe reflejar tablas, índices, constraints, precisiones y FKs exactamente como el design-time model; `has-pending-model-changes` debe quedar limpio.
3. **Orden seguro de DDL.** Crear cabecera antes del detalle; crear FKs sólo después de tablas/columnas requeridas; en rollback retirar primero FKs/índices dependientes y luego tablas en orden inverso.
4. **Preflight fail-closed.** Antes de aplicar DDL, verificar ausencia de colisiones de tablas/índices/constraints y existencia de dependencias `Proveedores`, `OrdenesCompra`, `OrdenCompraDetalles`, `Productos`, `ProductoVariantes`.
5. **Postcheck de integridad.** Verificar unicidad `ProveedorId + NumeroFactura`, ausencia de huérfanos, checks activos y precisiones `18,4`.
6. **Datos/backfill.** No inventar FacturaProveedor a partir de órdenes o recepciones existentes. Si no hay fuente documental autoritativa, el backfill debe ser **NO-OP explícito** y quedar documentado por C.3.
7. **Separación funcional.** La migración no debe crear stock, Kardex, costeo ni movimientos financieros; N2.4.C es persistencia documental.
8. **MySQL/recovery.** Ejecutar preflight → migración → postcheck en el gate efímero y validar rollback/recovery sin tocar Producción.

## Recomendaciones para la migración N2.4.C

- Mantener nombres canónicos ya definidos por EF para constraints, índices y FKs, evitando renombrados innecesarios posteriores.
- Preservar `Restrict` en referencias maestras/documentales y `Cascade` únicamente en cabecera→detalle.
- No añadir índice global único a `NumeroFactura`; la unicidad correcta es por proveedor.
- No reducir precisión monetaria por debajo de `18,4`; redondeo de presentación/cálculo pertenece a aplicación/dominio, no a una truncación persistente.
- El postcheck debe comparar también conteo de huérfanos y duplicados, no sólo presencia de objetos de esquema.

## Observaciones de QA

Las pruebas `N24FacturaProveedorConstraintModelTests` ya verifican por design-time model la existencia de los check constraints principales de cabecera y detalle. Los gates de `c5b5153e...` terminaron verdes para Development, Acceptance, Fase 8, M10, M13 y recovery MySQL; los fallos legacy de otros workflows no son atribuibles a esta revisión documental y no deben usarse para reabrir N2.4.C.1.

## P0/P1

- P0: 0
- P1: 0 dentro del modelo actual.
- REQUIRED antes del rollup N2.4.C: migración ejecutable + snapshot + preflight/postcheck + recovery/CI verdes y revisión C.3 de datos/reconciliación/rollback.

## Recomendación final

**C.2 puede marcarse LISTO** con este review integrado. El padre `N2.4.C` debe permanecer bloqueado hasta completar C.3 y la migración/snapshot/gates obligatorios de C.1/C.
