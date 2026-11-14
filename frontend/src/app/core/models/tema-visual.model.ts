export interface TemaVisual {
  colorPrimario: string;
  colorSecundario: string;
  colorAcento: string;
  fondoPrincipal: string;
  fondoTarjetas: string;
  menuLateral: string;
  barraSuperior: string;
  encabezados: string;
  botonesPrincipales: string;
  textoPrincipal: string;
  textoSecundario: string;
  colorExito: string;
  colorAdvertencia: string;
  colorError: string;
  colorInformacion: string;
  fechaActualizacion?: string;
}

/** Metadatos para renderizar el formulario de administración de forma
 * genérica (etiqueta legible + variable CSS destino de cada campo). */
export const CAMPOS_TEMA: { clave: keyof TemaVisual; etiqueta: string; variableCss: string }[] = [
  { clave: 'colorPrimario', etiqueta: 'Color primario', variableCss: '--color-primary' },
  { clave: 'colorSecundario', etiqueta: 'Color secundario', variableCss: '--color-primary-dark' },
  { clave: 'colorAcento', etiqueta: 'Color de acento', variableCss: '--color-accent' },
  { clave: 'fondoPrincipal', etiqueta: 'Fondo principal', variableCss: '--color-bg' },
  { clave: 'fondoTarjetas', etiqueta: 'Fondo de tarjetas', variableCss: '--color-surface' },
  { clave: 'menuLateral', etiqueta: 'Menú lateral', variableCss: '--color-sidebar' },
  { clave: 'barraSuperior', etiqueta: 'Barra superior', variableCss: '--color-topbar' },
  { clave: 'encabezados', etiqueta: 'Encabezados', variableCss: '--color-heading' },
  { clave: 'botonesPrincipales', etiqueta: 'Botones principales', variableCss: '--color-button' },
  { clave: 'textoPrincipal', etiqueta: 'Texto principal', variableCss: '--color-text' },
  { clave: 'textoSecundario', etiqueta: 'Texto secundario', variableCss: '--color-text-muted' },
  { clave: 'colorExito', etiqueta: 'Éxito', variableCss: '--color-success' },
  { clave: 'colorAdvertencia', etiqueta: 'Advertencia', variableCss: '--color-warning' },
  { clave: 'colorError', etiqueta: 'Error', variableCss: '--color-danger' },
  { clave: 'colorInformacion', etiqueta: 'Información', variableCss: '--color-info' }
];
