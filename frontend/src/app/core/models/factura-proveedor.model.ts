export type EstadoFacturaProveedorNombre = 'Borrador' | 'Registrada' | 'Anulada';
export type EstadoFacturaProveedor = EstadoFacturaProveedorNombre | 1 | 2 | 3;

export interface FacturaProveedorFiltro {
  page: number;
  pageSize: number;
  estado?: EstadoFacturaProveedorNombre | null;
  proveedorId?: number | null;
  ordenCompraId?: number | null;
  numero?: string | null;
  desde?: string | null;
  hasta?: string | null;
  search?: string | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc' | null;
}

export interface FacturaProveedorDetalleInput {
  ordenCompraDetalleId: number;
  cantidadFacturada: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  observacion?: string | null;
}

export interface FacturaProveedorFormValue {
  proveedorId: number;
  ordenCompraId: number;
  numeroFactura: string;
  fechaEmisionUtc: string;
  fechaVencimientoUtc?: string | null;
  moneda: string;
  referenciaFiscal?: string | null;
  observaciones?: string | null;
  detalles: FacturaProveedorDetalleInput[];
}

export interface FacturaProveedorDetalle {
  id: number;
  ordenCompraDetalleId: number;
  productoId: number;
  productoVarianteId?: number | null;
  cantidadFacturada: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  subtotal: number;
  total: number;
  productoSkuSnapshot?: string | null;
  productoNombreSnapshot: string;
  productoMarcaSnapshot?: string | null;
  productoModeloSnapshot?: string | null;
  productoColorSnapshot?: string | null;
  productoTallaSnapshot?: string | null;
  observacion?: string | null;
}

export interface FacturaProveedor {
  id: number;
  numeroFactura: string;
  proveedorId: number;
  ordenCompraId: number;
  proveedorNombreSnapshot: string;
  proveedorDocumentoSnapshot?: string | null;
  moneda: string;
  fechaEmisionUtc: string;
  fechaVencimientoUtc?: string | null;
  referenciaFiscal?: string | null;
  observaciones?: string | null;
  estado: EstadoFacturaProveedor;
  fechaRegistroUtc?: string | null;
  registradaPorUsuarioId?: number | null;
  registradaPorNombreSnapshot?: string | null;
  fechaAnulacionUtc?: string | null;
  anuladaPorUsuarioId?: number | null;
  motivoAnulacion?: string | null;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  esEditable: boolean;
  detalles: FacturaProveedorDetalle[];
}
