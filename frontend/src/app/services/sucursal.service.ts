import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Sucursal, SucursalFiltro, SucursalFormValue, SucursalPagina } from '../core/models/sucursal.model';

@Injectable({ providedIn: 'root' })
export class SucursalService {
  private readonly apiUrl = `${environment.apiUrl}/sucursales`;

  constructor(private http: HttpClient) {}

  buscar(filtro: SucursalFiltro): Observable<ApiResponse<SucursalPagina>> {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanoPagina', filtro.tamanoPagina);

    if (filtro.buscar?.trim()) params = params.set('buscar', filtro.buscar.trim());
    if (filtro.activa !== undefined) params = params.set('activa', filtro.activa);
    if (filtro.empresaId !== undefined) params = params.set('empresaId', filtro.empresaId);

    return this.http.get<ApiResponse<SucursalPagina>>(this.apiUrl, { params });
  }

  getActivas(empresaId?: number): Observable<ApiResponse<Sucursal[]>> {
    const params = empresaId ? new HttpParams().set('empresaId', empresaId) : undefined;
    return this.http.get<ApiResponse<Sucursal[]>>(`${this.apiUrl}/activas`, { params });
  }

  getById(id: number): Observable<ApiResponse<Sucursal>> {
    return this.http.get<ApiResponse<Sucursal>>(`${this.apiUrl}/${id}`);
  }

  create(value: SucursalFormValue): Observable<ApiResponse<Sucursal>> {
    return this.http.post<ApiResponse<Sucursal>>(this.apiUrl, value);
  }

  update(id: number, value: SucursalFormValue): Observable<ApiResponse<Sucursal>> {
    return this.http.put<ApiResponse<Sucursal>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<Sucursal>> {
    return this.http.patch<ApiResponse<Sucursal>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Sucursal>> {
    return this.http.patch<ApiResponse<Sucursal>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
