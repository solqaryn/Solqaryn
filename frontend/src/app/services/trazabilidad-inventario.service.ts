import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  ConfiguracionTrazabilidadVariante,
  ConfigurarTrazabilidadVarianteRequest,
  CrearLoteInventarioRequest,
  CrearSerieInventarioRequest,
  LoteInventario,
  SerieInventario
} from '../core/models/trazabilidad-inventario.model';

@Injectable({ providedIn: 'root' })
export class TrazabilidadInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/trazabilidad-inventario`;

  constructor(private http: HttpClient) {}

  getConfiguracion(productoVarianteId: number): Observable<ApiResponse<ConfiguracionTrazabilidadVariante>> {
    return this.http.get<ApiResponse<ConfiguracionTrazabilidadVariante>>(`${this.apiUrl}/variantes/${productoVarianteId}/configuracion`);
  }

  configurar(productoVarianteId: number, request: ConfigurarTrazabilidadVarianteRequest): Observable<ApiResponse<ConfiguracionTrazabilidadVariante>> {
    return this.http.put<ApiResponse<ConfiguracionTrazabilidadVariante>>(`${this.apiUrl}/variantes/${productoVarianteId}/configuracion`, request);
  }

  getLotes(productoVarianteId: number): Observable<ApiResponse<PagedResult<LoteInventario>>> {
    const params = new HttpParams().set('page', 1).set('pageSize', 100).set('productoVarianteId', productoVarianteId);
    return this.http.get<ApiResponse<PagedResult<LoteInventario>>>(`${this.apiUrl}/lotes`, { params });
  }

  crearLote(request: CrearLoteInventarioRequest): Observable<ApiResponse<LoteInventario>> {
    return this.http.post<ApiResponse<LoteInventario>>(`${this.apiUrl}/lotes`, request);
  }

  desactivarLote(id: number): Observable<ApiResponse<LoteInventario>> {
    return this.http.post<ApiResponse<LoteInventario>>(`${this.apiUrl}/lotes/${id}/desactivar`, {});
  }

  getSeries(productoVarianteId: number): Observable<ApiResponse<PagedResult<SerieInventario>>> {
    const params = new HttpParams().set('page', 1).set('pageSize', 100).set('productoVarianteId', productoVarianteId);
    return this.http.get<ApiResponse<PagedResult<SerieInventario>>>(`${this.apiUrl}/series`, { params });
  }

  crearSerie(request: CrearSerieInventarioRequest): Observable<ApiResponse<SerieInventario>> {
    return this.http.post<ApiResponse<SerieInventario>>(`${this.apiUrl}/series`, request);
  }

  darDeBajaSerie(id: number): Observable<ApiResponse<SerieInventario>> {
    return this.http.post<ApiResponse<SerieInventario>>(`${this.apiUrl}/series/${id}/baja`, {});
  }
}
