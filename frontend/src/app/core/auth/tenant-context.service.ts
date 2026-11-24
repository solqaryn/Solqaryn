import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, map, shareReplay, tap, throwError, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

const EMPRESA_SOLICITADA_KEY = 'solqaryn_empresa_solicitada_id';
const TOKEN_ACTOR_KEY = 'solqaryn_token';

/** Identidad empresarial que el servidor autorizó a mostrar al usuario actual. */
export interface EmpresaSeleccionable {
  empresaId: number;
  nombre: string;
}

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
  // Se deriva exclusivamente de GET autenticado; no se guarda en localStorage.
  private readonly _idsMembresiasListadas = signal<readonly number[]>([]);
  // La UI/login y los guards pueden verificar el mismo tenant simultáneamente.
  // Compartir esa petición evita que una segunda selección borre un contexto
  // que el servidor acaba de confirmar y provoque un bucle hacia /login.
  private seleccionEnCurso: {
    empresaId: number;
    version: number;
    tokenOrigen: string | null;
    solicitud: Observable<ApiResponse<TenantContextoVerificado>>;
  } | null = null;
  private tokenContextoVerificado: string | null = null;
  private versionSeleccion = 0;
  private versionListado = 0;

  readonly empresaSolicitadaId = computed(() => this._empresaSolicitadaId());
  readonly contextoVerificado = computed(() => this._contextoVerificado());
  readonly empresaIdVerificada = computed(() => this._contextoVerificado()?.empresaId ?? null);
  readonly rolVerificado = computed(() => this._contextoVerificado()?.rolNombre ?? null);
  readonly tieneContextoVerificado = computed(() => this._contextoVerificado() !== null);
  readonly puedeCambiarEmpresa = computed(() => {
    const actual = this._contextoVerificado()?.empresaId;
    const ids = this._idsMembresiasListadas();
    return actual != null && ids.length > 1 && ids.includes(actual);
  });

  constructor(private readonly http: HttpClient) {}

  /** Snapshot autorizado por HTTP para la caché de permisos; nunca restaura autoridad desde storage. */
  contextoParaPermisos(): TenantContextoVerificado | null {
    const contexto = this._contextoVerificado();
    if (!this.tokenContextoVerificado || localStorage.getItem(TOKEN_ACTOR_KEY) !== this.tokenContextoVerificado) return null;
    return contexto;
  }

  listarEmpresasSeleccionables(): Observable<EmpresaSeleccionable[]> {
    const version = this.versionSeleccion;
    const listado = ++this.versionListado;
    const tokenOrigen = localStorage.getItem(TOKEN_ACTOR_KEY);
    // La lista se resuelve en MySQL por membresías activas del JWT actual.
    // Este método nunca considera localStorage ni cabeceras tenant una autoridad.
    return this.http.get<ApiResponse<EmpresaSeleccionable[]>>(`${this.apiUrl}/empresas`).pipe(
      timeout({ first: 15_000 }),
      map(respuesta => {
        // Los resultados de A nunca pueden habilitar el selector de B tras
        // un cambio de JWT concurrente (incluido desde otra pestaña).
        if (localStorage.getItem(TOKEN_ACTOR_KEY) !== tokenOrigen) {
          throw new Error('La identidad cambió durante la consulta empresarial.');
        }
        const empresas = respuesta?.success === true ? respuesta.data : null;
        if (!Array.isArray(empresas) || empresas.some(empresa =>
          !Number.isInteger(empresa?.empresaId) || empresa.empresaId <= 0 ||
          typeof empresa.nombre !== 'string' || empresa.nombre.trim().length === 0
        )) {
          throw new Error('El servidor no devolvió una lista empresarial válida.');
        }
        // IDs duplicados o respuestas obsoletas no crean una opción de cambio.
        const ids = empresas.map(empresa => empresa.empresaId);
        if (new Set(ids).size !== ids.length) {
          throw new Error('El servidor devolvió empresas duplicadas.');
        }
        if (version === this.versionSeleccion && listado === this.versionListado) {
          this._idsMembresiasListadas.set(ids);
        }
        return empresas;
      }),
      catchError(error => {
        if (version === this.versionSeleccion && listado === this.versionListado) {
          this._idsMembresiasListadas.set([]);
        }
        return throwError(() => error);
      })
    );
  }

  seleccionarEmpresa(empresaId: number): Observable<ApiResponse<TenantContextoVerificado>> {
    if (!Number.isInteger(empresaId) || empresaId <= 0) {
      this.limpiar();
      return throwError(() => new Error('El identificador de empresa no es válido.'));
    }

    // Una única validación HTTP por selección: un segundo guard o el
    // selector no puede invalidar la primera al intentar la misma empresa.
    const tokenOrigen = localStorage.getItem(TOKEN_ACTOR_KEY);
    if (this.seleccionEnCurso?.empresaId === empresaId &&
        this.seleccionEnCurso.tokenOrigen === tokenOrigen) {
      return this.seleccionEnCurso.solicitud;
    }

    const version = ++this.versionSeleccion;
    // La lista autenticada del contexto anterior no puede habilitar el switch de B.
    this._idsMembresiasListadas.set([]);
    this._empresaSolicitadaId.set(empresaId);
    this.tokenContextoVerificado = null;
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
          if (version !== this.versionSeleccion ||
              this._empresaSolicitadaId() !== empresaId ||
              localStorage.getItem(TOKEN_ACTOR_KEY) !== tokenOrigen) {
            throw new Error('La selección empresarial fue sustituida.');
          }
          const contexto = respuesta?.success === true ? respuesta.data : null;
          if (!this.esContextoCoincidente(contexto, empresaId)) {
            throw new Error('El servidor no confirmó un contexto tenant válido.');
          }
          this.tokenContextoVerificado = tokenOrigen;
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

    this.seleccionEnCurso = { empresaId, version, tokenOrigen, solicitud };
    return solicitud;
  }

  limpiar(): void {
    // Invalida también respuestas HTTP que lleguen después de cerrar/salir.
    this.versionSeleccion++;
    this.versionListado++;
    this.seleccionEnCurso = null;
    this._empresaSolicitadaId.set(null);
    this.tokenContextoVerificado = null;
    this._contextoVerificado.set(null);
    this._idsMembresiasListadas.set([]);
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
