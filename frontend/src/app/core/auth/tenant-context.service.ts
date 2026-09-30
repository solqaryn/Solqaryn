import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';
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

    // Registrar la intención de selección no concede autoridad. El contexto
    // verificado permanece vacío hasta recibir una respuesta server-side válida.
    this._empresaSolicitadaId.set(empresaId);
    this._contextoVerificado.set(null);
    localStorage.setItem(EMPRESA_SOLICITADA_KEY, String(empresaId));

    return this.http
      .get<ApiResponse<TenantContextoVerificado>>(`${this.apiUrl}/${empresaId}`)
      .pipe(
        tap((respuesta) => {
          const contexto = respuesta.success ? respuesta.data : null;
          if (!this.esContextoCoincidente(contexto, empresaId)) {
            this.limpiar();
            throw new Error('El servidor no confirmó un contexto tenant válido.');
          }

          this._contextoVerificado.set(contexto);
        }),
        catchError((error) => {
          // 401/403, respuestas inválidas y fallos de transporte revocan cualquier
          // selección/contexto local para evitar reutilización cross-tenant.
          this.limpiar();
          return throwError(() => error);
        })
      );
  }

  limpiar(): void {
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
