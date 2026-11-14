export interface CostoEnvio {
  id: number;
  nombre: string;
  descripcion?: string;
  departamento?: string;
  ciudad?: string;
  zona?: string;
  modalidad?: string;
  monto: number;
  vigenteDesde?: string;
  vigenteHasta?: string;
  prioridad: number;
  esPredeterminado: boolean;
  activo: boolean;
  estaVigente: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

export interface GuardarCostoEnvio {
  nombre: string;
  descripcion?: string;
  departamento?: string;
  ciudad?: string;
  zona?: string;
  modalidad?: string;
  monto: number;
  vigenteDesde?: string | null;
  vigenteHasta?: string | null;
  prioridad: number;
  esPredeterminado: boolean;
  activo: boolean;
}
