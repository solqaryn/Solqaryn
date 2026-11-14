import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import { PreparacionPedidoVenta } from './preparacion-pedido-venta.model';

@Injectable({ providedIn: 'root' })
export class PreparacionPedidoVentaService {
  private readonly url = `${environment.apiUrl}/preparaciones-pedido-venta`;

  constructor(private readonly http: HttpClient) {}

  getById(id: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.get<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}`);
  }

  getByPedidoVentaId(pedidoVentaId: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.get<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/pedido/${pedidoVentaId}`);
  }

  iniciar(pedidoVentaId: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/pedido/${pedidoVentaId}`, {});
  }

  completarPicking(id: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}/picking`, {});
  }

  completarPacking(id: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}/packing`, {});
  }

  despachar(id: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}/despachar`, {});
  }

  entregar(id: number): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}/entregar`, {});
  }

  cancelar(id: number, motivo: string): Observable<ApiResponse<PreparacionPedidoVenta>> {
    return this.http.post<ApiResponse<PreparacionPedidoVenta>>(`${this.url}/${id}/cancelar`, { motivo: motivo.trim() });
  }
}
