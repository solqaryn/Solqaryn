# OpenAPI N1.9 — Trazabilidad de inventario

## Autoridad

La especificación ejecutable se deriva de `TrazabilidadInventarioController` y sus DTOs. Este documento congela rutas, intención y permisos para revisión humana; no sustituye Swagger/OpenAPI generado por la API.

Base path: `/trazabilidad-inventario`.

Todas las operaciones requieren autenticación. No existe `AllowAnonymous` en la superficie N1.9.

## Configuración por variante

### GET `/variantes/{productoVarianteId}/configuracion`

Consulta política opt-in.

Permiso: `MovimientosInventario:Ver`.

Respuesta: `ApiResponse<ConfiguracionTrazabilidadVarianteDto>`.

### PUT `/variantes/{productoVarianteId}/configuracion`

Actualiza `ControlaLote`, `ControlaNumeroSerie`, `ControlaFechaVencimiento` y `DiasAlertaVencimiento`.

Permiso: `MovimientosInventario:Editar`.

Reglas fail-closed principales: variante válida/activa, configuración coherente, no habilitar dimensión nueva sobre stock existente sin adopción explícita y no deshabilitar dimensiones con identidades activas incompatibles.

## Lotes

### GET `/lotes`

Listado paginado y filtrado. Permiso `Ver`.

### GET `/lotes/{id}`

Detalle de lote. Permiso `Ver`.

### POST `/lotes`

Alta idempotente por identidad normalizada dentro de la variante. Permiso `Crear`.

### PUT `/lotes/{id}`

Edición de lote activo. Permiso `Editar`.

### POST `/lotes/{id}/desactivar`

Desactivación controlada. Permiso `Anular`.

## Series

### GET `/series`

Listado paginado y filtrado. Permiso `Ver`.

### GET `/series/{id}`

Detalle de serie. Permiso `Ver`.

### POST `/series`

Alta de identidad serial normalizada. Permiso `Crear`. La unicidad persistente protege concurrencia y la identidad debe respetar la política de la variante/lote.

### POST `/series/{id}/baja`

Transición controlada a baja. Permiso `Anular`.

## Errores

Las reglas de negocio se traducen mediante el pipeline global de errores de Solqaryn. Una petición inválida o una carrera de unicidad no debe degradarse a éxito parcial. Las operaciones críticas se ejecutan transaccionalmente y su auditoría estricta forma parte del mismo resultado.

## Seguridad

- `[Authorize]` a nivel de controller.
- `RequierePermiso` explícito en todas las acciones HTTP.
- módulos/acciones relacionales `MovimientosInventario`.
- correlation de auditoría desde `TraceIdentifier` saneado.
- sin secretos ni identidad sensible innecesaria en payloads de auditoría.