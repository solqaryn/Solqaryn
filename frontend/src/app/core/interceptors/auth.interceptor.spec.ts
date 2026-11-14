import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { SessionActivityService } from '../auth/session-activity.service';
import { authInterceptor } from './auth.interceptor';

describe('Auth interceptor - identidad plataforma y tenant aislados', () => {
  let http: HttpClient;
  let testing: HttpTestingController;

  beforeEach(() => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '1');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getToken: () => 'signed-test-only', isTokenExpired: () => false } },
        { provide: SessionActivityService, useValue: { cerrarPor401: vi.fn() } }
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    testing.verify();
    localStorage.removeItem('solqaryn_empresa_solicitada_id');
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
});
