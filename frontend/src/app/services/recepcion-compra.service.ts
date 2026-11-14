import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  RecepcionCompra,
  RecepcionCompraFiltro,
  RecepcionCompraFormValue,
  RecepcionCompraSaldoOrden
} from '../core/models/recepcion-compra.model';

@Injectable({ providedIn: 'root' })
export class RecepcionCompraService {
  private readonly apiUrl = `${environment.apiUrl}/recepciones-compra`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: RecepcionCompraFiltro): Observable<ApiResponse<PagedResult<RecepcionCompra>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));

    if (filtro.ordenCompraId) params = params.set('ordenCompraId', filtro.ordenCompraId);
    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.desdeUtc) params = params.set('desdeUtc', filtro.desdeUtc);
    if (filtro.hastaUtc) params = params.set('hastaUtc', filtro.hastaUtc);

    return this.http.get<ApiResponse<PagedResult<RecepcionCompra>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<RecepcionCompra>> {
    return this.http.get<ApiResponse<RecepcionCompra>>(`${this.apiUrl}/${id}`);
  }

  getSaldoOrden(ordenCompraId: number): Observable<ApiResponse<RecepcionCompraSaldoOrden>> {
    return this.http.get<ApiResponse<RecepcionCompraSaldoOrden>>(`${this.apiUrl}/ordenes/${ordenCompraId}/saldo`);
  }

  create(value: RecepcionCompraFormValue, idempotencyKey = this.generarIdempotencyKey()): Observable<ApiResponse<RecepcionCompra>> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<ApiResponse<RecepcionCompra>>(this.apiUrl, value, { headers });
  }

  update(id: number, value: Omit<RecepcionCompraFormValue, 'ordenCompraId'>): Observable<ApiResponse<RecepcionCompra>> {
    return this.http.put<ApiResponse<RecepcionCompra>>(`${this.apiUrl}/${id}`, value);
  }

  confirmar(id: number): Observable<ApiResponse<RecepcionCompra>> {
    return this.http.post<ApiResponse<RecepcionCompra>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivo: string): Observable<ApiResponse<RecepcionCompra>> {
    return this.http.post<ApiResponse<RecepcionCompra>>(`${this.apiUrl}/${id}/anular`, { motivo: motivo.trim() });
  }

  private generarIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `recepcion-compra:${uuid}` : `recepcion-compra:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  }
}
