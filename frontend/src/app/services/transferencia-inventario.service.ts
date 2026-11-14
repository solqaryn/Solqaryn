import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  AprobarTransferenciaInventario,
  CancelarTransferenciaInventario,
  DespacharTransferenciaInventario,
  RecibirTransferenciaInventario,
  TransferenciaInventario,
  TransferenciaInventarioFiltro,
  TransferenciaInventarioFormValue
} from '../core/models/transferencia-inventario.model';

@Injectable({ providedIn: 'root' })
export class TransferenciaInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/transferencias-inventario`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: TransferenciaInventarioFiltro): Observable<ApiResponse<PagedResult<TransferenciaInventario>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.search?.trim()) params = params.set('search', filtro.search.trim());
    if (filtro.sortBy?.trim()) params = params.set('sortBy', filtro.sortBy.trim());
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);
    if (filtro.estado !== undefined) params = params.set('estado', filtro.estado);
    if (filtro.almacenOrigenId !== undefined) params = params.set('almacenOrigenId', filtro.almacenOrigenId);
    if (filtro.almacenDestinoId !== undefined) params = params.set('almacenDestinoId', filtro.almacenDestinoId);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.numero?.trim()) params = params.set('numero', filtro.numero.trim());

    return this.http.get<ApiResponse<PagedResult<TransferenciaInventario>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.get<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}`);
  }

  create(value: TransferenciaInventarioFormValue): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(this.apiUrl, value);
  }

  update(id: number, value: TransferenciaInventarioFormValue): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.put<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}`, value);
  }

  solicitar(id: number): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}/solicitar`, {});
  }

  aprobar(id: number, value: AprobarTransferenciaInventario): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}/aprobar`, value);
  }

  despachar(id: number, value: DespacharTransferenciaInventario): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}/despachar`, value);
  }

  recibir(id: number, value: RecibirTransferenciaInventario): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}/recibir`, value);
  }

  cancelar(id: number, value: CancelarTransferenciaInventario): Observable<ApiResponse<TransferenciaInventario>> {
    return this.http.post<ApiResponse<TransferenciaInventario>>(`${this.apiUrl}/${id}/cancelar`, value);
  }
}
