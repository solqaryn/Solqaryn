import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  AccionMasivaPreview,
  AutocompletadoItem,
  AutomatizacionConfiguracion,
  AutomatizacionResumen
} from '../core/models/automatizacion.model';

@Injectable({ providedIn: 'root' })
export class AutomatizacionService {
  private readonly apiUrl = `${environment.apiUrl}/automatizaciones`;

  constructor(private readonly http: HttpClient) {}

  getConfiguracion(): Observable<ApiResponse<AutomatizacionConfiguracion>> {
    return this.http.get<ApiResponse<AutomatizacionConfiguracion>>(`${this.apiUrl}/configuracion`);
  }

  updateConfiguracion(config: Omit<AutomatizacionConfiguracion, 'versionReglas' | 'fechaActualizacion' | 'actualizadoPor'>): Observable<ApiResponse<AutomatizacionConfiguracion>> {
    return this.http.put<ApiResponse<AutomatizacionConfiguracion>>(`${this.apiUrl}/configuracion`, config);
  }

  getSugerencias(): Observable<ApiResponse<AutomatizacionResumen>> {
    return this.http.get<ApiResponse<AutomatizacionResumen>>(`${this.apiUrl}/sugerencias`);
  }

  autocompletar(contexto: string, q: string): Observable<ApiResponse<AutocompletadoItem[]>> {
    const params = new HttpParams().set('contexto', contexto).set('q', q);
    return this.http.get<ApiResponse<AutocompletadoItem[]>>(`${this.apiUrl}/autocompletar`, { params });
  }

  previsualizarAccionMasiva(accion: string, ids: number[]): Observable<ApiResponse<AccionMasivaPreview>> {
    return this.http.post<ApiResponse<AccionMasivaPreview>>(`${this.apiUrl}/acciones-masivas/previsualizar`, { accion, ids });
  }
}
