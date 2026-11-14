import { DOCUMENT } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../core/models/api-response.model';
import {
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

  private readonly _perfil = signal<TiendaCuentaPerfil | null>(null);
  readonly perfil = this._perfil.asReadonly();
  readonly autenticado = computed(() => this._perfil() !== null);

  registrar(nombre: string, correo: string, clave: string): Observable<TiendaCuentaPerfil> {
    return this.http.post<ApiResponse<TiendaCuentaSesion>>(`${this.baseUrl}/registrar`, { nombre, correo, clave }).pipe(
      map(res => this.validarSesion(res)),
      tap(sesion => this.aplicarSesion(sesion)),
      map(sesion => sesion.perfil)
    );
  }

  login(correo: string, clave: string): Observable<TiendaCuentaPerfil> {
    return this.http.post<ApiResponse<TiendaCuentaSesion>>(`${this.baseUrl}/login`, { correo, clave }).pipe(
      map(res => this.validarSesion(res)),
      tap(sesion => this.aplicarSesion(sesion)),
      map(sesion => sesion.perfil)
    );
  }

  restaurar(): Observable<TiendaCuentaPerfil | null> {
    const token = this.token();
    if (!token) {
      this.limpiarSesion();
      return of(null);
    }

    return this.http.get<ApiResponse<TiendaCuentaPerfil>>(this.baseUrl, { headers: this.headers() }).pipe(
      map(res => {
        if (!res.success || !this.perfilValido(res.data)) throw new Error('Sesión de cliente no válida.');
        return res.data;
      }),
      tap(perfil => this._perfil.set(perfil)),
      catchError(() => {
        this.limpiarSesion();
        return of(null);
      })
    );
  }

  logout(): Observable<void> {
    const headers = this.headers();
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/logout`, {}, { headers }).pipe(
      map(() => undefined),
      catchError(() => of(undefined)),
      tap(() => this.limpiarSesion())
    );
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
