export interface VentaDetalle {
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
  precioUnitario: number;
  subtotal: number;
  utilidadBruta: number;
}

export interface DescuentoAplicado {
  descuentoId: number;
  nombre: string;
  codigo?: string;
  tipo: string;
  valor: number;
  monto: number;
}

export interface ImpuestoAplicado {
  impuestoId: number;
  nombre: string;
  codigo?: string;
  tasa: number;
  baseImponible: number;
  monto: number;
  incluidoEnPrecio?: boolean;
}

export interface ResultadoCalculo {
  importeBruto: number;
  importeProductos?: number;
  subtotal: number;
  subtotalNeto?: number;
  descuentosAplicados: DescuentoAplicado[];
  totalDescuento: number;
  impuestosAplicados: ImpuestoAplicado[];
  totalImpuesto: number;
  impuestoIncluido: number;
  impuestoAdicional: number;
  costoEnvioId?: number;
  costoEnvioNombre?: string;
  costoEnvio?: number;
  envioExonerado?: boolean;
  motivoExoneracionEnvio?: string;
  total: number;
}

export interface Venta {
  id: number;
  numeroVenta: string;
  fecha: string;
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  estado: 'Borrador' | 'Confirmada' | 'Anulada';
  estadoPago: 'Pendiente' | 'Pagado' | 'Parcial';
  metodoPago: string;
  importeBruto: number;
  importeProductos: number;
  subtotal: number;
  descuento: number;
  impuesto: number;
  costoEnvio: number;
  costoEnvioId?: number;
  costoEnvioNombre?: string;
  envioExonerado: boolean;
  motivoExoneracionEnvio?: string;
  total: number;
  costoTotal: number;
  utilidadBruta: number;
  notas?: string;
  detalles: VentaDetalle[];
  descuentosAplicados: DescuentoAplicado[];
  impuestosAplicados: ImpuestoAplicado[];
  facturaId?: number;
  numeroFactura?: string;
  creadoPorNombreUsuario?: string;
  fechaCreacion: string;
  confirmadoPorNombreUsuario?: string;
  fechaConfirmacion?: string;
  anuladoPorNombreUsuario?: string;
  fechaAnulacion?: string;
  motivoAnulacion?: string;
}

export interface VentaDetalleInput {
  productoId: number;
  productoVarianteId?: number | null;
  cantidad: number;
  precioUnitario: number;
}

export interface VentaFormValue {
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  metodoPago: string;
  estadoPago: string;
  descuento: number;
  impuesto: number;
  codigoPromocional?: string;
  costoEnvioId?: number;
  envioExonerado: boolean;
  motivoExoneracionEnvio?: string;
  notas?: string;
  detalles: VentaDetalleInput[];
}
