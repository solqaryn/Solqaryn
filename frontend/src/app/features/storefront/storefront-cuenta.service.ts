import { fiscalCheckoutValido } from './storefront-checkout.rules';
import { DOCUMENT } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, defer, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import {
  CrearPedidoCheckoutRequest,
  TiendaCuentaPerfil,
  TiendaCuentaSesion,
  TiendaDireccionCliente,
  TiendaNotificacionPedido,
  TiendaPedidoCuenta
} from './storefront.models';

const SESSION_KEY = 'storefront:cuenta:session:v1';

@Injectable({ providedIn: 'root' })
export class StorefrontCuentaService {
  private readonly http = inject(HttpClient);
  private readonly document = inject(DOCUMENT);
  private readonly baseUrl = `${environment.apiUrl}/tienda/cuenta`;

  private revisionSesion = 0;
  private readonly _perfil = signal<TiendaCuentaPerfil | null>(null);
  readonly perfil = this._perfil.asReadonly();
  readonly autenticado = computed(() => this._perfil() !== null);

  registrar(nombre: string, correo: string, clave: string): Observable<TiendaCuentaPerfil> {
    return this.iniciarSesion('registrar', { nombre, correo, clave });
  }

  login(correo: string, clave: string): Observable<TiendaCuentaPerfil> {
    return this.iniciarSesion('login', { correo, clave });
  }

  private iniciarSesion(accion: 'registrar' | 'login', datos: { nombre?: string; correo: string; clave: string }): Observable<TiendaCuentaPerfil> {
    return defer(() => {
      const revision = ++this.revisionSesion;
      return this.http.post<ApiResponse<TiendaCuentaSesion>>(`${this.baseUrl}/${accion}`, datos).pipe(
        map(res => this.validarSesion(res)),
        tap(sesion => {
          if (revision !== this.revisionSesion) throw new Error('La sesión cambió antes de completar el acceso.');
          this.aplicarSesion(sesion);
        }),
        map(sesion => sesion.perfil)
      );
    });
  }

  restaurar(): Observable<TiendaCuentaPerfil | null> {
    return defer(() => {
      const token = this.token();
      const revision = this.revisionSesion;
      if (!token) {
        this._perfil.set(null);
        return of(null);
      }
      const vigente = () => revision === this.revisionSesion && token === this.token();
      return this.http.get<ApiResponse<TiendaCuentaPerfil>>(this.baseUrl, { headers: this.headers() }).pipe(
        map(res => {
          if (!vigente()) return null;
          if (!res.success || !this.perfilValido(res.data)) throw new Error('Sesión de cliente no válida.');
          return res.data;
        }),
        tap(perfil => { if (perfil && vigente()) this._perfil.set(perfil); }),
        catchError(() => {
          if (vigente()) this.limpiarSesion();
          return of(null);
        })
      );
    });
  }

  logout(): Observable<void> {
    return defer(() => {
      const headers = this.headers();
      // Invalidar localmente antes de esperar la red. La respuesta de un cierre
      // anterior no puede borrar una sesión iniciada posteriormente.
      this.limpiarSesion();
      return this.http.post<ApiResponse<object>>(`${this.baseUrl}/logout`, {}, { headers }).pipe(
        map(() => undefined),
        catchError(() => of(undefined))
      );
    });
  }

  direcciones(): Observable<TiendaDireccionCliente[]> {
    return this.getLista<TiendaDireccionCliente>('direcciones');
  }

