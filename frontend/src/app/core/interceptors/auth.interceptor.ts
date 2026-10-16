import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';

const EMPRESA_SOLICITADA_KEY = 'solqaryn_empresa_solicitada_id';
const TENANT_HEADER = 'X-Empresa-Id';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const sessionActivity = inject(SessionActivityService);

  const token = authService.getToken();
  if (token && authService.isTokenExpired()) {
    sessionActivity.cerrarPor401();
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
      if (error.status === 401) {
        sessionActivity.cerrarPor401();
      }
      return throwError(() => error);
    })
  );
};
