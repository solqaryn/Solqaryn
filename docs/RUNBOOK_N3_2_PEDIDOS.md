# Runbook N3.2 — PedidoVenta

## Propósito

Guía operativa para verificar, recuperar y, cuando sea imprescindible, revertir la persistencia/API de PedidoVenta sin tocar Producción desde VAEP.

## Prechecks

1. Confirmar entorno y base objetivo; esta documentación no autoriza operaciones en Producción.
2. Confirmar que la migración `20260824080000_N3_2_PedidoVentaPersistencia` está en el historial esperado.
3. Verificar existencia y estructura de `Cotizaciones`, `Clientes`, `Productos` y `ProductoVariantes` antes de aplicar la migración.
4. Ejecutar el preflight/postcheck N3.2 disponible en el repositorio y abortar ante cualquier violación.
5. Mantener PR #2 Draft y trabajar exclusivamente en `Desarrollo`.

## Validación posterior

Después de la migración, comprobar como mínimo:

- `PedidosVenta` y `PedidoVentaDetalles` existen;
- las cinco FKs esperadas existen;
- `CotizacionId` mantiene unicidad cuando no es null;
- `IdempotencyKey` mantiene unicidad;
- fingerprint y estado satisfacen sus checks;
- no existen cantidades <= 0 ni precios unitarios negativos;
- build, unitarias, integración MySQL, frontend y gates causales permanecen verdes.

## Operación funcional

- Crear Pedido: `POST /pedidos-venta` con `Idempotency-Key` y una Cotización elegible.
- Consultar: `GET /pedidos-venta` / `GET /pedidos-venta/{id}`.
- Editar: sólo mientras el Pedido permanezca en Borrador.
- Confirmar: transición Borrador -> Confirmado.
- Anular: transición Confirmado -> Anulado con motivo.

Los permisos son `Ventas:Ver/Crear/Editar/Confirmar/Anular` según endpoint.

## Rollback

La migración implementa un `DownGuard`: antes de eliminar `PedidoVentaDetalles` y `PedidosVenta`, suma los registros existentes y exige cero. Por tanto:

- con datos N3.2 existentes, el rollback debe fallar cerrado;
- no borrar/truncar datos para “hacer pasar” el DownGuard;
- si un entorno autorizado requiere reversión con datos, primero debe existir una estrategia explícita de preservación/restauración de esos datos y una ventana operativa aprobada fuera de esta automatización;
- este runbook no afirma una política universal de backup ni autoriza rollback productivo.

## Recuperación ante fallo

- Error de migración: inspeccionar el SQL/job causal, corregir sólo N3.2 y repetir en entorno controlado.
- Error idempotente: verificar key/fingerprint y que el replay corresponda al mismo payload; payload diferente debe fallar cerrado.
- Error RBAC: verificar autenticación y permiso exacto; no introducir bypass administrativo.
- Error frontend: aislar contrato HTTP/RBAC antes de modificar backend ya certificado.

## Condición de salida

Un incidente N3.2 sólo se considera resuelto cuando la causa está corregida, las validaciones proporcionales están verdes y no quedan P0/P1 atribuibles al punto.
