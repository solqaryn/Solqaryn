import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { CuentaContable, CuentaContableInput } from '../models/cuenta-contable.model';

@Injectable({ providedIn: 'root' })
export class CuentaContableService {
  private readonly apiUrl = `${environment.apiUrl}/cuentas-contables`;

  constructor(private readonly http: HttpClient) {}

  getAll(): Observable<ApiResponse<CuentaContable[]>> {
    return this.http.get<ApiResponse<CuentaContable[]>>(this.apiUrl);
  }

  getById(id: number): Observable<ApiResponse<CuentaContable>> {
    return this.http.get<ApiResponse<CuentaContable>>(`${this.apiUrl}/${id}`);
  }

  create(input: CuentaContableInput): Observable<ApiResponse<CuentaContable>> {
    return this.http.post<ApiResponse<CuentaContable>>(this.apiUrl, input);
  }

  update(id: number, input: CuentaContableInput): Observable<ApiResponse<CuentaContable>> {
    return this.http.put<ApiResponse<CuentaContable>>(`${this.apiUrl}/${id}`, input);
  }
}
