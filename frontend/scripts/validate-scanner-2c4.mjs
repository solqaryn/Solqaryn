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
const angular = JSON.parse(read('angular.json'));
const dialog = read('src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.ts');
const input = read('src/app/shared/codigo-scanner-input/codigo-scanner-input.component.ts');
const venta = read('src/app/features/ventas/venta-form.component.ts');
const compra = read('src/app/features/compras/compra-form.component.ts');
const ventaHtml = read('src/app/features/ventas/venta-form.component.html');
const compraHtml = read('src/app/features/compras/compra-form.component.html');
const ventaService = read('src/app/services/venta.service.ts');
const compraService = read('src/app/services/compra.service.ts');
const vercel = read('vercel.json');

assert(!packageJson.dependencies?.['html5-qrcode'], 'html5-qrcode debe estar completamente retirado.');
assert(packageJson.dependencies?.['barcode-detector'] === '3.2.2', 'barcode-detector debe quedar fijado en 3.2.2.');
assert(packageJson.dependencies?.['zxing-wasm'] === '3.1.3', 'zxing-wasm debe quedar fijado en 3.1.3.');
assert(dialog.includes("await import('barcode-detector/ponyfill')"), 'el detector debe cargarse de forma diferida.');
assert(dialog.includes('prepareZXingModule'), 'ZXing WASM debe inicializarse explícitamente.');
assert(dialog.includes("assets/wasm/zxing_reader.wasm"), 'el WASM debe servirse same-origin desde assets.');
assert(!/jsdelivr|unpkg|esm\.sh/i.test(dialog), 'el escáner no debe depender de CDN runtime.');
assert(dialog.includes('navigator.mediaDevices.getUserMedia'), 'la cámara debe usar MediaDevices.');
assert(dialog.includes('detector.detect(video)') && dialog.includes('detector.detect(archivo)'), 'cámara e imagen deben pasar por el mismo detector.');
assert(dialog.includes("track.stop()"), 'el stream de cámara debe liberar todas las pistas.');
for (const formato of ['qr_code', 'ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39']) {
  assert(dialog.includes(`'${formato}'`), `falta formato de escaneo ${formato}.`);
}

const assets = angular.projects?.['solqaryn-frontend']?.architect?.build?.options?.assets ?? [];
const wasmAsset = assets.find((asset) =>
  typeof asset === 'object'
  && asset.glob === 'zxing_reader.wasm'
  && String(asset.input).includes('node_modules/zxing-wasm/dist/reader')
  && String(asset.output).includes('assets/wasm')
);
assert(Boolean(wasmAsset), 'angular.json debe copiar zxing_reader.wasm al mismo origen.');

assert(dialog.includes('10 * 1024 * 1024'), 'debe existir límite de imagen de 10 MB.');
assert(dialog.includes('16_000_000') && dialog.includes('4096'), 'deben existir límites de dimensiones y megapíxeles.');
assert(input.includes('CodigoScannerDialogComponent'), 'el lector físico debe integrar el diálogo de cámara.');
assert(!input.includes('window.addEventListener'), 'el lector no debe capturar eventos globales ni robar el foco.');
assert(venta.includes('cantidad consolidada') && compra.includes('cantidad consolidada'), 'los escaneos repetidos deben consolidarse.');
assert(ventaHtml.includes('app-codigo-scanner-input') && compraHtml.includes('app-codigo-scanner-input'), 'ventas y compras deben mostrar el escáner.');
assert(ventaService.includes('/productos/por-codigo') && compraService.includes('/productos/por-codigo'), 'ambos formularios deben usar resolución exacta.');
assert(vercel.includes('camera=(self), microphone=(), geolocation=()'), 'Vercel debe restringir cámara al propio origen.');

console.log('Fase 2C.4: barcode-detector + ZXing WASM same-origin, cámara/imagen e integración aprobados.');
