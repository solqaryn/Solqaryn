import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiAvailabilityService } from './api-availability.service';
import { AuthService } from './auth.service';

describe('C02-01.M05.04: JWT renewal cannot revive an obsolete session', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  const sesion = (token: string, nombreUsuario: string) => ({
    token,
    nombreUsuario,
    nombreCompleto: nombreUsuario,
    rol: 'Operador',
    expiraEn: '2035-01-01T00:00:00Z'
  });

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem('solqaryn_token', 'jwt-A');
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [{
        provide: ApiAvailabilityService,
        useValue: { esperarAntesDeAutenticar: () => of(undefined) }
      }]
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('accepts a renewed JWT only while the original session remains current', () => {
    let completed = false;
    auth.renovarSesion().subscribe({ next: () => { completed = true; } });
    http.expectOne(`${environment.apiUrl}/auth/renovar`).flush({
      success: true, data: sesion('jwt-A-renewed', 'actorA')
    });
    expect(completed).toBe(true);
    expect(auth.getToken()).toBe('jwt-A-renewed');
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-A-renewed');
  });

  it('never restores an old actor after logout while POST /renovar is pending', () => {
    let failed = false;
    auth.renovarSesion().subscribe({ error: () => { failed = true; } });
    const request = http.expectOne(`${environment.apiUrl}/auth/renovar`);
    auth.logout();
    request.flush({ success: true, data: sesion('jwt-A-renewed', 'actorA') });
    expect(failed).toBe(true);
    expect(auth.getToken()).toBeNull();
    expect(localStorage.getItem('solqaryn_token')).toBeNull();
  });

  it('cannot overwrite a newer actor B login with a delayed actor A refresh', () => {
    let failed = false;
    auth.renovarSesion().subscribe({ error: () => { failed = true; } });
    const pendingOld = http.expectOne(`${environment.apiUrl}/auth/renovar`);

    auth.login({ nombreUsuario: 'actorB', password: 'test-password' }).subscribe();
    http.expectOne(`${environment.apiUrl}/auth/login`).flush({
      success: true, data: sesion('jwt-B', 'actorB')
    });
    pendingOld.flush({ success: true, data: sesion('jwt-A-renewed', 'actorA') });

    expect(failed).toBe(true);
    expect(auth.getToken()).toBe('jwt-B');
    expect(auth.nombreUsuario()).toBe('actorB');
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-B');
  });

  it('refuses a stale refresh if another tab replaced the persisted JWT', () => {
    let failed = false;
    auth.renovarSesion().subscribe({ error: () => { failed = true; } });
    const pendingOld = http.expectOne(`${environment.apiUrl}/auth/renovar`);
    localStorage.setItem('solqaryn_token', 'jwt-from-another-tab');
    pendingOld.flush({ success: true, data: sesion('jwt-A-renewed', 'actorA') });
    expect(failed).toBe(true);
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-from-another-tab');
    expect(auth.getToken()).toBe('jwt-A');
  });
});
