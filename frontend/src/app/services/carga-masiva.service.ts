import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  CargaMasiva,
  CargaMasivaConfiguracion,
  CargaMasivaDetalle,
  CargaMasivaProgreso,
  TipoCargaMasiva
} from '../core/models/carga-masiva.model';

@Injectable({ providedIn: 'root' })
export class CargaMasivaService {
  private readonly apiUrl = `${environment.apiUrl}/cargas-masivas`;

  constructor(private http: HttpClient) {}

  getConfiguracion(): Observable<ApiResponse<CargaMasivaConfiguracion>> {
    return this.http.get<ApiResponse<CargaMasivaConfiguracion>>(`${this.apiUrl}/configuracion`);
  }

  getPaged(page = 1, pageSize = 10, search = ''): Observable<ApiResponse<PagedResult<CargaMasiva>>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('sortBy', 'FechaCreacion')
      .set('sortDirection', 'desc');
    if (search.trim()) params = params.set('search', search.trim());
    return this.http.get<ApiResponse<PagedResult<CargaMasiva>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<CargaMasivaDetalle>> {
    return this.http.get<ApiResponse<CargaMasivaDetalle>>(`${this.apiUrl}/${id}`);
  }

  getProgreso(id: number): Observable<ApiResponse<CargaMasivaProgreso>> {
    return this.http.get<ApiResponse<CargaMasivaProgreso>>(`${this.apiUrl}/${id}/progreso`);
  }

  validar(tipo: TipoCargaMasiva, archivo: File): Observable<ApiResponse<CargaMasivaDetalle>> {
    const formData = new FormData();
    formData.append('tipo', tipo);
    formData.append('archivo', archivo, archivo.name);
    return this.http.post<ApiResponse<CargaMasivaDetalle>>(`${this.apiUrl}/validar`, formData);
  }

  confirmar(id: number): Observable<ApiResponse<CargaMasivaDetalle>> {
    return this.http.post<ApiResponse<CargaMasivaDetalle>>(`${this.apiUrl}/${id}/confirmar`, {});
  }

  descargarPlantilla(
    tipo: TipoCargaMasiva,
    formato: 'csv' | 'xlsx',
    version?: string
  ): Observable<Blob> {
    let params = new HttpParams().set('formato', formato);
    if (version?.trim()) params = params.set('version', version.trim());
    return this.http.get(`${this.apiUrl}/plantillas/${tipo}`, {
      params,
      responseType: 'blob'
    });
  }

  descargarErrores(id: number, formato: 'csv' | 'xlsx'): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/errores`, {
      params: { formato },
      responseType: 'blob'
    });
  }
}
