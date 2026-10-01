import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const repoDir = path.resolve(frontendDir, '..');
const featureDir = path.join(frontendDir, 'src/app/features/storefront');
const readFeature = name => readFile(path.join(featureDir, name), 'utf8');

const [
  routes,
  paths,
  config,
  cartTs,
  cartHtml,
  checkoutTs,
  checkoutHtml,
  checkoutScss,
  checkoutRules,
  pedidoTs,
  pedidoHtml,
  pedidoScss,
  pedidoService,
  storefrontService,
  models,
  empresaIdentity,
  empresaConfigService,
  backendController,
  whatsappController,
  backendDto
] = await Promise.all([
  readFile(path.join(frontendDir, 'src/app/app.routes.ts'), 'utf8'),
  readFeature('storefront.paths.ts'),
  readFeature('storefront.config.ts'),
  readFeature('storefront-carrito.component.ts'),
  readFeature('storefront-carrito.component.html'),
  readFeature('storefront-checkout.component.ts'),
  readFeature('storefront-checkout.component.html'),
  readFeature('storefront-checkout.component.scss'),
  readFeature('storefront-checkout.rules.ts'),
  readFeature('storefront-pedido.component.ts'),
  readFeature('storefront-pedido.component.html'),
  readFeature('storefront-pedido.component.scss'),
  readFeature('storefront-pedido.service.ts'),
  readFeature('storefront.service.ts'),
  readFeature('storefront.models.ts'),
  readFeature('storefront-identidad.service.ts'),
  readFile(path.join(frontendDir, 'src/app/services/empresa-configuracion.service.ts'), 'utf8'),
  readFile(path.join(repoDir, 'backend/src/API/Controllers/TiendaController.cs'), 'utf8'),
  readFile(path.join(repoDir, 'backend/src/API/Controllers/WhatsAppController.cs'), 'utf8'),
  readFile(path.join(repoDir, 'backend/src/Application/DTOs/TiendaCheckoutDto.cs'), 'utf8')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };
const routeLine = pathValue => routes.split('\n').find(line => line.includes(`path: '${pathValue}'`)) || '';

const checkoutRoute = routeLine('tienda/checkout');
const pedidoRoute = routeLine('tienda/pedido/:id');
expect(checkoutRoute.includes('StorefrontCheckoutComponent'), 'Debe existir la página pública /tienda/checkout.');
expect(pedidoRoute.includes('StorefrontPedidoComponent'), 'Debe existir la página pública /tienda/pedido/:id.');
expect(!checkoutRoute.includes('authGuard') && !checkoutRoute.includes('permisoGuard'), 'Checkout no debe exigir autenticación administrativa.');
expect(!pedidoRoute.includes('authGuard') && !pedidoRoute.includes('permisoGuard'), 'Confirmación pública no debe exigir autenticación administrativa.');
expect(paths.includes("checkout: '/tienda/checkout'"), 'Las rutas canónicas deben declarar checkout.');
expect(paths.includes("pedido: (id: string | number)"), 'Las rutas canónicas deben construir la referencia de pedido.');
expect(cartHtml.includes('[href]="enlaceCheckout()"') && cartHtml.includes('Continuar al checkout'), 'El carrito no vacío debe conectar con checkout.');
expect(cartTs.includes("`${STOREFRONT_PATHS.checkout}?fuente=bd`"), 'La vista previa de base de datos debe conservar la fuente al pasar del carrito al checkout.');
expect(checkoutTs.includes("this.route.snapshot.queryParamMap.get('fuente') === 'bd'"), 'Checkout debe recuperar la fuente de vista previa solo de forma explícita.');
expect(checkoutTs.includes('!environment.production && this.config.mostrarControlesVistaPrevia'), 'La selección por query debe quedar limitada al modo de dev.');

expect(config.includes("export type ModoCarrito = 'whatsapp' | 'tarjeta' | 'ambos'"), 'La configuración debe conservar los tres modos comerciales.');
expect(config.includes('endpointCheckoutTarjeta: null'), 'Tarjeta debe permanecer fail-closed por defecto.');
expect(config.includes('origenesCheckoutPermitidos: []'), 'La allowlist de pago debe estar vacía por defecto.');
expect(empresaConfigService.includes('getWhatsAppPublico()'), 'La identidad pública debe poder consultar el número operativo de WhatsApp sin secretos.');
expect(empresaConfigService.includes('/whatsapp/publico'), 'El fallback público de WhatsApp debe usar un endpoint dedicado no administrativo.');
expect(empresaIdentity.includes('this.empresaService.getWhatsAppPublico()'), 'La identidad debe resolver WhatsApp Business cuando el contacto legacy esté vacío.');
expect(empresaIdentity.includes('if (config.whatsApp?.trim()) return of(config);'), 'Un WhatsApp público explícito debe conservar prioridad sin consultas innecesarias.');
expect(whatsappController.includes('[HttpGet("publico")]') && whatsappController.includes('[AllowAnonymous]'), 'WhatsApp debe exponer únicamente un contacto público explícito para el storefront.');
expect(
  whatsappController.includes('WhatsAppPublicoResponse')
    && whatsappController.includes('numeros.Count == 1')
    && whatsappController.includes('numeros.Count > 1')
    && whatsappController.includes('coincidencias.Count == 1')
    && whatsappController.includes('WhatsApp público no expuesto'),
  'El endpoint público debe resolver un único número inequívoco y fallar cerrado ante múltiples tenants activos.'
);
expect(!/TokenSecretoReferencia|WebhookSecretoReferencia/.test((whatsappController.match(/GetPublicoAsync[\s\S]*?\n    }/m)?.[0] || '')), 'El endpoint público de WhatsApp no debe leer ni exponer referencias secretas.');

