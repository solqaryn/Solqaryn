/**
 * Catálogo exclusivo de pruebas QA. El Router efectivo de Angular verifica
 * igualdad de pantallas con este catálogo en app.routes.centros-costo.spec.ts.
 * Los identificadores de ejemplo no acreditan existencia de registros reales.
 */
export const RESPONSIVE_ROUTES = [
  '/', '/login',
  '/tienda', '/tienda/productos', '/tienda/ofertas', '/tienda/categorias',
  '/tienda/carrito', '/tienda/checkout', '/tienda/cuenta', '/tienda/producto/demo',
  '/tienda/categoria/demo', '/tienda/pedido/1',
  '/dashboard', '/productos', '/productos/nuevo', '/productos/1', '/productos/1/editar', '/productos/1/variantes',
  '/categorias', '/categorias/nueva', '/categorias/1/editar',
  '/sucursales', '/sucursales/nueva', '/sucursales/1/editar',
  '/almacenes', '/almacenes/nuevo', '/almacenes/1/editar',
  '/ubicaciones-almacen', '/ubicaciones-almacen/nueva', '/ubicaciones-almacen/1/editar',
  '/colores', '/tallas', '/marcas', '/modelos', '/metodos-pago',
  '/proveedores', '/proveedores/nuevo', '/proveedores/1/editar',
  '/clientes', '/clientes/nuevo', '/clientes/1/editar',
  '/tipo-clientes', '/tipo-clientes/nuevo', '/tipo-clientes/1/editar',
  '/usuarios', '/usuarios/1', '/usuarios/1/editar',
  '/roles', '/roles/nuevo', '/roles/1/editar', '/permisos',
  '/descuentos', '/descuentos/nuevo', '/descuentos/1/editar',
  '/impuestos', '/impuestos/nuevo', '/impuestos/1/editar',
  '/costos-envio', '/cargas-masivas', '/auditoria', '/configuracion', '/periodos-contables', '/perfil',
  '/solicitudes-compra', '/ordenes-compra', '/ordenes-compra/nueva', '/ordenes-compra/1/editar',
  '/compras', '/compras/nueva', '/compras/1', '/compras/1/editar',
  '/facturas-proveedor', '/facturas-proveedor/nueva', '/facturas-proveedor/1', '/facturas-proveedor/1/editar',
  '/compras/1/three-way-match', '/devoluciones-proveedor', '/devoluciones-proveedor/nueva', '/devoluciones-proveedor/1', '/devoluciones-proveedor/1/editar',
  '/notas-credito-proveedor', '/notas-credito-proveedor/nueva', '/notas-credito-proveedor/1', '/notas-credito-proveedor/1/editar', '/evaluaciones-proveedor',
  '/recepciones-compra', '/recepciones-compra/nueva', '/recepciones-compra/1',
  '/ventas', '/ventas/nueva', '/ventas/1', '/ventas/1/editar',
  '/cotizaciones', '/cotizaciones/nueva', '/cotizaciones/1', '/cotizaciones/1/editar',
  '/pedidos-venta', '/pedidos-venta/nuevo', '/pedidos-venta/1', '/pedidos-venta/1/editar', '/pedidos-venta/1/preparacion', '/preparaciones-pedido-venta',
  '/devoluciones-clientes', '/devoluciones-clientes/nueva', '/devoluciones-clientes/1',
  '/notas-credito-cliente', '/notas-credito-cliente/nueva', '/notas-credito-cliente/1',
  '/cuentas-por-cobrar', '/cuentas-por-pagar', '/facturas/1', '/facturas/1/pagos',
  '/finanzas', '/plan-cuentas', '/cuentas-bancarias', '/asientos-contables', '/estados-financieros', '/centros-costo',
  '/centro-reportes', '/centro-reportes/financieros', '/centro-reportes/administrativos',
  '/centro-reportes/inventario/valorizacion', '/centro-reportes/inventario/kardex', '/centro-reportes/inventario/stock-health', '/centro-reportes/inventario/reconciliacion',
  '/centro-reportes/ventas', '/centro-reportes/compras', '/centro-reportes/rentabilidad',
  '/inventario/movimientos', '/inventario/costeo', '/inventario/transferencias', '/inventario/transferencias/nueva', '/inventario/transferencias/1', '/inventario/transferencias/1/editar',
  '/inventario/conteos', '/inventario/conteos/nuevo', '/inventario/conteos/1', '/inventario/conteos/1/editar',
  '/inventario/reservas', '/inventario/reservas/nueva', '/inventario/reservas/1', '/inventario/reservas/1/editar',
  '/inventario/ajustes', '/inventario/ajustes/nuevo', '/inventario/ajustes/1', '/inventario/ajustes/1/editar',
  '/inventario/existencias', '/inventario/existencias/nueva', '/inventario/existencias/1/editar', '/inventario/trazabilidad/1'
] as const;

export const RESPONSIVE_VIEWPORTS = [
  { name: '320', width: 320, height: 568, mobile: true },
  { name: '360', width: 360, height: 800, mobile: true },
  { name: '375', width: 375, height: 812, mobile: true },
  { name: '390', width: 390, height: 844, mobile: true },
  { name: '412', width: 412, height: 915, mobile: true },
  { name: '414', width: 414, height: 896, mobile: true },
  { name: '430', width: 430, height: 932, mobile: true },
  { name: '480', width: 480, height: 960, mobile: true },
  { name: '768', width: 768, height: 1024, mobile: true },
  { name: '1024', width: 1024, height: 768, mobile: false },
  { name: '1280', width: 1280, height: 800, mobile: false },
  { name: '1440', width: 1440, height: 900, mobile: false },
  { name: '1920', width: 1920, height: 1080, mobile: false },
  { name: '2048', width: 2048, height: 1152, mobile: false },
  { name: '2400', width: 2400, height: 1350, mobile: false },
  { name: '2560', width: 2560, height: 1440, mobile: false }
] as const;

export const REQUIRED_QA_WIDTHS = [320, 360, 390, 414, 480, 768, 1024, 1280, 1440, 1920, 2048, 2400, 2560] as const;

export function canonicalRoute(path: string): string {
  return path.split('/').filter(Boolean).map((part) => part.startsWith(':') ? (
    part.toLowerCase().includes('slug') ? 'demo' : '1'
  ) : part).join('/').replace(/^/, '/');
}
