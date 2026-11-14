# ERP-N1.5 — Kardex empresarial — Auditoría y preflight

## 1. Objetivo

Este preflight define el delta necesario para convertir `MovimientoInventario` en un Kardex empresarial trazable por variante y contexto físico, sin implementar todavía el changeset amplio de N1.5.

Alcance rector del punto: cada movimiento debe permitir reconstruir de forma inequívoca variante, almacén/ubicación, cantidad anterior, movimiento, cantidad posterior, costo, fecha, documento origen, usuario y correlación.

## 2. Estado real confirmado

### 2.1 Dominio `MovimientoInventario`

El modelo ya contiene una base relevante:

- `ProductoId` y `ProductoVarianteId`;
- `AlmacenId` y `UbicacionAlmacenId`, ambos nullable para compatibilidad histórica;
- `Tipo` y `Causa`;
- `Cantidad`, `StockAnterior`, `StockNuevo`;
- `CostoUnitario` y `PrecioUnitario`;
- snapshots de marca/modelo/color/talla/SKU;
- FKs tipadas `CompraId`, `VentaId`, `ConsumoInsumoId`, `AjusteInventarioId`;
- usuario creador y fecha.

La entidad todavía **no persiste `CorrelationId`**. Por tanto, hoy no existe una correlación durable del movimiento independiente de los logs/auditoría HTTP.

### 2.2 Persistencia y origen tipado

`IMovimientoInventarioRepository` y `MovimientoInventarioRepository` ya soportan origen tipado de Compra, Venta, Consumo y Ajuste. El repositorio valida que exista como máximo un origen tipado por movimiento y conserva `ReferenciaTipo/ReferenciaId` sólo como snapshot legacy.

La consulta de movimientos actual filtra únicamente por:

- producto;
- tipo;
- fecha desde/hasta.

No existe aún contrato de filtro por variante, almacén, ubicación, causa u origen documental. El query está limitado a 200 registros y ordenado por fecha descendente.

### 2.3 Servicio y DTO de consulta

`MovimientoInventarioService` pierde parte de la información que el modelo/persistencia ya conoce:

- no proyecta `AjusteInventarioId` como origen tipado;
- `OrigenTipo`/`OrigenId` sólo resuelven Compra, Venta y Consumo;
- no expone `AlmacenId` ni `UbicacionAlmacenId`;
- no expone `Causa`;
- no expone `CostoUnitario` ni `PrecioUnitario`;
- no expone `CreadoPorUsuarioId`;
- no existe `CorrelationId` que proyectar.

El DTO actual tampoco contiene esos campos.

### 2.4 API

El endpoint existente es:

`GET /inventario/movimientos`

Está protegido por `[Authorize]` y permiso relacional `MovimientosInventario:Ver`, pero sólo acepta `productoId`, `tipo`, `desde` y `hasta`.

El Kardex empresarial necesita al menos filtros adicionales por variante, almacén, ubicación y origen, además de paginación explícita para sustituir el `Take(200)` opaco.

## 3. Writers inspeccionados

### 3.1 Ajustes de inventario

El writer N1.4 de `AjusteInventarioService` ya es el consumidor más alineado con el modelo objetivo:

- trabaja sobre `ExistenciaVariante.StockFisico`;
- registra `ProductoVarianteId`;
- registra `AlmacenId` y `UbicacionAlmacenId`;
- materializa `StockAnterior` y `StockNuevo` físicos;
- registra costo;
- usa origen tipado `AjusteInventario`;
- registra usuario y fecha.

Gap restante para Kardex: correlation ID durable y exposición completa en consulta/DTO.

### 3.2 Compras

La confirmación de compra registra movimientos con variante, cantidad, stock anterior/nuevo, costo, usuario y origen tipado Compra, pero el writer inspeccionado **no fija `AlmacenId`/`UbicacionAlmacenId`** en el movimiento. Además, continúa calculando el snapshot del movimiento desde el bridge legacy `ProductoVariante.Cantidad`/`Producto.Cantidad` en ese flujo.

