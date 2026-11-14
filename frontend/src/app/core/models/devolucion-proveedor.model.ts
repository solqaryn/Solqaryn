export type EstadoDevolucionProveedorNombre = 'Borrador' | 'Confirmada' | 'Anulada';
export type EstadoDevolucionProveedor = EstadoDevolucionProveedorNombre | 1 | 2 | 3;

export interface DevolucionProveedorFiltro {
  proveedorId?: number | null;
  ordenCompraId?: number | null;
  recepcionCompraId?: number | null;
  facturaProveedorId?: number | null;
  estado?: EstadoDevolucionProveedor | null;
  desdeUtc?: string | null;
  hastaUtc?: string | null;
  page: number;
  pageSize: number;
}

export interface DevolucionProveedorDetalleInput {
  recepcionCompraDetalleId: number;
  cantidad: number;
}

export interface DevolucionProveedorCreateValue {
  recepcionCompraId: number;
  facturaProveedorId: number;
  motivo: string;
  observaciones?: string | null;
  detalles: DevolucionProveedorDetalleInput[];
}

export interface DevolucionProveedorUpdateValue {
  motivo: string;
  observaciones?: string | null;
  detalles: DevolucionProveedorDetalleInput[];
}

export interface DevolucionProveedorDetalle {
  id: number;
  recepcionCompraDetalleId: number;
  ordenCompraDetalleId: number;
  productoId: number;
  productoVarianteId?: number | null;
  almacenId: number;
  ubicacionAlmacenId?: number | null;
  cantidad: number;
  costoUnitarioSnapshot: number;
  impuestoUnitarioSnapshot: number;
  subtotalCredito: number;
  impuestoCredito: number;
  totalCredito: number;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot: string;
}

export interface DevolucionProveedor {
  id: number;
  numeroDevolucion: string;
  proveedorId: number;
  ordenCompraId: number;
  recepcionCompraId: number;
  facturaProveedorId: number;
  proveedorNombreSnapshot: string;
  moneda: string;
  motivo: string;
  observaciones?: string | null;
  estado: EstadoDevolucionProveedor;
  fechaConfirmacionUtc?: string | null;
  confirmadaPorUsuarioId?: number | null;
  confirmadaPorNombreSnapshot?: string | null;
  fechaAnulacionUtc?: string | null;
  anuladaPorUsuarioId?: number | null;
  motivoAnulacion?: string | null;
  subtotalCredito: number;
  impuestoCredito: number;
  totalCredito: number;
  detalles: DevolucionProveedorDetalle[];
}
