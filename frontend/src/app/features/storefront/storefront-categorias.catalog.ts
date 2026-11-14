import type { CategoriaCatalogoPublico, CategoriaTienda } from './storefront.models';

/** Convierte exclusivamente el contrato público de categorías al modelo visual canónico. */
export function mapearCategoriaTienda(categoria: CategoriaCatalogoPublico): CategoriaTienda {
  const id = Number(categoria.id);
  const nombre = typeof categoria.nombre === 'string' ? categoria.nombre.trim() : '';
  const slug = typeof categoria.slug === 'string' ? categoria.slug.trim() : '';
  const cantidad = categoria.totalProductos;

  if (!Number.isSafeInteger(id) || id <= 0 || !nombre || !slug) {
    throw new Error('Categoría pública no válida.');
  }
  if (cantidad !== null && (!Number.isSafeInteger(cantidad) || cantidad < 0)) {
    throw new Error('Conteo público de categoría no válido.');
  }

  return {
    id,
    nombre,
    slug,
    descripcion: typeof categoria.descripcion === 'string' ? categoria.descripcion.trim() : '',
    cantidadProductos: cantidad
  };
}

const CATEGORIAS_EJEMPLO: readonly CategoriaTienda[] = [
  { id: 1, nombre: 'Computadoras', slug: 'demo-categoria-1', descripcion: '', cantidadProductos: 3 },
  { id: 2, nombre: 'Audio', slug: 'demo-categoria-2', descripcion: '', cantidadProductos: 3 },
  { id: 3, nombre: 'Celulares', slug: 'demo-categoria-3', descripcion: '', cantidadProductos: 2 },
  { id: 4, nombre: 'Gaming', slug: 'demo-categoria-4', descripcion: '', cantidadProductos: 2 },
  { id: 5, nombre: 'Accesorios', slug: 'demo-categoria-5', descripcion: '', cantidadProductos: 2 },
  { id: 6, nombre: 'Hogar inteligente', slug: 'demo-categoria-6', descripcion: '', cantidadProductos: 2 }
];

/** Fixtures de preview, separados de producción y sin sustituir errores del backend. */
export function crearCategoriasTiendaEjemplo(): CategoriaTienda[] {
  return CATEGORIAS_EJEMPLO.map(categoria => ({ ...categoria }));
}
