export interface CreateNotaCreditoCliente {
  facturaId: number;
  montoCredito: number;
  motivo: string;
  observaciones?: string | null;
}

export interface NotaCreditoCliente {
  id: number;
  facturaId: number;
  ventaId: number;
  moneda: string;
  montoCredito: number;
  motivo: string;
  observaciones?: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
  creadoPorUsuarioId?: number | null;
  creadoPorNombreUsuario?: string | null;
}
