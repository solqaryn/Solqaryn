export interface Descuento {
  id: number;
  nombre: string;
  descripcion?: string;
  codigoPromocional?: string;
  tipo: 'Porcentaje' | 'MontoFijo';
  valor: number;
  fechaInicio?: string;
  fechaFin?: string;
  montoMinimo?: number;
  montoMaximoDescuento?: number;
  cantidadMinima?: number;
  requiereAprobacion: boolean;
  acumulable: boolean;
  prioridad: number;
  limiteTotalUsos?: number;
  limiteUsosPorCliente?: number;
  usosRealizados: number;
  activo: boolean;
  productoIds: number[];
  categoriaIds: number[];
  clienteIds: number[];
  rolIds: number[];
  fechaCreacion: string;
  fechaActualizacion?: string;
}

export interface GuardarDescuentoValue {
  nombre: string;
  descripcion?: string;
  codigoPromocional?: string;
  tipo: 'Porcentaje' | 'MontoFijo';
  valor: number;
  fechaInicio?: string;
  fechaFin?: string;
  montoMinimo?: number;
  montoMaximoDescuento?: number;
  cantidadMinima?: number;
  requiereAprobacion: boolean;
  acumulable: boolean;
  prioridad: number;
  limiteTotalUsos?: number;
  limiteUsosPorCliente?: number;
  productoIds: number[];
  categoriaIds: number[];
  clienteIds: number[];
  rolIds: number[];
}
