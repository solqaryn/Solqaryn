import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  CrearPeriodoContableDto,
  PeriodoContable,
  PeriodoContableQuery
} from '../models/periodo-contable.model';

@Injectable({ providedIn: 'root' })
export class PeriodoContableService {
  private readonly apiUrl = `${environment.apiUrl}/periodos-contables`;

  constructor(private readonly http: HttpClient) {}

  getPaged(query: PeriodoContableQuery): Observable<ApiResponse<PagedResult<PeriodoContable>>> {
    let params = new HttpParams()
      .set('page', query.page.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.fechaDesde) params = params.set('fechaDesde', query.fechaDesde);
    if (query.fechaHasta) params = params.set('fechaHasta', query.fechaHasta);
    if (query.estado != null) params = params.set('estado', query.estado.toString());

    return this.http.get<ApiResponse<PagedResult<PeriodoContable>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<PeriodoContable>> {
    return this.http.get<ApiResponse<PeriodoContable>>(`${this.apiUrl}/${id}`);
  }

  create(dto: CrearPeriodoContableDto): Observable<ApiResponse<PeriodoContable>> {
    return this.http.post<ApiResponse<PeriodoContable>>(this.apiUrl, dto);
  }

  cerrar(id: number): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.apiUrl}/${id}/cerrar`, {});
  }
}
