import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CuentaBancaria,
  CreateCuentaBancariaDto,
  UpdateCuentaBancariaDto,
  CuentaBancariaQueryFilter
} from '../models/cuenta-bancaria';
import { CuentaBancariaPage } from '../models/cuenta-bancaria-page';

@Injectable({
  providedIn: 'root'
})
export class CuentaBancariaService {
  private readonly apiUrl = `${environment.apiUrl}/cuentas-bancarias`;

  constructor(private readonly http: HttpClient) {}

  getAll(filter?: CuentaBancariaQueryFilter): Observable<CuentaBancariaPage<CuentaBancaria>> {
    let params = new HttpParams();

    if (filter) {
      if (filter.page != null) params = params.set('page', filter.page.toString());
      if (filter.pageSize != null) params = params.set('pageSize', filter.pageSize.toString());
      if (filter.bancoId != null) params = params.set('bancoId', filter.bancoId.toString());
      if (filter.estado != null) params = params.set('estado', filter.estado.toString());
      if (filter.moneda != null) params = params.set('moneda', filter.moneda);
      if (filter.searchTerm != null) params = params.set('searchTerm', filter.searchTerm);
    }

    return this.http.get<CuentaBancariaPage<CuentaBancaria>>(this.apiUrl, { params });
  }

  getActivas(): Observable<CuentaBancaria[]> {
    return this.http.get<CuentaBancaria[]>(`${this.apiUrl}/activas`);
  }

  getById(id: number): Observable<CuentaBancaria> {
    return this.http.get<CuentaBancaria>(`${this.apiUrl}/${id}`);
  }

  create(dto: CreateCuentaBancariaDto): Observable<CuentaBancaria> {
    return this.http.post<CuentaBancaria>(this.apiUrl, dto);
  }

  update(id: number, dto: UpdateCuentaBancariaDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  activar(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/desactivar`, {});
  }
}
