export enum TipoCuentaContable {
  Activo = 1,
  Pasivo = 2,
  Patrimonio = 3,
  Ingreso = 4,
  Gasto = 5,
  Costo = 6
}

export interface CuentaContable {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  tipo: TipoCuentaContable;
  cuentaPadreId: number | null;
  aceptaMovimientos: boolean;
  activa: boolean;
  esRaiz: boolean;
  subcuentas: CuentaContable[];
}

export interface CuentaContableInput {
  codigo: string;
  nombre: string;
  descripcion: string | null;
  tipo: TipoCuentaContable;
  cuentaPadreId: number | null;
  aceptaMovimientos: boolean;
  activa: boolean;
}
