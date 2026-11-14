export interface AutomatizacionConfiguracion {
  diasBorradorVentaAlerta: number;
  diasBorradorCompraAlerta: number;
  diasCargaPendienteAlerta: number;
  diasMovimientoFinancieroPendienteAlerta: number;
  limiteSugerencias: number;
  limiteAutocompletado: number;
  mostrarRecordatoriosDashboard: boolean;
  versionReglas: string;
  fechaActualizacion?: string | null;
  actualizadoPor?: string | null;
}

export interface AutomatizacionSugerencia {
  codigo: string;
  modulo: string;
  severidad: string;
  titulo: string;
  detalle: string;
  cantidad: number;
  ruta: string;
  requiereConfirmacion: boolean;
}

export interface AutomatizacionResumen {
  versionReglas: string;
  generadoEnUtc: string;
  totalSugerencias: number;
  sugerencias: AutomatizacionSugerencia[];
}

export interface AutocompletadoItem {
  id: number;
  contexto: string;
  etiqueta: string;
  detalle?: string | null;
  codigo?: string | null;
}

export interface AccionMasivaPreview {
  accion: string;
  solicitados: number;
  aplicables: number;
  omitidos: number;
  soloVistaPrevia: boolean;
  requiereConfirmacion: boolean;
  idsAplicables: number[];
  advertencias: string[];
}
