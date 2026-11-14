# Regla canónica de integridad de precios

Los precios base de catálogo son **server-authoritative**. Ninguna interfaz de ventas,
cotizaciones o pedidos puede ofrecer edición arbitraria del precio unitario.

## Aplicación

- La UI muestra el precio como dato de catálogo de solo lectura.
- El cliente envía producto, variante y cantidad; el precio enviado por compatibilidad no
  es una fuente de verdad.
- El backend resuelve `ProductoVariante.Precio` y usa `Producto.Precio` sólo como respaldo
  del catálogo cuando la variante no tiene precio.
- Un precio vigente no configurado se rechaza; nunca se acepta un precio suministrado por
  el navegador para completar el dato.
- La venta persiste `VentaDetalle.PrecioUnitario` como snapshot histórico. Cambios futuros
  del catálogo no reescriben ventas existentes.
- Descuentos, promociones, impuestos y envío son mecanismos separados del precio base.

## Excepciones

`PRICE_OVERRIDE` permanece denegado. Una futura excepción deberá tener capability RBAC,
motivo obligatorio, precio original y nuevo, diferencia, usuario, timestamp, venta,
auditoría y límites explícitos.

## Cobertura mínima

La prueba de aplicación verifica que una solicitud con `precioUnitario=1` para una variante
de L. 200 calcula L. 200, y que un producto sin precio vigente se rechaza. La UI de ventas
y cotizaciones usa controles `readonly`; el gate responsive también comprueba que el campo
de ventas conserve ese contrato.
