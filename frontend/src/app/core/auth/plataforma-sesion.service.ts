import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

/**
 * Solo GET /plataforma/sesion puede conferir autoridad global a la UI.
 * Nunca se deriva de claims del JWT, el rol empresarial ni EmpresaId local.
 */
export interface PlataformaSesion {
  nombreUsuario: string;
  rolNombre: string;
  ambito: 'Plataforma';
  esAdministrador: true;
}

@Injectable({ providedIn: 'root' })
export class PlataformaSesionService {
  private readonly url = environment.apiUrl + '/plataforma/sesion';
  private readonly _sesion = signal<PlataformaSesion | null>(null);
  // Invalida las respuestas HTTP anteriores a una revocacion o nueva comprobacion.
  private versionSesion = 0;

  readonly sesion = this._sesion.asReadonly();

  constructor(private readonly http: HttpClient) {}

  /** Cada navegación protegida revalida el grant contra MySQL; sin caché. */
  obtener(): Observable<PlataformaSesion> {
    const version = ++this.versionSesion;
    this._sesion.set(null);
    return this.http.get<ApiResponse<PlataformaSesion>>(this.url).pipe(
      map((respuesta) => {
        if (version !== this.versionSesion) {
          throw new Error('La comprobacion de sesion fue invalidada.');
        }
        const dato = respuesta?.data;
        if (respuesta?.success !== true ||
            dato?.ambito !== 'Plataforma' ||
            dato?.esAdministrador !== true ||
            typeof dato?.nombreUsuario !== 'string' || !dato.nombreUsuario.trim() ||
            typeof dato?.rolNombre !== 'string' || !dato.rolNombre.trim()) {
          throw new Error('El servidor no confirmó una sesión de plataforma válida.');
        }
        return dato;
      }),
      tap((dato) => {
        if (version === this.versionSesion) this._sesion.set(dato);
      }),
      catchError((error) => {
        // Un rechazo antiguo tampoco puede borrar una sesion posterior valida.
        if (version === this.versionSesion) this._sesion.set(null);
        return throwError(() => error);
      })
    );
  }

  limpiar(): void {
    this.versionSesion++;
    this._sesion.set(null);
  }
}
