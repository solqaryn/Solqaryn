import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { StorefrontCuentaService } from './storefront-cuenta.service';

describe('QA — aislamiento de sesión de cuenta Storefront', () => {
  const key = 'storefront:cuenta:session:v1';
  const base = `${environment.apiUrl}/tienda/cuenta`;
  const anterior = 'a'.repeat(40);
  const nueva = 'b'.repeat(40);
  const perfilAnterior = { id: 1, nombre: 'Cliente anterior QA', correo: 'anterior@example.invalid' };
  const perfilNuevo = { id: 2, nombre: 'Cliente nuevo QA', correo: 'nuevo@example.invalid' };
  const respuesta = <T>(data: T) => ({ success: true, message: '', errors: [], data });
  const sesionNueva = () => respuesta({ token: nueva, expiraUtc: new Date(Date.now() + 60000).toISOString(), perfil: perfilNuevo });
  let service: StorefrontCuentaService;
  let http: HttpTestingController;
  let original: string | null;

  beforeEach(() => {
    original = sessionStorage.getItem(key);
    sessionStorage.setItem(key, anterior);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(StorefrontCuentaService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => {
    http.verify();
    if (original === null) sessionStorage.removeItem(key);
    else sessionStorage.setItem(key, original);
  });

  it('no restaura una sesión cuya respuesta llega después de cerrar sesión', () => {
    const resultado = vi.fn();
    service.restaurar().subscribe(resultado);
    const restore = http.expectOne(base);
    service.logout().subscribe();
    const logout = http.expectOne(base + '/logout');
    expect(logout.request.headers.get('X-Storefront-Session')).toBe(anterior);
    expect(sessionStorage.getItem(key)).toBeNull();
    restore.flush(respuesta(perfilAnterior));
    logout.flush(respuesta({}));
    expect(resultado).toHaveBeenCalledWith(null);
    expect(service.perfil()).toBeNull();
  });

  it('un 401 tardío de la restauración anterior no borra una sesión nueva', () => {
    service.restaurar().subscribe();
    const restore = http.expectOne(base);
    service.login(perfilNuevo.correo, 'ClaveSinteticaQA').subscribe();
    http.expectOne(base + '/login').flush(sesionNueva());
    restore.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    expect(service.perfil()).toEqual(perfilNuevo);
    expect(sessionStorage.getItem(key)).toBe(nueva);
  });

  it('una respuesta tardía de logout no invalida un nuevo inicio de sesión', () => {
    service.logout().subscribe();
    const logout = http.expectOne(base + '/logout');
    service.login(perfilNuevo.correo, 'ClaveSinteticaQA').subscribe();
    http.expectOne(base + '/login').flush(sesionNueva());
    logout.flush(respuesta({}));
    expect(service.perfil()).toEqual(perfilNuevo);
    expect(sessionStorage.getItem(key)).toBe(nueva);
  });
});