for (const required of [
  'this.servicio.validarCheckout(this.referenciasCheckout())',
  'this.carrito.hidratar(productos',
  'Validators.required',
  'Validators.email',
  'Validators.maxLength(600)',
  "this.config.modoCarrito !== 'tarjeta'",
  "this.config.modoCarrito !== 'whatsapp'",
  'urlCheckoutPermitida(respuesta.checkoutUrl, this.config.origenesCheckoutPermitidos)',
  "this.guardarRecibo(validado, this.utilizarDatosBaseDatos() ? 'whatsapp-preparado' : 'demo')",
  'this.document.defaultView?.location.assign(segura)',
  'const productoVarianteId = item.productoVarianteId ?? null',
  'productoVarianteId,',
  'modeloId: productoVarianteId === null ? item.modeloId : null',
  'const agrupacion = productoVarianteId === null',
  'modeloNombre: agrupacion.modeloNombre',
  'marcaNombre: agrupacion.marcaNombre',
  'confirmarSalidaWhatsapp(evento: Event)',
  'evento.preventDefault()',
  'if (!this.validacionVigente())'
]) {
  expect(checkoutTs.includes(required), `Checkout debe contener la salvaguarda: ${required}.`);
}
expect(!checkoutTs.includes('localStorage'), 'Checkout no debe guardar datos del comprador en localStorage.');
expect(checkoutTs.includes("this.identidad.config().nombreComercial || 'Tienda'"), 'El cierre por WhatsApp debe usar la marca comercial pública.');
expect(!checkoutTs.includes('mensajeWhatsappCheckout(\n      this.identidad.nombreSistema()'), 'Checkout no debe filtrar el nombre interno del sistema al mensaje de WhatsApp.');
expect(!checkoutTs.includes('numeroTarjeta') && !checkoutTs.includes('cvv') && !checkoutTs.includes('pinTarjeta'), 'Checkout no debe capturar credenciales de tarjeta.');
expect(checkoutTs.includes("if (!endpoint || !this.tarjetaConfigurada())"), 'Tarjeta debe bloquearse si falta endpoint/origen seguro.');
expect(checkoutHtml.includes('confirmarSalidaWhatsapp($event)'), 'El enlace WhatsApp debe poder cancelar la navegación si la validación venció.');

