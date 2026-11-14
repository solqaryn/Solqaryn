import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { CreateMovimientoManualValue, CreateRevisionValue, FinanzasResumen, MovimientoFinanciero, RevisionFinanciera } from '../core/models/finanzas.model';

@Injectable({ providedIn: 'root' })
export class FinanzasService {
  private readonly apiUrl = `${environment.apiUrl}/finanzas`;

  constructor(private http: HttpClient) {}

  getResumen(): Observable<ApiResponse<FinanzasResumen>> {
    return this.http.get<ApiResponse<FinanzasResumen>>(`${this.apiUrl}/resumen`);
  }

  getMovimientos(desde?: string, hasta?: string): Observable<ApiResponse<MovimientoFinanciero[]>> {
    let params = new HttpParams();
    if (desde) params = params.set('desde', desde);
    if (hasta) params = params.set('hasta', hasta);
    return this.http.get<ApiResponse<MovimientoFinanciero[]>>(`${this.apiUrl}/movimientos`, { params });
  }

  registrarManual(value: CreateMovimientoManualValue): Observable<ApiResponse<MovimientoFinanciero>> {
    return this.http.post<ApiResponse<MovimientoFinanciero>>(`${this.apiUrl}/movimientos/manual`, value);
  }

  anularMovimiento(id: number, motivoAnulacion: string): Observable<ApiResponse<MovimientoFinanciero>> {
    return this.http.post<ApiResponse<MovimientoFinanciero>>(`${this.apiUrl}/movimientos/${id}/anular`, { motivoAnulacion });
  }

  getRevisiones(): Observable<ApiResponse<RevisionFinanciera[]>> {
    return this.http.get<ApiResponse<RevisionFinanciera[]>>(`${this.apiUrl}/revisiones`);
  }

  registrarRevision(value: CreateRevisionValue): Observable<ApiResponse<RevisionFinanciera>> {
    return this.http.post<ApiResponse<RevisionFinanciera>>(`${this.apiUrl}/revisiones`, value);
  }
}
