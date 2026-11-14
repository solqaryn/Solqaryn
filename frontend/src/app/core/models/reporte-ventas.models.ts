import { PagedRequest } from './api-response.model';

export interface ReporteVentasFiltroDto extends PagedRequest {
  desde?: string;
  hasta?: string;
  vendedorId?: number;
  clienteId?: number;
  sucursalId?: number;
  categoriaId?: number;
  productoId?: number;
  varianteId?: number;
  marcaId?: number;
  modeloId?: number;
  colorId?: number;
  tallaId?: number;
}

export interface ReporteVentasResumenDto {
  importeBruto: number;
  subtotal: number;
  descuento: number;
  impuesto: number;
  total: number;
  costoTotal: number;
  utilidadBruta: number;
}

export interface ReporteVentasDetalleDto {
  ventaId: number;
  numeroVenta: string;
  fecha: string;
  clienteId?: number;
  clienteNombre: string;
  vendedorId?: number;
  vendedorNombre?: string;
  sucursalId?: number;
  categoriaId?: number;
  productoId: number;
  productoVarianteId?: number;
  productoNombre: string;
  productoMarca: string;
  productoModelo: string;
  productoColor?: string;
  productoTalla?: string;
  productoSku?: string;
  cantidad: number;
  precioUnitario: number;
  costoUnitario: number;
  subtotal: number;
  utilidadBruta: number;
}
