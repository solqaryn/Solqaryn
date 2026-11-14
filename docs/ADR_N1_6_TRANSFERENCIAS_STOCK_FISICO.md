# ADR — N1.6 Transferencias y autoridad de stock físico

## Estado

Aceptado para `Desarrollo` dentro de ERP-N1.6.

## Contexto

Las transferencias internas mueven inventario entre contextos físicos y no pueden depender de cantidades agregadas legacy. El sistema ya dispone de `ExistenciaVariante` como autoridad por `ProductoVariante + Almacen + Ubicacion`, de Kardex tipado y de correlación durable.

## Decisión

1. `ExistenciaVariante.StockFisico` es la única autoridad para decidir y materializar los movimientos físicos de una transferencia.
2. El despacho bloquea las existencias de origen/destino, descuenta origen y crea tránsito hacia el destino dentro de una transacción.
3. La recepción consume el tránsito y materializa únicamente la cantidad efectivamente recibida.
4. Faltantes, dañados y sobrantes permanecen discrepancias explícitas; no se transforman silenciosamente en stock disponible.
5. La cancelación de una transferencia `EnTransito` revierte de forma transaccional el movimiento físico y registra Kardex de reversión.
6. Cada transición física usa un `CorrelationId` determinístico distinto para `despachar`, `recibir` y `cancelar`.
7. Kardex conserva `TransferenciaInventarioId` como origen tipado y permite consultar el mismo origen por `OrigenTipo` + `OrigenId`.
8. `ProductoVariante.Cantidad` no decide concurrencia ni disponibilidad de una transferencia.

## Invariantes

- no puede despacharse más stock que el físicamente disponible en la clave de origen;
- una operación fallida no deja el documento en un estado distinto del stock;
- la misma variante en ubicaciones diferentes mantiene identidades físicas independientes;
- una recepción parcial no convierte faltantes o dañados en disponibilidad;
- una transición inválida falla antes de mutar estado, timestamps o actores;
- reintentos no deben duplicar efectos físicos.

## Consecuencias

### Positivas

- elimina doble autoridad de stock en transferencias;
- mantiene trazabilidad física y documental alineada;
- permite auditoría y diagnóstico mediante Kardex/CorrelationId;
- soporta almacenes y ubicaciones múltiples sin colapsar la identidad por variante.

### Costos

- las transiciones requieren locks y unidad transaccional;
- fixtures y E2E deben crear contexto físico determinístico;
- históricos sin dimensión física no pueden inventar `AlmacenId`/`UbicacionAlmacenId`.

## Rollback

El rollback no se implementa mediante edición manual de cantidades ni DDL destructivo. Para WIP operativo se usa la transición de cancelación/reversión soportada por dominio y Kardex. Para código en `Desarrollo`, cualquier reversión se hace mediante commits explícitos, sin force-push.

Producción queda fuera del alcance de esta decisión y requiere procedimiento separado.

## Evidencia

El cierre QA de N1.6.G quedó sobre `fc9729bad8d8dfd6f9c402c8e0ff2ca66de3bf9f`, con Desarrollo `#31954861360`, aceptación integral `#31954861314`, Fase 8 `#31954861485` y M13 `#31954861328`, todos `SUCCESS`.