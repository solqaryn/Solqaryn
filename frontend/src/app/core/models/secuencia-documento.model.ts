export interface SecuenciaDocumentoConsulta {
  empresaId: number;
  sucursalId: number | null;
  tipoDocumento: string;
  ultimoValor: number;
  prefijo: string;
  longitudNumero: number;
  activa: boolean;
  ultimoNumeroFormateado: string | null;
}

export interface SecuenciaDocumentoSiguiente {
  empresaId: number;
  sucursalId: number | null;
  tipoDocumento: string;
  valor: number;
  numero: string;
}

export interface ReservarSecuenciaDocumentoRequest {
  empresaId: number;
  sucursalId: number | null;
  tipoDocumento: string;
}
