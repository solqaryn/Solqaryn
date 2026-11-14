# ADR N2.6 — Autoridad física y Kardex en devoluciones a proveedor

## Estado

Aceptado para ERP-N2.6.

## Contexto

Una devolución a proveedor afecta mercancía que previamente ingresó mediante RecepcionCompra y puede estar asociada a FacturaProveedor y OrdenCompra. El sistema ya dispone de una autoridad física empresarial basada en `ExistenciaVariante`, con dimensión por variante, almacén y ubicación. Crear un segundo origen de verdad o reutilizar `ProductoVariante.Cantidad` introduciría divergencias entre stock, reservas, tránsito y Kardex.

## Decisión

1. `ExistenciaVariante` es la única autoridad cuantitativa física utilizada por N2.6.
2. `ProductoVariante.Cantidad` no se usa para decidir ni validar la devolución.
3. Confirmar una devolución bloquea previamente las claves físicas mediante `IExistenciaVarianteConcurrencyService`.
4. La mutación física se encapsula en `IDevolucionProveedorinventarioProcessor`.
5. El movimiento de inventario se registra mediante `IDevolucionProveedorKardexWriter` dentro de la misma frontera transaccional.
6. La anulación de una devolución confirmada revierte mediante el mismo modelo de autoridad, locks y Kardex; no se implementa como una simple transición documental desconectada del stock.
7. Auditoría, persistencia del estado y mutación física deben compartir la unidad transaccional para impedir estados parciales.

## Identidad física

La identidad que debe preservarse en cada línea comprende como mínimo:

- producto/variante aplicable;
- almacén;
- ubicación cuando aplique;
- detalle de recepción origen.

Los snapshots documentales no sustituyen esa identidad física; sirven para conservar evidencia histórica legible aun cuando los catálogos evolucionen.

## Concurrencia

La devolución compite potencialmente con ventas, ajustes, reservas, transferencias y otros procesos de stock. Por ello el patrón aceptado es pesimista sobre las existencias involucradas antes de calcular/aplicar la transición. La validación que dependa del stock debe realizarse después de adquirir el lock correspondiente y dentro de la misma transacción.

## Idempotencia

La idempotencia documental de creación se protege mediante `Idempotency-Key` + fingerprint. La idempotencia del lifecycle se protege además por el estado del agregado: reintentar Confirmar sobre `Confirmada` o Anular sobre `Anulada` no debe duplicar persistencia, Kardex ni auditoría.

## Consecuencias

### Positivas

- Un solo origen de verdad para stock físico.
- Trazabilidad determinística desde documento hasta movimiento.
- Menor riesgo de overselling o stock negativo por carreras.
- Reintentos seguros del lifecycle.
- Auditoría y Kardex coherentes con el estado persistido.

### Costos

- Las operaciones requieren transacción y locks físicos.
- Los tests deben cubrir concurrencia, rollback, idempotencia y ausencia de efectos duplicados.
- El controller no debe implementar lógica física directamente; la coordinación permanece en Application.

## Rechazos explícitos

Se rechazan para N2.6:

- actualizar `ProductoVariante.Cantidad` como autoridad;
- alterar stock sin lock de `ExistenciaVariante`;
- registrar Kardex fuera de la transacción principal;
- aceptar una devolución documental como equivalente a una reversión física completa sin ejecutar el procesador;
- inferir ubicación/variante cuando el contrato persistido no lo demuestra.

## Verificación

La decisión se considera preservada cuando las regresiones de N2.6 demuestran, como mínimo, idempotencia de Confirmar/Anular, RBAC fail-closed, auditoría estricta y gates de integración/MySQL sin divergencia de modelo.