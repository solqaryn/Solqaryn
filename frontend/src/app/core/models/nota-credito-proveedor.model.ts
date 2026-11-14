export type EstadoNotaCreditoProveedorNombre = 'Borrador' | 'Registrada' | 'Anulada';
export type EstadoNotaCreditoProveedor = EstadoNotaCreditoProveedorNombre | 1 | 2 | 3;

export interface NotaCreditoProveedorFiltro {
  page: number;
  pageSize: number;
  estado?: EstadoNotaCreditoProveedorNombre | null;
  proveedorId?: number | null;
  facturaProveedorId?: number | null;
  devolucionProveedorId?: number | null;
  numero?: string | null;
  desde?: string | null;
  hasta?: string | null;
  search?: string | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc' | null;
}

export interface NotaCreditoProveedor {
  id: number;
  numeroNotaCredito: string;
  proveedorId: number;
  facturaProveedorId: number;
  devolucionProveedorId?: number | null;
  proveedorNombreSnapshot: string;
  moneda: string;
  fechaEmisionUtc: string;
  referenciaFiscal?: string | null;
  motivo: string;
  observaciones?: string | null;
  subtotalCredito: number;
  impuestoCredito: number;
  totalCredito: number;
  estado: EstadoNotaCreditoProveedor;
  fechaRegistroUtc?: string | null;
  registradaPorUsuarioId?: number | null;
  registradaPorNombreSnapshot?: string | null;
  fechaAnulacionUtc?: string | null;
  anuladaPorUsuarioId?: number | null;
  motivoAnulacion?: string | null;
}

export interface CreateNotaCreditoProveedor {
  numeroNotaCredito: string;
  facturaProveedorId: number;
  devolucionProveedorId?: number | null;
  fechaEmisionUtc: string;
  moneda: string;
  referenciaFiscal?: string | null;
  motivo: string;
  observaciones?: string | null;
  subtotalCredito: number;
  impuestoCredito: number;
}

export interface UpdateNotaCreditoProveedor {
  numeroNotaCredito: string;
  fechaEmisionUtc: string;
  moneda: string;
  referenciaFiscal?: string | null;
  motivo: string;
  observaciones?: string | null;
  subtotalCredito: number;
  impuestoCredito: number;
}
