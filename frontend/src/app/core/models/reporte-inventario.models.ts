import { PagedRequest } from './api-response.model';

export interface ReporteInventarioFiltroBaseDto extends PagedRequest {
  desde?: string;
  hasta?: string;
  sucursalId?: number;
  almacenId?: number;
  ubicacionAlmacenId?: number;
}
