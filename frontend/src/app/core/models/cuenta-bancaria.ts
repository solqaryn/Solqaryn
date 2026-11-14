export enum EstadoCuentaBancaria {
  Activa = 1,
  Inactiva = 2
}

export enum TipoOperacionBancaria {
  Deposito = 1,
  Retiro = 2,
  Transferencia = 3,
  Comision = 4,
  Interes = 5,
  ConciliacionAjuste = 6
}

export interface CuentaBancaria {
  id: number;
  bancoId: number;
  nombre: string;
  numeroCuenta: string;
  moneda: string;
  saldoInicial: number;
  estado: EstadoCuentaBancaria;
}

export interface CreateCuentaBancariaDto {
  bancoId: number;
  nombre: string;
  numeroCuenta: string;
  moneda: string;
  saldoInicial: number;
}

export interface UpdateCuentaBancariaDto {
  nombre: string;
}

export interface OperacionBancariaDto {
  tipoOperacion: TipoOperacionBancaria;
  monto: number;
  cuentaDestinoId?: number | null;
  referencia: string;
}

export interface CuentaBancariaQueryFilter {
  page?: number;
  pageSize?: number;
  bancoId?: number;
  estado?: EstadoCuentaBancaria;
  moneda?: string;
  searchTerm?: string;
}
