import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';

export interface CreditoCliente {
  id: number;
  clienteId: number;
  moneda: string;
  limiteCredito: number;
  diasCredito: number;
  umbralAlertaPorcentaje: number | null;
  bloqueadoAutomaticamente: boolean;
  motivoBloqueo: string | null;
  bloqueadoUtc: string | null;
  montoExcepcion: number | null;
  excepcionVigenteHastaUtc: string | null;
  excepcionAutorizadaPor: string | null;
  excepcionAutorizadaUtc: string | null;
}

export interface CreditoClientePolitica {
  moneda: string;
  limiteCredito: number;
  diasCredito: number;
  umbralAlertaPorcentaje: number | null;
}

@Injectable({ providedIn: 'root' })
export class CreditoClienteService {
  private readonly apiUrl = `${environment.apiUrl}/creditos-clientes`;

  constructor(private http: HttpClient) {}

  getByCliente(clienteId: number): Observable<ApiResponse<CreditoCliente[]>> {
    return this.http.get<ApiResponse<CreditoCliente[]>>(`${this.apiUrl}/cliente/${clienteId}`);
  }

  crear(clienteId: number, value: CreditoClientePolitica): Observable<ApiResponse<CreditoCliente>> {
    return this.http.post<ApiResponse<CreditoCliente>>(this.apiUrl, { clienteId, ...value });
  }

  actualizar(id: number, value: CreditoClientePolitica): Observable<ApiResponse<CreditoCliente>> {
    return this.http.put<ApiResponse<CreditoCliente>>(`${this.apiUrl}/${id}`, value);
  }

  bloquear(id: number, motivo: string): Observable<ApiResponse<CreditoCliente>> {
    return this.http.post<ApiResponse<CreditoCliente>>(`${this.apiUrl}/${id}/bloqueo-automatico`, { motivo });
  }

  liberarBloqueo(id: number): Observable<ApiResponse<CreditoCliente>> {
    return this.http.delete<ApiResponse<CreditoCliente>>(`${this.apiUrl}/${id}/bloqueo-automatico`);
  }

  autorizarExcepcion(id: number, monto: number, vigenteHastaUtc: string): Observable<ApiResponse<CreditoCliente>> {
    return this.http.post<ApiResponse<CreditoCliente>>(`${this.apiUrl}/${id}/excepcion`, { monto, vigenteHastaUtc });
  }

  revocarExcepcion(id: number): Observable<ApiResponse<CreditoCliente>> {
    return this.http.delete<ApiResponse<CreditoCliente>>(`${this.apiUrl}/${id}/excepcion`);
  }
}
