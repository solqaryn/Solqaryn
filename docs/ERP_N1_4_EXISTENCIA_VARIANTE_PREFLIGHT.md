# ERP-N1.4.A — ExistenciaVariante — Auditoría y preflight

Fecha: 2026-08-14  
Plan rector: `PLAN_MAESTRO_ERP_V5`  
Rama autorizada: `Desarrollo`  
Baseline inspeccionado: `c32eac7ee32abfb41b5d3da90ac31bd614d2c494`  
Estado: **PRELIGHT COMPLETADO — listo para N1.4.B**

---

## 1. Objetivo

Diseñar la transición desde el stock global/por variante legado hacia una autoridad normalizada de existencias por:

```text
ProductoVariante + Almacen + UbicacionAlmacen opcional
```

con los campos de negocio requeridos por ERP-N1.4:

- stock físico;
- stock reservado;
- stock disponible;
- stock en tránsito;
- stock mínimo;
- stock máximo.

La transición debe preservar integridad histórica, concurrencia pesimista, anulaciones/reversiones, valoración y auditoría sin permitir doble autoridad de inventario.

---

## 2. Estado real confirmado

### 2.1 No existe `ExistenciaVariante`

`AppDbContext` contiene `Producto`, `ProductoVariante`, `MovimientoInventario`, `Almacen`/`UbicacionAlmacen` vía configuraciones ya certificadas, pero no existe entidad/DbSet/repositorio `ExistenciaVariante` ni una relación de stock con Almacén/Ubicación.

### 2.2 Autoridad legacy de stock

La autoridad operativa vigente es:

```text
ProductoVariante.Cantidad     // stock por variante
Producto.Cantidad             // total consolidado por producto
```

`ProductoVariante` también conserva `UmbralStockBajo`, y las propiedades `TieneStockBajo` / `EstaAgotada` se calculan directamente desde `Cantidad`.

`ProductoDto` y `ProductoVarianteDto` exponen esas cantidades como stock vivo. `CreateProductoVarianteDto`/`UpdateProductoVarianteDto` todavía contienen `Cantidad` y `UmbralStockBajo`.

`ProductoVarianteService` ya impide editar stock desde el mantenimiento normal en variantes existentes, pero:

- una variante comercial nueva puede nacer con `Cantidad`;
- una variante técnica nueva puede nacer con `Cantidad`;
- eliminación/conversión todavía decide usando `variante.Cantidad == 0`;
- la proyección de compatibilidad del producto deriva de las cantidades de variantes.

### 2.3 Escrituras transaccionales actuales

Se confirmaron cuatro productores principales de stock:

1. `CompraService`
   - al confirmar: incrementa `ProductoVariante.Cantidad` y `Producto.Cantidad`;
   - al anular: decrementa ambos;
   - genera snapshots `StockAnterior/StockNuevo` en movimientos y snapshots de valoración en detalle de compra.

2. `VentaService`
   - al confirmar: decrementa variante y producto;
   - al anular: repone ambos;
   - la validación de disponibilidad usa `variante.Cantidad`.

3. `AjusteInventarioService`
   - al confirmar: reemplaza la cantidad objetivo de variante/producto;
   - al anular: restaura por diferencia/snapshot;
   - conserva precondiciones de cantidad esperada para detectar concurrencia.

4. `ConsumoInsumoService`
   - al confirmar: decrementa producto/variante;
   - al anular: repone producto/variante;
   - genera movimientos tipados de inventario.

### 2.4 Concurrencia actual

`InventarioConcurrencyService` exige transacción activa y hace lock pesimista `FOR UPDATE` sobre:

```text
Productos
ProductoVariantes
```

Las demandas se consolidan por `(ProductoId, ProductoVarianteId)`, se ordenan para reducir deadlocks y el stock disponible se valida contra `Producto.Cantidad`/`ProductoVariante.Cantidad`.

Por tanto, crear `ExistenciaVariante` sin migrar esta unidad de bloqueo produciría una autoridad física nueva con locks sobre la autoridad antigua: **no es aceptable**.

### 2.5 Los documentos operativos no conocen Almacén

