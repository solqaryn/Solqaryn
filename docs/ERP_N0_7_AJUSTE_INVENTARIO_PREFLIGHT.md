# ERP-N0.7 — AjusteInventario formal — Preflight

## Estado

**N0.7.A — Auditoría y preflight.** Este documento define el baseline y la transición requerida antes de implementar dominio, EF/migraciones, API o frontend.

Base inspeccionada: `5d4b08487bf119b521783bf74c778d0b1f2bf905` (`Desarrollo`).

## Objetivo del punto

Convertir el ajuste manual de stock actual en un **documento empresarial auditable** con cabecera/detalle y ciclo de vida:

`Borrador -> Confirmado -> Anulado`

El documento debe registrar cantidades antes/después, diferencia, costo relevante, motivo, usuario/fechas y movimientos de inventario trazables. Crear/editar un borrador no debe tocar stock. El stock sólo cambia al confirmar; una anulación debe revertir mediante movimientos inversos, no borrar ni reescribir historia.

## Estado real actual

Existe una superficie de ajuste directo:

- `backend/src/API/Controllers/InventarioAjustesController.cs`
  - `POST /inventario/ajustes/producto`
  - `POST /inventario/ajustes/variante`
  - permiso actual: `MovimientosInventario:Crear`.
- `backend/src/Application/Interfaces/IInventarioAjusteService.cs`
- `backend/src/Application/Services/InventarioAjusteService.cs`
- `backend/src/Application/DTOs/AjusteStockDto.cs`
- `backend/tests/Solqaryn.Tests/InventarioAjusteServiceTests.cs`

El flujo actual recibe `NuevaCantidad + Motivo`, toma lock, muta stock inmediatamente, crea `MovimientoInventario` y audita. No existe cabecera/detalle, número documental, Borrador, Confirmar, Anular ni snapshot documental de costo.

Los movimientos actuales identifican el origen mediante strings legacy como `AjusteProducto` y `AjusteProductoVariante`. Esos strings no deben convertirse en una nueva autoridad paralela después de N0.6.

La búsqueda dirigida no encontró consumidor Angular del endpoint de ajuste directo. Esto reduce el riesgo de transición frontend, pero no autoriza a asumir que no existen consumidores HTTP externos; la API legacy debe tratarse de forma compatible o deprecada explícitamente.

## Integración obligatoria con ERP-N0.6

N0.6 estableció que los documentos origen usan relaciones tipadas. Actualmente `OrigenMovimientoInventario` y `TipoOrigenMovimientoInventario` cubren Compra, Venta y ConsumoInsumo; `MovimientoInventario` dispone de `CompraId`, `VentaId` y `ConsumoInsumoId`.

N0.7 debe extender ese contrato con **AjusteInventario**:

- `TipoOrigenMovimientoInventario.AjusteInventario`.
- `OrigenMovimientoInventario.DesdeAjusteInventario(id)`.
- `AjusteInventarioId` nullable en `MovimientoInventario`.
- FK, índice, snapshot/modelo EF y constraints de exclusividad actualizados.
- `MovimientoInventarioOrigenPersistido` y DTO/API de movimientos deben exponer el nuevo origen tipado.

La excepción transitoria de N0.6 que permite cero FKs para ajustes legacy no debe perpetuarse como arquitectura final. Un movimiento producido por el nuevo documento debe tener exactamente una FK `AjusteInventarioId`.

## Modelo de dominio propuesto para N0.7.B

### AjusteInventario

Reutilizar `ConfirmableEntity` para no duplicar auditoría de confirmar/anular.

Campos mínimos:

- `Id`.
- `NumeroAjuste` único y estable.
- `Estado`: `Borrador`, `Confirmado`, `Anulado`.
- `Motivo` obligatorio.
- `Observaciones` opcional.
- auditoría base de creación/actualización.
- usuario/fecha de confirmación.
- usuario/fecha/motivo de anulación.
- colección `Detalles`.

### AjusteInventarioDetalle

Campos mínimos:

- `AjusteInventarioId`.
- `ProductoId`.
- `ProductoVarianteId` nullable.
- snapshots identificativos útiles: nombre/SKU/dimensiones cuando aplique.
- `CantidadObjetivo` durante Borrador.
- `CantidadAnteriorSnapshot` materializada al confirmar.
- `CantidadNuevaSnapshot` materializada al confirmar.
- `DiferenciaSnapshot = nueva - anterior`.
- `CostoUnitarioSnapshot` al confirmar.
- `ImpactoCostoSnapshot = diferencia * costo` cuando aplique.

