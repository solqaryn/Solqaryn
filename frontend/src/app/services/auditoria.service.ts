import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { RegistroAuditoria } from '../core/models/auditoria.model';

export interface AuditoriaFiltro {
  usuarioId?: number;
  modulo?: string;
  accion?: string;
  entidad?: string;
  referenciaId?: number;
  resultado?: string;
  texto?: string;
  desde?: string;
  hasta?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class AuditoriaService {
  private readonly apiUrl = `${environment.apiUrl}/auditoria`;

  constructor(private http: HttpClient) {}

  getFiltered(filtro: AuditoriaFiltro): Observable<ApiResponse<PagedResult<RegistroAuditoria>>> {
    let params = new HttpParams().set('page', filtro.page).set('pageSize', filtro.pageSize);
    if (filtro.usuarioId) params = params.set('usuarioId', filtro.usuarioId);
    if (filtro.modulo) params = params.set('modulo', filtro.modulo);
    if (filtro.accion) params = params.set('accion', filtro.accion);
    if (filtro.entidad) params = params.set('entidad', filtro.entidad);
    if (filtro.referenciaId) params = params.set('referenciaId', filtro.referenciaId);
    if (filtro.resultado) params = params.set('resultado', filtro.resultado);
    if (filtro.texto) params = params.set('texto', filtro.texto);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    return this.http.get<ApiResponse<PagedResult<RegistroAuditoria>>>(this.apiUrl, { params });
  }
}
