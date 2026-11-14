import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  CreateExistenciaVariante,
  ExistenciaVariante,
  ExistenciaVarianteFiltro,
  UpdateExistenciaVarianteConfiguracion
} from '../core/models/existencia-variante.model';

@Injectable({ providedIn: 'root' })
export class ExistenciaVarianteService {
  private readonly apiUrl = `${environment.apiUrl}/existencias-variante`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: ExistenciaVarianteFiltro): Observable<ApiResponse<PagedResult<ExistenciaVariante>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.productoId) params = params.set('productoId', filtro.productoId);
    if (filtro.productoVarianteId) params = params.set('productoVarianteId', filtro.productoVarianteId);
    if (filtro.almacenId) params = params.set('almacenId', filtro.almacenId);
    if (filtro.ubicacionAlmacenId) params = params.set('ubicacionAlmacenId', filtro.ubicacionAlmacenId);
    if (filtro.soloRaizAlmacen !== undefined) params = params.set('soloRaizAlmacen', filtro.soloRaizAlmacen);
    if (filtro.stockBajo !== undefined) params = params.set('stockBajo', filtro.stockBajo);
    if (filtro.agotada !== undefined) params = params.set('agotada', filtro.agotada);
    if (filtro.sortBy) params = params.set('sortBy', filtro.sortBy);
    if (filtro.sortDirection) params = params.set('sortDirection', filtro.sortDirection);

    return this.http.get<ApiResponse<PagedResult<ExistenciaVariante>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<ExistenciaVariante>> {
    return this.http.get<ApiResponse<ExistenciaVariante>>(`${this.apiUrl}/${id}`);
  }

  create(value: CreateExistenciaVariante): Observable<ApiResponse<ExistenciaVariante>> {
    return this.http.post<ApiResponse<ExistenciaVariante>>(this.apiUrl, value);
  }

  updateConfiguracion(
    id: number,
    value: UpdateExistenciaVarianteConfiguracion
  ): Observable<ApiResponse<ExistenciaVariante>> {
    return this.http.put<ApiResponse<ExistenciaVariante>>(`${this.apiUrl}/${id}/configuracion`, value);
  }
}
