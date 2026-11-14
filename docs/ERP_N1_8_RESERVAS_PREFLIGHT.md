# ERP-N1.8 — Reservas — Auditoría y preflight

## Estado

**Punto:** N1.8.A — Auditoría y preflight  
**Rama:** `Desarrollo`  
**Objetivo:** diseñar reservas de inventario para pedidos/ventas diferenciando stock físico, reservado y disponible, con prevención transaccional de overselling y sin crear una segunda autoridad de stock.

## Conclusión ejecutiva

Solqaryn **ya tiene la base física correcta para reservas**, pero todavía no tiene un agregado ni lifecycle de reserva.

`ExistenciaVariante` es la autoridad de stock vivo por `ProductoVariante + Almacen + Ubicacion` y ya persiste:

- `StockFisico`;
- `StockReservado`;
- `StockDisponible = StockFisico - StockReservado` como columna calculada;
- `StockTransito`;
- umbrales mínimo/máximo.

La entidad impide `StockReservado < 0` y `StockReservado > StockFisico`. La persistencia mantiene una clave física NULL-safe única por variante/almacén/ubicación y una FK compuesta que impide enlazar una ubicación de otro almacén.

El hueco de N1.8 no es agregar otro contador de stock: es **dar identidad, lifecycle, idempotencia y concurrencia al compromiso que explica `StockReservado`**.

## Hallazgos dirigidos

### 1. No existe agregado de Reserva

La inspección dirigida no encontró entidad/servicio/API `Reserva` existente. Por tanto N1.8 debe introducir una capacidad aditiva y no migrar un módulo legacy de reservas.

### 2. `ExistenciaVariante` ya modela reservado y disponible

La entidad canónica contiene `StockFisico`, `StockReservado`, `StockDisponible` y `StockTransito`. `StockDisponible` no es input independiente: se deriva siempre de `StockFisico - StockReservado`.

**Decisión:** N1.8 no crea `StockDisponible` ni otra tabla de saldos. Las operaciones de reservar/liberar/consumir deben bloquear y actualizar la misma fila `ExistenciaVariante`.

### 3. La clave física autoritativa ya existe

`IExistenciaVarianteConcurrencyService` define `InventarioExistenciaClave`:

`ProductoVarianteId + AlmacenId + UbicacionAlmacenId`

El servicio de concurrencia existente valida deducciones contra `StockDisponible`, no contra cantidades legacy.

**Decisión:** una línea de reserva debe apuntar a esa misma clave física. No se admiten reservas ambiguas sólo por `ProductoId` o por variante sin almacén.

### 4. Venta ya persiste contexto físico, pero su contrato público no lo transporta

`VentaDetalle` ya contiene `AlmacenId` y `UbicacionAlmacenId` nullable por compatibilidad histórica.

Sin embargo `VentaDetalleInputDto` actualmente sólo recibe:

- `ProductoId`;
- `ProductoVarianteId`;
- `Cantidad`;
- `PrecioUnitario`.

Eso significa que un pedido/venta nuevo todavía no selecciona explícitamente la existencia física a reservar.

**Gap de contrato:** N1.8 debe definir cómo una venta/pedido identifica de forma inequívoca `AlmacenId` y `UbicacionAlmacenId` para cada línea reservable. Para operaciones nuevas ese contexto debe ser obligatorio cuando se usa una variante gestionada por `ExistenciaVariante`; la nulabilidad histórica no debe convertirse en fallback operativo.

### 5. Confirmación de Venta todavía usa el concurrency service legacy

`VentaService.ConfirmarAsync` construye `InventarioDemanda(ProductoId, ProductoVarianteId, Cantidad)` y usa `IInventarioConcurrencyService`, que bloquea `Producto`/`ProductoVariante` legacy y modifica `Producto.Cantidad` / `ProductoVariante.Cantidad`.

La misma venta dispone ya de campos físicos N1.4 en sus detalles, pero esa ruta de confirmación no los utiliza para decidir la deducción.

**Riesgo crítico:** introducir reservas sobre `ExistenciaVariante.StockReservado` sin reconciliar la confirmación de Venta permitiría que la reserva y la deducción final consultaran autoridades distintas. N1.8 debe cerrar esa brecha antes de declarar prevención de overselling.

## Arquitectura propuesta

### Autoridad de stock

`ExistenciaVariante` continúa siendo la única autoridad de:

`StockDisponible = StockFisico - StockReservado`

La reserva es un **documento/compromiso explicativo** que modifica `StockReservado` bajo lock; no es un saldo paralelo.

### Agregado candidato

`ReservaInventario`

Campos mínimos de cabecera:

- `Id`;
- `NumeroReserva` o código estable;
- `Estado`;
- `VentaId` nullable durante transición, con restricción de una reserva activa canónica por venta cuando corresponda;
- `FechaExpiracion` nullable/configurable;
- actor/timestamps de creación, activación, liberación, consumo, expiración y cancelación;
- `CorrelationId`/referencia idempotente cuando aplique;
- motivo de liberación/cancelación.

