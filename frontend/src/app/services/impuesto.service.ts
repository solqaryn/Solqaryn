import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { Impuesto, GuardarImpuestoValue } from '../core/models/impuesto.model';

@Injectable({ providedIn: 'root' })
export class ImpuestoService {
  private readonly apiUrl = `${environment.apiUrl}/impuestos`;
  constructor(private http: HttpClient) {}

  getAll(incluirEliminados = false): Observable<ApiResponse<Impuesto[]>> {
    return this.http.get<ApiResponse<Impuesto[]>>(this.apiUrl, { params: { incluirEliminados } });
  }
  getById(id: number): Observable<ApiResponse<Impuesto>> {
    return this.http.get<ApiResponse<Impuesto>>(`${this.apiUrl}/${id}`);
  }
  create(valor: GuardarImpuestoValue): Observable<ApiResponse<Impuesto>> {
    return this.http.post<ApiResponse<Impuesto>>(this.apiUrl, valor);
  }
  update(id: number, valor: GuardarImpuestoValue): Observable<ApiResponse<Impuesto>> {
    return this.http.put<ApiResponse<Impuesto>>(`${this.apiUrl}/${id}`, valor);
  }
  activar(id: number): Observable<ApiResponse<Impuesto>> {
    return this.http.patch<ApiResponse<Impuesto>>(`${this.apiUrl}/${id}/activar`, {});
  }
  desactivar(id: number): Observable<ApiResponse<Impuesto>> {
    return this.http.patch<ApiResponse<Impuesto>>(`${this.apiUrl}/${id}/desactivar`, {});
  }
  eliminarLogico(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }
  eliminarPermanente(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}/permanente`);
  }
  duplicar(id: number, nuevoNombre: string, nuevoCodigo: string): Observable<ApiResponse<Impuesto>> {
    return this.http.post<ApiResponse<Impuesto>>(`${this.apiUrl}/${id}/duplicar`, { nuevoNombre, nuevoCodigo });
  }
}
