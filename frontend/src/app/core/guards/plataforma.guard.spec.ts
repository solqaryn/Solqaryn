import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, isObservable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { PlataformaSesionService } from '../auth/plataforma-sesion.service';
import { plataformaGuard } from './plataforma.guard';

describe('Guard independiente del tenant - SOLQARYN Plataforma', () => {
  const auth = { isAuthenticated: vi.fn(() => true) };
  const url = environment.apiUrl + '/plataforma/sesion';
  let http: HttpTestingController;

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [provideRouter([]), { provide: AuthService, useValue: auth }]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function iniciarGuard() {
    return TestBed.runInInjectionContext(() =>
      plataformaGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
  }

  it('deniega sin JWT válido y no consulta la API', () => {
    auth.isAuthenticated.mockReturnValue(false);
    const resultado = iniciarGuard();
    expect(resultado).toBeInstanceOf(UrlTree);
    expect(String(resultado)).toBe('/login');
    expect(TestBed.inject(PlataformaSesionService).sesion()).toBeNull();
    http.expectNone(url);
  });

  it('autoriza la plataforma sin seleccionar EmpresaId cuando MySQL lo valida', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    const resultado = iniciarGuard();
    expect(isObservable(resultado)).toBe(true);
    if (!isObservable(resultado)) throw new Error('Se esperaba verificación HTTP.');
    const veredicto = firstValueFrom(resultado);
    const peticion = http.expectOne(url);
    expect(peticion.request.method).toBe('GET');
    peticion.flush({
      success: true,
      data: { nombreUsuario: 'usuario-global', rolNombre: 'Super Admin', ambito: 'Plataforma', esAdministrador: true }
    });
    expect(await veredicto).toBe(true);
  });

  it('revoca navegación por 403 sin dar acceso empresarial alternativo', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    const resultado = iniciarGuard();
    expect(isObservable(resultado)).toBe(true);
    if (!isObservable(resultado)) throw new Error('Se esperaba verificación HTTP.');
    const veredicto = firstValueFrom(resultado);
    http.expectOne(url).flush({}, { status: 403, statusText: 'Forbidden' });
    const destino = await veredicto;
    expect(destino).toBeInstanceOf(UrlTree);
    expect(String(destino)).toBe('/login?plataforma=denegado');
  });

  it('rechaza respuesta global falsa aun con estado HTTP 200', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    const resultado = iniciarGuard();
    expect(isObservable(resultado)).toBe(true);
    if (!isObservable(resultado)) throw new Error('Se esperaba verificación HTTP.');
    const veredicto = firstValueFrom(resultado);
    http.expectOne(url).flush({
      success: true,
      data: { nombreUsuario: 'admin-empresa', rolNombre: 'Administrador', ambito: 'Empresa', esAdministrador: true }
    });
    const destino = await veredicto;
    expect(destino).toBeInstanceOf(UrlTree);
    expect(String(destino)).toBe('/login?plataforma=no-disponible');
  });
});
