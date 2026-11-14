import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { MisPermisos, PermisoMatrizItem, PermisoCatalogo, CrearPermisoValue } from '../core/models/permiso.model';

@Injectable({ providedIn: 'root' })
export class PermisoService {
  private readonly apiUrl = `${environment.apiUrl}/permisos`;

  constructor(private http: HttpClient) {}

  /** La matriz ahora es por rol dinámico (rolId), no un endpoint fijo para "Vendedor". */
  getMatriz(rolId: number): Observable<ApiResponse<PermisoMatrizItem[]>> {
    return this.http.get<ApiResponse<PermisoMatrizItem[]>>(`${this.apiUrl}/matriz/${rolId}`);
  }

  updateMatriz(rolId: number, permisos: PermisoMatrizItem[]): Observable<ApiResponse<PermisoMatrizItem[]>> {
    return this.http.put<ApiResponse<PermisoMatrizItem[]>>(`${this.apiUrl}/matriz/${rolId}`, { permisos });
  }

  getMisPermisos(empresaId: number): Observable<ApiResponse<MisPermisos>> {
    return this.http.get<ApiResponse<MisPermisos>>(`${this.apiUrl}/mis-permisos/empresa/${empresaId}`);
  }

  // ---- Catálogo de permisos (sección 5) ----

  getCatalogo(incluirEliminados = false): Observable<ApiResponse<PermisoCatalogo[]>> {
    return this.http.get<ApiResponse<PermisoCatalogo[]>>(`${this.apiUrl}/catalogo`, {
      params: { incluirEliminados }
    });
  }

  crearPermiso(valor: CrearPermisoValue): Observable<ApiResponse<PermisoCatalogo>> {
    return this.http.post<ApiResponse<PermisoCatalogo>>(`${this.apiUrl}/catalogo`, valor);
  }

  actualizarPermiso(id: number, valor: { nombre: string; descripcion?: string }): Observable<ApiResponse<PermisoCatalogo>> {
    return this.http.put<ApiResponse<PermisoCatalogo>>(`${this.apiUrl}/catalogo/${id}`, valor);
  }

  activarPermiso(id: number): Observable<ApiResponse<PermisoCatalogo>> {
    return this.http.patch<ApiResponse<PermisoCatalogo>>(`${this.apiUrl}/catalogo/${id}/activar`, {});
  }

  desactivarPermiso(id: number): Observable<ApiResponse<PermisoCatalogo>> {
    return this.http.patch<ApiResponse<PermisoCatalogo>>(`${this.apiUrl}/catalogo/${id}/desactivar`, {});
  }

  eliminarPermiso(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/catalogo/${id}`);
  }

  eliminarPermisoPermanente(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/catalogo/${id}/permanente`);
  }
}
