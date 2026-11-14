export interface EmpresaConfiguracion {
  id: number;
  nombreComercial: string;
  razonSocial?: string;
  eslogan: string;
  rtn?: string;
  telefono?: string;
  correo?: string;
  direccion?: string;
  sitioWeb?: string;
  facebook?: string;
  instagram?: string;
  whatsApp?: string;
  logoUrl?: string;
  nombreVisibleSistema: string;
  descripcionSistema: string;
  mensajeLogin: string;
  copyright: string;
  mostrarCopyright: boolean;
  usarAnioAutomaticoCopyright: boolean;
  encabezadoActivo: boolean;
  encabezadoTexto?: string;
  piePaginaActivo: boolean;
  piePaginaTexto?: string;
  moneda: string;
  zonaHoraria: string;
  formatoFecha: string;
  informacionFiscal?: string;
  textoLegal?: string;
  textoFactura?: string;
  textoReportes?: string;
}

export type ActualizarEmpresaConfiguracionValue = Omit<EmpresaConfiguracion, 'id' | 'logoUrl'>;
