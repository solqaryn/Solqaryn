import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Almacen, AlmacenFiltro, AlmacenFormValue, AlmacenPagina, TipoAlmacenOpcion } from '../core/models/almacen.model';

@Injectable({ providedIn: 'root' })
export class AlmacenService {
  private readonly apiUrl = `${environment.apiUrl}/almacenes`;

  constructor(private http: HttpClient) {}

  buscar(filtro: AlmacenFiltro): Observable<ApiResponse<AlmacenPagina>> {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanoPagina', filtro.tamanoPagina);

    if (filtro.buscar?.trim()) params = params.set('buscar', filtro.buscar.trim());
    if (filtro.activo !== undefined) params = params.set('activo', filtro.activo);
    if (filtro.sucursalId !== undefined) params = params.set('sucursalId', filtro.sucursalId);
    if (filtro.tipo?.trim()) params = params.set('tipo', filtro.tipo.trim());

    return this.http.get<ApiResponse<AlmacenPagina>>(this.apiUrl, { params });
  }

  getActivos(sucursalId?: number): Observable<ApiResponse<Almacen[]>> {
    const params = sucursalId ? new HttpParams().set('sucursalId', sucursalId) : undefined;
    return this.http.get<ApiResponse<Almacen[]>>(`${this.apiUrl}/activos`, { params });
  }

  getTipos(): Observable<ApiResponse<TipoAlmacenOpcion[]>> {
    return this.http.get<ApiResponse<TipoAlmacenOpcion[]>>(`${this.apiUrl}/tipos`);
  }

  getById(id: number): Observable<ApiResponse<Almacen>> {
    return this.http.get<ApiResponse<Almacen>>(`${this.apiUrl}/${id}`);
  }

  create(value: AlmacenFormValue): Observable<ApiResponse<Almacen>> {
    return this.http.post<ApiResponse<Almacen>>(this.apiUrl, value);
  }

  update(id: number, value: AlmacenFormValue): Observable<ApiResponse<Almacen>> {
    return this.http.put<ApiResponse<Almacen>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<Almacen>> {
    return this.http.patch<ApiResponse<Almacen>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Almacen>> {
    return this.http.patch<ApiResponse<Almacen>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
