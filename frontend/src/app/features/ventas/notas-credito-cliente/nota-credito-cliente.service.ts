import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { CreateNotaCreditoCliente, NotaCreditoCliente } from './nota-credito-cliente.model';

@Injectable({ providedIn: 'root' })
export class NotaCreditoClienteService {
  private readonly url = `${environment.apiUrl}/notas-credito-cliente`;

  constructor(private readonly http: HttpClient) {}

  getById(id: number): Observable<ApiResponse<NotaCreditoCliente>> {
    return this.http.get<ApiResponse<NotaCreditoCliente>>(`${this.url}/${id}`);
  }

  create(dto: CreateNotaCreditoCliente): Observable<ApiResponse<NotaCreditoCliente>> {
    return this.http.post<ApiResponse<NotaCreditoCliente>>(this.url, dto);
  }
}
