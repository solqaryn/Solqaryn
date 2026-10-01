import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LoginRequest, LoginResponse } from '../models/auth.model';

const TOKEN_KEY = 'solqaryn_token';
const USER_KEY = 'solqaryn_user';
const NOMBRE_COMPLETO_KEY = 'solqaryn_nombre_completo';
const ROL_KEY = 'solqaryn_rol';
const FOTO_PERFIL_KEY = 'solqaryn_foto_perfil';
const EXPIRA_KEY = 'solqaryn_expira_en';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly _nombreUsuario = signal<string | null>(localStorage.getItem(USER_KEY));
  private readonly _nombreCompleto = signal<string | null>(localStorage.getItem(NOMBRE_COMPLETO_KEY));
  private readonly _rol = signal<string | null>(localStorage.getItem(ROL_KEY));
  private readonly _fotoPerfilUrl = signal<string | null>(localStorage.getItem(FOTO_PERFIL_KEY));
  private readonly _expiraEn = signal<string | null>(localStorage.getItem(EXPIRA_KEY));

  readonly isAuthenticated = computed(() => !!this._token() && !this.isTokenExpired());
  readonly nombreUsuario = computed(() => this._nombreUsuario());
  readonly nombreCompleto = computed(() => this._nombreCompleto());
  readonly rol = computed(() => this._rol());
  readonly fotoPerfilUrl = computed(() => this._fotoPerfilUrl());
  readonly esAdministrador = computed(() => this._rol() === 'Administrador');
  readonly expiraEn = computed(() => this._expiraEn());

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/login`, request).pipe(
      tap((res) => {
        if (res.success) this.aplicarSesion(res.data);
      })
    );
  }

  renovarSesion(): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.apiUrl}/renovar`, {}).pipe(
      tap((res) => {
        if (res.success) this.aplicarSesion(res.data);
      })
    );
  }

  actualizarIdentidad(datos: {
    nombreUsuario: string;
    nombreCompleto: string;
    fotoPerfilUrl?: string | null;
  }): void {
    this._nombreUsuario.set(datos.nombreUsuario);
    this._nombreCompleto.set(datos.nombreCompleto);
    this._fotoPerfilUrl.set(datos.fotoPerfilUrl || null);

    localStorage.setItem(USER_KEY, datos.nombreUsuario);
    localStorage.setItem(NOMBRE_COMPLETO_KEY, datos.nombreCompleto);
    this.persistirFoto(datos.fotoPerfilUrl || null);
  }

  logout(): void {
    this._token.set(null);
    this._nombreUsuario.set(null);
    this._nombreCompleto.set(null);
    this._rol.set(null);
    this._fotoPerfilUrl.set(null);
    this._expiraEn.set(null);

    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(NOMBRE_COMPLETO_KEY);
    localStorage.removeItem(ROL_KEY);
    localStorage.removeItem(FOTO_PERFIL_KEY);
    localStorage.removeItem(EXPIRA_KEY);
  }

  getToken(): string | null {
    return this._token();
  }

  isTokenExpired(): boolean {
    const expiraEn = this._expiraEn();
    if (!expiraEn) return false;
    const expira = Date.parse(expiraEn);
    return Number.isFinite(expira) && Date.now() >= expira;
  }

  private aplicarSesion(data: LoginResponse): void {
    this._token.set(data.token);
    this._nombreUsuario.set(data.nombreUsuario);
    this._nombreCompleto.set(data.nombreCompleto);
    this._rol.set(data.rol);
    this._fotoPerfilUrl.set(data.fotoPerfilUrl || null);
    this._expiraEn.set(data.expiraEn);

    localStorage.setItem(TOKEN_KEY, data.token);
    localStorage.setItem(USER_KEY, data.nombreUsuario);
    localStorage.setItem(NOMBRE_COMPLETO_KEY, data.nombreCompleto);
    localStorage.setItem(ROL_KEY, data.rol);
    localStorage.setItem(EXPIRA_KEY, data.expiraEn);
    this.persistirFoto(data.fotoPerfilUrl || null);
  }

  private persistirFoto(url: string | null): void {
    if (url) localStorage.setItem(FOTO_PERFIL_KEY, url);
    else localStorage.removeItem(FOTO_PERFIL_KEY);
  }
}
