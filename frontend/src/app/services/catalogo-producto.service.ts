import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  CatalogoProducto,
  CatalogoProductoFormValue,
  TipoCatalogoProducto
} from '../core/models/catalogo-producto.model';

@Injectable({ providedIn: 'root' })
export class CatalogoProductoService {
  private readonly rutas: Record<TipoCatalogoProducto, string> = {
    Color: 'colores',
    Talla: 'tallas',
    Marca: 'marcas',
    Modelo: 'modelos'
  };

  constructor(private http: HttpClient) {}

  getAll(tipo: TipoCatalogoProducto, buscar = '', padreId?: number | null): Observable<ApiResponse<CatalogoProducto[]>> {
    let params = new HttpParams();
    if (buscar.trim()) params = params.set('buscar', buscar.trim());
    if (padreId) params = params.set(tipo === 'Modelo' ? 'marcaId' : 'padreId', padreId);
    return this.http.get<ApiResponse<CatalogoProducto[]>>(this.url(tipo), { params });
  }

  getActivos(tipo: TipoCatalogoProducto, padreId?: number | null): Observable<ApiResponse<CatalogoProducto[]>> {
    let params = new HttpParams();
    if (padreId) params = params.set(tipo === 'Modelo' ? 'marcaId' : 'padreId', padreId);
    const segmento = tipo === 'Marca' ? 'activas' : 'activos';
    return this.http.get<ApiResponse<CatalogoProducto[]>>(`${this.url(tipo)}/${segmento}`, { params });
  }

  getById(tipo: TipoCatalogoProducto, id: number): Observable<ApiResponse<CatalogoProducto>> {
    return this.http.get<ApiResponse<CatalogoProducto>>(`${this.url(tipo)}/${id}`);
  }

  create(tipo: TipoCatalogoProducto, value: CatalogoProductoFormValue): Observable<ApiResponse<CatalogoProducto>> {
    return this.http.post<ApiResponse<CatalogoProducto>>(this.url(tipo), value);
  }

  update(tipo: TipoCatalogoProducto, id: number, value: CatalogoProductoFormValue): Observable<ApiResponse<CatalogoProducto>> {
    return this.http.put<ApiResponse<CatalogoProducto>>(`${this.url(tipo)}/${id}`, value);
  }

  activar(tipo: TipoCatalogoProducto, id: number): Observable<ApiResponse<CatalogoProducto>> {
    return this.http.patch<ApiResponse<CatalogoProducto>>(`${this.url(tipo)}/${id}/activar`, {});
  }

  desactivar(tipo: TipoCatalogoProducto, id: number): Observable<ApiResponse<CatalogoProducto>> {
    return this.http.patch<ApiResponse<CatalogoProducto>>(`${this.url(tipo)}/${id}/desactivar`, {});
  }

  delete(tipo: TipoCatalogoProducto, id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.url(tipo)}/${id}`);
  }

  private url(tipo: TipoCatalogoProducto): string {
    return `${environment.apiUrl}/${this.rutas[tipo]}`;
  }
}