Los contratos actuales de:

- `CompraDetalleInputDto`;
- `VentaDetalleInputDto`;
- `AjusteInventarioDetalleInputDto`;
- `ConsumoInsumoDetalleInputDto`;

solo contienen Producto/Variante/Cantidad (más precio/costo cuando aplica). No contienen `AlmacenId` ni `UbicacionAlmacenId`.

Por ello el origen/destino de stock debe incorporarse explícitamente antes del cutover. No se autoriza inferir el Almacén de una existencia arbitraria.

### 2.6 Snapshots históricos

`MovimientoInventario.StockAnterior/StockNuevo` y los snapshots de valoración de compras son evidencia histórica. **No son autoridad de stock vivo y deben permanecer inmutables**.

`AppDbContext` usa actualmente `Producto.Cantidad` y `ProductoVariante.Cantidad` para capturar/restaurar snapshots de valoración de compras; esta lógica debe ser migrada antes de retirar la semántica legacy.

---

## 3. Modelo objetivo propuesto

Entidad autoritativa:

```text
ExistenciaVariante : AuditableEntity
- Id
- ProductoVarianteId          requerido
- AlmacenId                   requerido
- UbicacionAlmacenId          opcional
- StockFisico                 >= 0
- StockReservado              >= 0 y <= StockFisico
- StockDisponible             generado = StockFisico - StockReservado
- StockTransito               >= 0
- StockMinimo                 >= 0
- StockMaximo                 nullable; si existe >= StockMinimo
- auditoría heredada
```

### 3.1 Autoridad

Después del cutover:

```text
ExistenciaVariante.StockFisico/Reservado/Transito = autoridad persistente
StockDisponible = proyección calculada por BD
ProductoVariante.Cantidad = NO autoridad
Producto.Cantidad = NO autoridad
```

Mientras existan columnas legacy por compatibilidad, deberán ser **proyecciones one-way derivadas** de `ExistenciaVariante`, nunca entradas independientes ni condiciones de concurrencia.

### 3.2 Semántica de disponible

No se permitirá editar `StockDisponible` directamente.

```text
StockDisponible = StockFisico - StockReservado
```

Se recomienda columna generada/persistida por MySQL para que la invariante exista también fuera de la capa de aplicación.

`StockTransito` no se suma a físico/disponible hasta la recepción efectiva.

### 3.3 Umbrales

`ProductoVariante.UmbralStockBajo` es legacy global. El nuevo `StockMinimo` pertenece a la existencia y permite umbrales distintos por Almacén/Ubicación.

En el backfill inicial:

```text
StockMinimo = ProductoVariante.UmbralStockBajo
```

Luego los consumidores de stock bajo deben migrar a existencia/agregados. El campo legacy no puede continuar como autoridad paralela.

### 3.4 Ubicación opcional y pertenencia al Almacén

Si `UbicacionAlmacenId` existe, debe pertenecer al mismo `AlmacenId`.

N1.3 ya dejó la clave alternativa:

```text
UbicacionAlmacen (AlmacenId, Id)
```

N1.4 debe reutilizarla con FK compuesta:

```text
ExistenciaVariante (AlmacenId, UbicacionAlmacenId)
  -> UbicacionAlmacen (AlmacenId, Id)
```

### 3.5 Unicidad con ubicación nullable

Se requiere una sola existencia para cada clave lógica:

```text
(ProductoVarianteId, AlmacenId, UbicacionAlmacenId?)
```

MySQL permite múltiples `NULL` dentro de un índice `UNIQUE`, por lo que un índice simple no garantiza una única fila “sin ubicación”.

Diseño físico recomendado:

```text
UbicacionClave = COALESCE(UbicacionAlmacenId, 0)   // columna generada/sombra
UNIQUE (ProductoVarianteId, AlmacenId, UbicacionClave)
```

Los IDs reales son positivos; `0` se reserva exclusivamente como discriminador físico, no como FK ni valor de dominio.

---

## 4. Cutover sin doble autoridad

### Etapa 1 — modelo aditivo y preflight

