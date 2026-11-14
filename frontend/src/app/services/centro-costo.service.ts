import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  CentroCosto,
  CentroCostoFiltro,
  CentroCostoPagina,
  CreateCentroCostoDto,
  TipoCentroCosto,
  UpdateCentroCostoDto
} from '../core/models/centro-costo.model';

@Injectable({ providedIn: 'root' })
export class CentroCostoService {
  private readonly apiUrl = `${environment.apiUrl}/centros-costo`;

  constructor(private readonly http: HttpClient) {}

  buscar(filtro: CentroCostoFiltro): Observable<ApiResponse<CentroCostoPagina>> {
    let params = new HttpParams()
      .set('pagina', String(filtro.pagina))
      .set('tamanoPagina', String(filtro.tamanoPagina));

    if (filtro.termino?.trim()) params = params.set('termino', filtro.termino.trim());
    if (filtro.tipo !== undefined) params = params.set('tipo', String(filtro.tipo));
    if (filtro.sucursalId !== undefined) params = params.set('sucursalId', String(filtro.sucursalId));
    if (filtro.activo !== undefined) params = params.set('activo', String(filtro.activo));

    return this.http.get<ApiResponse<CentroCostoPagina>>(this.apiUrl, { params });
  }

  getActivos(tipo?: TipoCentroCosto, sucursalId?: number): Observable<ApiResponse<CentroCosto[]>> {
    let params = new HttpParams();
    if (tipo !== undefined) params = params.set('tipo', String(tipo));
    if (sucursalId !== undefined) params = params.set('sucursalId', String(sucursalId));
    return this.http.get<ApiResponse<CentroCosto[]>>(`${this.apiUrl}/activos`, { params });
  }

  getById(id: number): Observable<ApiResponse<CentroCosto>> {
    return this.http.get<ApiResponse<CentroCosto>>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateCentroCostoDto): Observable<ApiResponse<CentroCosto>> {
    return this.http.post<ApiResponse<CentroCosto>>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateCentroCostoDto): Observable<ApiResponse<CentroCosto>> {
    return this.http.put<ApiResponse<CentroCosto>>(`${this.apiUrl}/${id}`, dto);
  }

  cambiarEstado(id: number, activo: boolean): Observable<ApiResponse<CentroCosto>> {
    const action = activo ? 'activar' : 'desactivar';
    return this.http.patch<ApiResponse<CentroCosto>>(`${this.apiUrl}/${id}/${action}`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
}
