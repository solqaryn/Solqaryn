export const STOREFRONT_BASE_PATH = 'tienda' as const;

/**
 * URLs públicas canónicas definidas por el Plan Maestro v1.1.
 * Fase 9 incorpora /tienda/ofertas como destino público de promociones vigentes.
 * Este módulo construye rutas; no genera slugs. Los slugs deben venir del contrato público.
 */
export const STOREFRONT_PATHS = {
  inicio: '/tienda',
  productos: '/tienda/productos',
  categorias: '/tienda/categorias',
  ofertas: '/tienda/ofertas',
  carrito: '/tienda/carrito',
  checkout: '/tienda/checkout',
  cuenta: '/tienda/cuenta',
  categoria: (slug: string) => `/tienda/categoria/${encodeURIComponent(slug)}`,
  producto: (slug: string) => `/tienda/producto/${encodeURIComponent(slug)}`,
  pedido: (id: string | number) => `/tienda/pedido/${encodeURIComponent(String(id))}`
} as const;
