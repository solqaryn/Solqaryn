import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { vi } from 'vitest';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { ApiAvailabilityService } from './api-availability.service';

const READY = environment.apiUrl + '/health/ready';
const LOGIN = environment.apiUrl + '/auth/login';

describe('Readiness previa al login (Render Free)', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    vi.useFakeTimers();
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
    localStorage.clear();
  });

  it('solo envia POST /auth/login despues de readiness 200, exactamente una vez', () => {
    const auth = TestBed.inject(AuthService);
    const disponibilidad = TestBed.inject(ApiAvailabilityService);
    auth.login({ nombreUsuario: 'operador', password: 'secreto' }).subscribe();

    const ready = httpMock.expectOne(READY);
    expect(ready.request.method).toBe('GET');
    expect(ready.request.headers.has('Authorization')).toBe(false);
    expect(ready.request.headers.has('X-Empresa-Id')).toBe(false);
    expect(disponibilidad.estado()).toBe('comprobando');
    httpMock.expectNone(LOGIN);
    ready.flush({ status: 'ready', database: 'connected' });

    const login = httpMock.expectOne(LOGIN);
    expect(login.request.method).toBe('POST');
    expect(login.request.body).toEqual({ nombreUsuario: 'operador', password: 'secreto' });
    login.flush({
      success: true, message: 'Acceso correcto.',
      data: {
        token: 'token-de-prueba', nombreUsuario: 'operador',
        nombreCompleto: 'Operador', rol: 'Operador', expiraEn: '2030-01-01T00:00:00Z'
      }
    });
    httpMock.expectNone(LOGIN);
    expect(auth.isAuthenticated()).toBe(true);
    expect(disponibilidad.estado()).toBe('inactivo');
  });

  it('reintenta solo la sonda GET cuando el proxy retorna 503; no duplica credenciales', () => {
    const auth = TestBed.inject(AuthService);
    const disponibilidad = TestBed.inject(ApiAvailabilityService);
    auth.login({ nombreUsuario: 'operador', password: 'clave' }).subscribe();

    httpMock.expectOne(READY).flush(
      { success: false, code: 'API_UPSTREAM_UNAVAILABLE' },
      { status: 503, statusText: 'Service Unavailable' }
    );
    expect(disponibilidad.estado()).toBe('reintentando');
    httpMock.expectNone(LOGIN);
    vi.advanceTimersByTime(2500);

    const segundo = httpMock.expectOne(READY);
    expect(segundo.request.method).toBe('GET');
    expect(segundo.request.headers.has('Authorization')).toBe(false);
    segundo.flush({ status: 'ready', database: 'connected' });

    const post = httpMock.expectOne(LOGIN);
    post.flush({ success: false, message: 'Credenciales incorrectas.' }, { status: 401, statusText: 'Unauthorized' });
    httpMock.expectNone(LOGIN);
    expect(disponibilidad.estado()).toBe('inactivo');
  });

  it('no reintenta el GET ante 401, ni envia autenticacion', () => {
    const auth = TestBed.inject(AuthService);
    let error: HttpErrorResponse | undefined;
    auth.login({ nombreUsuario: 'operador', password: 'clave' }).subscribe({
      error: err => { error = err; }
    });
    httpMock.expectOne(READY).flush({}, { status: 401, statusText: 'Unauthorized' });
    vi.advanceTimersByTime(10000);
    expect(error?.status).toBe(401);
    httpMock.expectNone(READY);
    httpMock.expectNone(LOGIN);
  });

  it('detiene en tres intentos de readiness fallidos y jamas envia POST', () => {
    const auth = TestBed.inject(AuthService);
    const disponibilidad = TestBed.inject(ApiAvailabilityService);
    let error: HttpErrorResponse | undefined;
    auth.login({ nombreUsuario: 'operador', password: 'clave' }).subscribe({
      error: err => { error = err; }
    });
    httpMock.expectOne(READY).flush({}, { status: 503, statusText: 'Unavailable' });
    vi.advanceTimersByTime(2500);
    httpMock.expectOne(READY).flush({}, { status: 503, statusText: 'Unavailable' });
    vi.advanceTimersByTime(5000);
    httpMock.expectOne(READY).flush({}, { status: 503, statusText: 'Unavailable' });
    expect(error?.status).toBe(503);
    expect(disponibilidad.estado()).toBe('inactivo');
    httpMock.expectNone(READY);
    httpMock.expectNone(LOGIN);
  });
});
