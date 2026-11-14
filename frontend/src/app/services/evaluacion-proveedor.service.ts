import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { EvaluacionProveedor, EvaluacionProveedorFiltro } from '../core/models/evaluacion-proveedor.model';

@Injectable({ providedIn: 'root' })
export class EvaluacionProveedorService {
  private readonly apiUrl = `${environment.apiUrl}/evaluaciones-proveedor`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: EvaluacionProveedorFiltro): Observable<ApiResponse<PagedResult<EvaluacionProveedor>>> {
    let params = new HttpParams()
      .set('page', Math.max(1, Math.trunc(filtro.page)))
      .set('pageSize', Math.max(1, Math.min(100, Math.trunc(filtro.pageSize))));

    if (filtro.proveedorId) params = params.set('proveedorId', filtro.proveedorId.toString());
    if (filtro.ordenCompraId) params = params.set('ordenCompraId', filtro.ordenCompraId.toString());
    if (filtro.recepcionCompraId) params = params.set('recepcionCompraId', filtro.recepcionCompraId.toString());
    if (filtro.desdeUtc) params = params.set('desdeUtc', filtro.desdeUtc);
    if (filtro.hastaUtc) params = params.set('hastaUtc', filtro.hastaUtc);

    return this.http.get<ApiResponse<PagedResult<EvaluacionProveedor>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<EvaluacionProveedor>> {
    return this.http.get<ApiResponse<EvaluacionProveedor>>(`${this.apiUrl}/${id}`);
  }

  generarPorRecepcion(recepcionCompraId: number): Observable<ApiResponse<EvaluacionProveedor>> {
    return this.http.post<ApiResponse<EvaluacionProveedor>>(
      `${this.apiUrl}/recepciones/${recepcionCompraId}/generar`,
      {}
    );
  }
}
