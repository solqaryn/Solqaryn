import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  AuditoriaResumen,
  ResumenAdministrativo,
  RolPermisosReporte,
  UsuarioAccesoReporte
} from '../core/models/reporte-administrativo.model';

@Injectable({ providedIn: 'root' })
export class ReporteAdministrativoService {
  private readonly apiUrl = `${environment.apiUrl}/reportes-administrativos`;

  constructor(private readonly http: HttpClient) {}

  getResumen(desde?: string, hasta?: string): Observable<ApiResponse<ResumenAdministrativo>> {
    return this.http.get<ApiResponse<ResumenAdministrativo>>(`${this.apiUrl}/resumen`, {
      params: this.periodo(desde, hasta)
    });
  }

  getUsuariosAccesos(): Observable<ApiResponse<UsuarioAccesoReporte[]>> {
    return this.http.get<ApiResponse<UsuarioAccesoReporte[]>>(`${this.apiUrl}/usuarios-accesos`);
  }

  getRolesPermisos(): Observable<ApiResponse<RolPermisosReporte[]>> {
    return this.http.get<ApiResponse<RolPermisosReporte[]>>(`${this.apiUrl}/roles-permisos`);
  }

  getAuditoriaResumen(desde?: string, hasta?: string): Observable<ApiResponse<AuditoriaResumen>> {
    return this.http.get<ApiResponse<AuditoriaResumen>>(`${this.apiUrl}/auditoria-resumen`, {
      params: this.periodo(desde, hasta)
    });
  }

  exportar(tipo: 'usuarios' | 'roles' | 'auditoria', formato: 'csv' | 'xlsx' | 'pdf', desde?: string, hasta?: string): Observable<HttpResponse<Blob>> {
    let params = this.periodo(desde, hasta).set('formato', formato);
    return this.http.get(`${this.apiUrl}/exportar/${tipo}`, {
      params,
      responseType: 'blob',
      observe: 'response'
    });
  }

  private periodo(desde?: string, hasta?: string): HttpParams {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return params;
  }
}
