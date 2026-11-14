export enum TipoEstadoFinanciero {
  BalanceGeneral = 1,
  EstadoResultados = 2,
  BalanceComprobacion = 3,
  LibroDiario = 4,
  LibroMayor = 5,
  FlujoEfectivo = 6,
}

export interface EstadoFinancieroFiltro {
  periodoContableId?: number;
  fechaDesde?: string;
  fechaHasta?: string;
}

export interface EstadoFinancieroLinea {
  cuentaContableId: number;
  cuentaCodigo?: string | null;
  cuentaNombre?: string | null;
  saldo: number;
  esRaiz: boolean;
}

export interface EstadoFinancieroTotal {
  etiqueta: string;
  valor: number;
}

export interface EstadoFinanciero {
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  lineas: EstadoFinancieroLinea[];
  totales: EstadoFinancieroTotal[];
}
