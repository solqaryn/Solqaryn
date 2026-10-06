# Fase 2C.4 — Frontend del escáner

## Alcance implementado

- Integración en los formularios de ventas y compras.
- Entrada dedicada para lectores USB y Bluetooth que emulan teclado y finalizan con Enter.
- Reenfoque controlado después de cada lectura, sin listeners globales ni apropiación del foco de otros campos.
- Resolución exacta mediante los endpoints certificados en la Fase 2C.3.
- Consolidación de líneas repetidas por `ProductoId + ProductoVarianteId`.
- Validación de existencias en ventas; las compras admiten variantes con stock cero.
- Cámara trasera mediante `html5-qrcode` 2.3.8, cargado dinámicamente.
- Lectura local de imágenes JPG, JPEG, PNG y WEBP sin transferir el archivo al servidor.
- Límites locales de archivo: 10 MB, 4096 px por lado y 16 megapíxeles.
- Formatos admitidos: QR, EAN-13, EAN-8, UPC-A, UPC-E, Code 128 y Code 39.
- Liberación del stream al detener, cerrar, obtener resultado o destruir el componente.
- Cabecera de Vercel `Permissions-Policy: camera=(self), microphone=(), geolocation=()`.
- Validación estática específica integrada en `npm run lint`.

## Validaciones automatizadas ejecutadas

- `npm ci`.
- TypeScript sin emisión.
- Validación de calidad del frontend.
- Contrato estático de Fase 2C.4.
- Compilación Angular de producción.
- Verificación de carga diferida de `html5-qrcode` mediante chunk independiente.

## Pruebas físicas pendientes del propietario

Estas comprobaciones requieren hardware real y no se consideran ejecutadas por CI:

1. Cámara trasera en Android sobre HTTPS.
2. Cámara trasera en iPhone/Safari sobre HTTPS.
3. Denegación y posterior concesión del permiso de cámara.
4. Lectura de imagen válida y rechazo de formatos, tamaños o dimensiones inválidas.
5. Pistola USB configurada con sufijo Enter.
6. Lector Bluetooth configurado con sufijo Enter.
7. Escaneos rápidos repetidos y recuperación del foco.
8. Confirmación visual de que interactuar con otros campos no devuelve el foco al lector.

## Gobernanza

- Rama exclusiva: `Desarrollo`.
- PR oficial: #2, `Desarrollo -> main`, abierto y en borrador.
- `main` no se modifica.
- Producción permanece congelada y no se utiliza para estas validaciones.
