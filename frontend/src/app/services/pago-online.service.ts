import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';

export enum EstadoPagoOnline {
  Pendiente = 1,
  Confirmado = 2,
  Fallido = 3,
  Cancelado = 4,
  Expirado = 5
}

export interface IniciarPagoOnlineRequest {
  empresaId: number;
  facturaId: number;
  proveedor: string;
  monto: number;
  moneda: string;
}

export interface PagoOnline {
  id: number;
  empresaId: number;
  facturaId: number;
  proveedor: string;
  referenciaProveedor?: string | null;
  monto: number;
  moneda: string;
  estado: EstadoPagoOnline;
  urlPago?: string | null;
  creadoUtc: string;
  confirmadoUtc?: string | null;
  ultimoError?: string | null;
}

export interface InicioPagoOnlineResultado {
  pago: PagoOnline;
  reutilizado: boolean;
}

export interface PaginaPagosOnline {
  items: PagoOnline[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

@Injectable({ providedIn: 'root' })
export class PagoOnlineService {
  private readonly apiUrl = `${environment.apiUrl}/pagos-online`;

  constructor(private readonly http: HttpClient) {}

  iniciar(
    solicitud: IniciarPagoOnlineRequest,
    idempotencyKey: string
  ): Observable<ApiResponse<InicioPagoOnlineResultado>> {
    const headers = new HttpHeaders().set('Idempotency-Key', idempotencyKey);
    return this.http.post<ApiResponse<InicioPagoOnlineResultado>>(
      `${this.apiUrl}/iniciar`,
      solicitud,
      { headers }
    );
  }

  listar(
    empresaId: number,
    facturaId: number,
    estado?: EstadoPagoOnline,
    pagina = 1,
    tamanoPagina = 25
  ): Observable<ApiResponse<PaginaPagosOnline>> {
    let params = new HttpParams()
      .set('empresaId', empresaId)
      .set('facturaId', facturaId)
      .set('pagina', pagina)
      .set('tamanoPagina', tamanoPagina);

    if (estado !== undefined) params = params.set('estado', estado);

    return this.http.get<ApiResponse<PaginaPagosOnline>>(this.apiUrl, { params });
  }
}
