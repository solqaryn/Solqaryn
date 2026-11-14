export interface TipoCliente {
  id: number;
  codigo: string;
  esSistema: boolean;
  nombre: string;
  nombreNormalizado: string;
  descripcion?: string;
  colorHex: string;
  activo: boolean;
  orden: number;
  esPredeterminado: boolean;
  totalClientesAsignados: number;
}

export interface TipoClienteFormValue {
  nombre: string;
  descripcion?: string;
  colorHex: string;
  activo: boolean;
  orden: number;
  esPredeterminado: boolean;
}
