import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { MovimientoInventario } from '../core/models/movimiento-inventario.model';

export interface MovimientoInventarioQuery {
  page: number;
  pageSize: number;
  productoId?: number;
  productoVarianteId?: number;
  almacenId?: number;
  ubicacionAlmacenId?: number;
  tipo?: string;
  causa?: string;
  correlationId?: string;
  origenTipo?: string;
  origenId?: number;
  desde?: string;
  hasta?: string;
}

@Injectable({ providedIn: 'root' })
export class MovimientoInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/inventario/movimientos`;

  constructor(private http: HttpClient) {}

  getFiltered(productoId?: number, tipo?: string): Observable<ApiResponse<MovimientoInventario[]>> {
    let params = new HttpParams();
    if (productoId) params = params.set('productoId', productoId);
    if (tipo) params = params.set('tipo', tipo);
    return this.http.get<ApiResponse<MovimientoInventario[]>>(this.apiUrl, { params });
  }

  getPaged(query: MovimientoInventarioQuery): Observable<ApiResponse<PagedResult<MovimientoInventario>>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    params = this.setNumber(params, 'productoId', query.productoId);
    params = this.setNumber(params, 'productoVarianteId', query.productoVarianteId);
    params = this.setNumber(params, 'almacenId', query.almacenId);
    params = this.setNumber(params, 'ubicacionAlmacenId', query.ubicacionAlmacenId);
    params = this.setText(params, 'tipo', query.tipo);
    params = this.setText(params, 'causa', query.causa);
    params = this.setText(params, 'correlationId', query.correlationId);
    params = this.setText(params, 'origenTipo', query.origenTipo);
    params = this.setNumber(params, 'origenId', query.origenId);
    params = this.setText(params, 'desde', query.desde);
    params = this.setText(params, 'hasta', query.hasta);

    return this.http.get<ApiResponse<PagedResult<MovimientoInventario>>>(`${this.apiUrl}/paged`, { params });
  }

  private setNumber(params: HttpParams, key: string, value?: number): HttpParams {
    return value !== undefined && value !== null && Number.isFinite(value)
      ? params.set(key, value)
      : params;
  }

  private setText(params: HttpParams, key: string, value?: string): HttpParams {
    const normalized = value?.trim();
    return normalized ? params.set(key, normalized) : params;
  }
}
