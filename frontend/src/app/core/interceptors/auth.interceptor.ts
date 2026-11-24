import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';

import { CREDENCIAL_SUCURSAL } from '../auth/tenant-context.service';

const EMPRESA_SOLICITADA_KEY = 'solqaryn_empresa_solicitada_id';
const TENANT_HEADER = 'X-Empresa-Id';
const TOKEN_KEY = 'solqaryn_token';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);

  const token = authService.getToken();
  const tokenPersistido = localStorage.getItem(TOKEN_KEY);
  // A late rejection belongs to the session that sent the request, not to a
  // replacement login or a session changed by another tab. Never adopt the
  // token from storage as a new authenticated identity here.
  const mismaSesion = () => authService.getToken() === token &&
    localStorage.getItem(TOKEN_KEY) === tokenPersistido && tokenPersistido === token;
  // A contextual credential is never a base login token or a general API grant.
  const contextual = req.context.get(CREDENCIAL_SUCURSAL);
  if (contextual) {
    const apiBase = environment.apiUrl.replace(/\/$/, '');
    if (req.method !== 'GET' || req.urlWithParams !== `${apiBase}/tenant-context/sesion/sucursal` ||
        !token || token !== contextual.baseToken || !mismaSesion() || authService.isTokenExpired() ||
        localStorage.getItem(EMPRESA_SOLICITADA_KEY) !== String(contextual.empresaId)) {
      return throwError(() => ({ status: 401, message: 'Contexto de sucursal no vigente.' }));
    }
    // A 401/403 from this scoped resource must not replace or close the base session.
    return next(req.clone({ setHeaders: {
      Authorization: `Bearer ${contextual.token}`,
      [TENANT_HEADER]: String(contextual.empresaId),
      'X-Sucursal-Id': String(contextual.sucursalId)
    }, redirect: 'error' }));
  }
  // A shared-storage logout or replacement login must not dispatch a request
  // using this tab's stale in-memory identity. Never adopt storage as authority.
  if (token && tokenPersistido !== token) {
    const apiBase = environment.apiUrl.replace(/\/$/, '');
    // An explicit login is the recovery path; it must be unauthenticated.
    if (req.method === 'POST' && req.url === `${apiBase}/auth/login`) {
      return next(req.clone({
        headers: req.headers.delete('Authorization').delete(TENANT_HEADER)
      }));
    }
    return throwError(() => ({ status: 401, message: 'La sesión cambió. Inicia sesión nuevamente.' }));
  }
  if (token && authService.isTokenExpired()) {
    if (mismaSesion()) sessionActivity.cerrarPor401();
    return throwError(() => ({ status: 401, message: 'Token expirado' }));
  }

  const empresaSolicitadaRaw = localStorage.getItem(EMPRESA_SOLICITADA_KEY);
  const empresaSolicitadaId = Number(empresaSolicitadaRaw);
  const tenantSolicitado = Number.isInteger(empresaSolicitadaId) && empresaSolicitadaId > 0
    ? String(empresaSolicitadaId)
    : null;

  // X-Empresa-Id expresa únicamente el tenant solicitado por el cliente. Nunca
  // concede autoridad: los filtros backend vuelven a resolver UsuarioEmpresa y
  // fallan cerrado si la membresía/rol no corresponde al usuario autenticado.
  const headers: Record<string, string> = {};
  if (token) headers['Authorization'] = `Bearer ${token}`;

  // El control-plane de SOLQARYN (y el propio login) jamás debe arrastrar
  // la selección de la última empresa. El JWT identifica a la persona;
  // la autoridad global se comprueba desde MySQL en el endpoint protegido.
  const apiBase = environment.apiUrl.endsWith('/') ? environment.apiUrl.slice(0, -1) : environment.apiUrl;
  const identidadGlobal = req.url.startsWith(`${apiBase}/plataforma/`) ||
    req.url.startsWith(`${apiBase}/auth/`);
  if (token && tenantSolicitado && !identidadGlobal) {
    headers[TENANT_HEADER] = tenantSolicitado;
  }

  const authReq = Object.keys(headers).length > 0
    ? req.clone({ setHeaders: headers })
    : req;

  return next(authReq).pipe(
    catchError((error) => {
      if (error.status === 401 && mismaSesion()) {
        sessionActivity.cerrarPor401();
      }
      return throwError(() => error);
    })
  );
};
