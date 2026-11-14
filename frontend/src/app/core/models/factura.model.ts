import { DescuentoAplicado, ImpuestoAplicado } from './venta.model';

export type FacturaFormatoCodigo = 'a4' | 'carta' | 'legal' | 'oficio' | 'a5' | 'pos58' | 'pos80';
export type EstadoFactura =
  | 'Borrador'
  | 'Emitida'
  | 'Pagada'
  | 'ParcialmentePagada'
  | 'Vencida'
  | 'Anulada'
  | 'Cancelada';

export interface FacturaFormatoPdf {
  codigo: FacturaFormatoCodigo;
  nombre: string;
  descripcion: string;
  anchoMm: number;
  altoMm?: number;
  esContinuo: boolean;
  usoRecomendado: string;
}

export interface FacturaDetalle {
  productoId: number;
  productoVarianteId?: number;
  productoNombre: string;
  productoMarca: string;
  productoModelo: string;
  varianteColor?: string;
  varianteTalla?: string;
  varianteSku?: string;
  cantidad: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  subtotal: number;
  totalLinea: number;
  observaciones?: string;
}

export interface FacturaPago {
  id: number;
  fechaPago: string;
  monto: number;
  montoRecibido: number;
  cambio: number;
  metodoPago: string;
  bancoId?: number;
  bancoCodigo?: string;
  bancoNombre?: string;
  referencia?: string;
  observaciones?: string;
  anulado: boolean;
  fechaAnulacion?: string;
  motivoAnulacion?: string;
}

export interface RegistrarFacturaPago {
  fechaPago?: string;
  monto: number;
  metodoPago: string;
  bancoId?: number;
  referencia?: string;
  observaciones?: string;
}

export interface Factura {
  id: number;
  ventaId: number;
  numeroVentaOrigen: string;
  numeroFactura: string;
  codigoInterno?: string;
  fechaEmision: string;
  fechaVencimiento?: string;
  estado: EstadoFactura;
  moneda: string;
  condicionPago?: string;
  referencia?: string;
  empresaNombre: string;
  empresaRTN?: string;
  empresaTelefono?: string;
  empresaCorreo?: string;
  empresaDireccion?: string;
  empresaEslogan?: string;
  empresaTextoFactura?: string;
  empresaTextoLegal?: string;
  empresaCopyright?: string;
  empresaLogoUrl?: string;
  clienteNombre: string;
  clienteTelefono?: string;
  clienteIdentidadORTN?: string;
  clienteCorreo?: string;
  clienteDireccion?: string;
  vendedorNombreUsuario: string;
  generadaPorNombreUsuario?: string;
  importeBruto: number;
  subtotal: number;
  descuento: number;
  impuesto: number;
  impuestoIncluido: number;
  impuestoAdicional: number;
  costoEnvio: number;
  costoEnvioId?: number;
  costoEnvioNombre?: string;
  envioExonerado: boolean;
  motivoExoneracionEnvio?: string;
  total: number;
  totalPagado: number;
  saldoPendiente: number;
  metodoPago: string;
  estadoPago: string;
  observaciones?: string;
  detalles: FacturaDetalle[];
  pagos: FacturaPago[];
  descuentosAplicados: DescuentoAplicado[];
  impuestosAplicados: ImpuestoAplicado[];
  fechaAnulacion?: string;
  anuladaPorNombreUsuario?: string;
  motivoAnulacion?: string;
}

export interface EnlaceCompartir {
  urlPdfPublica: string;
  fechaExpiracion: string;
  mensajeWhatsApp: string;
  telefonoSugerido: string;
}

export interface HistorialEnvio {
  id: number;
  canal: 'WhatsApp' | 'Correo';
  destinatario: string;
  resultado: string;
  error?: string;
  usuarioNombre?: string;
  fecha: string;
}

export interface ResultadoEnvioCorreo {
  exito: boolean;
  mensaje: string;
  codigo: string;
  esTransitorio: boolean;
  yaProcesado: boolean;
  intentos: number;
  messageId?: string;
}

export interface EstadoConfiguracionSmtp {
  configurado: boolean;
  host: string;
  puerto: number;
  usaTls: boolean;
  modoSeguridad: string;
  requiereAutenticacion: boolean;
  remitenteEnmascarado: string;
  maximoIntentos: number;
  timeoutSegundos: number;
  mensaje: string;
}

export interface ResultadoDiagnosticoSmtp {
  exito: boolean;
  codigo: string;
  mensaje: string;
  host: string;
  puerto: number;
  modoSeguridad: string;
  autenticado: boolean;
  duracionMilisegundos: number;
}
