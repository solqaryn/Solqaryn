# N2.3 — Contrato HTTP de RecepcionCompra

Base de ruta del controlador: `/recepciones-compra`. Todos los endpoints requieren autenticación y autorización relacional mediante `RequierePermiso(ModuloSistema.Compras, ...)`.

## Endpoints implementados

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| GET | `/recepciones-compra` | Compras/Ver | Buscar recepciones paginadas mediante `RecepcionCompraQueryDto`. |
| GET | `/recepciones-compra/{id}` | Compras/Ver | Obtener una recepción por identificador. Devuelve ProblemDetails 404 cuando no existe. |
| GET | `/recepciones-compra/ordenes/{ordenCompraId}/saldo` | Compras/Ver | Consultar saldo por línea: ordenado, aceptado acumulado y pendiente. |
| POST | `/recepciones-compra` | Compras/Crear | Crear borrador. Requiere encabezado `Idempotency-Key`. |
| PUT | `/recepciones-compra/{id}` | Compras/Editar | Actualizar recepción mientras esté en Borrador. |
| POST | `/recepciones-compra/{id}/confirmar` | Compras/Confirmar | Confirmar y materializar stock/Kardex. |
| POST | `/recepciones-compra/{id}/anular` | Compras/Anular | Anular y revertir stock/Kardex cuando sea seguro. |

## Idempotency-Key
El alta rechaza la ausencia del encabezado con HTTP 400. El servicio normaliza la clave, limita su longitud a 128 caracteres y permite únicamente alfanuméricos y `- _ . :`.

La clave se enlaza a un fingerprint SHA-256 del payload canónico. El mismo request puede repetirse de forma segura; una clave reutilizada con payload diferente produce conflicto.

## Reglas del request de creación/edición
Una recepción debe:
- apuntar a una OrdenCompra existente y en estado Aprobada;
- incluir al menos un detalle;
- usar líneas pertenecientes a esa OrdenCompra;
- usar un almacén existente, activo y no eliminado;
- si informa ubicación, ésta debe estar activa y pertenecer al almacén seleccionado;
- mantener cantidades de recepción/diferencias válidas según el dominio;
- no duplicar en el mismo documento la misma clave física línea+almacén+ubicación.

## Confirmación
La operación es transaccional e idempotente respecto del estado: si ya está Recibida, devuelve el documento actual. Para una recepción Borrador valida nuevamente la orden, acumulados de recepciones previas y límite de cantidad ordenada; luego materializa existencia física, registra Kardex, cambia estado y registra auditoría estricta.

## Anulación
La operación es transaccional e idempotente respecto del estado Anulada. Solo una recepción Recibida puede anularse. El body `AnularRecepcionCompraDto` debe contener un motivo válido. Si existen movimientos de inventario posteriores relacionados con la recepción, la operación se rechaza para impedir una reversión histórica insegura.

## Respuestas y errores
Las respuestas exitosas usan `ApiResponse<T>`. Los errores de recurso/regla/conflicto son transformados por la infraestructura común de errores; el controlador usa ProblemDetails explícito para 404 de detalle/saldo y para la ausencia de `Idempotency-Key`.

## Contrato de seguridad
La UI no sustituye el control del API. `Authorize` y `RequierePermiso` son obligatorios en servidor. Las mutaciones requieren además un usuario autenticado válido dentro del servicio antes de registrar actor/auditoría.

## Evidencia
Este contrato fue contrastado con `RecepcionesCompraController` y `RecepcionCompraService` del baseline funcional `8b8b95ce0573653452cee7ca5024d82bdb184d88`, certificado por M13 `#32320525485` SUCCESS.