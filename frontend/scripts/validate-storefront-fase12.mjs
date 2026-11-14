import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptsDir = path.dirname(fileURLToPath(import.meta.url));
const frontendDir = path.resolve(scriptsDir, '..');
const repoDir = path.resolve(frontendDir, '..');
const readFrontend = relative => readFile(path.join(frontendDir, relative), 'utf8');
const readRepo = relative => readFile(path.join(repoDir, relative), 'utf8');

const [
  routes,
  paths,
  accountService,
  accountComponent,
  accountHtml,
  productTs,
  productHtml,
  checkoutTs,
  headerHtml,
  robotsJs,
  sitemapJs,
  backendAccount,
  backendPublicStore,
  backendDtos,
  backendEntities,
  backendConfig,
  migration,
  modelSnapshot
] = await Promise.all([
  readFrontend('src/app/app.routes.ts'),
  readFrontend('src/app/features/storefront/storefront.paths.ts'),
  readFrontend('src/app/features/storefront/storefront-cuenta.service.ts'),
  readFrontend('src/app/features/storefront/storefront-cuenta.component.ts'),
  readFrontend('src/app/features/storefront/storefront-cuenta.component.html'),
  readFrontend('src/app/features/storefront/storefront-producto.component.ts'),
  readFrontend('src/app/features/storefront/storefront-producto.component.html'),
  readFrontend('src/app/features/storefront/storefront-checkout.component.ts'),
  readFrontend('src/app/features/storefront/storefront-header.component.html'),
  readFrontend('api/robots.js'),
  readFrontend('api/sitemap.js'),
  readRepo('backend/src/API/Controllers/TiendaCuentaController.cs'),
  readRepo('backend/src/API/Controllers/TiendaController.cs'),
  readRepo('backend/src/Application/DTOs/TiendaCuentaDto.cs'),
  readRepo('backend/src/Domain/Entities/TiendaCuentaCliente.cs'),
  readRepo('backend/src/Infrastructure/Persistence/Configurations/TiendaCuentaClienteConfiguration.cs'),
  readRepo('backend/src/Infrastructure/Migrations/20260919011500_StorefrontFase12CuentaCliente.cs'),
  readRepo('backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs')
]);

const failures = [];
const expect = (condition, message) => { if (!condition) failures.push(message); };

const checkoutRoute = routes.split('\n').find(line => line.includes("path: 'tienda/checkout'")) || '';
const accountRoute = routes.split('\n').find(line => line.includes("path: 'tienda/cuenta'")) || '';
expect(Boolean(checkoutRoute), 'Checkout público debe conservar su ruta.');
expect(Boolean(accountRoute), 'Cuenta de cliente debe tener ruta propia.');
expect(!checkoutRoute.includes('authGuard') && !checkoutRoute.includes('permisoGuard'), 'Checkout invitado no debe depender del login administrativo.');
expect(!accountRoute.includes('authGuard') && !accountRoute.includes('permisoGuard'), 'La cuenta pública no debe reutilizar guards administrativos.');
expect(paths.includes("cuenta: '/tienda/cuenta'"), 'Las rutas canónicas deben exponer Mi cuenta.');
expect(backendPublicStore.includes('[AllowAnonymous]'), 'La tienda pública debe seguir permitiendo compra invitada.');

expect(accountService.includes('sessionStorage'), 'La sesión del cliente debe persistirse solo durante la sesión del navegador.');
expect(!accountService.includes('localStorage'), 'La sesión de cliente no debe persistirse en localStorage.');
expect(accountService.includes("'X-Storefront-Session'"), 'La cuenta debe usar un encabezado de sesión separado.');
expect(!accountService.includes('Authorization'), 'La cuenta cliente no debe reutilizar el Bearer/JWT administrativo.');
expect(!accountComponent.includes('AuthService'), 'La UI de cuenta no debe depender del servicio de autenticación administrativo.');
expect(headerHtml.includes('/tienda/cuenta'), 'El header debe exponer acceso opcional a Mi cuenta.');

