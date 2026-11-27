export const MAX_MB_FOTO_CATEGORIA = 15;
export const MAX_BYTES_FOTO_CATEGORIA = MAX_MB_FOTO_CATEGORIA * 1024 * 1024;
// Conservar el multipart de cinco fotografías por debajo del presupuesto del proxy.
export const MAX_BYTES_FOTO_OPTIMIZADA_CATEGORIA = 750 * 1024;

export function esFotoCategoriaValida(archivo: Pick<File, 'type' | 'size'>): boolean {
  return ['image/jpeg', 'image/png', 'image/webp'].includes(archivo.type)
    && archivo.size > 0 && archivo.size <= MAX_BYTES_FOTO_CATEGORIA;
}
