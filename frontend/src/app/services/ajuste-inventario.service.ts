import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  AjusteInventario,
  AjusteInventarioFiltro,
  AjusteInventarioFormValue
} from '../core/models/ajuste-inventario.model';

@Injectable({ providedIn: 'root' })
export class AjusteInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/inventario/ajustes`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: AjusteInventarioFiltro): Observable<ApiResponse<PagedResult<AjusteInventario>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.search) params = params.set('search', filtro.search);
    if (filtro.sortBy) params = params.set('sortBy', filtro.sortBy);
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);
    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.productoId) params = params.set('productoId', filtro.productoId);
    if (filtro.productoVarianteId) {
      params = params.set('productoVarianteId', filtro.productoVarianteId);
    }

    return this.http.get<ApiResponse<PagedResult<AjusteInventario>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<AjusteInventario>> {
    return this.http.get<ApiResponse<AjusteInventario>>(`${this.apiUrl}/${id}`);
  }

  create(value: AjusteInventarioFormValue): Observable<ApiResponse<AjusteInventario>> {
    return this.http.post<ApiResponse<AjusteInventario>>(this.apiUrl, value);
  }

  update(id: number, value: AjusteInventarioFormValue): Observable<ApiResponse<AjusteInventario>> {
    return this.http.put<ApiResponse<AjusteInventario>>(`${this.apiUrl}/${id}`, value);
  }

  confirmar(id: number): Observable<ApiResponse<AjusteInventario>> {
    return this.http.post<ApiResponse<AjusteInventario>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivoAnulacion: string): Observable<ApiResponse<AjusteInventario>> {
    return this.http.post<ApiResponse<AjusteInventario>>(
      `${this.apiUrl}/${id}/anular`,
      { motivoAnulacion }
    );
  }
}
