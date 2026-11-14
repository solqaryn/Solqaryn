export type EstadoDevolucionCliente = 'Borrador' | 'Confirmada' | 'Anulada' | 1 | 2 | 3;
export type TipoResolucionDevolucionCliente = 'Reintegro' | 'Cambio' | 'CreditoAFavor' | 1 | 2 | 3;

export interface DevolucionClienteDetalle {
  id: number;
  ventaDetalleId: number;
  productoId: number;
  productoVarianteId?: number | null;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot: string;
  productoMarcaSnapshot: string;
  productoModeloSnapshot: string;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
  cantidad: number;
  cantidadVendidaSnapshot: number;
  precioUnitarioSnapshot: number;
  resolucion: TipoResolucionDevolucionCliente;
  montoReferencia: number;
}

export interface DevolucionCliente {
  id: number;
  ventaId: number;
  facturaId?: number | null;
  estado: EstadoDevolucionCliente;
  observaciones?: string | null;
  idempotencyKey?: string | null;
  montoReferencia: number;
  fechaCreacion: string;
  fechaConfirmacion?: string | null;
  fechaAnulacion?: string | null;
  motivoAnulacion?: string | null;
  detalles: DevolucionClienteDetalle[];
}

export interface CreateDevolucionClienteDetalle {
  ventaDetalleId: number;
  cantidad: number;
  resolucion: TipoResolucionDevolucionCliente;
}

export interface CreateDevolucionCliente {
  ventaId: number;
  facturaId?: number | null;
  observaciones?: string | null;
  detalles: CreateDevolucionClienteDetalle[];
}

export interface DevolucionClienteFiltro {
  page: number;
  pageSize: number;
  ventaId?: number | null;
  estado?: EstadoDevolucionCliente | null;
}
