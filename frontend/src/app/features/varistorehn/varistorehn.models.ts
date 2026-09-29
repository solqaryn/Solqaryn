/**
 * Contratos canonicos del escaparate publico de VariStoreHn.
 * Independientes de componentes Angular y de modelos administrativos.
 */
export interface ImagenCatalogo {
  url: string;
  orden: number;
  esPrincipal: boolean;
}

export interface ModeloCatalogoPublico {
  productoVarianteId?: number;
  modeloId?: number;
  modeloNombre?: string;
  marcaNombre?: string;
  sku?: string | null;
  precio: number;
  precioOferta?: number | null;
  ofertaActiva?: boolean;
  ofertaNombre?: string | null;
  ofertaInicioUtc?: string | null;
  ofertaFinUtc?: string | null;
  ahorro?: number;
  porcentajeAhorro?: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  estadoDisponibilidad?: string;
  imagenes: ImagenCatalogo[];
}

/** Frontera HTTP publica. Los campos comerciales reservados no se inventan. */
export interface ProductoCatalogoPublico {
  id: number;
  slug?: string;
  nombre: string;
  descripcion?: string;
  categoriaId?: number | null;
  categoriaNombre?: string;
  marcaNombre?: string;
  modeloNombre?: string;
  precio: number;
  precioOferta?: number | null;
  ofertaActiva?: boolean;
  ofertaNombre?: string | null;
  ofertaInicioUtc?: string | null;
  ofertaFinUtc?: string | null;
  ahorro?: number;
  porcentajeAhorro?: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  estadoDisponibilidad?: string;
  sku?: string | null;
  activo?: boolean;
  esDestacado?: boolean;
  fechaCreacion?: string;
  imagenPrincipalUrl?: string;
  imagenes: ImagenCatalogo[];
  modelos: ModeloCatalogoPublico[];
}

export interface ModeloCatalogoResumenPublico {
  productoVarianteId: number;
  modeloId?: number | null;
  modeloNombre?: string | null;
  marcaNombre?: string | null;
  sku?: string | null;
  precio: number;
  precioOferta?: number | null;
  ofertaActiva?: boolean;
  ofertaNombre?: string | null;
  ahorro?: number;
  porcentajeAhorro?: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  estadoDisponibilidad?: string;
}

/**
 * Contrato ligero para tarjetas/listados. La galería y la descripción completa
 * pertenecen exclusivamente al detalle público.
 */
export interface ProductoCatalogoResumenPublico {
  id: number;
  slug: string;
  nombre: string;
  descripcionResumen?: string | null;
  categoriaId?: number | null;
  categoriaNombre?: string | null;
  marcaNombre?: string | null;
  modeloNombre?: string | null;
  precio: number;
  precioOferta?: number | null;
  ofertaActiva?: boolean;
  ofertaNombre?: string | null;
  ahorro?: number;
  porcentajeAhorro?: number;
  cantidadDisponible: number;
  estaAgotado: boolean;
  estadoDisponibilidad?: string;
  esDestacado?: boolean;
  fechaCreacion?: string;
  imagenPrincipalUrl?: string | null;
  modelos: ModeloCatalogoResumenPublico[];
}

export interface CategoriaCatalogoPublico {
  id: number;
  slug: string;
  nombre: string;
  descripcion?: string | null;
  /** Null significa que la fuente publica aun no calculo el conteo; nunca equivale a cero. */
  totalProductos: number | null;
}

export interface ModeloTienda {
  clave: string;
  productoVarianteId?: number | null;
  modeloId: number | null;
  nombre: string;
  marca: string;
  sku: string;
  precio: number;
  precioOferta: number | null;
  ofertaActiva: boolean;
  ofertaNombre: string;
  ofertaInicioUtc: string | null;
  ofertaFinUtc: string | null;
  ahorro: number;
  porcentajeAhorro: number;
  stock: number;
  disponible: boolean;
  estadoDisponibilidad: EstadoDisponibilidad;
  imagenes: string[];
}

/** Modelo unico que deben consumir todas las paginas publicas de producto. */
export interface ProductoTienda {
  id: number;
  slug: string;
  nombre: string;
  descripcion: string;
  categoriaId: number | null;
  categoria: string;
  marca: string;
  sku: string;
  precio: number;
  precioOferta: number | null;
  ofertaActiva: boolean;
  ofertaNombre: string;
  ofertaInicioUtc: string | null;
  ofertaFinUtc: string | null;
  ahorro: number;
  porcentajeAhorro: number;
  stock: number;
  disponible: boolean;
  estadoDisponibilidad: EstadoDisponibilidad;
  activo: boolean;
  destacado: boolean;
  fechaCreacion: string | null;
  imagenes: string[];
  modelos: ModeloTienda[];
  ilustracion?: string;
}

