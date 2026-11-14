import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { CostoEnvio, GuardarCostoEnvio } from '../core/models/costo-envio.model';

@Injectable({ providedIn: 'root' })
export class CostoEnvioService {
  private readonly apiUrl = `${environment.apiUrl}/costos-envio`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<CostoEnvio[]>> {
    return this.http.get<ApiResponse<CostoEnvio[]>>(this.apiUrl);
  }

  getPredeterminado(): Observable<ApiResponse<CostoEnvio>> {
    return this.http.get<ApiResponse<CostoEnvio>>(`${this.apiUrl}/predeterminado`);
  }

  getById(id: number): Observable<ApiResponse<CostoEnvio>> {
    return this.http.get<ApiResponse<CostoEnvio>>(`${this.apiUrl}/${id}`);
  }

  create(value: GuardarCostoEnvio): Observable<ApiResponse<CostoEnvio>> {
    return this.http.post<ApiResponse<CostoEnvio>>(this.apiUrl, value);
  }

  update(id: number, value: GuardarCostoEnvio): Observable<ApiResponse<CostoEnvio>> {
    return this.http.put<ApiResponse<CostoEnvio>>(`${this.apiUrl}/${id}`, value);
  }

  cambiarEstado(id: number, activo: boolean): Observable<ApiResponse<object>> {
    return this.http.patch<ApiResponse<object>>(`${this.apiUrl}/${id}/estado`, { activo });
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
