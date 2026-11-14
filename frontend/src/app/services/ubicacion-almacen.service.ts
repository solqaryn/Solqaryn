import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  TipoUbicacionAlmacenOpcion,
  UbicacionAlmacen,
  UbicacionAlmacenFiltro,
  UbicacionAlmacenFormValue,
  UbicacionAlmacenPagina
} from '../core/models/ubicacion-almacen.model';

@Injectable({ providedIn: 'root' })
export class UbicacionAlmacenService {
  private readonly apiUrl = `${environment.apiUrl}/ubicaciones-almacen`;

  constructor(private http: HttpClient) {}

  buscar(filtro: UbicacionAlmacenFiltro): Observable<ApiResponse<UbicacionAlmacenPagina>> {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanoPagina', filtro.tamanoPagina);

    if (filtro.buscar?.trim()) params = params.set('buscar', filtro.buscar.trim());
    if (filtro.almacenId !== undefined) params = params.set('almacenId', filtro.almacenId);
    if (filtro.ubicacionPadreId !== undefined) params = params.set('ubicacionPadreId', filtro.ubicacionPadreId);
    if (filtro.soloRaiz) params = params.set('soloRaiz', true);
    if (filtro.tipo?.trim()) params = params.set('tipo', filtro.tipo.trim());
    if (filtro.activa !== undefined) params = params.set('activa', filtro.activa);

    return this.http.get<ApiResponse<UbicacionAlmacenPagina>>(this.apiUrl, { params });
  }

  getActivas(almacenId?: number, ubicacionPadreId?: number): Observable<ApiResponse<UbicacionAlmacen[]>> {
    let params = new HttpParams();
    if (almacenId !== undefined) params = params.set('almacenId', almacenId);
    if (ubicacionPadreId !== undefined) params = params.set('ubicacionPadreId', ubicacionPadreId);
    return this.http.get<ApiResponse<UbicacionAlmacen[]>>(`${this.apiUrl}/activas`, { params });
  }

  getTipos(): Observable<ApiResponse<TipoUbicacionAlmacenOpcion[]>> {
    return this.http.get<ApiResponse<TipoUbicacionAlmacenOpcion[]>>(`${this.apiUrl}/tipos`);
  }

  getById(id: number): Observable<ApiResponse<UbicacionAlmacen>> {
    return this.http.get<ApiResponse<UbicacionAlmacen>>(`${this.apiUrl}/${id}`);
  }

  create(value: UbicacionAlmacenFormValue): Observable<ApiResponse<UbicacionAlmacen>> {
    return this.http.post<ApiResponse<UbicacionAlmacen>>(this.apiUrl, value);
  }

  update(id: number, value: UbicacionAlmacenFormValue): Observable<ApiResponse<UbicacionAlmacen>> {
    return this.http.put<ApiResponse<UbicacionAlmacen>>(`${this.apiUrl}/${id}`, value);
  }

  activar(id: number): Observable<ApiResponse<UbicacionAlmacen>> {
    return this.http.patch<ApiResponse<UbicacionAlmacen>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<UbicacionAlmacen>> {
    return this.http.patch<ApiResponse<UbicacionAlmacen>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