No conviene fijar `CantidadAnterior` al crear el borrador: el stock puede cambiar antes de confirmar. Debe calcularse bajo lock durante Confirmar.

## Reglas de stock y variantes

`Producto.Cantidad` es la cantidad consolidada. Cuando existen variantes, debe continuar representando la suma física de todas las variantes no eliminadas, incluidas variantes temporalmente inactivas.

Por tanto:

- detalle sobre variante: bloquear Producto + Variante, cambiar `Variante.Cantidad` y recalcular `Producto.Cantidad` desde todas las variantes no eliminadas dentro de la misma transacción;
- detalle sobre producto sin variante: cambiar stock de producto directamente sólo cuando el modelo no dependa de variantes;
- impedir que un ajuste directo al producto rompa la suma consolidada de variantes;
- cantidades nunca negativas;
- producto/variante eliminados o inconsistentes deben fallar cerrado;
- un documento sin diferencias reales no debe confirmarse como operación efectiva.

## Ciclo transaccional

### Crear/editar Borrador

- valida estructura, IDs y motivo;
- persiste cabecera/detalles;
- **no modifica stock**;
- sólo Borrador puede editarse.

### Confirmar

Una sola transacción:

1. cargar el documento con lock y exigir `Borrador`;
2. bloquear productos/variantes afectados en orden determinista para minimizar deadlocks;
3. volver a validar existencia/estado y coherencia producto-variante;
4. capturar cantidades/costos actuales;
5. calcular diferencias;
6. aplicar stock;
7. recalcular consolidado de producto cuando corresponda;
8. crear un `MovimientoInventario` por detalle efectivo con `Tipo=Ajuste`, `Causa=AjusteManual` y origen `AjusteInventarioId` tipado;
9. materializar snapshots del detalle;
10. marcar `Confirmado`, usuario y fecha;
11. commit atómico.

### Anular

Una sola transacción:

1. exigir documento `Confirmado` y motivo de anulación;
2. bloquear inventario afectado;
3. validar que la reversión puede aplicarse sin stock negativo/inconsistente;
4. aplicar el inverso de cada diferencia confirmada;
5. recalcular stock consolidado;
6. crear movimientos inversos `Tipo=Reversion`, ligados al **mismo `AjusteInventarioId`**;
7. marcar `Anulado`, usuario, fecha y motivo;
8. conservar intactos los movimientos originales y snapshots de confirmación.

No borrar ni reescribir los movimientos de confirmación.

## Estrategia de compatibilidad del endpoint legacy

No deben existir dos autoridades capaces de mutar stock.

Estrategia recomendada:

1. crear API canónica documental;
2. migrar frontend a la API documental;
3. conservar temporalmente los dos POST legacy sólo como **adaptadores de compatibilidad**, no como servicios de stock independientes;
4. cada POST legacy deberá crear y confirmar atómicamente un AjusteInventario de una sola línea, reutilizando el mismo caso de uso canónico;
5. marcar/documentar deprecación y retirar los adaptadores en la fase de saneamiento cuando no existan consumidores.

Esto conserva la semántica HTTP actual para consumidores externos sin mantener una segunda implementación de stock.

## API objetivo para N0.7.D

Contrato mínimo sugerido:

- `GET /inventario/ajustes` — filtros/paginación.
- `GET /inventario/ajustes/{id}`.
- `POST /inventario/ajustes` — crear Borrador.
- `PUT /inventario/ajustes/{id}` — editar Borrador.
- `POST /inventario/ajustes/{id}/confirmar`.
- `POST /inventario/ajustes/{id}/anular`.

Las respuestas deben usar el contrato común de API/ProblemDetails vigente. Confirmar/Anular deben ser idempotentes de forma explícita: repetir una transición ya aplicada no puede duplicar movimientos ni stock.

## Persistencia y migración para N0.7.C

Tablas nuevas esperadas:

- `AjustesInventario`.
- `AjustesInventarioDetalles`.

Cambios adicionales:

- `MovimientosInventario.AjusteInventarioId` nullable + FK/index.
- ampliación de constraints/checks/triggers transitorios de N0.6.
- snapshot EF actualizado sin drift.

No existe dato histórico que pueda convertirse de forma confiable en cabecera/detalle completa únicamente a partir de los strings `AjusteProducto/AjusteProductoVariante`; inventar documentos retroactivos sería riesgoso. Los movimientos legacy deben conservarse como historia previa y el nuevo contrato aplica **hacia adelante**, salvo que una auditoría específica demuestre datos suficientes para backfill documental.

La migración debe ser forward-safe, compatible con MySQL administrado y `sql_require_primary_key=ON`, con preflight/postcheck y sin cambios en Producción.

