import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface ReporteInventarioValorizacionResumenDto {
  valorInventarioCosto: number | null;
  valorInventarioCostoMercaderia: number | null;
  valorInventarioCostoInsumosAdministrativos: number | null;
  valorPotencialVentaMercaderia: number | null;
}

@Injectable({ providedIn: 'root' })
export class ReporteInventarioValorizacionService {
  private readonly endpoint = `${environment.apiUrl}/inventario/reportes/valorizacion/resumen`;

  constructor(private readonly http: HttpClient) {}

  getResumen(): Observable<ApiResponse<ReporteInventarioValorizacionResumenDto>> {
    return this.http.get<ApiResponse<ReporteInventarioValorizacionResumenDto>>(this.endpoint);
  }
}
