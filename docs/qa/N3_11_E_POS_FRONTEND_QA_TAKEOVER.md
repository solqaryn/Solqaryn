# N3.11.E — POS — Frontend y UX — QA Takeover

## Dictamen

`N3.11.E` no requiere una segunda pantalla POS independiente en el estado contractual actual. La superficie autorizada de venta rápida ya está materializada dentro del feature existente `ventas` y cualquier POS separado obligaría a fijar decisiones que permanecen explícitamente `DECISION_PENDING`.

## Evidencia confirmada

- La ruta `ventas/nueva` está protegida por `authGuard` + `permisoGuard` con `Ventas/Crear` y carga `VentaFormComponent`.
- `VentaFormComponent` ya soporta búsqueda remota de producto por nombre/marca/modelo/color/SKU/código de barras.
- El formulario ya soporta lectura de SKU/código de barras mediante `CodigoScannerInputComponent` y resuelve el producto usando el servicio vigente de Venta.
- Al escanear/buscar una variante existente, la cantidad se consolida y se valida contra `cantidadDisponible`.
- La misma superficie ya soporta cliente final o cliente existente, método de pago, estado de pago, cálculo real de descuentos/impuestos/envío, estados de carga/error y guardado de borrador mediante el contrato de Venta actual.
- La UI no necesita crear un nuevo modelo POS para reutilizar Venta/Facturación/Reserva/Devolución ya autorizados.

## Límites contractuales preservados

Este cierre NO introduce ni autoriza:

- sesión/cajero/terminal POS;
- split o multi-tender atómico;
- cálculo/orquestación de cambio de efectivo;
- suspensión/reanudación de tickets;
- política de impresión/reimpresión de recibos;
- idempotencia específica POS;
- RBAC específico POS;
- nuevas reglas fiscales, de inventario o comerciales.

Esas decisiones deben permanecer fuera de N3.11.E mientras no exista autoridad explícita.

## Evaluación UX/A11y del baseline existente

La superficie actual ya contiene estados de loading, mensajes de error, `role="alert"`/`role="status"` donde aplica, etiquetas de formulario, `aria-label` en acciones iconográficas, feedback de búsqueda/escaneo y navegación protegida. Cualquier mejora posterior debe preservar estas garantías y no puede ampliar el contrato funcional sin decisión previa.

## Cierre

Resultado: `N3.11.E = N/A_PRODUCT_DELTA / QA_TAKEOVER_CERTIFIED`.

Motivo: el objetivo seguro de venta rápida ya está cubierto por `ventas/nueva`; una pantalla POS separada no es técnicamente justificable sin inventar decisiones contractuales pendientes. No se requiere cambio de producto para cerrar este parent.

P0 conocido: 0.
P1 conocido: 0 dentro del alcance autorizado de N3.11.E.

Los artifacts Jules del parent se consideran soporte/review adicional. Ningún `COMPLETED` Jules promueve el parent automáticamente y ningún patch se integra sin REVIEW-FIRST.
