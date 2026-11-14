import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  ActualizarReservaInventarioValue,
  ReservaInventario,
  ReservaInventarioFiltro,
  ReservaInventarioFormValue
} from '../core/models/reserva-inventario.model';

@Injectable({ providedIn: 'root' })
export class ReservaInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/reservas-inventario`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: ReservaInventarioFiltro): Observable<ApiResponse<PagedResult<ReservaInventario>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.busqueda?.trim()) params = params.set('busqueda', filtro.busqueda.trim());
    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.ventaId !== undefined) params = params.set('ventaId', filtro.ventaId);
    if (filtro.almacenId !== undefined) params = params.set('almacenId', filtro.almacenId);
    if (filtro.expiraDesde) params = params.set('expiraDesde', filtro.expiraDesde);
    if (filtro.expiraHasta) params = params.set('expiraHasta', filtro.expiraHasta);

    return this.http.get<ApiResponse<PagedResult<ReservaInventario>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<ReservaInventario>> {
    return this.http.get<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}`);
  }

  create(value: ReservaInventarioFormValue): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(this.apiUrl, value);
  }

  update(id: number, value: ActualizarReservaInventarioValue): Observable<ApiResponse<ReservaInventario>> {
    return this.http.put<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}/activar`, {});
  }

  consumir(id: number): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}/consumir`, {});
  }

  liberar(id: number, motivo: string): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}/liberar`, { motivo });
  }

  expirar(id: number): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}/expirar`, {});
  }

  cancelar(id: number, motivo: string): Observable<ApiResponse<ReservaInventario>> {
    return this.http.post<ApiResponse<ReservaInventario>>(`${this.apiUrl}/${id}/cancelar`, { motivo });
  }
}
