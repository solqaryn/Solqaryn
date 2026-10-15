import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PlataformaSesionService } from './plataforma-sesion.service';

describe('Microfase 3 - autoridad global frontend fail-closed', () => {
  let http: HttpTestingController;
  let plataforma: PlataformaSesionService;
  const endpoint = environment.apiUrl + '/plataforma/sesion';

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    http = TestBed.inject(HttpTestingController);
    plataforma = TestBed.inject(PlataformaSesionService);
  });

  afterEach(() => http.verify());

  it('solo acepta la respuesta global emitida por el backend', () => {
    let autorizado = false;
    plataforma.obtener().subscribe(() => { autorizado = true; });
    const request = http.expectOne(endpoint);
    expect(request.request.method).toBe('GET');
    request.flush({
      success: true,
      data: { nombreUsuario: 'usuario-plataforma', rolNombre: 'Superadministrador', ambito: 'Plataforma', esAdministrador: true }
    });
    expect(autorizado).toBe(true);
    expect(plataforma.sesion()?.ambito).toBe('Plataforma');
  });

  it('rechaza administradores empresariales incluso con HTTP 200', () => {
    let error = false;
    plataforma.obtener().subscribe({ error: () => { error = true; } });
    http.expectOne(endpoint).flush({
      success: true,
      data: { nombreUsuario: 'admin-tenant', rolNombre: 'Administrador', ambito: 'Empresa', esAdministrador: true }
    });
    expect(error).toBe(true);
    expect(plataforma.sesion()).toBeNull();
  });

  it('pierde acceso inmediatamente ante revocación 403', () => {
    plataforma.obtener().subscribe();
    http.expectOne(endpoint).flush({
      success: true,
      data: { nombreUsuario: 'usuario-plataforma', rolNombre: 'Superadministrador', ambito: 'Plataforma', esAdministrador: true }
    });
    expect(plataforma.sesion()).not.toBeNull();

    let denegado = false;
    plataforma.obtener().subscribe({ error: () => { denegado = true; } });
    http.expectOne(endpoint).flush({}, { status: 403, statusText: 'Forbidden' });
    expect(denegado).toBe(true);
    expect(plataforma.sesion()).toBeNull();
  });

  it('rechaza 200 ambiguo o sin datos de autoridad', () => {
    let error = false;
    plataforma.obtener().subscribe({ error: () => { error = true; } });
    http.expectOne(endpoint).flush({ success: true, data: null });
    expect(error).toBe(true);
    expect(plataforma.sesion()).toBeNull();
  });
});
