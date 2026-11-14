export type TipoCatalogoProducto = 'Color' | 'Talla' | 'Marca' | 'Modelo';

export interface CatalogoProducto {
  id: number;
  tipo: TipoCatalogoProducto;
  nombre: string;
  descripcion?: string;
  codigoVisual?: string;
  orden: number;
  activo: boolean;
  catalogoPadreId?: number;
  catalogoPadreNombre?: string;
  totalProductos: number;
  totalModelos: number;
  creadoPorNombreUsuario?: string;
  actualizadoPorNombreUsuario?: string;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface CatalogoProductoFormValue {
  nombre: string;
  descripcion?: string;
  codigoVisual?: string;
  orden: number;
  catalogoPadreId?: number | null;
}
