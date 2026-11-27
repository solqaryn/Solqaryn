import { MAX_BYTES_FOTO_CATEGORIA, MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA, esFotoCategoriaValida } from './categoria-galeria-limites';

describe('Límites de galería de categorías', () => {
  it('acepta originales de hasta 15 MB en los tres formatos permitidos', () => {
    expect(MAX_BYTES_FOTO_CATEGORIA).toBe(15 * 1024 * 1024);
    for (const type of ['image/jpeg', 'image/png', 'image/webp']) {
      expect(esFotoCategoriaValida({ type, size: MAX_BYTES_FOTO_CATEGORIA })).toBe(true);
    }
  });

  it('rechaza archivos vacíos, formatos no permitidos y 15 MB más un byte', () => {
    expect(esFotoCategoriaValida({ type: 'image/png', size: 0 })).toBe(false);
    expect(esFotoCategoriaValida({ type: 'image/gif', size: 100 })).toBe(false);
    expect(esFotoCategoriaValida({ type: 'image/png', size: MAX_BYTES_FOTO_CATEGORIA + 1 })).toBe(false);
  });

  it('mantiene cinco imágenes optimizadas dentro del presupuesto multipart', () => {
    expect(MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA).toBe(750 * 1024);
    expect(5 * MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA).toBeLessThan(4 * 1024 * 1024);
  });
});
