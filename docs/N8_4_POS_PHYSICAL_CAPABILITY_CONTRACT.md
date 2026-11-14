# N8.4 — Contrato mínimo de validación POS físico

Autoridad operativa: `docs/VAEP_AUTHORITY.md`
Rama: `Desarrollo`

## Autoridades reutilizadas

N8.4 no crea una segunda autoridad POS. La venta rápida permanece bajo `Venta` y la UI `ventas/nueva`, conforme a `docs/CERTIFICACION_N3_11_POS.md`. La impresión permanece bajo `Facturación` y sus permisos existentes.

## Capacidades observadas

### Lector de códigos

`VentaFormComponent` integra `CodigoScannerInputComponent` y resuelve SKU/código de barras mediante `buscarProductoPorCodigo`. El componente de captura declara soporte de lector USB/Bluetooth actuando como teclado, procesa `Enter`, limpia el valor y devuelve el foco después de la lectura. También mantiene cámara/imagen como canal separado.

Contrato de validación física: un lector keyboard-wedge autorizado debe introducir el código en el campo activo, emitir `Enter`, resolver exactamente un producto/variante y preservar la consolidación/stock ya implementados. Debe registrarse modelo, conexión, configuración de sufijo, navegador/OS, actor, timestamp, código probado, esperado/real y PASS/FAIL.

### Impresora

`FacturaViewComponent` soporta formatos PDF `pos58` y `pos80`, exige `Facturacion/Imprimir` y entrega el PDF al visor/driver del navegador/SO. N8.4 no autoriza un driver nativo alternativo ni impresión silenciosa.

Contrato de validación física: una impresora térmica autorizada debe recibir POS58 o POS80 desde el flujo existente y producir un ticket legible, sin truncar campos esenciales. Debe registrarse modelo, driver, conexión, papel, navegador/OS, factura de prueba no productiva, esperado/real y PASS/FAIL.

### Cajón de efectivo y otros periféricos

No existe una integración directa certificada de cajón/terminal/periférico en N3.11. N8.4 no puede declarar soporte por inferencia. Si un cajón depende del pulso del driver o de la impresora, la prueba debe demostrarlo físicamente y documentar la cadena real; si requiere protocolo/API/servicio local nuevo, se registra como gap y no se implementa sin contrato explícito.

## Evidencia obligatoria para cierre físico

Por dispositivo: actor/rol autorizado, tenant/empresa de Desarrollo, timestamp UTC, fabricante/modelo, conexión, SO/navegador/driver cuando aplique, pasos, dato de prueba no productivo, resultado esperado, resultado real, PASS/FAIL y referencia de defecto si falla. No se aceptan simulaciones como sustituto de presencia física.

## Rollback

Este contrato es documental. No modifica dominio, esquema, API, RBAC, Producción ni deploy. Cualquier cambio funcional posterior deberá aportar rollback específico y pruebas causales.
