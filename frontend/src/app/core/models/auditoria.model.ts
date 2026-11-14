export interface RegistroAuditoria {
  id: number;
  fecha: string;
  usuarioId?: number;
  nombreUsuario: string;
  modulo: string;
  accion: string;
  entidad?: string;
  referenciaId?: number;
  descripcion: string;
  valoresAnteriores?: string;
  valoresNuevos?: string;
  motivo?: string;
  ip?: string;
  userAgent?: string;
  correlationId?: string;
  resultado: 'Exito' | 'Error' | 'Rechazado' | 'Denegado';
  error?: string;
}
