export interface Impuesto {
  id: number;
  nombre: string;
  codigo: string;
  descripcion?: string;
  tipo: 'Porcentaje' | 'MontoFijo';
  tasa: number;
  montoFijo?: number;
  fechaInicio?: string;
  fechaFin?: string;
  incluidoEnPrecio: boolean;
  seCalculaAntesDescuento: boolean;
  acumulativo: boolean;
  prioridad: number;
  requiereRetencion: boolean;
  activo: boolean;
  productoIds: number[];
  categoriaIds: number[];
  operaciones: string[];
  clienteExentoIds: number[];
  proveedorExentoIds: number[];
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface GuardarImpuestoValue {
  nombre: string;
  codigo: string;
  descripcion?: string;
  tipo: 'Porcentaje' | 'MontoFijo';
  tasa: number;
  montoFijo?: number;
  fechaInicio?: string;
  fechaFin?: string;
  incluidoEnPrecio: boolean;
  seCalculaAntesDescuento: boolean;
  acumulativo: boolean;
  prioridad: number;
  requiereRetencion: boolean;
  productoIds: number[];
  categoriaIds: number[];
  operaciones: string[];
  clienteExentoIds: number[];
  proveedorExentoIds: number[];
}