for (const required of [
  'Confirma tus datos y tu forma de compra',
  'Nombre completo',
  'Teléfono',
  'Correo electrónico',
  'Notas para el comercio',
  'Total validado',
  'Preparar pedido por WhatsApp',
  'Tarjeta no disponible',
  'Nunca ingreses número de tarjeta, CVV o PIN'
]) {
  expect(checkoutHtml.includes(required), `La UI de checkout debe incluir: ${required}.`);
}
expect(!checkoutHtml.includes('Fase 6'), 'La UI pública no debe exponer lenguaje interno del roadmap.');
expect(!/formControlName\s*=\s*["'](?:tarjeta|numeroTarjeta|cardNumber|cvv|cvc|pin)["']/i.test(checkoutHtml), 'No debe existir ningún campo de captura de tarjeta.');
expect(!/type\s*=\s*["']password["']/i.test(checkoutHtml), 'Checkout no debe capturar secretos de pago.');

expect(checkoutRules.includes("destino.protocol === 'https:'"), 'La redirección de tarjeta debe exigir HTTPS.');
expect(checkoutRules.includes('permitidos.has(destino.origin)'), 'La redirección de tarjeta debe exigir origen allowlisted.');
expect(checkoutRules.includes('encodeURIComponent') === false, 'Las reglas puras deben devolver contenido, no abrir URLs ni manipular el navegador.');

expect(storefrontService.includes("private readonly urlValidarCheckout = `${this.urlTienda}/checkout/validar`"), 'El cliente HTTP debe apuntar a /tienda/checkout/validar.');
expect(storefrontService.includes('this.http.post<ApiResponse<CheckoutValidado>>'), 'La revalidación debe usar POST tipado.');
expect(storefrontService.includes("!ruta.startsWith('tienda/')") && storefrontService.includes("ruta.includes('..')"), 'El endpoint configurable de tarjeta debe permanecer dentro de la frontera publica /tienda y aceptar solo rutas relativas seguras.');
expect(storefrontService.includes('checkoutValidado(data)'), 'El cliente debe validar estructuralmente la respuesta de checkout.');

const checkoutItemBlock = models.match(/export interface CheckoutItemRequest \{([\s\S]*?)\n\}/)?.[1] || '';
expect(
  checkoutItemBlock.includes('productoId')
    && checkoutItemBlock.includes('modeloId')
    && checkoutItemBlock.includes('modeloNombre')
    && checkoutItemBlock.includes('marcaNombre')
    && checkoutItemBlock.includes('unidades'),
  'El request frontend debe identificar exactamente producto/agrupación/cantidad.'
);
expect(!/precio|total|stock/i.test(checkoutItemBlock), 'El request frontend no debe enviar precio, total ni stock como autoridad.');

const backendItemBlock = backendDto.match(/public sealed class CheckoutTiendaItemRequestDto\s*\{([\s\S]*?)\n\}/)?.[1] || '';
expect(
  backendItemBlock.includes('ProductoId')
    && backendItemBlock.includes('ModeloId')
    && backendItemBlock.includes('ModeloNombre')
    && backendItemBlock.includes('MarcaNombre')
    && backendItemBlock.includes('Unidades'),
  'El DTO backend debe aceptar identidad de agrupación y cantidad, nunca importes.'
);
expect(!/Precio|Total|Stock/i.test(backendItemBlock), 'El DTO backend no debe aceptar precio, total ni stock del cliente.');
for (const required of [
  '[AllowAnonymous]',
  '[HttpPost("checkout/validar")]',
  '_productoService.GetByIdAsync(solicitud.ProductoId)',
  'producto.Variantes.Where(variante => variante.Activo)',
  'variante.Id == solicitud.ProductoVarianteId.Value',
  'variante.ModeloId == solicitud.ModeloId',
  'string.Equals(variante.ModeloNombre ?? string.Empty, solicitud.ModeloNombre ?? string.Empty, StringComparison.Ordinal)',
  'string.Equals(variante.MarcaNombre ?? string.Empty, solicitud.MarcaNombre ?? string.Empty, StringComparison.Ordinal)',
  'if (stock <= 0 || stock < solicitud.Unidades)',
  'var precioVigente = oferta?.PrecioOferta ?? precio;',
  'Total = precioVigente * solicitud.Unidades',
  'var subtotal = lineas.Sum(linea => linea.Total)',
  'Guid.NewGuid().ToString("N")',
  'TimeSpan.FromMinutes(10)'
]) {
  expect(backendController.includes(required), `Backend debe conservar la validación autoritativa: ${required}.`);
}
expect(!backendController.includes('IPedidoVentaService') && !backendController.includes('ICotizacionService'), 'La validación pública no debe saltarse permisos reutilizando servicios administrativos de pedido/cotización.');

expect(pedidoService.includes('sessionStorage.setItem'), 'El recibo UX debe persistirse solo en sessionStorage.');
expect(!pedidoService.includes('localStorage'), 'El recibo UX no debe usar localStorage.');
expect(!/nombre|telefono|correo|notas/i.test(pedidoService), 'El recibo persistido no debe incorporar PII del comprador.');
expect(pedidoService.includes('MAX_VIGENCIA_MS'), 'El recibo debe tener caducidad limitada.');
expect(pedidoHtml.includes('no contiene ni almacena tus datos de contacto ni información de tarjetas'), 'La confirmación debe explicar el límite de datos persistidos.');
expect(pedidoHtml.includes('No encontramos una confirmación vigente en esta sesión'), 'La ruta de pedido debe manejar referencias ausentes/expiradas sin inventar estado.');
expect(!pedidoTs.includes('localStorage') && !pedidoTs.includes('sessionStorage'), 'El componente de pedido debe delegar persistencia al servicio dedicado.');

for (const [name, scss] of [['checkout', checkoutScss], ['pedido', pedidoScss]]) {
  expect(scss.includes('var(--color-bg)') && scss.includes('var(--color-surface)') && scss.includes('var(--color-primary)'), `${name} debe consumir tokens de tema de empresa.`);
  expect(!/#[0-9a-f]{3,8}\b/i.test(scss), `${name} no debe introducir hex propios.`);
  expect(!/\brgb(?:a)?\s*\(/i.test(scss), `${name} no debe introducir RGB propios.`);
  expect(!/\bhsl(?:a)?\s*\(/i.test(scss), `${name} no debe introducir HSL propios.`);
  expect(/min-height\s*:\s*44px/.test(scss), `${name} debe conservar touch targets de al menos 44 px.`);
  expect(scss.includes('@media'), `${name} debe tener tratamiento responsive explícito.`);
}

if (failures.length) {
  console.error('Fase 6 — validación de checkout/pedido FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

console.info('Fase 6 — checkout/pedido: rutas públicas, revalidación server-side, selección inequívoca, vencimiento seguro, WhatsApp, tarjeta fail-closed, recibo efímero, tema y privacidad aprobados.');