/** Modelo unico que deben consumir todas las paginas publicas de categoria. */
export interface CategoriaTienda {
  id: number;
  nombre: string;
  slug: string;
  descripcion: string;
  imagenUrl?: string;
  /** Null preserva la diferencia entre 'sin productos' y 'conteo no disponible'. */
  cantidadProductos: number | null;
}

export interface ItemCarrito {
  clave: string;
  productoId: number;
  productoVarianteId?: number | null;
  modeloClave: string;
  modeloId: number | null;
  nombre: string;
  modelo: string;
  precio: number;
  precioNormal: number;
  ahorro: number;
  ofertaActiva: boolean;
  ofertaNombre: string;
  estadoDisponibilidad: EstadoDisponibilidad;
  stock: number;
  unidades: number;
  imagen: string;
  ilustracion: string;
}

export interface ReferenciaCarrito {
  productoId: number;
  modeloClave: string;
  unidades: number;
}

/**
 * Identidad mínima de la selección. ProductoVarianteId es la autoridad cuando existe;
 * modelo/marca quedan como compatibilidad fail-closed para catálogos legacy sin identidad física.
 */
export interface CheckoutItemRequest {
  productoId: number;
  productoVarianteId?: number | null;
  modeloId: number | null;
  modeloNombre: string | null;
  marcaNombre: string | null;
  unidades: number;
}

export interface CheckoutValidarRequest {
  items: CheckoutItemRequest[];
}

export interface CheckoutLineaValidada {
  productoId: number;
  productoVarianteId?: number | null;
  modeloId: number | null;
  nombre: string;
  modelo?: string | null;
  sku?: string | null;
  unidades: number;
  stockDisponible: number;
  precioUnitario: number;
  total: number;
}

/** Snapshot calculado por el servidor. No equivale a reserva ni a pedido ERP. */
export interface CheckoutValidado {
  validacionId: string;
  expiraUtc: string;
  subtotal: number;
  total: number;
  lineas: CheckoutLineaValidada[];
}

export interface DatosCompradorCheckout {
  nombre: string;
  telefono: string;
  correo?: string;
  notas?: string;
}

export interface CheckoutTarjetaRequest {
  validacionId: string;
  items: CheckoutItemRequest[];
  comprador: DatosCompradorCheckout;
  idempotencyKey: string;
}

export interface CheckoutTarjetaResponse {
  checkoutUrl: string;
  referencia?: string;
}

export type EstadoPedidoPublico = 'whatsapp-preparado' | 'tarjeta-redirigida' | 'demo';

/** Recibo local no sensible: sirve para UX, nunca como autoridad transaccional. */
export interface ReciboPedidoPublico {
  referencia: string;
  estado: EstadoPedidoPublico;
  creadoUtc: string;
  expiraUtc: string;
  total: number;
  moneda: string;
  lineas: CheckoutLineaValidada[];
}

export type OrdenCatalogo = 'destacados' | 'relevancia' | 'precio-asc' | 'precio-desc' | 'recientes' | 'nombre';

export interface FiltrosCatalogo {
  busqueda: string;
  categoria: string;
  soloDisponibles: boolean;
  soloOfertas: boolean;
  precioMinimo: number | null;
  precioMaximo: number | null;
  orden: OrdenCatalogo;
}

export type EstadoConsultaPublica = 'loading' | 'empty' | 'error' | 'success';
/** Recurso individual: "not-found" es distinto de una colección pública vacía. */
export type EstadoRecursoPublico = EstadoConsultaPublica | 'not-found';
export type EstadoDisponibilidad = 'available' | 'lowStock' | 'outOfStock';
export type EstadoPromocion = 'none' | 'active' | 'expired';


export interface TiendaCuentaPerfil {
  id: number;
  nombre: string;
  correo: string;
}

export interface TiendaCuentaSesion {
  token: string;
  expiraUtc: string;
  perfil: TiendaCuentaPerfil;
}

export interface TiendaDireccionCliente {
  id: number;
  alias: string;
  recibe: string;
  telefono: string;
  direccion: string;
  predeterminada: boolean;
}

export interface TiendaPedidoCuentaLinea {
  productoId: number;
  productoVarianteId?: number | null;
  nombre: string;
  modelo?: string | null;
  cantidad: number;
  precioUnitario: number;
  total: number;
}

export interface TiendaPedidoCuenta {
  id: number;
  estado: string;
  total: number;
  fechaUtc: string;
  lineas: TiendaPedidoCuentaLinea[];
}

export interface TiendaNotificacionPedido {
  pedidoId: number;
  estado: string;
  mensaje: string;
  fechaUtc: string;
}
