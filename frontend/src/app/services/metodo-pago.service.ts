import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { BancoLookup, MetodoPago, MetodoPagoCreate, MetodoPagoUpdate, ReordenarMetodoPago } from '../core/models/metodo-pago.model';

@Injectable({ providedIn: 'root' })
export class MetodoPagoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/metodos-pago`;

  getAll(): Observable<ApiResponse<MetodoPago[]>> {
    return this.http.get<ApiResponse<MetodoPago[]>>(this.url);
  }

  getActivos(): Observable<ApiResponse<MetodoPago[]>> {
    return this.http.get<ApiResponse<MetodoPago[]>>(`${this.url}/activos`);
  }

  getBancosActivos(): Observable<ApiResponse<BancoLookup[]>> {
    return this.http.get<ApiResponse<BancoLookup[]>>(`${this.url}/bancos-activos`);
  }

  getById(id: number): Observable<ApiResponse<MetodoPago>> {
    return this.http.get<ApiResponse<MetodoPago>>(`${this.url}/${id}`);
  }

  create(value: MetodoPagoCreate): Observable<ApiResponse<MetodoPago>> {
    return this.http.post<ApiResponse<MetodoPago>>(this.url, value);
  }

  update(id: number, value: MetodoPagoUpdate): Observable<ApiResponse<MetodoPago>> {
    return this.http.put<ApiResponse<MetodoPago>>(`${this.url}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<MetodoPago>> {
    return this.http.patch<ApiResponse<MetodoPago>>(`${this.url}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<MetodoPago>> {
    return this.http.patch<ApiResponse<MetodoPago>>(`${this.url}/${id}/desactivar`, {});
  }

  reordenar(items: ReordenarMetodoPago[]): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.url}/orden`, items);
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.url}/${id}`);
  }
}
