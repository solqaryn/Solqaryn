import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  CreateDevolucionCliente,
  DevolucionCliente,
  DevolucionClienteFiltro
} from '../core/models/devolucion-cliente.model';

@Injectable({ providedIn: 'root' })
export class DevolucionClienteService {
  private readonly apiUrl = `${environment.apiUrl}/devoluciones-clientes`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: DevolucionClienteFiltro): Observable<ApiResponse<PagedResult<DevolucionCliente>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));
    if (filtro.ventaId) params = params.set('ventaId', filtro.ventaId);
    if (filtro.estado) params = params.set('estado', filtro.estado.toString());
    return this.http.get<ApiResponse<PagedResult<DevolucionCliente>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<DevolucionCliente>> {
    return this.http.get<ApiResponse<DevolucionCliente>>(`${this.apiUrl}/${id}`);
  }

  create(value: CreateDevolucionCliente, idempotencyKey = this.generarIdempotencyKey()): Observable<ApiResponse<DevolucionCliente>> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<ApiResponse<DevolucionCliente>>(this.apiUrl, value, { headers });
  }

  confirmar(id: number): Observable<ApiResponse<DevolucionCliente>> {
    return this.http.post<ApiResponse<DevolucionCliente>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  anular(id: number, motivo: string): Observable<ApiResponse<DevolucionCliente>> {
    return this.http.post<ApiResponse<DevolucionCliente>>(`${this.apiUrl}/${id}/anular`, { motivo: motivo.trim() });
  }

  private generarIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `devolucion-cliente:${uuid}` : `devolucion-cliente:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  }
}
