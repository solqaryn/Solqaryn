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

  it('una revocacion con GET pendiente descarta el 200 tardio y no restaura autoridad', () => {
    let exitos = 0;
    let rechazado = false;
    plataforma.obtener().subscribe({ next: () => exitos++, error: () => { rechazado = true; } });
    const pendiente = http.expectOne(endpoint);
    plataforma.limpiar();
    pendiente.flush({
      success: true,
      data: { nombreUsuario: 'admin-antiguo', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
    });
    expect(rechazado).toBe(true);
    expect(exitos).toBe(0);
    expect(plataforma.sesion()).toBeNull();
  });

  it('un 200 anterior no reemplaza la sesion confirmada por comprobacion posterior', () => {
    let antiguoRechazado = false;
    plataforma.obtener().subscribe({ error: () => { antiguoRechazado = true; } });
    const anterior = http.expectOne(endpoint);
    plataforma.obtener().subscribe();
    const actual = http.expectOne(endpoint);
    actual.flush({
      success: true,
      data: { nombreUsuario: 'admin-actual', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
    });
    anterior.flush({
      success: true,
      data: { nombreUsuario: 'admin-antiguo', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
    });
    expect(antiguoRechazado).toBe(true);
    expect(plataforma.sesion()?.nombreUsuario).toBe('admin-actual');
  });

  it('el error tardio de una solicitud sustituida no revoca la sesion mas reciente', () => {
    plataforma.obtener().subscribe({ error: () => {} });
    const anterior = http.expectOne(endpoint);
    plataforma.obtener().subscribe();
    const actual = http.expectOne(endpoint);
    actual.flush({
      success: true,
      data: { nombreUsuario: 'admin-vigente', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }
    });
    anterior.flush({ message: 'credencial-antigua' }, { status: 403, statusText: 'Forbidden' });
    expect(plataforma.sesion()?.nombreUsuario).toBe('admin-vigente');
  });
});
