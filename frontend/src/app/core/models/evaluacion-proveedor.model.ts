export interface EvaluacionProveedor {
  id: number;
  proveedorId: number;
  ordenCompraId: number;
  recepcionCompraId: number;
  fechaEsperadaUtc: string;
  fechaRecepcionUtc: string;
  cantidadOrdenada: number;
  cantidadAceptada: number;
  cantidadDanada: number;
  cantidadSobrante: number;
}

export interface EvaluacionProveedorFiltro {
  page: number;
  pageSize: number;
  proveedorId?: number | null;
  ordenCompraId?: number | null;
  recepcionCompraId?: number | null;
  desdeUtc?: string | null;
  hastaUtc?: string | null;
}
