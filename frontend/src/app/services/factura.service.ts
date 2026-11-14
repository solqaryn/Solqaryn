import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  EnlaceCompartir,
  EstadoConfiguracionSmtp,
  EstadoFactura,
  Factura,
  FacturaFormatoCodigo,
  FacturaFormatoPdf,
  HistorialEnvio,
  RegistrarFacturaPago,
  ResultadoDiagnosticoSmtp,
  ResultadoEnvioCorreo
} from '../core/models/factura.model';

@Injectable({ providedIn: 'root' })
export class FacturaService {
  private readonly apiUrl = `${environment.apiUrl}/facturas`;
  private readonly cuentasPorCobrarUrl = `${environment.apiUrl}/cuentas-por-cobrar`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Factura[]>> {
    return this.http.get<ApiResponse<Factura[]>>(this.apiUrl);
  }

  getCuentasPorCobrar(): Observable<ApiResponse<Factura[]>> {
    return this.http.get<ApiResponse<Factura[]>>(this.cuentasPorCobrarUrl);
  }

  getById(id: number): Observable<ApiResponse<Factura>> {
    return this.http.get<ApiResponse<Factura>>(`${this.apiUrl}/${id}`);
  }

  getByVenta(ventaId: number): Observable<ApiResponse<Factura>> {
    return this.http.get<ApiResponse<Factura>>(`${this.apiUrl}/venta/${ventaId}`);
  }

  registrarPago(id: number, pago: RegistrarFacturaPago): Observable<ApiResponse<Factura>> {
    return this.http.post<ApiResponse<Factura>>(`${this.apiUrl}/${id}/pagos`, pago);
  }

  anularPago(id: number, pagoId: number, motivo: string): Observable<ApiResponse<Factura>> {
    return this.http.post<ApiResponse<Factura>>(`${this.apiUrl}/${id}/pagos/${pagoId}/anular`, { motivo });
  }

  cambiarEstado(id: number, estado: EstadoFactura, motivo?: string): Observable<ApiResponse<Factura>> {
    return this.http.post<ApiResponse<Factura>>(`${this.apiUrl}/${id}/estado`, { estado, motivo });
  }

  getFormatosPdf(): Observable<ApiResponse<FacturaFormatoPdf[]>> {
    return this.http.get<ApiResponse<FacturaFormatoPdf[]>>(`${this.apiUrl}/formatos-pdf`);
  }

  getEstadoCorreo(): Observable<ApiResponse<EstadoConfiguracionSmtp>> {
    return this.http.get<ApiResponse<EstadoConfiguracionSmtp>>(`${this.apiUrl}/correo/estado`);
  }

  probarConexionCorreo(): Observable<ApiResponse<ResultadoDiagnosticoSmtp>> {
    return this.http.post<ApiResponse<ResultadoDiagnosticoSmtp>>(`${this.apiUrl}/correo/probar`, {});
  }

  descargarPdf(id: number, formato: FacturaFormatoCodigo = 'a4'): Observable<Blob> {
    const params = new HttpParams().set('formato', formato);
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { params, responseType: 'blob' });
  }

  prepararWhatsApp(id: number): Observable<ApiResponse<EnlaceCompartir>> {
    return this.http.post<ApiResponse<EnlaceCompartir>>(`${this.apiUrl}/${id}/compartir/whatsapp`, {});
  }

  revocarEnlaces(id: number): Observable<ApiResponse<{ enlacesRevocados: number }>> {
    return this.http.post<ApiResponse<{ enlacesRevocados: number }>>(`${this.apiUrl}/${id}/compartir/revocar`, {});
  }

  registrarIntentoEnvio(
    id: number,
    canal: 'WhatsApp' | 'Correo',
    destinatario: string,
    resultado = 'Iniciado',
    error?: string
  ): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.apiUrl}/${id}/compartir/registrar`, {
      canal,
      destinatario,
      resultado,
      error
    });
  }

  getHistorialEnvios(id: number): Observable<ApiResponse<HistorialEnvio[]>> {
    return this.http.get<ApiResponse<HistorialEnvio[]>>(`${this.apiUrl}/${id}/historial-envios`);
  }

  enviarPorCorreo(
    id: number,
    destinatario: string,
    idempotencyKey: string
  ): Observable<ApiResponse<ResultadoEnvioCorreo>> {
    const headers = new HttpHeaders().set('Idempotency-Key', idempotencyKey);
    return this.http.post<ApiResponse<ResultadoEnvioCorreo>>(
      `${this.apiUrl}/${id}/compartir/correo`,
      { destinatario },
      { headers }
    );
  }
}
