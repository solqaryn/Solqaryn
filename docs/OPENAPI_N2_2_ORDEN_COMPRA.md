# Contrato HTTP ERP-N2.2 — OrdenCompra

## Convenciones

Base path: `/ordenes-compra`.

Todos los endpoints requieren autenticación y permisos relacionales mediante `RequierePermiso`. Las respuestas exitosas usan el envelope común `ApiResponse<T>`; los errores de recurso/regla usan la infraestructura ProblemDetails de Solqaryn.

La aprobación de OrdenCompra es documental: ningún endpoint de este contrato materializa recepción, stock, Kardex, costeo o finanzas.

## Endpoints

### GET `/ordenes-compra`

Permiso: `Compras:Ver`.

Query: `OrdenCompraFiltroDto`, incluyendo paginación y filtros soportados por el backend.

Respuesta: `ApiResponse<PagedResult<OrdenCompraDto>>`.

Reglas:

- `page >= 1`;
- `pageSize` está normalizado al límite operativo del backend;
- filtros inválidos fallan cerrado según contrato de aplicación.

### GET `/ordenes-compra/{id}`

Permiso: `Compras:Ver`.

Respuesta exitosa: `ApiResponse<OrdenCompraDto>`.

Inexistente: `404 ProblemDetails` con título `Orden de compra no encontrada`.

### POST `/ordenes-compra`

Permiso: `Compras:Crear`.

Header obligatorio:

```http
Idempotency-Key: <clave-estable-por-intento>
```

Body: `CreateOrdenCompraDto`.

Respuesta: `201 Created` + `ApiResponse<OrdenCompraDto>` y Location resoluble mediante `GetById`.

Idempotencia:

- header ausente/whitespace: `400 ProblemDetails` antes de invocar servicio;
- clave válida + mismo payload: replay idempotente;
- misma clave + payload distinto: conflicto fail-closed;
- longitud máxima contractual: 128 caracteres;
- el fingerprint interno SHA-256 no se expone por API.

### PUT `/ordenes-compra/{id}`

Permiso: `Compras:Editar`.

Body: `UpdateOrdenCompraDto`.

Sólo una orden `Borrador` puede modificarse. Las líneas/condiciones aprobadas no se reescriben mediante este endpoint.

### POST `/ordenes-compra/{id}/enviar-aprobacion`

Permiso: `Compras:Confirmar`.

Transición: `Borrador -> PendienteAprobacion`.

Precondiciones principales: documento válido, proveedor, moneda, al menos una línea y detalles consistentes.

### POST `/ordenes-compra/{id}/aprobar`

Permiso: `Compras:Aprobar`.

Transición: `PendienteAprobacion -> Aprobada`.

Efecto: compromiso comercial aprobado y auditoría. **No** recibe mercancía ni altera inventario.

### POST `/ordenes-compra/{id}/cancelar`

Permiso: `Compras:Anular`.

Body: `CancelarOrdenCompraDto` con motivo obligatorio.

Transición terminal hacia `Cancelada`, conservando actor/fecha/motivo.

## OrdenCompraDto — semántica

El DTO de salida representa el documento y conserva:

- identidad/número;
- estado;
- proveedor y snapshots;
- `SolicitudCompraId` opcional;
- moneda/condiciones/fecha esperada/observaciones;
- fechas y actores de lifecycle;
- detalles;
- snapshots históricos de SKU, nombre, marca, modelo, color y talla cuando aplican;
- totales derivados del compromiso comercial.

Los campos internos de idempotencia/fingerprint no forman parte del contrato público.

## Seguridad

Matriz mínima:

| Operación | Permiso |
| --- | --- |
| Listar / detalle | `Compras:Ver` |
| Crear | `Compras:Crear` |
| Editar | `Compras:Editar` |
| Enviar a aprobación | `Compras:Confirmar` |
| Aprobar | `Compras:Aprobar` |
| Cancelar | `Compras:Anular` |

`[Authorize]` protege globalmente el controller. No existe `[AllowAnonymous]` en la superficie N2.2.

## Errores y observabilidad

- 401: usuario no autenticado;
- 403: autenticado sin grant requerido;
- 404: orden inexistente;
- 400/ProblemDetails: contrato o regla de negocio inválida;
- conflicto de idempotencia: fail-closed conforme al servicio.

Correlation/logging/auditoría reutilizan la infraestructura transversal; datos sensibles no deben copiarse innecesariamente a payloads de auditoría.

## Evidencia

Contrato implementado en `OrdenesCompraController` y servicios N2.2.D. Baseline final funcional `b4d477e2de25077c459d02b479968c93c93bc910`; Development `#32218997006`, Acceptance `#32218996971` y M13 `#32218996978` SUCCESS.