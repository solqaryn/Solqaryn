import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedResult } from '../core/models/api-response.model';
import { AjusteInventario } from '../core/models/ajuste-inventario.model';
import {
  CapturarConteoInventarioLinea,
  ConteoInventario,
  ConteoInventarioFiltro,
  ConteoInventarioFormValue
} from '../core/models/conteo-inventario.model';

@Injectable({ providedIn: 'root' })
export class ConteoInventarioService {
  private readonly apiUrl = `${environment.apiUrl}/conteos-inventario`;

  constructor(private readonly http: HttpClient) {}

  getPaged(filtro: ConteoInventarioFiltro): Observable<ApiResponse<PagedResult<ConteoInventario>>> {
    let params = new HttpParams()
      .set('page', filtro.page)
      .set('pageSize', filtro.pageSize);

    if (filtro.search?.trim()) params = params.set('search', filtro.search.trim());
    if (filtro.almacenId !== undefined) params = params.set('almacenId', filtro.almacenId);
    if (filtro.ubicacionAlmacenId !== undefined) params = params.set('ubicacionAlmacenId', filtro.ubicacionAlmacenId);
    if (filtro.categoriaId !== undefined) params = params.set('categoriaId', filtro.categoriaId);
    if (filtro.tipo !== undefined) params = params.set('tipo', filtro.tipo);
    if (filtro.estado !== undefined) params = params.set('estado', filtro.estado);
    if (filtro.desde) params = params.set('desde', filtro.desde);
    if (filtro.hasta) params = params.set('hasta', filtro.hasta);

    return this.http.get<ApiResponse<PagedResult<ConteoInventario>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<ConteoInventario>> {
    return this.http.get<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}`);
  }

  create(value: ConteoInventarioFormValue): Observable<ApiResponse<ConteoInventario>> {
    return this.http.post<ApiResponse<ConteoInventario>>(this.apiUrl, value);
  }

  update(id: number, value: ConteoInventarioFormValue): Observable<ApiResponse<ConteoInventario>> {
    return this.http.put<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}`, value);
  }

  iniciar(id: number): Observable<ApiResponse<ConteoInventario>> {
    return this.http.post<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}/iniciar`, {});
  }

  capturar(id: number, detalleId: number, cantidadContada: number): Observable<ApiResponse<ConteoInventario>> {
    return this.http.put<ApiResponse<ConteoInventario>>(
      `${this.apiUrl}/${id}/detalles/${detalleId}/captura`,
      { cantidadContada }
    );
  }

  capturarLote(id: number, lineas: CapturarConteoInventarioLinea[]): Observable<ApiResponse<ConteoInventario>> {
    return this.http.put<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}/detalles/captura-lote`, { lineas });
  }

  cerrar(id: number): Observable<ApiResponse<ConteoInventario>> {
    return this.http.post<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}/cerrar`, {});
  }

  aprobar(id: number): Observable<ApiResponse<ConteoInventario>> {
    return this.http.post<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}/aprobar`, {});
  }

  generarAjuste(id: number): Observable<ApiResponse<AjusteInventario>> {
    return this.http.post<ApiResponse<AjusteInventario>>(`${this.apiUrl}/${id}/generar-ajuste`, {});
  }

  cancelar(id: number, motivo: string): Observable<ApiResponse<ConteoInventario>> {
    return this.http.post<ApiResponse<ConteoInventario>>(`${this.apiUrl}/${id}/cancelar`, { motivo });
  }
}
