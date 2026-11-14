import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { DashboardKpiConfiguracion, DashboardKpiResuelto, DashboardResumen } from '../core/models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly apiUrl = `${environment.apiUrl}/dashboard`;
  private readonly kpiUrl = `${environment.apiUrl}/dashboard/kpis`;

  constructor(private http: HttpClient) {}

  getResumen(): Observable<ApiResponse<DashboardResumen>> {
    return this.http.get<ApiResponse<DashboardResumen>>(`${this.apiUrl}/resumen`);
  }

  getKpiConfiguracion(): Observable<DashboardKpiConfiguracion[]> {
    return this.http.get<DashboardKpiConfiguracion[]>(`${this.kpiUrl}/configuracion`);
  }

  guardarKpiConfiguracion(configuracion: DashboardKpiConfiguracion[]): Observable<DashboardKpiConfiguracion[]> {
    return this.http.put<DashboardKpiConfiguracion[]>(`${this.kpiUrl}/configuracion`, configuracion);
  }

  getKpisResueltos(): Observable<DashboardKpiResuelto[]> {
    return this.http.get<DashboardKpiResuelto[]>(`${this.kpiUrl}/resueltos`);
  }
}
