export interface MetodoPago {
  id: number;
  codigo: string;
  nombre: string;
  tipo: string;
  activo: boolean;
  requiereReferencia: boolean;
  requiereBanco: boolean;
  permiteCambio: boolean;
  orden: number;
  metadata?: string | null;
}

export interface BancoLookup {
  id: number;
  codigo: string;
  nombre: string;
}

export interface MetodoPagoCreate {
  codigo: string;
  nombre: string;
  tipo: string;
  activo: boolean;
  requiereReferencia: boolean;
  requiereBanco: boolean;
  permiteCambio: boolean;
  orden: number;
  metadata?: string | null;
}

export type MetodoPagoUpdate = Omit<MetodoPagoCreate, 'codigo'>;

export interface ReordenarMetodoPago {
  id: number;
  orden: number;
}
