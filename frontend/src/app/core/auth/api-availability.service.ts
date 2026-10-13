import { HttpBackend, HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, defer, finalize, map, retry, throwError, timer } from 'rxjs';
import { environment } from '../../../environments/environment';

export type EstadoDisponibilidadApi = 'inactivo' | 'comprobando' | 'reintentando';

const ESTADOS_TRANSITORIOS = new Set([0, 502, 503, 504]);

/**
 * Readiness a demanda antes de autenticar.
 * El GET no transmite JWT, datos de usuario ni contrasena y no crea un keep-alive.
 */
@Injectable({ providedIn: 'root' })
export class ApiAvailabilityService {
  // HttpBackend omite deliberadamente el interceptor de JWT/tenant para la sonda publica.
  private readonly httpPublico = new HttpClient(inject(HttpBackend));
  readonly estado = signal<EstadoDisponibilidadApi>('inactivo');

  esperarAntesDeAutenticar(): Observable<void> {
    let intento = 0;
    return defer(() => {
      intento += 1;
      this.estado.set(intento === 1 ? 'comprobando' : 'reintentando');
      return this.httpPublico.get<unknown>(environment.apiUrl + '/health/ready');
    }).pipe(
      // Unicamente GET /health/ready: maximo tres solicitudes. Nunca reintentar el POST de login.
      retry({
        count: 2,
        delay: (error: unknown, numeroReintento: number) => {
          if (!(error instanceof HttpErrorResponse) || !ESTADOS_TRANSITORIOS.has(error.status)) {
            return throwError(() => error);
          }
          this.estado.set('reintentando');
          return timer(numeroReintento * 2500);
        }
      }),
      map(() => undefined),
      finalize(() => this.estado.set('inactivo'))
    );
  }
}
