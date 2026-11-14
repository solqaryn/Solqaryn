# Contrato HTTP — ERP-N2.8 Cuentas por Pagar

Base: `/api/cuentasporpagar`. Todos los endpoints requieren autenticación y permisos relacionales del módulo Finanzas.

| Método | Ruta | Permiso | Propósito |
|---|---|---|---|
| GET | `/abiertas?proveedorId={id?}` | `Finanzas/Ver` | Listar obligaciones abiertas, opcionalmente por proveedor. |
| GET | `/proveedor/{proveedorId}` | `Finanzas/Ver` | Consultar obligaciones de un proveedor. |
| GET | `/{id}` | `Finanzas/Ver` | Consultar una cuenta por pagar. |
| POST | `/registrar-factura/{facturaProveedorId}` | `Finanzas/Crear` | Crear de forma idempotente la obligación derivada de una factura registrada. |
| POST | `/{id}/aplicar` | `Finanzas/Editar` | Registrar una aplicación de saldo permitida por dominio. |
| POST | `/{id}/anular-ultimo-pago` | `Finanzas/Editar` | Registrar la reversión trazable del último movimiento aplicable. |

## Request de aplicación

Campos lógicos: `ReferenciaIdempotencia` requerida, `Tipo`, `Monto`, `FechaAplicacion`, `Referencia` opcional, `Comentario` opcional y `ConfirmadoAdministrativamente`.

La API falla cerrado ante referencia idempotente vacía, entidad inexistente o transición inválida. Las mutaciones se ejecutan en transacción y no implican cambios de inventario.

## Respuestas y errores

Se preserva el envelope HTTP transversal del proyecto, correlation ID y manejo de errores existente. El controller y los DTOs compilados son la autoridad ejecutable; este documento no inventa códigos ni payloads no materializados.

## Seguridad

- Controller bajo `[Authorize]`.
- `Finanzas/Ver` para lectura.
- `Finanzas/Crear` para registrar obligación desde factura.
- `Finanzas/Editar` para registrar/revertir movimientos.
- Sin bypass anónimo en la superficie N2.8.
