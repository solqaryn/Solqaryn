export enum TipoInventario {
  MercaderiaVenta = 1,
  InsumoAdministrativo = 2
}

export interface ProductoImagen {
  id: number;
  url: string;
  orden: number;
  esPrincipal: boolean;
  productoVarianteId?: number | null;
}

export interface ProductoVariante {
  id: number;
  productoId: number;
  productoNombre: string;
  marcaId?: number | null;
  marcaNombre?: string | null;
  modeloId?: number | null;
  modeloNombre?: string | null;
  colorId?: number | null;
  colorNombre?: string | null;
  colorCodigoVisual?: string | null;
  tallaId?: number | null;
  tallaNombre?: string | null;
  etiqueta?: string;
  sku: string;
  codigoBarras?: string | null;
  cantidad: number;
  umbralStockBajo: number;
  costo: number;
  precio: number;
  activo: boolean;
  eliminado?: boolean;
  esTecnica?: boolean;
  tieneStockBajo: boolean;
  estaAgotada: boolean;
  estadoInventario: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface ProductoVarianteFormValue {
  id?: number;
  marcaId?: number | null;
  modeloId?: number | null;
  colorId?: number | null;
  tallaId?: number | null;
  sku?: string;
  codigoBarras?: string;
  cantidad: number;
  umbralStockBajo: number;
  costo: number;
  precio: number;
  activo?: boolean;
}

export interface Producto {
  id: number;
  nombre: string;
  marca: string;
  modelo: string;
  descripcion?: string;
  tipoInventario?: TipoInventario;
  cantidad: number;
  costo: number;
  precio: number;
  precioMinimo: number;
  precioMaximo: number;
  umbralStockBajo: number;
  tieneStockBajo: boolean;
  estaAgotado: boolean;
  estadoInventario: string;
  activo: boolean;
  esDestacado?: boolean;
  categoriaId?: number;
  categoriaNombre?: string;
  colorId?: number;
  colorNombre?: string;
  colorCodigoVisual?: string;
  tallaId?: number;
  tallaNombre?: string;
  marcaId?: number;
  marcaNombre?: string;
  modeloId?: number;
  modeloNombre?: string;
  imagenPrincipalUrl?: string;
  imagenes: ProductoImagen[];
  totalImagenes: number;
  variantes: ProductoVariante[];
  totalVariantes: number;
  usaVariantes: boolean;
  creadoPorNombreUsuario?: string;
  actualizadoPorNombreUsuario?: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface ProductoEscaneadoBase {
  productoId: number;
  productoVarianteId: number;
  productoNombre: string;
  marca: string;
  modelo: string;
  marcaId?: number | null;
  marcaNombre?: string | null;
  modeloId?: number | null;
  modeloNombre?: string | null;
  colorId?: number | null;
  colorNombre?: string | null;
  tallaId?: number | null;
  tallaNombre?: string | null;
  etiqueta?: string;
  esVarianteTecnica: boolean;
  sku: string;
  codigoBarras?: string | null;
  cantidadDisponible: number;
  precio: number;
  imagenMiniaturaUrl?: string | null;
}

export interface ProductoEscaneadoVenta extends ProductoEscaneadoBase {}

export interface ProductoEscaneadoCompra extends ProductoEscaneadoBase {
  costo: number;
}

export interface ProductoFormValue {
  nombre: string;
  marca: string;
  modelo: string;
  descripcion?: string;
  tipoInventario: TipoInventario;
  esDestacado?: boolean;
  cantidad: number;
  costo: number;
  precio: number;
  umbralStockBajo: number;
  categoriaId?: number | null;
  colorId?: number | null;
  tallaId?: number | null;
  marcaId?: number | null;
  modeloId?: number | null;
  variantes: ProductoVarianteFormValue[];
  imagenesNuevas?: File[];
  imagenesAEliminarIds?: number[];
  imagenPrincipalId?: number | null;
}

export interface AjusteStockRequest {
  cantidadActualEsperada: number;
  cantidadNueva: number;
  motivo: string;
}

export interface AjusteStockResultado {
  productoId: number;
  productoVarianteId?: number;
  cantidadAnterior: number;
  cantidadNueva: number;
  diferencia: number;
  motivo: string;
}
