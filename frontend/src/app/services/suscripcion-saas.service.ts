import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import {
  EntitlementModuloSaaS,
  LimiteSuscripcionSaaS,
  OnboardingSuscripcionSaaS,
  PaginaSuscripcionSaaS,
  SuscripcionSaaS
} from '../core/models/suscripcion-saas.model';

@Injectable({ providedIn: 'root' })
export class SuscripcionSaaSService {
  private readonly apiUrl = `${environment.apiUrl}/saas/tenants`;

  constructor(private readonly http: HttpClient) {}

  obtenerActual(empresaId: number): Observable<ApiResponse<SuscripcionSaaS>> {
    return this.http.get<ApiResponse<SuscripcionSaaS>>(`${this.tenantUrl(empresaId)}/suscripcion`);
  }

  obtenerLimites(
    empresaId: number,
    clave?: string,
    pagina = 1,
    tamanoPagina = 50
  ): Observable<ApiResponse<PaginaSuscripcionSaaS<LimiteSuscripcionSaaS>>> {
    let params = new HttpParams()
      .set('pagina', pagina)
      .set('tamanoPagina', tamanoPagina);
    const normalizada = clave?.trim();
    if (normalizada) params = params.set('clave', normalizada);

    return this.http.get<ApiResponse<PaginaSuscripcionSaaS<LimiteSuscripcionSaaS>>>(
      `${this.tenantUrl(empresaId)}/limites`,
      { params }
    );
  }

  obtenerEntitlementModulo(
    empresaId: number,
    moduloClave: string
  ): Observable<ApiResponse<EntitlementModuloSaaS>> {
    const clave = moduloClave.trim().toUpperCase();
    if (!clave) throw new Error('La clave del módulo es obligatoria.');

    return this.http.get<ApiResponse<EntitlementModuloSaaS>>(
      `${this.tenantUrl(empresaId)}/modulos/${encodeURIComponent(clave)}/entitlement`
    );
  }

  onboarding(
    empresaId: number,
    request: OnboardingSuscripcionSaaS,
    idempotencyKey: string
  ): Observable<ApiResponse<SuscripcionSaaS>> {
    const key = idempotencyKey.trim();
    if (!key) throw new Error('Idempotency-Key es obligatoria.');

    return this.http.post<ApiResponse<SuscripcionSaaS>>(
      `${this.tenantUrl(empresaId)}/onboarding`,
      request,
      { headers: new HttpHeaders({ 'Idempotency-Key': key }) }
    );
  }

  private tenantUrl(empresaId: number): string {
    if (!Number.isInteger(empresaId) || empresaId <= 0) {
      throw new Error('Se requiere un tenant verificado.');
    }
    return `${this.apiUrl}/${empresaId}`;
  }
}
