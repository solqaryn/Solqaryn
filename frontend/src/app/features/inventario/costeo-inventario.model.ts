export enum MetodoCosteoInventario {
  PromedioPonderado = 1,
  FIFO = 2,
  Estandar = 3
}

export interface MetodoCosteoInventarioOption {
  id: MetodoCosteoInventario;
  nombre: string;
}

export interface PoliticaCosteoInventario {
  id: number;
  empresaConfiguracionId: number;
  metodo: MetodoCosteoInventario;
  metodoNombre: string;
  vigenteDesdeUtc: string;
  vigenteHastaUtc?: string | null;
  estaVigente: boolean;
  motivo: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface PoliticaCosteoInventarioQuery {
  page?: number;
  pageSize?: number;
  metodo?: MetodoCosteoInventario;
  vigente?: boolean;
  desdeUtc?: string;
  hastaUtc?: string;
}

export interface CambiarPoliticaCosteoInventarioRequest {
  metodo: MetodoCosteoInventario;
  motivo: string;
}
