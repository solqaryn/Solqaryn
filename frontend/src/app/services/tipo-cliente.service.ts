import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { TipoCliente, TipoClienteFormValue } from '../core/models/tipo-cliente.model';

@Injectable({ providedIn: 'root' })
export class TipoClienteService {
  private readonly apiUrl = `${environment.apiUrl}/tipo-clientes`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<TipoCliente[]>> {
    return this.http.get<ApiResponse<TipoCliente[]>>(this.apiUrl);
  }

  getActivos(): Observable<ApiResponse<TipoCliente[]>> {
    return this.http.get<ApiResponse<TipoCliente[]>>(`${this.apiUrl}/activos`);
  }

  getById(id: number): Observable<ApiResponse<TipoCliente>> {
    return this.http.get<ApiResponse<TipoCliente>>(`${this.apiUrl}/${id}`);
  }

  create(value: TipoClienteFormValue): Observable<ApiResponse<TipoCliente>> {
    return this.http.post<ApiResponse<TipoCliente>>(this.apiUrl, value);
  }

  update(id: number, value: TipoClienteFormValue): Observable<ApiResponse<TipoCliente>> {
    return this.http.put<ApiResponse<TipoCliente>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<TipoCliente>> {
    return this.http.patch<ApiResponse<TipoCliente>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<TipoCliente>> {
    return this.http.patch<ApiResponse<TipoCliente>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