Crear tabla/constraints/FKs sin cambiar todavía escritores operativos. Ninguna fila nueva debe convertirse en autoridad hasta validar el backfill.

### Etapa 2 — backfill determinista

El histórico actual no tiene dimensión Almacén. Regla fail-closed:

1. si todas las cantidades legacy son cero, el sistema puede iniciar existencias vacías y exigir Almacén explícito en operaciones futuras;
2. si existe cualquier `ProductoVariante.Cantidad > 0`:
   - con exactamente un Almacén activo y válido: ese Almacén puede ser destino determinista del backfill, con `UbicacionAlmacenId = NULL`;
   - con cero Almacenes válidos: abortar;
   - con más de un Almacén válido: **abortar y exigir mapeo explícito de distribución histórica**; jamás repartir, escoger “el primero” o duplicar stock.

El postcheck debe demostrar:

```text
SUM(ExistenciaVariante.StockFisico por Variante) == ProductoVariante.Cantidad legacy
SUM(ExistenciaVariante.StockFisico por Producto) == Producto.Cantidad legacy
```

antes de habilitar el cutover.

### Etapa 3 — migrar locks y escritores

`InventarioConcurrencyService` debe cambiar la unidad de demanda hacia una clave de existencia, por ejemplo:

```text
ProductoVarianteId + AlmacenId + UbicacionAlmacenId?
```

Locks `FOR UPDATE` deben tomarse en orden determinista por la clave compuesta para evitar deadlocks.

Compra/Venta/Ajuste/Consumo deben mutar **solo ExistenciaVariante**. Si las columnas legacy deben permanecer temporalmente, se recalculan one-way dentro de la misma transacción como compatibilidad, sin validarlas ni aceptarlas como input.

### Etapa 4 — migrar lectores y contratos

Migrar:

- DTOs de producto/variante;
- catálogo y escáner;
- dashboard/stock bajo/agotado;
- reportes de inventario por variante;
- formularios frontend;
- cargas masivas/importaciones/exportaciones que transporten cantidad;
- validaciones de eliminación/conversión de variantes;
- valoración de compras y cualquier cálculo agregado.

Los mantenimientos de Producto/Variante deben dejar de aceptar cantidad viva. Stock/umbrales pasan al mantenimiento/operación de existencias.

### Etapa 5 — retirar autoridad legacy

Una vez probado que no existen escritores/lectores decisorios de `Producto.Cantidad`/`ProductoVariante.Cantidad`:

- marcar campos legacy como compatibilidad read-only si aún son necesarios para una transición corta;
- añadir pruebas/guardas que prohíban escrituras directas;
- retirar físicamente solo cuando ningún histórico/serializer/report dependerá de ellos.

No se autoriza mantener dos autoridades editables “por compatibilidad”.

---

## 5. Cambios requeridos en documentos operativos

### Compra

La recepción debe tener `AlmacenId` obligatorio. `UbicacionAlmacenId` puede ser opcional por detalle o recepción según la granularidad elegida en B/D, pero debe pertenecer al mismo Almacén.

Una compra no puede confirmar stock sin destino explícito.

### Venta

La salida debe tener Almacén origen obligatorio antes de confirmar.

N1.4 no debe inventar asignación multi-almacén automática. Si una venta requiere múltiples Almacenes, debe modelarse explícitamente en una fase posterior o mediante detalles de origen, no seleccionando stock de forma implícita.

### AjusteInventario

Cada detalle debe apuntar a una existencia concreta. La precondición de concurrencia debe comparar el `StockFisico` esperado de esa existencia, no `ProductoVariante.Cantidad` global.

### ConsumoInsumo

Cada consumo debe indicar Almacén origen; la reversión debe volver a la misma existencia/origen histórico.

### Reversiones/anulaciones

Para que una anulación sea determinista incluso si la configuración cambia, el documento/movimiento debe preservar la clave de existencia usada al confirmar (Almacén y Ubicación cuando aplique).

---

## 6. MovimientoInventario

`MovimientoInventario` seguirá siendo ledger/snapshot histórico, no saldo vivo.

N1.4 debe extenderlo con contexto de stock suficiente para trazabilidad:

