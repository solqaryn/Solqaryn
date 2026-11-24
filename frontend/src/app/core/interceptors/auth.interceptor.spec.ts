import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';
import { CREDENCIAL_SUCURSAL } from '../auth/tenant-context.service';
import { authInterceptor } from './auth.interceptor';

describe('Auth interceptor - identidad plataforma y tenant aislados', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let token: string | null;
  let cerrarPor401: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    token = 'signed-test-only';
    cerrarPor401 = vi.fn();
    localStorage.setItem('solqaryn_token', token);
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getToken: () => token, isTokenExpired: () => false } },
        { provide: SessionActivityService, useValue: { cerrarPor401 } }
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    testing.verify();
    localStorage.removeItem('solqaryn_empresa_solicitada_id');
    localStorage.removeItem('solqaryn_token');
  });

  it('usa sólo el JWT contextual en el recurso permitido y no cierra la sesión base por su 401', () => {
    const context = new HttpContext().set(CREDENCIAL_SUCURSAL, { baseToken: 'signed-test-only', token: 'scoped-test', empresaId: 1, sucursalId: 5 });
    const url = environment.apiUrl + '/tenant-context/sesion/sucursal';
    http.get(url, { context }).subscribe({ error: () => undefined });
    const req = testing.expectOne(url);
    expect(req.request.headers.get('Authorization')).toBe('Bearer scoped-test');
    expect(req.request.headers.get('X-Sucursal-Id')).toBe('5');
    req.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(cerrarPor401).not.toHaveBeenCalled();
    http.get(environment.apiUrl + '/plataforma/sesion', { context }).subscribe({ error: () => undefined });
    testing.expectNone(environment.apiUrl + '/plataforma/sesion');
    expect(localStorage.getItem('solqaryn_token')).toBe('signed-test-only');
  });

  it('no adjunta contexto empresarial a la comprobación de autoridad global', () => {
    const url = environment.apiUrl + '/plataforma/sesion';
    http.get(url).subscribe();
    const req = testing.expectOne(url);
    expect(req.request.headers.get('Authorization')).toBe('Bearer signed-test-only');
    expect(req.request.headers.has('X-Empresa-Id')).toBe(false);
    req.flush({ success: true, data: {} });
  });

  it('no adjunta empresa guardada a login ni renovación', () => {
    const url = environment.apiUrl + '/auth/renovar';
    http.post(url, {}).subscribe();
    const req = testing.expectOne(url);
    expect(req.request.headers.has('X-Empresa-Id')).toBe(false);
    req.flush({ success: true, data: {} });
  });

  it('mantiene empresa solicitada únicamente en la ruta empresarial para la revalidación backend', () => {
    const url = environment.apiUrl + '/tenant-context/1';
    http.get(url).subscribe();
    const req = testing.expectOne(url);
    expect(req.request.headers.get('X-Empresa-Id')).toBe('1');
    req.flush({ success: true, data: {} });
  });

  it('401 vigente conserva el cierre de sesión fail-closed', () => {
    const url = environment.apiUrl + '/tenant-context/1';
    let status = 0;
    http.get(url).subscribe({ error: e => { status = e.status; } });
    testing.expectOne(url).flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(cerrarPor401).toHaveBeenCalledTimes(1);
  });

  it('401 tardío de la sesión A no cierra una sesión B autenticada posteriormente', () => {
    const url = environment.apiUrl + '/tenant-context/1';
    let status = 0;
    http.get(url).subscribe({ error: e => { status = e.status; } });
    const old = testing.expectOne(url);
    expect(old.request.headers.get('Authorization')).toBe('Bearer signed-test-only');
    token = 'signed-new-session';
    localStorage.setItem('solqaryn_token', token);
    http.get(url).subscribe();
    const current = testing.expectOne(url);
    expect(current.request.headers.get('Authorization')).toBe('Bearer signed-new-session');
    current.flush({ success: true, data: {} });
    old.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(cerrarPor401).not.toHaveBeenCalled();
    expect(localStorage.getItem('solqaryn_token')).toBe('signed-new-session');
  });

  it('401 de otra pestaña no elimina la identidad nueva persistida', () => {
    const url = environment.apiUrl + '/tenant-context/1';
    let status = 0;
    http.get(url).subscribe({ error: e => { status = e.status; } });
    const old = testing.expectOne(url);
    localStorage.setItem('solqaryn_token', 'other-tab-session');
    old.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(cerrarPor401).not.toHaveBeenCalled();
    expect(localStorage.getItem('solqaryn_token')).toBe('other-tab-session');
  });

  it('rechaza antes de enviar una petición con JWT de memoria sustituido en otra pestaña', () => {
    const url = environment.apiUrl + '/tenant-context/1';
    localStorage.setItem('solqaryn_token', 'other-tab-session');
    let status = 0;
    http.get(url).subscribe({ error: e => { status = e.status; } });
    testing.expectNone(url);
    expect(status).toBe(401);
    expect(cerrarPor401).not.toHaveBeenCalled();
    expect(localStorage.getItem('solqaryn_token')).toBe('other-tab-session');
  });

  it('no envía una mutación con JWT de memoria después de un logout compartido', () => {
    const url = environment.apiUrl + '/productos/1';
    localStorage.removeItem('solqaryn_token');
    let status = 0;
    http.delete(url).subscribe({ error: e => { status = e.status; } });
    testing.expectNone(url);
    expect(status).toBe(401);
    expect(cerrarPor401).not.toHaveBeenCalled();
    expect(localStorage.getItem('solqaryn_token')).toBeNull();
  });

  it('permite recuperar la sesión por POST login sin JWT viejo ni tenant solicitado', () => {
    const url = environment.apiUrl.replace(/\/$/, '') + '/auth/login';
    localStorage.setItem('solqaryn_token', 'other-tab-session');
    http.post(url, {}, { headers: { Authorization: 'Bearer signed-test-only', 'X-Empresa-Id': '1' } }).subscribe();
    const req = testing.expectOne(url);
    expect(req.request.headers.has('Authorization')).toBe(false);
    expect(req.request.headers.has('X-Empresa-Id')).toBe(false);
    req.flush({ success: true, data: {} });
    expect(cerrarPor401).not.toHaveBeenCalled();
  });
});
