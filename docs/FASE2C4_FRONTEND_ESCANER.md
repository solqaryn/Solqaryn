# Fase 2C.4 — Frontend del escáner

## Alcance implementado

- Captura compartida para lectores físicos USB y Bluetooth.
- Consulta exacta de SKU o código de barras mediante los endpoints de ventas y compras de la Fase 2C.3.
- Consolidación de lecturas repetidas por `ProductoId + ProductoVarianteId`.
- Validación de stock disponible en ventas.
- Recepción de productos con stock previo igual a cero en compras.
- Conservación del costo únicamente en el flujo de compras.
- Restablecimiento del foco después de cada lectura.
- Mensajes accesibles de éxito y error.
- Escáner por cámara con carga diferida de `html5-qrcode`.
- Lectura desde imágenes locales JPG, JPEG, PNG y WEBP sin enviarlas al servidor.
- Liberación del stream al cerrar el diálogo o destruir el componente.
- Formatos habilitados: QR, EAN-13, EAN-8, UPC-A, UPC-E, Code 128 y Code 39.
- Política de cámara configurada para el frontend.

## Validaciones

- `npm run lint` incluye el contrato automatizado `validate-scanner-2c4.mjs`.
- `npm run build:prod` debe finalizar sin errores.
- Playwright incluye `fase2c4-escaner-frontend.spec.ts` para ventas y compras.

## Gobernanza

- Rama exclusiva: `Desarrollo`.
- `main` permanece congelada.
- PR #2 permanece abierto y en borrador.
- No se ejecutaron cambios sobre Producción.