```text
AlmacenId
UbicacionAlmacenId? / snapshot identificador si corresponde
```

y conservar `StockAnterior/StockNuevo` como snapshots de la existencia afectada.

No se debe reconstruir el saldo vivo sumando movimientos en cada operación normal; la autoridad es `ExistenciaVariante`.

---

## 7. Costo y valoración

La implementación actual usa cantidades globales/por variante para promedio ponderado y snapshots de compra.

N1.4 debe separar dos conceptos:

- **cantidad física por existencia**: nueva autoridad `ExistenciaVariante`;
- **costo de la variante/producto**: puede continuar global inicialmente si el Plan Maestro no exige valoración por Almacén todavía.

No se debe introducir costo por ubicación de forma implícita en N1.4. Sin embargo, cualquier promedio ponderado que use cantidad debe sumar `StockFisico` de las existencias pertinentes y no leer la cantidad legacy.

---

## 8. Reservado y tránsito: alcance preciso

ERP-N1.4 crea y protege los campos `StockReservado` y `StockTransito`.

No se inferirá todavía un motor completo de:

- reservas de pedidos;
- picking/packing;
- transferencias inter-almacén;
- recepción parcial en tránsito;
- asignación automática FEFO/FIFO/WMS.

Esas operaciones deben usar servicios explícitos en sus fases correspondientes. En N1.4, las invariantes y contratos quedan preparados para que ningún consumidor pueda producir disponible negativo o modificar disponible directamente.

---

## 9. Riesgos principales

### R1 — doble autoridad
**Crítico.** Crear existencias sin migrar locks/escritores dejaría `Cantidad` y `StockFisico` divergiendo.

Mitigación: cutover coordinado, proyección one-way temporal y pruebas de ausencia de escritores legacy.

### R2 — backfill sin dimensión Almacén
**Crítico.** El histórico no dice dónde está el stock.

Mitigación: backfill solo con destino determinista; múltiples Almacenes + stock positivo = fail-closed y mapeo explícito.

### R3 — deadlocks/race conditions
**Alto.** Cambia la granularidad de lock de Variante a Existencia.

Mitigación: consolidación y orden determinista por clave de existencia, transacción obligatoria y pruebas concurrentes reales MySQL.

### R4 — anulaciones históricas
**Alto.** Operaciones confirmadas antes del cutover no contienen Almacén.

Mitigación: backfill establece el Almacén histórico canónico y el cutover debe conservar bridge determinista para documentos pre-N1.4 hasta que sean irreversibles/retirados.

### R5 — unicidad con NULL
**Alto.** MySQL aceptaría duplicadas “sin ubicación”.

Mitigación: discriminador generado `COALESCE(UbicacionAlmacenId,0)` + UNIQUE.

### R6 — reportes/UI/cargas
**Alto.** Cantidad legacy está expuesta ampliamente.

Mitigación: inventario de consumidores y migración de proyecciones antes de retirar autoridad.

### R7 — umbral global versus umbral por existencia
**Medio.** `UmbralStockBajo` actual es global por variante.

Mitigación: backfill a `StockMinimo`, después lectura de alertas desde existencias/agregados.

---

## 10. Rollback

Persistencia N1.4 debe ser forward-safe.

Antes del cutover:

- tabla nueva puede eliminarse si el preflight/backfill falla y ningún escritor la usa;
- no borrar ni modificar cantidades legacy antes de demostrar equivalencia.

Después del cutover:

- rollback automático a doble escritura queda prohibido;
- restauración requiere snapshot/backup compatible o corrección forward;
- cualquier compatibilidad legacy debe ser proyección derivada, nunca autoridad reactivable de forma silenciosa.

---

## 11. Matriz mínima de pruebas

### Dominio

- físico/reservado/tránsito/mínimo no negativos;
- reservado no supera físico;
- disponible deriva exactamente de físico-reservado;
- máximo nulo o >= mínimo;
- Ubicación opcional.

### Persistencia MySQL 8.4

