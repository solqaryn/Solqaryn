# N5.3.A — Backend/Data Preflight de reportes de ventas

Autoridad operativa: `docs/VAEP_AUTHORITY.md`  
Parent: `N5.3.A`  
Scope: `N5.3.A.1.BACKEND_DATA_PREFLIGHT`  
Owner de takeover: `CHATGPT_VAEP`  

## Dictamen

El backend actual contiene datos suficientes para soportar gran parte de las dimensiones solicitadas, pero **no existe todavía un contrato/query de reportes de ventas que componga de forma segura todas las dimensiones**. Este preflight no implementa N5.3.B+; fija las fuentes, joins, vacíos y gates que la implementación deberá respetar.

## Fuentes autoritativas encontradas

### Cabecera de venta

`backend/src/Domain/Entities/Venta.cs` contiene:

- periodo: `Fecha`;
- cliente: `ClienteId` y snapshots `ClienteNombre`, identidad/RTN, teléfono, correo y dirección;
- usuario: por herencia de `AuditableEntity`, `CreadoPorUsuarioId` / `CreadoPorNombreUsuario`; adicionalmente `ConfirmadoPorUsuarioId` / `ConfirmadoPorNombreUsuario` en `ConfirmableEntity`;
- estado documental y pago;
- importes (`ImporteBruto`, `Subtotal`, `Descuento`, `Impuesto`, `Total`, `CostoTotal`, `UtilidadBruta`).

**Gap material:** `Venta` no posee `SucursalId` directo. Por tanto un filtro de sucursal no debe inventarse a partir del usuario ni de metadatos no persistidos.

### Detalle de venta

`backend/src/Domain/Entities/VentaDetalle.cs` contiene:

- `ProductoId` y `ProductoVarianteId`;
- `AlmacenId` / `UbicacionAlmacenId` como contexto físico;
- cantidad, precio, costo snapshot, subtotal y utilidad bruta;
- snapshots de nombre, marca, modelo, color, talla y SKU del producto vendido.

Para reportes históricos, los snapshots del detalle son una fuente durable para nombre/marca/modelo/color/talla/SKU al momento de la venta y evitan reescribir la historia cuando cambie el catálogo actual.

### Sucursal

`Almacen` posee `SucursalId` y relación `Sucursal`. Por ello la dimensión sucursal puede derivarse desde `VentaDetalle.AlmacenId -> Almacen.SucursalId` cuando el detalle conserva `AlmacenId`.

**Implicación:** una venta con detalles de más de un almacén/sucursal no debe proyectarse ingenuamente a una única sucursal de cabecera. La semántica del reporte debe definir si la unidad de agregación es venta, detalle o sucursal-detalle.

### Producto, categoría y variante

`Producto` posee `CategoriaId` y maestros normalizados `MarcaId`, `ModeloId`, `ColorId`, `TallaId`, mientras `ProductoVariante` es la autoridad operacional de variante y contiene `MarcaId`, `ModeloId`, `ColorId`, `TallaId`, SKU y código de barras.

Para historia de ventas:

- categoría: join por `VentaDetalle.ProductoId -> Producto.CategoriaId` (no existe snapshot de categoría en `VentaDetalle`);
- producto: `ProductoId`, con `ProductoNombreSnapshot` para presentación histórica;
- variante: `ProductoVarianteId`;
- marca/modelo/color/talla: preferir snapshots del detalle para representación histórica; usar IDs/maestros actuales sólo para filtros que explícitamente requieran identidad maestra y estén documentados como tal.

## Query paths existentes y limitaciones

`VentaRepository.ConIncludes()` carga método de pago, detalles/producto/imágenes, variante/color, factura, descuentos e impuestos y usa `AsSplitQuery()`. `GetPagedAsync(PagedRequest)` aplica alcance de usuario fail-closed y soporta hoy búsqueda textual, paginación, orden por fecha/total/cliente y `UsuarioIdScope` sólo para administrador.

Los helpers de métricas existentes (`GetTotalDelMesAsync`, `GetIngresosDelMesAsync`, `GetCuentasPorCobrarAsync`, `GetUtilidadBrutaTotalAsync`) son agregados acotados al alcance del usuario; **no equivalen** a un motor de reportes multidimensional.

No se encontró un `ReporteVentaFiltroDto`/query equivalente que soporte conjuntamente periodo, usuario, cliente, sucursal, categoría, producto, variante, marca, modelo, color y talla. Extender `GetPagedAsync` indiscriminadamente sería riesgoso porque su `ConIncludes()` está orientado a list/detail y puede producir carga excesiva para agregaciones/reportes.

## Contrato de dimensiones requerido para N5.3.B+

