import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Proveedor, ProveedorFormValue } from '../core/models/proveedor.model';

@Injectable({ providedIn: 'root' })
export class ProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/proveedores`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Proveedor[]>> {
    return this.http.get<ApiResponse<Proveedor[]>>(this.apiUrl);
  }

  getActivos(): Observable<ApiResponse<Proveedor[]>> {
    return this.http.get<ApiResponse<Proveedor[]>>(`${this.apiUrl}/activos`);
  }

  getById(id: number): Observable<ApiResponse<Proveedor>> {
    return this.http.get<ApiResponse<Proveedor>>(`${this.apiUrl}/${id}`);
  }

  buscar(termino: string): Observable<ApiResponse<Proveedor[]>> {
    return this.http.get<ApiResponse<Proveedor[]>>(`${this.apiUrl}/buscar`, { params: { termino } });
  }

  create(value: ProveedorFormValue): Observable<ApiResponse<Proveedor>> {
    return this.http.post<ApiResponse<Proveedor>>(this.apiUrl, value);
  }

  update(id: number, value: ProveedorFormValue): Observable<ApiResponse<Proveedor>> {
    return this.http.put<ApiResponse<Proveedor>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<Proveedor>> {
    return this.http.patch<ApiResponse<Proveedor>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Proveedor>> {
    return this.http.patch<ApiResponse<Proveedor>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