Esto impide garantizar que el Kardex de compra identifique la existencia física exacta introducida por ERP-N1.4.

### 3.3 Ventas

La confirmación de venta registra variante, cantidad, stock anterior/nuevo, precio, costo, usuario y origen tipado Venta, pero el writer inspeccionado **no fija `AlmacenId`/`UbicacionAlmacenId`** y sigue obteniendo el snapshot de stock desde el bridge legacy de inventario.

### 3.4 Consumos administrativos

Confirmar y anular consumo registran variante, causa, cantidad, stock anterior/nuevo, costo, usuario y origen tipado Consumo, pero tampoco fijan almacén/ubicación y continúan usando el bridge legacy para el snapshot de stock.

## 4. Gaps priorizados

### P0 — Integridad del Kardex

1. **Contexto físico incompleto en writers**: Compra, Venta y Consumo deben generar movimientos contra una existencia concreta y persistir `AlmacenId`/`UbicacionAlmacenId`.
2. **Stock del movimiento debe provenir de la autoridad física**: `StockAnterior/StockNuevo` de writers nuevos debe corresponder a `ExistenciaVariante`, no a `ProductoVariante.Cantidad`.
3. **Correlation ID durable**: `MovimientoInventario` necesita almacenar el identificador de correlación de la operación que lo originó.

### P1 — Contrato de lectura empresarial

4. DTO y mapping deben exponer contexto físico, causa, costos/precio, usuario, correlación y los cuatro orígenes tipados, incluido Ajuste.
5. `OrigenTipo/OrigenId` debe resolver `AjusteInventarioId`; hoy se pierde en la capa Application.
6. Filtros de Kardex: producto, variante, almacén, ubicación, tipo, causa, origen/documento y rango de fechas.
7. Paginación determinista; eliminar el límite implícito `Take(200)` como contrato de negocio.

### P2 — Históricos y calidad de datos

8. Los movimientos previos a N1.4 pueden carecer legítimamente de almacén/ubicación. No se debe inventar contexto físico durante backfill.
9. La migración de `CorrelationId` debe admitir histórico nullable y exigirlo en writers nuevos mediante servicio/factory, no mediante un default ficticio.

## 5. Diseño objetivo propuesto

### 5.1 Entidad

Agregar a `MovimientoInventario`:

```text
CorrelationId string?  // nullable sólo por histórico; writers nuevos deben llenarlo
```

Conservar nullable `AlmacenId`/`UbicacionAlmacenId` en base por compatibilidad histórica, pero aplicar una regla de aplicación fail-closed: todo writer post-N1.5 que afecte una variante física debe persistir almacén y la ubicación efectiva cuando aplique.

### 5.2 DTO

Extender `MovimientoInventarioDto` con:

- `AlmacenId`, `AlmacenCodigo`, `AlmacenNombre`;
- `UbicacionAlmacenId`, `UbicacionCodigo`, `UbicacionNombre`;
- `Causa`;
- `CostoUnitario`, `PrecioUnitario`;
- `CreadoPorUsuarioId`;
- `CorrelationId`;
- `AjusteInventarioId`;
- origen tipado completo.

### 5.3 Filtro paginado

Crear un filtro explícito de Kardex con:

- `ProductoId`;
- `ProductoVarianteId`;
- `AlmacenId`;
- `UbicacionAlmacenId`;
- `Tipo`;
- `Causa`;
- `OrigenTipo`/`OrigenId`;
- `Desde`/`Hasta`;
- `Page`/`PageSize`.

Orden estable sugerido: `Fecha DESC, Id DESC`.

### 5.4 Writers

La materialización de movimientos debe recibir un contexto físico ya bloqueado/autoritativo. No se permite resolver almacén/ubicación después de mutar stock ni derivarlos de un default ambiguo.

Secuencia objetivo por operación:

