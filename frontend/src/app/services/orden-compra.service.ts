import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  OrdenCompra,
  OrdenCompraFiltro,
  OrdenCompraFormValue
} from '../core/models/orden-compra.model';

@Injectable({ providedIn: 'root' })
export class OrdenCompraService {
  private readonly apiUrl = `${environment.apiUrl}/ordenes-compra`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: OrdenCompraFiltro): Observable<ApiResponse<PagedResult<OrdenCompra>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));

    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId);
    if (filtro.solicitudCompraId) params = params.set('solicitudCompraId', filtro.solicitudCompraId);
    if (filtro.numero?.trim()) params = params.set('numero', filtro.numero.trim());
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.search?.trim()) params = params.set('search', filtro.search.trim());
    if (filtro.sortBy?.trim()) params = params.set('sortBy', filtro.sortBy.trim());
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);

    return this.http.get<ApiResponse<PagedResult<OrdenCompra>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<OrdenCompra>> {
    return this.http.get<ApiResponse<OrdenCompra>>(`${this.apiUrl}/${id}`);
  }

  create(value: OrdenCompraFormValue, idempotencyKey = this.generarIdempotencyKey()): Observable<ApiResponse<OrdenCompra>> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<ApiResponse<OrdenCompra>>(this.apiUrl, value, { headers });
  }

  update(id: number, value: OrdenCompraFormValue): Observable<ApiResponse<OrdenCompra>> {
    return this.http.put<ApiResponse<OrdenCompra>>(`${this.apiUrl}/${id}`, value);
  }

  enviarAprobacion(id: number): Observable<ApiResponse<OrdenCompra>> {
    return this.http.post<ApiResponse<OrdenCompra>>(`${this.apiUrl}/${id}/enviar-aprobacion`, {});
  }

  aprobar(id: number): Observable<ApiResponse<OrdenCompra>> {
    return this.http.post<ApiResponse<OrdenCompra>>(`${this.apiUrl}/${id}/aprobar`, {});
  }

  cancelar(id: number, motivo: string): Observable<ApiResponse<OrdenCompra>> {
    return this.http.post<ApiResponse<OrdenCompra>>(`${this.apiUrl}/${id}/cancelar`, { motivo: motivo.trim() });
  }

  private generarIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `orden-compra:${uuid}` : `orden-compra:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  }
}
