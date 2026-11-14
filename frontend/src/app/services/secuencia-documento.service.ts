import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  ReservarSecuenciaDocumentoRequest,
  SecuenciaDocumentoConsulta,
  SecuenciaDocumentoSiguiente
} from '../core/models/secuencia-documento.model';

@Injectable({ providedIn: 'root' })
export class SecuenciaDocumentoService {
  private readonly apiUrl = `${environment.apiUrl}/secuencias-documento`;

  constructor(private readonly http: HttpClient) {}

  obtener(empresaId: number, sucursalId: number | null, tipoDocumento: string): Observable<ApiResponse<SecuenciaDocumentoConsulta>> {
    let params = new HttpParams()
      .set('empresaId', empresaId)
      .set('tipoDocumento', tipoDocumento.trim());

    if (sucursalId !== null) {
      params = params.set('sucursalId', sucursalId);
    }

    return this.http.get<ApiResponse<SecuenciaDocumentoConsulta>>(this.apiUrl, { params });
  }

  reservarSiguiente(request: ReservarSecuenciaDocumentoRequest): Observable<ApiResponse<SecuenciaDocumentoSiguiente>> {
    return this.http.post<ApiResponse<SecuenciaDocumentoSiguiente>>(`${this.apiUrl}/siguiente`, request);
  }
}