| Dimensión | Fuente recomendada | Observación/gate |
|---|---|---|
| Periodo | `Venta.Fecha` | rango UTC inclusivo/exclusivo explícito; evitar ambigüedad de timezone |
| Usuario | `Venta.CreadoPorUsuarioId` | mantener `IUsuarioScopeService`; no confiar en parámetro HTTP para no-admin |
| Cliente | `Venta.ClienteId` + snapshots | decidir semántica de cliente eliminado/histórico |
| Sucursal | `VentaDetalle.AlmacenId -> Almacen.SucursalId` | definir ventas con múltiples almacenes/sucursales |
| Categoría | `VentaDetalle.ProductoId -> Producto.CategoriaId` | no hay snapshot histórico de categoría; documentar semántica actual vs histórica |
| Producto | `VentaDetalle.ProductoId` | snapshot de nombre para display histórico |
| Variante | `VentaDetalle.ProductoVarianteId` | nullable por compatibilidad histórica |
| Marca | snapshot detalle / identidad maestra | no mezclar silenciosamente snapshot con maestro actual |
| Modelo | snapshot detalle / identidad maestra | mismo gate que marca |
| Color | snapshot detalle / variante actual | mismo gate histórico |
| Talla | snapshot detalle / variante actual | mismo gate histórico |

## Riesgos P0/P1 para la implementación

1. **P0 — alcance/RBAC:** todo query de ventas debe conservar el comportamiento fail-closed de `AplicarAlcance`; un no-admin jamás obtiene ventas de otro usuario por un parámetro de filtro.
2. **P0 — sucursal falsa:** no crear `SucursalId` derivado del usuario ni asumir una sola sucursal por venta sin evidencia. La relación disponible es por detalle/almacén.
3. **P1 — historia mutable:** usar únicamente maestros actuales para marca/modelo/color/talla puede cambiar el significado de una venta histórica; los snapshots existen y deben formar parte del contrato.
4. **P1 — categoría histórica:** no existe snapshot de categoría en `VentaDetalle`; si el producto cambia de categoría, un reporte por categoría actual puede reclasificar historia. La semántica debe ser explícita antes de certificar.
5. **P1 — performance:** evitar `ConIncludes()` como base de agregaciones amplias. N5.3.B debe diseñar proyección server-side (`Select`/grouping) y revisar índices/query plan antes de aceptar volumen empresarial.
6. **P1 — anulaciones/eliminados:** definir si métricas incluyen sólo `EstadoDocumento.Confirmada`, cómo excluyen anuladas y cómo tratan borrado lógico.

## Dependencias para los scopes paralelos

- J2/API debe fijar DTO/HTTP/paginación/export y errores sin relajar el alcance anterior.
- J3/frontend debe modelar filtros conforme a la semántica backend, especialmente sucursal y dimensiones históricas.
- J4/security debe verificar RBAC/row scope y campos financieros sensibles.
- J5/test/CI debe cubrir combinatoria crítica, no-admin forged `UsuarioId`, ventas multi-almacén, snapshots y nulos históricos.
- J6/integración debe consolidar DoD/rollback y decidir si se requiere migración/índice antes de N5.3.B.

## Acceptance implications / DoD de preflight

Este scope documental queda completo cuando:

- las fuentes de las 10 dimensiones están trazadas a entidades/campos reales;
- los gaps de sucursal, categoría histórica y contrato multidimensional quedan explícitos;
- se preserva el gate de `IUsuarioScopeService` fail-closed;
- no se atribuye `ACTIVE/PASS/LISTO` a transporte o a la sesión Jules que quedó esperando confirmación;
- no se implementa código de N5.3.B+, ni se toca `main`, Producción, secrets o PR #2.

## Validación proporcional

- Inspección estática de `Venta`, `VentaDetalle`, `Producto`, `ProductoVariante`, `Almacen`, `AuditableEntity`, `ConfirmableEntity` y `VentaRepository` en `Desarrollo`.
- Cambio docs-only: no se requiere migración, build ni despliegue para este preflight.
- REVIEW_FIRST de takeover: se verificó que cada afirmación material anterior tenga una fuente concreta en el repo; no se declara el parent `LISTO_REAL` por este documento aislado.

## REVIEW_FIRST

**Resultado del scope J1:** `REVIEW_ACCEPTED` para el preflight documental, con **0 P0 / 0 P1 abiertos dentro de este scope** porque los riesgos enumerados son gates obligatorios de la implementación posterior, no defectos introducidos por este preflight. El parent `N5.3.A` continúa abierto hasta reconciliar y aceptar los demás scopes J2–J6 y sus gates causales.
