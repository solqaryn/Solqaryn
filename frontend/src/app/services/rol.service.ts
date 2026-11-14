import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { ActualizarRolValue, CrearRolValue, Rol } from '../core/models/rol.model';

@Injectable({ providedIn: 'root' })
export class RolService {
  private readonly apiUrl = `${environment.apiUrl}/roles`;

  constructor(private http: HttpClient) {}

  getAll(incluirEliminados = false): Observable<ApiResponse<Rol[]>> {
    return this.http.get<ApiResponse<Rol[]>>(this.apiUrl, { params: { incluirEliminados } });
  }

  getById(id: number): Observable<ApiResponse<Rol>> {
    return this.http.get<ApiResponse<Rol>>(`${this.apiUrl}/${id}`);
  }

  create(valor: CrearRolValue): Observable<ApiResponse<Rol>> {
    return this.http.post<ApiResponse<Rol>>(this.apiUrl, valor);
  }

  update(id: number, valor: ActualizarRolValue): Observable<ApiResponse<Rol>> {
    return this.http.put<ApiResponse<Rol>>(`${this.apiUrl}/${id}`, valor);
  }

  activar(id: number): Observable<ApiResponse<Rol>> {
    return this.http.patch<ApiResponse<Rol>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Rol>> {
    return this.http.patch<ApiResponse<Rol>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  eliminarLogico(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  eliminarPermanente(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}/permanente`);
  }

  duplicar(id: number, nuevoNombre: string): Observable<ApiResponse<Rol>> {
    return this.http.post<ApiResponse<Rol>>(`${this.apiUrl}/${id}/duplicar`, { nuevoNombre });
  }
}
