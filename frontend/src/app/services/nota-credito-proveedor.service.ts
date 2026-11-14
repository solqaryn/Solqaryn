import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { CreateNotaCreditoProveedor, NotaCreditoProveedor, NotaCreditoProveedorFiltro, UpdateNotaCreditoProveedor } from '../core/models/nota-credito-proveedor.model';

@Injectable({ providedIn: 'root' })
export class NotaCreditoProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/notas-credito-proveedor`;
  constructor(private http: HttpClient) {}

  getPaged(filtro: NotaCreditoProveedorFiltro): Observable<ApiResponse<PagedResult<NotaCreditoProveedor>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));
    if (filtro.estado) params = params.set('estado', filtro.estado.toString());
    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId.toString());
    if (filtro.facturaProveedorId) params = params.set('facturaProveedorId', filtro.facturaProveedorId.toString());
    if (filtro.devolucionProveedorId) params = params.set('devolucionProveedorId', filtro.devolucionProveedorId.toString());
    if (filtro.numero?.trim()) params = params.set('numero', filtro.numero.trim());
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.search?.trim()) params = params.set('search', filtro.search.trim());
    if (filtro.sortBy?.trim()) params = params.set('sortBy', filtro.sortBy.trim());
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);
    return this.http.get<ApiResponse<PagedResult<NotaCreditoProveedor>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<NotaCreditoProveedor>> {
    return this.http.get<ApiResponse<NotaCreditoProveedor>>(`${this.apiUrl}/${id}`);
  }

  create(data: CreateNotaCreditoProveedor): Observable<ApiResponse<NotaCreditoProveedor>> {
    return this.http.post<ApiResponse<NotaCreditoProveedor>>(this.apiUrl, data);
  }

  update(id: number, data: UpdateNotaCreditoProveedor): Observable<ApiResponse<NotaCreditoProveedor>> {
    return this.http.put<ApiResponse<NotaCreditoProveedor>>(`${this.apiUrl}/${id}`, data);
  }

  registrar(id: number): Observable<ApiResponse<NotaCreditoProveedor>> {
    return this.http.post<ApiResponse<NotaCreditoProveedor>>(`${this.apiUrl}/${id}/registrar`, {});
  }

  anular(id: number, motivo: string): Observable<ApiResponse<NotaCreditoProveedor>> {
    return this.http.post<ApiResponse<NotaCreditoProveedor>>(`${this.apiUrl}/${id}/anular`, { motivo: motivo.trim() });
  }
}