`ReservaInventarioDetalle`

- `ReservaInventarioId`;
- `ProductoVarianteId`;
- `AlmacenId`;
- `UbicacionAlmacenId` nullable únicamente para existencia raíz;
- `CantidadReservada`;
- `CantidadConsumida`;
- snapshots necesarios de SKU/producto para histórico;
- estado o cantidades derivadas suficientes para impedir doble liberación/consumo.

## Lifecycle recomendado

Estados canónicos iniciales:

`Borrador → Activa → Consumida`

Salidas terminales adicionales:

- `Liberada`;
- `Expirada`;
- `Cancelada`.

Reglas:

1. `Borrador` no modifica stock reservado.
2. Activar bloquea todas las existencias en orden determinista y aumenta `StockReservado` sólo si existe `StockDisponible` suficiente.
3. Una reserva activa reduce inmediatamente `StockDisponible` porque éste es calculado desde la misma existencia.
4. Liberar/expirar/cancelar reduce `StockReservado` exactamente una vez.
5. Consumir una reserva activa debe transformar el compromiso en salida física de forma atómica: reducir `StockReservado` y `StockFisico` de la misma existencia bajo el mismo lock/transacción.
6. Consumir no puede volver a validar contra un contador legacy distinto.
7. Estados terminales son idempotentes o fallan cerrado según contrato; nunca duplican liberación ni deducción.

## Prevención de overselling

La regla central es:

`cantidad_a_reservar <= StockDisponible`

La comprobación y el incremento de `StockReservado` deben ocurrir bajo `SELECT ... FOR UPDATE`/lock pesimista sobre la fila `ExistenciaVariante` correspondiente.

No es suficiente:

1. leer `StockDisponible`;
2. liberar el lock;
3. insertar una reserva;
4. actualizar reservado después.

Ese patrón permitiría dos reservas concurrentes sobre el mismo disponible.

### Orden de locks

Para reservas multilínea se debe reutilizar el orden global determinista de claves físicas ya adoptado por `IExistenciaVarianteConcurrencyService`, evitando deadlocks por orden inconsistente.

## Integración con Venta/pedido

### Creación/edición

El contrato de venta debe poder identificar el origen físico reservable por línea. N1.8.B/D deberá decidir si se amplía `VentaDetalleInputDto` directamente o se introduce un caso de uso de pedido/reserva separado; en cualquiera de los dos diseños la clave física no puede inferirse escogiendo arbitrariamente un almacén.

### Reserva

Un borrador/pedido autorizado puede crear o activar una reserva asociada a su documento. La reserva debe ser idempotente frente a reintentos del cliente y no duplicarse por timeout.

### Confirmación

La confirmación de una venta con reserva activa debe:

1. bloquear la reserva/documento;
2. bloquear las mismas `ExistenciaVariante`;
3. verificar que la reserva sigue activa y suficiente;
4. decrementar `StockReservado` y `StockFisico` en una única transacción;
5. registrar Kardex desde la misma clave física;
6. marcar la reserva como consumida;
7. confirmar Venta/factura/finanzas según el workflow vigente.

Para una venta sin reserva, la deducción igualmente debe ir contra `ExistenciaVariante.StockDisponible`; de lo contrario existirían dos caminos con distinta autoridad.

### Anulación/reversión

La reversión de una venta ya consumida restaura físico conforme al lifecycle de Venta/Kardex; **no reactiva automáticamente una reserva antigua**. Si el negocio necesita volver a reservar, debe crearse una nueva transición explícita.

## Persistencia e integridad prevista para N1.8.C

Como mínimo:

- tablas `ReservasInventario` y `ReservaInventarioDetalles`;
- FK Restrict a Venta cuando aplique;
- FK a `ProductoVariante`;
- FK a `Almacen`;
- FK compuesta `Almacen + UbicacionAlmacen` para impedir cross-almacén;
- índices por estado, expiración y documento origen;
- unicidad/idempotencia para impedir dos reservas activas canónicas del mismo documento cuando el contrato así lo defina;
- checks de cantidades no negativas;
- no duplicar `StockDisponible` en la tabla de reserva;
- snapshot EF y migración forward-only;
- preflight read-only para reservas/ventas históricas antes de cualquier backfill.

No se debe backfillear una reserva activa para ventas históricas ya confirmadas; eso inventaría compromisos inexistentes.

## Expiración

La expiración automática debe ser transaccional e idempotente.

Cada reserva expirada debe liberar `StockReservado` exactamente una vez. Un worker que reintente una expiración ya terminal no puede volver a restar reservado.

La política de TTL debe ser configurable por negocio y no codificarse como constante irreversible en dominio.

## RBAC y auditoría

Superficie mínima prevista:

- Ver;
- Crear/Reservar;
- Liberar/Cancelar;
- Consumir si se expone como operación independiente;
- administrar expiración/reintentos sólo si el producto lo requiere.

Eventos auditables:

