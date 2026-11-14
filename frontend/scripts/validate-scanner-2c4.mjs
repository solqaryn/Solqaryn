import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = process.cwd();
const read = (path) => readFileSync(resolve(root, path), 'utf8');
const assert = (condition, message) => {
  if (!condition) {
    console.error(`Fase 2C.4: ${message}`);
    process.exit(1);
  }
};

const packageJson = JSON.parse(read('package.json'));
const dialog = read('src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.ts');
const input = read('src/app/shared/codigo-scanner-input/codigo-scanner-input.component.ts');
const venta = read('src/app/features/ventas/venta-form.component.ts');
const compra = read('src/app/features/compras/compra-form.component.ts');
const ventaHtml = read('src/app/features/ventas/venta-form.component.html');
const compraHtml = read('src/app/features/compras/compra-form.component.html');
const ventaService = read('src/app/services/venta.service.ts');
const compraService = read('src/app/services/compra.service.ts');
const vercel = read('vercel.json');

assert(packageJson.dependencies?.['html5-qrcode'] === '2.3.8', 'html5-qrcode debe quedar fijado en 2.3.8.');
assert(dialog.includes("await import('html5-qrcode')"), 'la cámara debe cargarse de forma diferida.');
assert(dialog.includes('.scanFile('), 'debe existir lectura local desde imagen.');
assert(dialog.includes('.stop()') && dialog.includes('.clear()'), 'el stream y el lector deben liberarse.');
assert(dialog.includes('10 * 1024 * 1024'), 'debe existir límite de imagen de 10 MB.');
assert(dialog.includes('16_000_000') && dialog.includes('4096'), 'deben existir límites de dimensiones y megapíxeles.');
assert(input.includes('CodigoScannerDialogComponent'), 'el lector físico debe integrar el diálogo de cámara.');
assert(!input.includes('window.addEventListener'), 'el lector no debe capturar eventos globales ni robar el foco.');
assert(venta.includes('cantidad consolidada') && compra.includes('cantidad consolidada'), 'los escaneos repetidos deben consolidarse.');
assert(ventaHtml.includes('app-codigo-scanner-input') && compraHtml.includes('app-codigo-scanner-input'), 'ventas y compras deben mostrar el escáner.');
assert(ventaService.includes('/productos/por-codigo') && compraService.includes('/productos/por-codigo'), 'ambos formularios deben usar resolución exacta.');
assert(vercel.includes('camera=(self), microphone=(), geolocation=()'), 'Vercel debe restringir cámara al propio origen.');

console.log('Fase 2C.4: validación estática del frontend del escáner aprobada.');