expect(backendAccount.includes('[AllowAnonymous]'), 'El controlador de cuenta pública debe permanecer separado de [Authorize] administrativo.');
expect(backendAccount.includes('[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]'), 'Perfil, direcciones e historial de cuenta deben responder no-store para proteger datos personales.');
expect(backendAccount.includes('[EnableRateLimiting("AuthLogin")]'), 'Registro/login de cliente deben tener rate limit.');
expect(backendAccount.includes('BCrypt.Net.BCrypt.HashPassword'), 'La contraseña de cliente debe almacenarse con BCrypt.');
expect(backendAccount.includes('limpio.Length > 150'), 'El correo de cuenta debe respetar el máximo de 150 del Cliente ERP.');
expect(accountComponent.includes('Validators.maxLength(150)'), 'El formulario de cuenta debe validar el mismo máximo de correo que el ERP.');
expect(backendAccount.includes('x.Activo && !x.Eliminado'), 'Favoritos no deben aceptar productos eliminados aunque conserven Activo=true.');
expect(backendAccount.includes('SHA256.HashData'), 'El token de sesión debe persistirse únicamente como hash SHA-256.');
expect(backendAccount.includes('RandomNumberGenerator.GetBytes(32)'), 'La sesión debe usar entropía criptográfica.');
expect(backendAccount.includes('x.ClienteId == cuenta.ClienteId'), 'Historial/detalle deben filtrar por el ClienteId resuelto desde la sesión.');
expect(!backendDtos.includes('ClienteId'), 'Los contratos públicos de cuenta no deben permitir al navegador escoger ClienteId.');
expect(backendAccount.includes('x.Id == id && x.ClienteId == cuenta.ClienteId'), 'Detalle de pedido debe bloquear IDOR.');
expect(backendAccount.includes('CuentaClienteId == cuenta.Id'), 'Direcciones/favoritos deben aislarse por cuenta.');

expect(backendEntities.includes('TiendaCuentaCliente') && backendEntities.includes('TiendaSesionCliente')
  && backendEntities.includes('TiendaDireccionCliente') && backendEntities.includes('TiendaFavoritoCliente'),
  'La persistencia debe separar cuenta, sesión, direcciones y favoritos.');
expect(backendConfig.includes('UX_TiendaCuentasCliente_Correo'), 'El correo de cuenta debe ser único.');
expect(backendConfig.includes('UX_TiendaSesionesCliente_TokenHash'), 'El hash de sesión debe ser único.');
expect(backendConfig.includes('UX_TiendaFavoritosCliente_Cuenta_Producto'), 'Favoritos no deben duplicarse por cuenta/producto.');
expect(migration.includes('TiendaCuentasCliente') && migration.includes('TiendaSesionesCliente')
  && migration.includes('TiendaDireccionesCliente') && migration.includes('TiendaFavoritosCliente'),
  'La migración debe materializar las cuatro tablas de cuenta.');
for (const entidad of ['TiendaCuentaCliente', 'TiendaSesionCliente', 'TiendaDireccionCliente', 'TiendaFavoritoCliente']) {
  expect(modelSnapshot.includes(`modelBuilder.Entity("Solqaryn.Domain.Entities.${entidad}"`),
    `El ModelSnapshot de EF debe incluir ${entidad} para no recrear tablas en migraciones futuras.`);
}

expect(accountHtml.includes('La cuenta es opcional'), 'La UI debe explicar que la cuenta es opcional.');
expect((accountHtml.match(/maxlength="150"/g) || []).length >= 2, 'Login y registro deben reflejar el límite ERP de correo de 150 caracteres.');
expect(accountComponent.includes("item.nombre.trim() === linea.modelo!.trim()"), 'La recompra histórica sin variante debe resolver por snapshot de modelo.');
expect(accountComponent.includes('producto.modelos.length === 1'), 'La recompra sin snapshot solo puede asumir modelo cuando existe una única opción segura.');
expect(accountHtml.includes('Recomprar con stock y precio actuales'), 'La recompra debe declarar que usa stock/precio vigentes.');
expect(productHtml.includes('alternarFavorito()'), 'El detalle de producto debe permitir guardar favoritos.');
expect(productTs.includes('this.cuentaCliente.agregarFavorito'), 'Favoritos deben persistirse en la cuenta, no solo localmente.');

expect(checkoutTs.includes('endpointCheckoutTarjeta') && checkoutTs.includes('tarjetaConfigurada()'), 'La pasarela debe seguir condicionada a configuración real.');
expect(!accountComponent.includes('recomendacion') && !accountComponent.includes('sucursal'), 'Fase 12 no debe inventar personalización o multisucursal sin necesidad operativa.');

expect(robotsJs.includes("'Disallow: /tienda/cuenta'"), 'robots debe bloquear Mi cuenta.');
expect(!sitemapJs.includes('/tienda/cuenta'), 'El sitemap no debe publicar Mi cuenta.');

if (failures.length) {
  console.error('Fase 12 — validación de cuenta/evolución FALLÓ:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exit(1);
}

assert.ok(true);
console.info('Fase 12 — cuenta opcional, aislamiento de pedidos, direcciones, favoritos, recompra y privacidad aprobados.');
