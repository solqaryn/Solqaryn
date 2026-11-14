import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { ActualizarEmpresaConfiguracionValue, EmpresaConfiguracion } from '../core/models/empresa-configuracion.model';
import type { TiendaIdentidadPublica } from '../features/storefront/storefront.models';

export interface WhatsAppPublico {
  numeroTelefonoE164: string | null;
  disponible: boolean;
}

export interface PlantillaCorreoEmpresa {
  tipoPlantilla: string;
  asunto: string;
  cuerpo: string;
  activa: boolean;
}

export interface ConfigEmpresaTenant {
  empresaId: number;
  nombre: string;
  rtn?: string | null;
  direccion?: string | null;
  logoUrl?: string | null;
  moneda: string;
  zonaHoraria: string;
  impuestosJson: string;
  emisionJson: string;
  correoRemitente?: string | null;
  correoNombreRemitente?: string | null;
  correoConfigurado: boolean;
  version: number;
  plantillasCorreo: PlantillaCorreoEmpresa[];
}

export interface ActualizarConfigEmpresaTenant {
  nombre: string;
  rtn?: string | null;
  direccion?: string | null;
  moneda: string;
  zonaHoraria: string;
  impuestosJson: string;
  emisionJson: string;
  correoRemitente?: string | null;
  correoNombreRemitente?: string | null;
  version: number;
}

export interface ActualizarPlantillaCorreoEmpresa {
  asunto: string;
  cuerpo: string;
  activa: boolean;
  version: number;
}

@Injectable({ providedIn: 'root' })
export class EmpresaConfiguracionService {
  private readonly apiUrl = `${environment.apiUrl}/empresa-configuracion`;

  constructor(private http: HttpClient) {}

  get(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.get<ApiResponse<EmpresaConfiguracion>>(this.apiUrl);
  }

  getPublica(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.get<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/publica`);
  }

  /** Identidad pública exclusivamente por vinculación Host+StoreKey verificada en Render. */
  getPublicaTienda(storeKey?: string): Observable<ApiResponse<TiendaIdentidadPublica>> {
    const key = storeKey === undefined ? '' : storeKey.trim();
    if (storeKey !== undefined && !/^[a-z][a-z0-9-]{0,46}[a-z0-9]$/.test(key))
      throw new Error('Clave de tienda no válida.');
    const path = storeKey === undefined
      ? `${this.apiUrl.replace(/\/empresa-configuracion$/, '')}/tienda/configuracion`
      : `${this.apiUrl.replace(/\/empresa-configuracion$/, '')}/tiendas/${encodeURIComponent(key)}/tienda/configuracion`;
    return this.http.get<ApiResponse<TiendaIdentidadPublica>>(path, {
      headers: { 'Cache-Control': 'no-store' }
    });
  }

  getWhatsAppPublico(): Observable<WhatsAppPublico> {
    return this.http.get<WhatsAppPublico>(`${environment.apiUrl}/whatsapp/publico`);
  }

  update(valor: ActualizarEmpresaConfiguracionValue): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.put<ApiResponse<EmpresaConfiguracion>>(this.apiUrl, valor);
  }

  updateLogo(file: File): Observable<ApiResponse<EmpresaConfiguracion>> {
    const formData = new FormData();
    formData.append('logo', file);
    return this.http.post<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/logo`, formData);
  }

  restaurarLogo(): Observable<ApiResponse<EmpresaConfiguracion>> {
    return this.http.delete<ApiResponse<EmpresaConfiguracion>>(`${this.apiUrl}/logo`);
  }

  getTenant(empresaId: number): Observable<ApiResponse<ConfigEmpresaTenant>> {
    return this.http.get<ApiResponse<ConfigEmpresaTenant>>(`${this.apiUrl}/tenant/${empresaId}`);
  }

  updateTenant(empresaId: number, valor: ActualizarConfigEmpresaTenant): Observable<ApiResponse<ConfigEmpresaTenant>> {
    return this.http.put<ApiResponse<ConfigEmpresaTenant>>(`${this.apiUrl}/tenant/${empresaId}`, valor);
  }

  updateTenantLogo(empresaId: number, file: File): Observable<ApiResponse<ConfigEmpresaTenant>> {
    const formData = new FormData();
    formData.append('logo', file);
    return this.http.post<ApiResponse<ConfigEmpresaTenant>>(`${this.apiUrl}/tenant/${empresaId}/logo`, formData);
  }

  restaurarTenantLogo(empresaId: number): Observable<ApiResponse<ConfigEmpresaTenant>> {
    return this.http.delete<ApiResponse<ConfigEmpresaTenant>>(`${this.apiUrl}/tenant/${empresaId}/logo`);
  }

  upsertTenantPlantilla(
    empresaId: number,
    tipoPlantilla: string,
    valor: ActualizarPlantillaCorreoEmpresa
  ): Observable<ApiResponse<ConfigEmpresaTenant>> {
    const tipo = encodeURIComponent(tipoPlantilla.trim());
    return this.http.put<ApiResponse<ConfigEmpresaTenant>>(
      `${this.apiUrl}/tenant/${empresaId}/plantillas/${tipo}`,
      valor
    );
  }

  desactivarTenantPlantilla(
    empresaId: number,
    tipoPlantilla: string,
    version: number
  ): Observable<ApiResponse<ConfigEmpresaTenant>> {
    const tipo = encodeURIComponent(tipoPlantilla.trim());
    return this.http.delete<ApiResponse<ConfigEmpresaTenant>>(
      `${this.apiUrl}/tenant/${empresaId}/plantillas/${tipo}?version=${encodeURIComponent(String(version))}`
    );
  }
}
