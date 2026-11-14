import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');
const read = name => readFile(path.join(featureDir, name), 'utf8');

const [productoHtml, productoTs, productoScss, carritoHtml, carritoScss, checkoutHtml, checkoutScss] = await Promise.all([
  read('storefront-producto.component.html'),
  read('storefront-producto.component.ts'),
  read('storefront-producto.component.scss'),
  read('storefront-carrito.component.html'),
  read('storefront-carrito.component.scss'),
  read('storefront-checkout.component.html'),
  read('storefront-checkout.component.scss')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

expect(productoHtml.includes('(pointerdown)="iniciarSwipe($event)"') && productoHtml.includes('(pointerup)="finalizarSwipe($event)"'),
  'La galería y fullscreen deben conservar swipe por Pointer Events.');
expect(productoScss.includes('touch-action:pan-y'), 'El swipe no debe bloquear el scroll vertical de la página.');
expect(productoScss.includes('overflow-x:clip'), 'El detalle debe impedir overflow horizontal accidental.');
expect(productoHtml.includes('<dialog #lightbox'), 'El fullscreen debe usar dialog nativo.');
expect(productoHtml.includes('(close)="alCerrarLightbox()"'), 'El fullscreen debe restaurar el foco al cerrar.');
expect(productoTs.includes('this.botonImagenPrincipal?.nativeElement.focus()'), 'El cierre del fullscreen debe devolver foco al disparador.');
expect(productoScss.includes('env(safe-area-inset-bottom)'), 'El CTA móvil debe respetar safe areas.');
expect(/\.store-footer\{[^}]*padding-bottom:calc\(82px \+ env\(safe-area-inset-bottom\)\)/.test(productoScss),
  'El CTA fijo no debe tapar el pie de página.');
expect(productoHtml.includes('class="feedback-toast"') && productoHtml.includes('aria-live="polite"'),
  'Agregar al carrito debe producir feedback visible y anunciado.');
expect(productoTs.includes('unidad agregada') && productoTs.includes('unidades agregadas'),
  'El feedback del carrito debe indicar la cantidad agregada.');

for (const target of ['.button', '.icon-button', '.quantity-control button']) {
  expect(productoScss.includes(target) || carritoScss.includes(target) || checkoutScss.includes(target),
    `Debe existir estilo táctil para ${target}.`);
}
expect(productoScss.includes('width:44px;height:44px') || productoScss.includes('min-height:44px'),
  'Los controles del detalle deben mantener targets táctiles de al menos 44px.');
expect(carritoScss.includes('min-height:44px'), 'Los controles del carrito deben mantener targets táctiles de al menos 44px.');
expect(checkoutScss.includes('min-height: 44px'), 'Los campos/botones del checkout deben mantener targets táctiles de al menos 44px.');

for (const campo of ['nombre', 'telefono', 'correo', 'notas']) {
  expect(checkoutHtml.includes(`mostrarError('${campo}') ? 'true' : null`),
    `El campo ${campo} debe exponer aria-invalid cuando falla.`);
  expect(checkoutHtml.includes(`id="${campo}-error"`), `El error de ${campo} debe tener un id asociable.`);
}
expect(checkoutHtml.includes('inputmode="tel"'), 'Teléfono debe optimizar el teclado móvil.');
expect(checkoutHtml.includes('inputmode="email"'), 'Correo debe optimizar el teclado móvil.');
expect(checkoutHtml.includes('[attr.aria-busy]="procesando()"'), 'Las acciones asíncronas de pago deben anunciar estado busy.');
expect(checkoutHtml.includes("procesando() ? 'Abriendo pago seguro…'"), 'El CTA de pago debe mostrar feedback loading.');

for (const html of [productoHtml, carritoHtml, checkoutHtml]) {
  const imagenes = [...html.matchAll(/<img\\b[^>]*>/gi)].map(match => match[0]);
  expect(imagenes.every(tag => /(?:\\balt|\\[alt\\])\\s*=/.test(tag)), 'Toda imagen HTML del flujo público debe tener alt o [alt].');
}
for (const scss of [productoScss, carritoScss, checkoutScss]) {
  expect(scss.includes(':focus-visible'), 'Cada pantalla crítica debe mantener foco visible de teclado.');
}

if (failures.length) {
  console.error('Fase 10 — validación responsive, UX y accesibilidad FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 10 — responsive, UX y accesibilidad: swipe, fullscreen, feedback, touch targets, safe areas, foco, labels y errores aprobados.');
