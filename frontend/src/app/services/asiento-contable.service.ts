import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { AsientoContableDto, CrearAsientoContableDto } from '../core/models/asiento-contable.model';

interface AsientosPage {
  items: AsientoContableDto[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class AsientoContableService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/asientos-contables`;

  getAll(params: { desde?: string; hasta?: string; numero?: string; pagina?: number; tamano?: number }): Observable<ApiResponse<AsientosPage>> {
    let httpParams = new HttpParams();
    if (params.desde) httpParams = httpParams.set('desde', params.desde);
    if (params.hasta) httpParams = httpParams.set('hasta', params.hasta);
    if (params.numero) httpParams = httpParams.set('numero', params.numero);
    if (params.pagina != null) httpParams = httpParams.set('pagina', params.pagina.toString());
    if (params.tamano != null) httpParams = httpParams.set('tamano', params.tamano.toString());
    return this.http.get<ApiResponse<AsientosPage>>(this.apiUrl, { params: httpParams });
  }

  getById(id: number): Observable<ApiResponse<AsientoContableDto>> {
    return this.http.get<ApiResponse<AsientoContableDto>>(`${this.apiUrl}/${id}`);
  }

  create(dto: CrearAsientoContableDto): Observable<ApiResponse<AsientoContableDto>> {
    return this.http.post<ApiResponse<AsientoContableDto>>(this.apiUrl, dto);
  }
}
