const NOMBRE_DESCARGA_MAXIMO = 180;

export function normalizarNombreDescarga(nombre: string, respaldo = 'archivo'): string {
  const limpio = nombre
    .replace(/[\u0000-\u001f\u007f]/g, '')
    .replace(/[<>:"/\\|?*]/g, '-')
    .replace(/\s+/g, ' ')
    .replace(/^\.+|\.+$/g, '')
    .trim()
    .slice(0, NOMBRE_DESCARGA_MAXIMO)
    .replace(/[. ]+$/g, '');

  return limpio || respaldo;
}

export function descargarBlobSeguro(blob: Blob, nombre: string, respaldo = 'archivo'): boolean {
  if (blob.size === 0) return false;

  const url = window.URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = normalizarNombreDescarga(nombre, respaldo);
  enlace.rel = 'noopener';
  enlace.style.display = 'none';
  document.body.appendChild(enlace);
  enlace.click();
  enlace.remove();
  window.setTimeout(() => window.URL.revokeObjectURL(url), 1000);
  return true;
}
