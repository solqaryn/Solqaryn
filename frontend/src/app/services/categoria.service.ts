import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Categoria, CategoriaFormValue, CategoriaGaleria } from '../core/models/categoria.model';

@Injectable({ providedIn: 'root' })
export class CategoriaService {
  private readonly apiUrl = `${environment.apiUrl}/categorias`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Categoria[]>> {
    return this.http.get<ApiResponse<Categoria[]>>(this.apiUrl);
  }

  getActivas(): Observable<ApiResponse<Categoria[]>> {
    return this.http.get<ApiResponse<Categoria[]>>(`${this.apiUrl}/activas`);
  }

  getById(id: number): Observable<ApiResponse<Categoria>> {
    return this.http.get<ApiResponse<Categoria>>(`${this.apiUrl}/${id}`);
  }

  create(value: CategoriaFormValue): Observable<ApiResponse<Categoria>> {
    return this.http.post<ApiResponse<Categoria>>(this.apiUrl, value);
  }

  update(id: number, value: CategoriaFormValue): Observable<ApiResponse<Categoria>> {
    return this.http.put<ApiResponse<Categoria>>(`${this.apiUrl}/${id}`, value);
  }

  obtenerGaleria(id: number): Observable<ApiResponse<CategoriaGaleria>> {
    return this.http.get<ApiResponse<CategoriaGaleria>>(this.apiUrl + '/' + id + '/imagenes');
  }

  guardarGaleria(id: number, version: string, orden: string[], archivos: File[]): Observable<ApiResponse<CategoriaGaleria>> {
    const datos = new FormData();
    datos.append('Version', version);
    orden.forEach(item => datos.append('Orden', item));
    archivos.forEach(archivo => datos.append('Archivos', archivo, archivo.name));
    return this.http.put<ApiResponse<CategoriaGaleria>>(this.apiUrl + '/' + id + '/imagenes', datos);
  }

  activar(id: number): Observable<ApiResponse<Categoria>> {
    return this.http.patch<ApiResponse<Categoria>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Categoria>> {
    return this.http.patch<ApiResponse<Categoria>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
