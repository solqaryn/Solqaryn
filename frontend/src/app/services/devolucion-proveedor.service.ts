import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  DevolucionProveedor,
  DevolucionProveedorCreateValue,
  DevolucionProveedorFiltro,
  DevolucionProveedorUpdateValue
} from '../core/models/devolucion-proveedor.model';

@Injectable({ providedIn: 'root' })
export class DevolucionProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/devoluciones-proveedor`;

  constructor(private http: HttpClient) {}

  getPaged(filtro: DevolucionProveedorFiltro): Observable<ApiResponse<PagedResult<DevolucionProveedor>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));

    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId);
    if (filtro.ordenCompraId) params = params.set('ordenCompraId', filtro.ordenCompraId);
    if (filtro.recepcionCompraId) params = params.set('recepcionCompraId', filtro.recepcionCompraId);
    if (filtro.facturaProveedorId) params = params.set('facturaProveedorId', filtro.facturaProveedorId);
    if (filtro.estado) params = params.set('estado', filtro.estado.toString());
    if (filtro.desdeUtc) params = params.set('desdeUtc', filtro.desdeUtc);
    if (filtro.hastaUtc) params = params.set('hastaUtc', filtro.hastaUtc);

    return this.http.get<ApiResponse<PagedResult<DevolucionProveedor>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<DevolucionProveedor>> {
    return this.http.get<ApiResponse<DevolucionProveedor>>(`${this.apiUrl}/${id}`);
  }

  create(value: DevolucionProveedorCreateValue, idempotencyKey = this.generarIdempotencyKey()): Observable<ApiResponse<DevolucionProveedor>> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<ApiResponse<DevolucionProveedor>>(this.apiUrl, value, { headers });
  }

  update(id: number, value: DevolucionProveedorUpdateValue): Observable<ApiResponse<DevolucionProveedor>> {
    return this.http.put<ApiResponse<DevolucionProveedor>>(`${this.apiUrl}/${id}`, value);
  }

  confirmar(id: number): Observable<ApiResponse<DevolucionProveedor>> {
    return this.http.post<ApiResponse<DevolucionProveedor>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivo: string): Observable<ApiResponse<DevolucionProveedor>> {
    return this.http.post<ApiResponse<DevolucionProveedor>>(`${this.apiUrl}/${id}/anular`, { motivo: motivo.trim() });
  }

  private generarIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `devolucion-proveedor:${uuid}` : `devolucion-proveedor:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  }
}
