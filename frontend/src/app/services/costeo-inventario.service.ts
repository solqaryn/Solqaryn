import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import {
  CambiarPoliticaCosteoInventarioRequest,
  MetodoCosteoInventarioOption,
  PoliticaCosteoInventario,
  PoliticaCosteoInventarioQuery
} from '../features/inventario/costeo-inventario.model';

@Injectable({ providedIn: 'root' })
export class CosteoInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/costeo-inventario`;

  constructor(private readonly http: HttpClient) {}

  getPoliticaVigente(): Observable<ApiResponse<PoliticaCosteoInventario>> {
    return this.http.get<ApiResponse<PoliticaCosteoInventario>>(`${this.apiUrl}/politica-vigente`);
  }

  getHistorial(query: PoliticaCosteoInventarioQuery = {}): Observable<ApiResponse<PagedResult<PoliticaCosteoInventario>>> {
    let params = new HttpParams()
      .set('page', query.page ?? 1)
      .set('pageSize', query.pageSize ?? 20);

    if (query.metodo !== undefined) params = params.set('metodo', query.metodo);
    if (query.vigente !== undefined) params = params.set('vigente', query.vigente);
    if (query.desdeUtc) params = params.set('desdeUtc', query.desdeUtc);
    if (query.hastaUtc) params = params.set('hastaUtc', query.hastaUtc);

    return this.http.get<ApiResponse<PagedResult<PoliticaCosteoInventario>>>(`${this.apiUrl}/politicas`, { params });
  }

  getMetodos(): Observable<ApiResponse<MetodoCosteoInventarioOption[]>> {
    return this.http.get<ApiResponse<MetodoCosteoInventarioOption[]>>(`${this.apiUrl}/metodos`);
  }

  cambiarPolitica(request: CambiarPoliticaCosteoInventarioRequest): Observable<ApiResponse<PoliticaCosteoInventario>> {
    return this.http.put<ApiResponse<PoliticaCosteoInventario>>(`${this.apiUrl}/politica-vigente`, request);
  }
}
