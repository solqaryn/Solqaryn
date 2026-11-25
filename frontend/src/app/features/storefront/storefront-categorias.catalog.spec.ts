import { mapearCategoriaTienda } from './storefront-categorias.catalog';

describe('Galería pública de categorías', () => {
  const categoria = { id: 7, slug: 'tennis', nombre: 'Tennis', totalProductos: 2 };

  it('conserva la portada y hasta cinco imágenes en su orden original', () => {
    const imagenes = Array.from({ length: 6 }, (_, i) => 'https://res.cloudinary.com/demo/image/upload/foto' + i + '.webp');
    const vista = mapearCategoriaTienda({ ...categoria, imagenes });
    expect(vista.imagenUrl).toBe(imagenes[0]);
    expect(vista.imagenes).toEqual(imagenes.slice(0, 5));
  });

  it('mantiene categorías existentes sin imágenes y descarta esquemas no HTTPS', () => {
    expect(mapearCategoriaTienda(categoria).imagenes).toEqual([]);
    expect(mapearCategoriaTienda({ ...categoria, imagenes: ['javascript:alert(1)', 'http://example.test/a.jpg'] }).imagenUrl).toBeUndefined();
  });
});
