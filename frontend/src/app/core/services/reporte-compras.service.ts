import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { ReporteComprasDetalleDto, ReporteComprasFiltroDto } from '../models/reporte-compras.models';

@Injectable({ providedIn: 'root' })
export class ReporteComprasService {
  private readonly endpoint = `${environment.apiUrl}/compras/reportes`;

  constructor(private readonly http: HttpClient) {}

  getDetalle(
    filtro: ReporteComprasFiltroDto,
  ): Observable<ApiResponse<PagedResult<ReporteComprasDetalleDto>>> {
    return this.http.get<ApiResponse<PagedResult<ReporteComprasDetalleDto>>>(
      `${this.endpoint}/detalle`,
      { params: this.toParams(filtro) },
    );
  }

  private toParams(filtro: ReporteComprasFiltroDto): HttpParams {
    let params = new HttpParams();
    Object.entries(filtro).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