- creación;
- activación;
- cambio de cantidad/clave física antes de activar;
- liberación;
- expiración;
- cancelación;
- consumo.

La auditoría debe registrar documento origen, reserva, clave física, cantidad y actor sin exponer secretos ni confiar en un correlation ID bruto del cliente.

## Frontend/UX

La UI debe diferenciar claramente:

- físico;
- reservado;
- disponible;
- cantidad solicitada/reservada.

Al seleccionar una línea se debe elegir una existencia real por almacén/ubicación y bloquear Guardar/Reservar si la cantidad supera el disponible conocido. Esta validación de UX es preventiva; el backend sigue siendo la autoridad y vuelve a validar bajo lock.

Estados requeridos: loading, error, vacío, reserva vencida, disponibilidad modificada por concurrencia y conflicto 409/ProblemDetails con mensaje accionable.

## Fuera de alcance de N1.8

- sustituir `ExistenciaVariante` por otro modelo de saldos;
- inventar almacén/ubicación para líneas históricas;
- permitir stock reservado superior al físico;
- reservar contra `Producto.Cantidad` o `ProductoVariante.Cantidad` como autoridad;
- crear lotes/series/vencimientos de N1.9;
- multiempresa N6;
- operaciones en Producción.

## Riesgos principales

### R1 — Doble autoridad durante Venta

**Severidad:** crítica.  
La confirmación actual de Venta todavía usa `IInventarioConcurrencyService` legacy. Debe migrarse al contexto físico antes de certificar reservas contra `ExistenciaVariante`.

### R2 — Reserva sin clave física

**Severidad:** alta.  
`VentaDetalleInputDto` no lleva `AlmacenId/UbicacionAlmacenId`. Reservar por variante solamente sería ambiguo cuando existen múltiples almacenes/ubicaciones.

### R3 — carrera reservar/reservar

**Severidad:** crítica.  
Dos transacciones pueden leer el mismo disponible si la validación no se hace bajo lock de la existencia.

### R4 — doble liberación/consumo

**Severidad:** crítica.  
Reintentos HTTP, workers de expiración y confirmación simultánea deben converger a una transición única e idempotente.

### R5 — expiración vs confirmación

**Severidad:** alta.  
Ambas operaciones deben serializar sobre reserva + existencias para evitar consumir una reserva que acaba de expirar o liberar una reserva ya consumida.

## Rollback

N1.8 debe ser forward-only en `Desarrollo`:

- revertir código mediante commits explícitos, nunca force-push;
- no borrar reservas históricas para “corregir” saldos;
- si una reserva activa quedó inconsistente, ejecutar transición compensatoria auditable que reconcilie `StockReservado` con el documento;
- migraciones posteriores deben corregir forward; rollback físico sólo con respaldo/restauración compatible;
- no operar Producción desde este punto.

## Matriz de pruebas mínima

### Dominio

- cantidad de reserva > 0;
- activación sólo desde estado permitido;
- liberación/expiración/cancelación exactamente una vez;
- consumo exactamente una vez;
- terminales no reabren silenciosamente;
- invariantes de cantidades.

### Concurrencia/integración MySQL

- dos reservas simultáneas cuyo total excede disponible: sólo una combinación válida puede comprometer stock;
- reserva y confirmación concurrentes;
- expiración y confirmación concurrentes;
- reserva multilínea con lock order determinista;
- `StockReservado <= StockFisico` siempre;
- `StockDisponible = StockFisico - StockReservado` después de cada transición.

### Venta

- contrato físico por línea;
- confirmar con reserva consume físico+reservado atómicamente;
- confirmar sin reserva valida `ExistenciaVariante`;
- anular no reactiva reservas antiguas;
- Kardex usa almacén/ubicación reales.

### API

- autenticación/RBAC;
- ProblemDetails para falta de stock/conflicto de versión/estado;
- reintento idempotente de reservar/liberar/consumir;
- filtros/paginación si existe mantenimiento de reservas.

### Frontend/E2E

- muestra físico/reservado/disponible diferenciados;
- selección de almacén/ubicación;
- conflicto de disponibilidad actualizado;
- reserva → confirmación/consumo;
- reserva → liberación;
- expiración visible sin doble liberación;
- responsive y accesibilidad.

## Criterio de aceptación de N1.8.A

N1.8.A queda satisfecho cuando este preflight está publicado y el tablero conserva como decisiones obligatorias para B–H:

1. `ExistenciaVariante` sigue siendo la única autoridad de saldo.
2. `StockReservado` se explica mediante documentos de reserva, no por un contador paralelo.
3. la reserva usa clave física `Variante + Almacén + Ubicación`;
4. overselling se impide con validación+actualización bajo el mismo lock;
5. Venta debe abandonar la deducción legacy antes del cierre de N1.8;
6. expiración, liberación y consumo son idempotentes;
7. ningún histórico recibe reservas inventadas por backfill;
8. N1.8 no invade lotes/series/vencimientos N1.9 ni multiempresa N6.
