import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  ReporteVentasDetalleDto,
  ReporteVentasFiltroDto,
  ReporteVentasResumenDto,
} from '../models/reporte-ventas.models';

@Injectable({ providedIn: 'root' })
export class ReporteVentasService {
  private readonly endpoint = `${environment.apiUrl}/ventas/reportes`;

  constructor(private readonly http: HttpClient) {}

  getResumen(filtro: ReporteVentasFiltroDto): Observable<ApiResponse<ReporteVentasResumenDto>> {
    return this.http.get<ApiResponse<ReporteVentasResumenDto>>(`${this.endpoint}/resumen`, {
      params: this.toParams(filtro),
    });
  }

  getDetalle(
    filtro: ReporteVentasFiltroDto,
  ): Observable<ApiResponse<PagedResult<ReporteVentasDetalleDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReporteVentasDetalleDto>>>(
      `${this.endpoint}/detalle`,
      { params: this.toParams(filtro) },
    );
  }

  private toParams(filtro: ReporteVentasFiltroDto): HttpParams {
    let params = new HttpParams();
    Object.entries(filtro).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
