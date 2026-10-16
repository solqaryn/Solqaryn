import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, shareReplay, tap, throwError, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

const EMPRESA_SOLICITADA_KEY = 'solqaryn_empresa_solicitada_id';

export interface TenantContextoVerificado {
  usuarioId: number;
  empresaId: number;
  rolId: number;
  rolNombre: string;
  esAdministrador: boolean;
}

/**
 * Contexto tenant del frontend.
 *
 * La empresa elegida por el cliente es solamente una solicitud de contexto. La
 * autoridad efectiva se materializa exclusivamente después de que el backend
 * valida UsuarioEmpresa para el usuario autenticado. Un valor persistido en
 * localStorage nunca se considera autoridad ni restaura un contexto verificado.
 */
@Injectable({ providedIn: 'root' })
export class TenantContextService {
  private readonly apiUrl = `${environment.apiUrl}/tenant-context`;

  private readonly _empresaSolicitadaId = signal<number | null>(this.leerEmpresaSolicitada());
  private readonly _contextoVerificado = signal<TenantContextoVerificado | null>(null);
  // La UI/login y los guards pueden verificar el mismo tenant simultáneamente.
  // Compartir esa petición evita que una segunda selección borre un contexto
  // que el servidor acaba de confirmar y provoque un bucle hacia /login.
  private seleccionEnCurso: {
    empresaId: number;
    version: number;
    solicitud: Observable<ApiResponse<TenantContextoVerificado>>;
  } | null = null;
  private versionSeleccion = 0;

  readonly empresaSolicitadaId = computed(() => this._empresaSolicitadaId());
  readonly contextoVerificado = computed(() => this._contextoVerificado());
  readonly empresaIdVerificada = computed(() => this._contextoVerificado()?.empresaId ?? null);
  readonly rolVerificado = computed(() => this._contextoVerificado()?.rolNombre ?? null);
  readonly tieneContextoVerificado = computed(() => this._contextoVerificado() !== null);

  constructor(private readonly http: HttpClient) {}

  seleccionarEmpresa(empresaId: number): Observable<ApiResponse<TenantContextoVerificado>> {
    if (!Number.isInteger(empresaId) || empresaId <= 0) {
      this.limpiar();
      return throwError(() => new Error('El identificador de empresa no es válido.'));
    }

    // Una única validación HTTP por selección: un segundo guard o el
    // selector no puede invalidar la primera al intentar la misma empresa.
    if (this.seleccionEnCurso?.empresaId === empresaId) {
      return this.seleccionEnCurso.solicitud;
    }

    const version = ++this.versionSeleccion;
    this._empresaSolicitadaId.set(empresaId);
    this._contextoVerificado.set(null);
    localStorage.setItem(EMPRESA_SOLICITADA_KEY, String(empresaId));

    const solicitud = this.http
      .get<ApiResponse<TenantContextoVerificado>>(`${this.apiUrl}/${empresaId}`)
      .pipe(
        // La recuperación nunca debe dejar «Verificando…» permanentemente.
        // Timeout de UX, no una concesión de autoridad ni un reintento POST.
        timeout({ first: 15_000 }),
        tap((respuesta) => {
          // Una respuesta de una empresa anterior no puede seleccionar ni
          // limpiar el tenant actual mientras se cambia de contexto.
          if (version !== this.versionSeleccion || this._empresaSolicitadaId() !== empresaId) {
            throw new Error('La selección empresarial fue sustituida.');
          }
          const contexto = respuesta?.success === true ? respuesta.data : null;
          if (!this.esContextoCoincidente(contexto, empresaId)) {
            throw new Error('El servidor no confirmó un contexto tenant válido.');
          }
          this._contextoVerificado.set(contexto);
        }),
        catchError((error) => {
          if (version === this.versionSeleccion) this.limpiar();
          return throwError(() => error);
        }),
        finalize(() => {
          if (this.seleccionEnCurso?.version === version) {
            this.seleccionEnCurso = null;
          }
        }),
        shareReplay({ bufferSize: 1, refCount: true })
      );

    this.seleccionEnCurso = { empresaId, version, solicitud };
    return solicitud;
  }

  limpiar(): void {
    // Invalida también respuestas HTTP que lleguen después de cerrar/salir.
    this.versionSeleccion++;
    this.seleccionEnCurso = null;
    this._empresaSolicitadaId.set(null);
    this._contextoVerificado.set(null);
    localStorage.removeItem(EMPRESA_SOLICITADA_KEY);
  }

  private esContextoCoincidente(
    contexto: TenantContextoVerificado | null | undefined,
    empresaSolicitadaId: number
  ): contexto is TenantContextoVerificado {
    return !!contexto &&
      Number.isInteger(contexto.usuarioId) && contexto.usuarioId > 0 &&
      Number.isInteger(contexto.empresaId) && contexto.empresaId === empresaSolicitadaId &&
      Number.isInteger(contexto.rolId) && contexto.rolId > 0 &&
      typeof contexto.rolNombre === 'string' && contexto.rolNombre.trim().length > 0;
  }

  private leerEmpresaSolicitada(): number | null {
    const raw = localStorage.getItem(EMPRESA_SOLICITADA_KEY);
    if (!raw) return null;

    const value = Number(raw);
    return Number.isInteger(value) && value > 0 ? value : null;
  }
}
