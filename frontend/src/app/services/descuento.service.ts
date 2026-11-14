import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Descuento, GuardarDescuentoValue } from '../core/models/descuento.model';

@Injectable({ providedIn: 'root' })
export class DescuentoService {
  private readonly apiUrl = `${environment.apiUrl}/descuentos`;
  constructor(private http: HttpClient) {}

  getAll(incluirEliminados = false): Observable<ApiResponse<Descuento[]>> {
    return this.http.get<ApiResponse<Descuento[]>>(this.apiUrl, { params: { incluirEliminados } });
  }
  getById(id: number): Observable<ApiResponse<Descuento>> {
    return this.http.get<ApiResponse<Descuento>>(`${this.apiUrl}/${id}`);
  }
  create(valor: GuardarDescuentoValue): Observable<ApiResponse<Descuento>> {
    return this.http.post<ApiResponse<Descuento>>(this.apiUrl, valor);
  }
  update(id: number, valor: GuardarDescuentoValue): Observable<ApiResponse<Descuento>> {
    return this.http.put<ApiResponse<Descuento>>(`${this.apiUrl}/${id}`, valor);
  }
  activar(id: number): Observable<ApiResponse<Descuento>> {
    return this.http.patch<ApiResponse<Descuento>>(`${this.apiUrl}/${id}/activar`, {});
  }
  desactivar(id: number): Observable<ApiResponse<Descuento>> {
    return this.http.patch<ApiResponse<Descuento>>(`${this.apiUrl}/${id}/desactivar`, {});
  }
  eliminarLogico(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
  eliminarPermanente(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}/permanente`);
  }
  duplicar(id: number, nuevoNombre: string): Observable<ApiResponse<Descuento>> {
    return this.http.post<ApiResponse<Descuento>>(`${this.apiUrl}/${id}/duplicar`, { nuevoNombre });
  }
}