  guardarDireccion(valor: Omit<TiendaDireccionCliente, 'id'>): Observable<TiendaDireccionCliente> {
    return this.http.post<ApiResponse<TiendaDireccionCliente>>(`${this.baseUrl}/direcciones`, valor, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error(res.message || 'No se pudo guardar la dirección.');
        return res.data;
      })
    );
  }

  eliminarDireccion(id: number): Observable<void> {
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/direcciones/${id}`, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success) throw new Error(res.message || 'No se pudo eliminar la dirección.');
      })
    );
  }

  favoritos(): Observable<number[]> {
    return this.getLista<number>('favoritos');
  }

  agregarFavorito(productoId: number): Observable<void> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/favoritos/${productoId}`, {}, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success) throw new Error(res.message || 'No se pudo guardar el favorito.');
      })
    );
  }

  quitarFavorito(productoId: number): Observable<void> {
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/favoritos/${productoId}`, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success) throw new Error(res.message || 'No se pudo quitar el favorito.');
      })
    );
  }

  pedidos(): Observable<TiendaPedidoCuenta[]> {
    return this.getLista<TiendaPedidoCuenta>('pedidos');
  }

  pedido(id: number): Observable<TiendaPedidoCuenta> {
    return this.http.get<ApiResponse<TiendaPedidoCuenta>>(`${this.baseUrl}/pedidos/${id}`, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error(res.message || 'Pedido no encontrado.');
        return res.data;
      })
    );
  }

  crearPedidoCheckout(datos: CrearPedidoCheckoutRequest, clave: string): Observable<TiendaPedidoCuenta> {
    return this.http.post<ApiResponse<TiendaPedidoCuenta>>(`${this.baseUrl}/pedidos/checkout`, datos, {
      headers: this.headers().set('X-Storefront-Order-Key', clave)
    }).pipe(map(res => this.validarPedidoCheckout(res)));
  }

  pedidoCheckout(id: number, clave?: string): Observable<TiendaPedidoCuenta> {
    const headers = clave ? this.headers().set('X-Storefront-Order-Key', clave) : this.headers();
    return this.http.get<ApiResponse<TiendaPedidoCuenta>>(`${this.baseUrl}/pedidos/checkout/${id}`, { headers })
      .pipe(map(res => this.validarPedidoCheckout(res)));
  }

  private validarPedidoCheckout(res: ApiResponse<TiendaPedidoCuenta>): TiendaPedidoCuenta {
    const data = res.data;
    if (!res.success || !data || !Number.isSafeInteger(data.id) || data.id <= 0 ||
        !Number.isFinite(data.total) || typeof data.subtotal !== 'number' || !Number.isFinite(data.subtotal) ||
        typeof data.costoEnvio !== 'number' || !Number.isFinite(data.costoEnvio) || data.costoEnvio < 0 ||
        !fiscalCheckoutValido(data.fiscal, data.subtotal, data.costoEnvio, data.total) ||
        !data.moneda || !/^[A-Z]{3}$/.test(data.moneda) ||
        !Array.isArray(data.lineas) || !data.lineas.length ||
        !Number.isFinite(Date.parse(data.fechaUtc))) {
      throw new Error('No se pudo verificar el pedido registrado.');
    }
    return data;
  }

  notificaciones(): Observable<TiendaNotificacionPedido[]> {
    return this.getLista<TiendaNotificacionPedido>('notificaciones');
  }

  private getLista<T>(path: string): Observable<T[]> {
    return this.http.get<ApiResponse<T[]>>(`${this.baseUrl}/${path}`, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success || !Array.isArray(res.data)) throw new Error(res.message || 'Respuesta de cuenta no válida.');
        return res.data;
      })
    );
  }

  private validarSesion(res: ApiResponse<TiendaCuentaSesion>): TiendaCuentaSesion {
    const data = res.data;
    if (!res.success || !data || typeof data.token !== 'string' || !/^[A-Za-z0-9_-]{32,128}$/.test(data.token)
      || !Number.isFinite(Date.parse(data.expiraUtc)) || !this.perfilValido(data.perfil)) {
      throw new Error(res.message || 'No se pudo iniciar la sesión de cliente.');
    }
    return data;
  }

  private perfilValido(perfil: TiendaCuentaPerfil | null | undefined): perfil is TiendaCuentaPerfil {
    return Boolean(perfil
      && Number.isSafeInteger(perfil.id)
      && perfil.id > 0
      && typeof perfil.nombre === 'string'
      && perfil.nombre.trim()
      && typeof perfil.correo === 'string'
      && perfil.correo.includes('@'));
  }

  private aplicarSesion(sesion: TiendaCuentaSesion): void {
    try {
      this.document.defaultView?.sessionStorage.setItem(SESSION_KEY, sesion.token);
    } catch {
      throw new Error('El navegador no permitió guardar la sesión de cliente.');
    }
    this._perfil.set(sesion.perfil);
  }

  private limpiarSesion(): void {
    ++this.revisionSesion;
    try {
      this.document.defaultView?.sessionStorage.removeItem(SESSION_KEY);
    } catch {
      // La sesión también queda invalidada en memoria.
    }
    this._perfil.set(null);
  }

  private token(): string {
    try {
      return this.document.defaultView?.sessionStorage.getItem(SESSION_KEY)?.trim() || '';
    } catch {
      return '';
    }
  }

  private headers(): HttpHeaders {
    const token = this.token();
    return token ? new HttpHeaders({ 'X-Storefront-Session': token }) : new HttpHeaders();
  }
}
