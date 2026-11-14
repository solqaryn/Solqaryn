import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { FacturaProveedor, FacturaProveedorFormValue, FacturaProveedorFiltro } from '../core/models/factura-proveedor.model';

@Injectable({ providedIn: 'root' })
export class FacturaProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/facturas-proveedor`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: FacturaProveedorFiltro): Observable<ApiResponse<PagedResult<FacturaProveedor>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));
    if (filtro.estado) params = params.set('estado', filtro.estado.toString());
    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId.toString());
    if (filtro.ordenCompraId) params = params.set('ordenCompraId', filtro.ordenCompraId.toString());
    if (filtro.numero?.trim()) params = params.set('numero', filtro.numero.trim());
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);
    if (filtro.search?.trim()) params = params.set('search', filtro.search.trim());
    if (filtro.sortBy?.trim()) params = params.set('sortBy', filtro.sortBy.trim());
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);
    return this.http.get<ApiResponse<PagedResult<FacturaProveedor>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<FacturaProveedor>> {
    return this.http.get<ApiResponse<FacturaProveedor>>(`${this.apiUrl}/${id}`);
  }

  create(data: FacturaProveedorFormValue): Observable<ApiResponse<FacturaProveedor>> {
    return this.http.post<ApiResponse<FacturaProveedor>>(this.apiUrl, data);
  }

  update(id: number, data: FacturaProveedorFormValue): Observable<ApiResponse<FacturaProveedor>> {
    return this.http.put<ApiResponse<FacturaProveedor>>(`${this.apiUrl}/${id}`, data);
  }

  registrar(id: number): Observable<ApiResponse<FacturaProveedor>> {
    return this.http.post<ApiResponse<FacturaProveedor>>(`${this.apiUrl}/${id}/registrar`, {});
  }

  anular(id: number, motivo: string): Observable<ApiResponse<FacturaProveedor>> {
    return this.http.post<ApiResponse<FacturaProveedor>>(`${this.apiUrl}/${id}/anular`, { motivo: motivo.trim() });
  }
}