## RBAC y auditoría para N0.7.F

Actualmente el ajuste directo usa `MovimientosInventario:Crear`.

N0.7.F deberá decidir y certificar una de estas opciones, sin bypass:

- módulo dedicado `AjustesInventario` con acciones `Ver/Crear/Editar/Confirmar/Anular`, recomendado para separación empresarial;
- o, sólo si el modelo de permisos vigente demuestra equivalencia suficiente, mapear explícitamente las transiciones a permisos existentes de inventario.

Confirmar y Anular deben tener autorización distinguible; no debe bastar un permiso genérico de lectura.

Todas las mutaciones deben dejar auditoría de usuario, estado anterior/nuevo y documento afectado.

## Frontend para N0.7.E

No se encontró consumidor Angular actual del endpoint legacy. El frontend nuevo deberá implementar:

- listado/bandeja de ajustes;
- creación/edición de Borrador;
- líneas producto/variante;
- vista de cantidad actual orientativa, dejando claro que el snapshot definitivo se toma al confirmar;
- acciones Confirmar/Anular según estado y permisos;
- detalle histórico con antes/después/diferencia/costo/usuarios/fechas;
- estados y errores concurrentes claros;
- accesibilidad y responsive conforme al patrón actual.

## Matriz QA para N0.7.G

### Dominio/unitarias

- estados válidos y transiciones prohibidas;
- detalle producto vs variante;
- cantidades negativas/no-op;
- cálculo de diferencia e impacto de costo;
- origen tipado `AjusteInventario` exige ID positivo y exclusividad.

### Integración MySQL

- crear Borrador no cambia stock;
- confirmar aplica exactamente una vez;
- locks/concurrencia evitan lost update;
- variante recalcula consolidado del producto;
- `AjusteInventarioId` es la autoridad del movimiento;
- mismatch tipado/legacy falla cerrado;
- anular revierte exactamente la diferencia confirmada;
- anulación no borra movimientos originales;
- FK/constraints/snapshot EF sin drift;
- historial completo desde cero y upgrade representativo.

### API/contrato

- RBAC por transición;
- ProblemDetails/errores de negocio;
- filtros/paginación;
- idempotencia de Confirmar/Anular;
- adaptadores legacy, si permanecen, producen el mismo documento canónico.

### E2E

Borrador → editar → confirmar → verificar stock/movimientos → anular → verificar reversión/histórico.

## Rollback

- antes de migración: respaldo/preflight de Desarrollo;
- migraciones sólo en entorno de Desarrollo/CI hasta autorización futura;
- no eliminar inmediatamente endpoints ni columnas legacy;
- si falla la implementación antes del cutover, mantener el flujo actual sin aplicar DDL destructivo;
- una vez existan documentos confirmados, no hacer rollback por borrado de cabeceras/movimientos: restaurar desde respaldo o aplicar corrección forward;
- retirada de adaptadores legacy sólo después de evidencia de no consumidores.

## Riesgos principales

1. **Doble autoridad de stock** si el servicio directo y el documento formal mutan por caminos distintos.
2. **Desalineación Producto/Variante** si no se recalcula el consolidado bajo la misma transacción.
3. **Snapshot obsoleto** si `CantidadAnterior` se captura en Borrador en vez de Confirmar.
4. **Reversión incorrecta** si Anular intenta volver a un stock absoluto en vez de aplicar el inverso de la diferencia confirmada.
5. **Pérdida de trazabilidad N0.6** si los nuevos movimientos continúan sin FK documental tipada.
6. **Compatibilidad externa** si se eliminan de golpe los POST legacy sin conocer consumidores externos.
7. **Concurrencia/deadlocks** si productos/variantes se bloquean sin orden determinista.

## Secuencia N0.7 aprobable

- **N0.7.A** — este preflight.
- **N0.7.B** — dominio/contratos: entidades, estados, detalle y extensión del origen tipado; sin DDL.
- **N0.7.C** — persistencia/EF/migración/preflight/postcheck.
- **N0.7.D** — repositorios, servicio, API y adaptadores legacy.
- **N0.7.E** — frontend/UX.
- **N0.7.F** — RBAC, auditoría, seguridad y observabilidad.
- **N0.7.G** — QA/regresión/CI.
- **N0.7.H** — documentación y certificación final.

## Criterio de cierre de N0.7.A

N0.7.A queda listo cuando este preflight esté publicado y reconciliado con VAEP, sin código funcional ni DDL. N0.7.B puede entonces implementar únicamente el contrato de dominio definido aquí.

No se modifica `main`, Producción, secretos, recursos productivos, merge/auto-merge del PR #2 ni se crean ramas nuevas.
