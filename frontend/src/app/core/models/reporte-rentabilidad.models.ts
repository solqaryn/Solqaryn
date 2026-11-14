export type RentabilidadAgrupacion = 'vendedor' | 'cliente' | 'producto' | 'categoria';

export interface ReporteRentabilidadDto {
  agrupacionId?: number;
  agrupacion: string;
  nombre: string;
  venta: number;
  costo: number;
  utilidadBruta: number;
  incluyeDescuentoEncabezadoEnUtilidad: boolean;
  semantica: string;
}