- FK Variante;
- FK Almacén;
- FK compuesta Ubicación→mismo Almacén;
- única existencia con/sin ubicación;
- checks/generados válidos en MySQL 8.4;
- snapshot EF sin drift.

### Backfill

- cero stock;
- un Almacén activo + stock positivo;
- cero Almacenes + stock positivo => falla;
- múltiples Almacenes + stock positivo => falla;
- equivalencia por variante y producto;
- preservación de `UmbralStockBajo` como `StockMinimo` inicial.

### Concurrencia

- transacción obligatoria;
- locks por existencia;
- demandas duplicadas consolidadas;
- orden determinista;
- dos deducciones concurrentes no producen stock negativo;
- reserva no produce disponible negativo.

### Flujos

- Compra confirma/anula en la misma existencia;
- Venta confirma/anula en la misma existencia;
- Ajuste confirma/anula por existencia;
- Consumo confirma/anula por existencia;
- movimientos conservan Almacén/Ubicación y snapshots correctos.

### Compatibilidad

- no existen escritores decisorios de `Producto.Cantidad`/`ProductoVariante.Cantidad` fuera de la proyección autorizada;
- DTOs/mantenimientos no aceptan stock legacy editable;
- catálogo/dashboard/reportes reflejan agregados de existencia;
- históricos anteriores al cutover permanecen consultables/reversibles de forma determinista.

### CI

- Release build + unitarias;
- integración MySQL 8.4;
- migración desde historial completo;
- upgrade desde esquema pre-N1.4 con dataset representativo;
- frontend lint/build;
- pruebas de regresión compra/venta/ajuste/consumo;
- prueba de concurrencia SQL real.

---

## 12. Secuencia recomendada B–H

### N1.4.B — Dominio y contratos

Crear `ExistenciaVariante`, invariantes puras y contratos que representen Almacén/Ubicación sin tocar todavía writers legacy.

### N1.4.C — Persistencia/migración/backfill

Tabla, FK compuesta, unicidad NULL-safe, checks/generados, preflight histórico, backfill determinista, postcheck y snapshot sin drift. La tabla aún no es autoridad operativa hasta D.

### N1.4.D — Aplicación/API/cutover de autoridad

Repositorio/servicio de existencia, nueva unidad de locking, Compra/Venta/Ajuste/Consumo, movimientos y valoración. Desde este punto, solo ExistenciaVariante puede decidir/mutar stock.

### N1.4.E — Frontend/UX y consumidores

Mantenimiento/consulta de existencias, selección explícita de Almacén/Ubicación, proyecciones de producto/variante, dashboard/reportes/cargas y eliminación de inputs legacy de cantidad.

### N1.4.F — RBAC/auditoría/seguridad/observabilidad

Permisos de existencia, auditoría de mutaciones, métricas sin PII y regresiones de autorización.

### N1.4.G — QA/regresión/CI

Certificación agregada, incluyendo upgrade histórico y concurrencia MySQL real.

### N1.4.H — Documentación/certificación

Reconciliar fuente canónica, `TASKS.md`, `CHANGELOG_AI.md` y VAEP; documentar autoridad final y cualquier proyección legacy temporal restante.

---

## 13. Criterio de salida de N1.4.A

N1.4.A queda listo cuando se acepta como dirección técnica que:

1. `ExistenciaVariante` será la única autoridad de stock vivo;
2. `StockDisponible` será derivado, no editable;
3. Ubicación opcional estará físicamente restringida al mismo Almacén;
4. la unicidad con `NULL` será protegida explícitamente;
5. Compra/Venta/Ajuste/Consumo deberán conocer Almacén antes del cutover;
6. el backfill nunca escogerá arbitrariamente un Almacén;
7. `Producto.Cantidad`/`ProductoVariante.Cantidad` dejarán de ser autoridad;
8. `MovimientoInventario` seguirá siendo ledger/snapshot histórico;
9. la valoración se refactorizará para usar existencias sin inventar valoración por Almacén no solicitada;
10. la transición tendrá preflight/postcheck/rollback fail-closed y pruebas de concurrencia MySQL reales.

**Siguiente foco:** `N1.4.B — ExistenciaVariante — Dominio y contratos`.