1. resolver/bloquear `ExistenciaVariante` concreta;
2. capturar `StockFisico` anterior;
3. aplicar transición;
4. capturar `StockFisico` nuevo;
5. construir movimiento con variante + almacén + ubicación + origen + usuario + correlación;
6. persistir movimiento y documento dentro de la misma transacción;
7. actualizar únicamente el bridge legacy como proyección de compatibilidad mientras siga vigente.

## 6. Migración y reconciliación

La evolución de datos debe ser conservadora:

- `CorrelationId` nullable en esquema para no falsificar históricos;
- no backfillear almacén/ubicación si el documento histórico no permite una reconstrucción determinista;
- ejecutar reportes de reconciliación que separen `HISTORICO_SIN_CONTEXTO` de movimientos nuevos inválidos;
- desde el cutover, un movimiento nuevo de inventario físico sin variante/almacén debe fallar cerrado cuando la operación exige existencia concreta;
- preservar FKs tipadas y snapshots legacy durante la transición.

## 7. Riesgos

- **R1 — doble autoridad**: writers que sigan leyendo `ProductoVariante.Cantidad` pueden producir Kardex diferente al stock físico real.
- **R2 — almacén implícito**: asignar un almacén predeterminado sin contexto documental puede falsificar trazabilidad.
- **R3 — origen perdido**: la omisión actual de Ajuste en `MovimientoInventarioService` degrada trazabilidad aunque la FK esté persistida.
- **R4 — histórico mezclado con inválido nuevo**: constraints demasiado agresivos romperían datos previos legítimos; deben distinguirse por cutover/aplicación.
- **R5 — scope de lectura**: el repositorio limita usuarios no administradores a movimientos creados por ellos. N1.5 debe confirmar si el alcance empresarial correcto es usuario, sucursal/almacén o permiso antes de cambiarlo.

## 8. Fuera de alcance de N1.5.A

Este preflight no implementa todavía:

- DDL/migración;
- cutover de Compra/Venta/Consumo;
- cambios masivos de API/frontend;
- backfill destructivo;
- cambios en Producción;
- modificación de `main` o merge del PR.

## 9. Estrategia de rollback

Cada subfase debe ser reversible por commits causales en `Desarrollo`, sin force-push.

Para esquema/datos:

1. conservar columnas legacy durante el cutover;
2. no eliminar FKs/snapshots hasta reconciliación posterior;
3. si un writer nuevo falla, revertir su cutover sin borrar movimientos ya emitidos;
4. cualquier rollback de Producción requiere autorización y runbook separado.

## 10. Matriz mínima de pruebas para N1.5

| Área | Caso obligatorio |
| --- | --- |
| Dominio | movimiento nuevo conserva variante + contexto físico + correlación |
| Origen | Compra/Venta/Consumo/Ajuste resuelven `OrigenTipo/OrigenId` correctamente |
| Writer Compra | entrada usa StockFisico anterior/nuevo de existencia exacta |
| Writer Venta | salida usa StockFisico anterior/nuevo de existencia exacta |
| Writer Consumo | salida/reversión usa existencia exacta |
| Writer Ajuste | conserva comportamiento N1.4 multiubicación |
| Consulta | filtros por variante/almacén/ubicación/origen/fecha son combinables |
| Paginación | orden `Fecha DESC, Id DESC` sin duplicados/saltos |
| Seguridad | endpoint exige autenticación y permiso relacional |
| Histórico | filas previas nullable siguen consultables sin datos inventados |
| Auditoría | correlation ID permite correlacionar request, auditoría y movimiento |
| MySQL | migración, índices/FKs y consultas ejecutan sobre MySQL 8.4 |

## 11. Criterios de salida del preflight

N1.5.A puede cerrarse cuando:

- el estado real y los gaps estén documentados con evidencia de código;
- quede explícito que Ajuste ya usa contexto físico y que Compra/Venta/Consumo todavía requieren cutover;
- el gap de `AjusteInventarioId` en la proyección de consulta esté registrado;
- la estrategia de correlación/histórico/rollback sea fail-closed y no invente datos;
- la siguiente microtarea pueda comenzar con un contrato técnico acotado, sin reescaneo global.
