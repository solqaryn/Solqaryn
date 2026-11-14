import { ImpuestoAplicado, ResultadoCalculo } from './venta.model';

export interface CompraDetalle {
  id: number;
  productoId: number;
  productoVarianteId?: number;
  productoNombre: string;
  productoMarca: string;
  productoModelo: string;
  productoColor?: string;
  productoTalla?: string;
  productoSku?: string;
  productoImagenPrincipalUrl?: string;
  cantidad: number;
  costoUnitario: number;
  subtotal: number;
}

export interface CompraDocumento {
  id: number;
  compraId: number;
  nombreOriginal: string;
  contentType: string;
  sizeBytes: number;
  esImagen: boolean;
  fechaCreacion: string;
  creadoPorNombreUsuario?: string;
}

export interface Compra {
  id: number;
  numeroCompra: string;
  fecha: string;
  proveedorNombre: string;
  proveedorTelefono?: string;
  proveedorDocumento?: string;
  documentoReferencia?: string;
  estado: 'Borrador' | 'Confirmada' | 'Anulada';
  estadoPago: 'Pendiente' | 'Pagado' | 'Parcial';
  metodoPago: 'Efectivo' | 'Transferencia' | 'Tarjeta' | 'Otro';
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  notas?: string;
  detalles: CompraDetalle[];
  impuestosAplicados: ImpuestoAplicado[];
  creadoPorNombreUsuario?: string;
  fechaCreacion: string;
  confirmadoPorNombreUsuario?: string;
  fechaConfirmacion?: string;
  anuladoPorNombreUsuario?: string;
  fechaAnulacion?: string;
  motivoAnulacion?: string;
}

export interface CompraDetalleInput {
  productoId: number;
  productoVarianteId?: number | null;
  cantidad: number;
  costoUnitario: number;
}

export interface CompraFormValue {
  proveedorNombre: string;
  proveedorTelefono?: string;
  proveedorDocumento?: string;
  documentoReferencia?: string;
  metodoPago: string;
  estadoPago: string;
  descuento: number;
  impuesto: number;
  notas?: string;
  detalles: CompraDetalleInput[];
}

export type { ResultadoCalculo };
