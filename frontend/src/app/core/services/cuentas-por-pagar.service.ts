import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import {
  AnularCuentaPorPagarDto,
  AplicarCuentaPorPagarDto,
  CuentaPorPagarDto,
  CuentaPorPagarFiltroDto,
  GenerarCuentaPorPagarDto,
  RevertirAplicacionCuentaPorPagarDto
} from '../models/cuenta-por-pagar.model';

@Injectable({ providedIn: 'root' })
export class CuentasPorPagarService {
  private readonly baseUrl = `${environment.apiUrl}/cuentas-por-pagar`;

  constructor(private readonly http: HttpClient) {}

  buscar(filtro: CuentaPorPagarFiltroDto): Observable<ApiResponse<PagedResult<CuentaPorPagarDto>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize)
      .set('sortDirection', filtro.sortDirection ?? 'asc');

    if (filtro.estado != null) params = params.set('estado', filtro.estado);
    if (filtro.condicionPago != null) params = params.set('condicionPago', filtro.condicionPago);
    if (filtro.proveedorId != null) params = params.set('proveedorId', filtro.proveedorId);
    if (filtro.facturaProveedorId != null) params = params.set('facturaProveedorId', filtro.facturaProveedorId);
    if (filtro.venceDesdeUtc) params = params.set('venceDesdeUtc', filtro.venceDesdeUtc);
    if (filtro.venceHastaUtc) params = params.set('venceHastaUtc', filtro.venceHastaUtc);
    if (filtro.moneda?.trim()) params = params.set('moneda', filtro.moneda.trim().toUpperCase());

    return this.http.get<ApiResponse<PagedResult<CuentaPorPagarDto>>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<CuentaPorPagarDto>> {
    return this.http.get<ApiResponse<CuentaPorPagarDto>>(`${this.baseUrl}/${id}`);
  }

  generar(dto: GenerarCuentaPorPagarDto): Observable<ApiResponse<CuentaPorPagarDto>> {
    return this.http.post<ApiResponse<CuentaPorPagarDto>>(`${this.baseUrl}/generar`, dto);
  }

  aplicar(id: number, dto: AplicarCuentaPorPagarDto): Observable<ApiResponse<CuentaPorPagarDto>> {
    return this.http.post<ApiResponse<CuentaPorPagarDto>>(`${this.baseUrl}/${id}/aplicaciones`, dto);
  }

  revertirAplicacion(id: number, dto: RevertirAplicacionCuentaPorPagarDto): Observable<ApiResponse<CuentaPorPagarDto>> {
    return this.http.post<ApiResponse<CuentaPorPagarDto>>(`${this.baseUrl}/${id}/aplicaciones/revertir`, dto);
  }

  anular(id: number, dto: AnularCuentaPorPagarDto): Observable<ApiResponse<CuentaPorPagarDto>> {
    return this.http.post<ApiResponse<CuentaPorPagarDto>>(`${this.baseUrl}/${id}/anular`, dto);
  }
}
