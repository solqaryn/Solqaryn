import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  SolicitudCompra,
  SolicitudCompraFiltro,
  SolicitudCompraFormValue
} from '../core/models/solicitud-compra.model';

@Injectable({ providedIn: 'root' })
export class SolicitudCompraService {
  private readonly apiUrl = `${environment.apiUrl}/solicitudes-compra`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: SolicitudCompraFiltro): Observable<ApiResponse<PagedResult<SolicitudCompra>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));

    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.numero?.trim()) params = params.set('numero', filtro.numero.trim());

    return this.http.get<ApiResponse<PagedResult<SolicitudCompra>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.get<ApiResponse<SolicitudCompra>>(`${this.apiUrl}/${id}`);
  }

  create(value: SolicitudCompraFormValue): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.post<ApiResponse<SolicitudCompra>>(this.apiUrl, value);
  }

  update(id: number, value: SolicitudCompraFormValue): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.put<ApiResponse<SolicitudCompra>>(`${this.apiUrl}/${id}`, value);
  }

  enviar(id: number): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.post<ApiResponse<SolicitudCompra>>(`${this.apiUrl}/${id}/enviar`, {});
  }

  aprobar(id: number): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.post<ApiResponse<SolicitudCompra>>(`${this.apiUrl}/${id}/aprobar`, {});
  }

  rechazar(id: number, motivo: string): Observable<ApiResponse<SolicitudCompra>> {
    return this.http.post<ApiResponse<SolicitudCompra>>(`${this.apiUrl}/${id}/rechazar`, { motivo: motivo.trim() });
  }
}
