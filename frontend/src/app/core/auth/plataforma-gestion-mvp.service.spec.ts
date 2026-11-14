import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PlataformaGestionMvpService } from './plataforma-gestion-mvp.service';

describe('Microfase 4 — cliente global de gestión MVP', () => {
  let http: HttpTestingController;
  let api: PlataformaGestionMvpService;
  const root = environment.apiUrl + '/plataforma/gestion';

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    http = TestBed.inject(HttpTestingController);
    api = TestBed.inject(PlataformaGestionMvpService);
  });
  afterEach(() => http.verify());

  it('consulta solo roles empresariales del tenant seleccionado', () => {
    let roles: { id: number }[] = [];
    api.roles(10).subscribe(data => { roles = data; });
    const request = http.expectOne(r => r.url === root + '/roles');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('empresaId')).toBe('10');
    request.flush({ success: true, data: [
      { id: 3, nombre: 'Administrador empresa', empresaId: null, esAdministrador: true }
    ] });
    expect(roles).toHaveLength(1);
  });

  it('rechaza un rol ambiguo o respuesta fallida del servidor', () => {
    let failed = false;
    api.roles(1).subscribe({ error: () => { failed = true; } });
    http.expectOne(r => r.url === root + '/roles').flush({
      success: true, data: [{ id: 1, nombre: 'Global', esAdministrador: 'true', empresaId: null }]
    });
    expect(failed).toBe(true);
  });

  it('identidad sin membresía se identifica como pendiente, no como sesión activa', () => {
    let pendiente = false;
    api.buscarUsuario('nuevo').subscribe(d => { pendiente = d?.pendienteAsignacion === true; });
    const req = http.expectOne(r => r.url === root + '/usuarios/buscar');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('nombreUsuario')).toBe('nuevo');
    req.flush({ success: true, data: {
      id: 7, nombreUsuario: 'nuevo', nombreCompleto: 'Nuevo usuario',
      activo: false, bloqueado: false, pendienteAsignacion: true
    } });
    expect(pendiente).toBe(true);
  });

  it('las altas de cuentas jamás envían EmpresaId como autorización', () => {
    let complete = false;
    api.crearUsuario('comercial', 'Operador Tienda', 'ClaveSegura!42')
      .subscribe(() => { complete = true; });
    const request = http.expectOne(root + '/usuarios');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      nombreUsuario: 'comercial', nombreCompleto: 'Operador Tienda', password: 'ClaveSegura!42'
    });
    expect(request.request.headers.has('X-Empresa-Id')).toBe(false);
    request.flush({ success: true, data: { id: 9, nombreUsuario: 'comercial' } });
    expect(complete).toBe(true);
  });

  it('no convierte un 403 de mutación en éxito', () => {
    let error = false;
    let cambio = false;
    api.cambiarRol(3, 9, 10).subscribe({
      next: () => { cambio = true; },
      error: () => { error = true; }
    });
    const req = http.expectOne(root + '/empresas/3/membresias/9/rol');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ rolId: 10 });
    req.flush({}, { status: 403, statusText: 'Forbidden' });
    expect(error).toBe(true);
    expect(cambio).toBe(false);
  });

  it('el alta exige confirmación del servidor con ID y admin correcto', () => {
    let failed = false;
    api.altaControlada('Nueva empresa', 5)
      .subscribe({ error: () => { failed = true; } });
    const req = http.expectOne(environment.apiUrl + '/plataforma/empresas/alta-controlada');
    expect(req.request.body).toEqual({ nombre: 'Nueva empresa', administradorUsuarioId: 5 });
    req.flush({ empresaId: 99, administradorUsuarioId: 8 });
    expect(failed).toBe(true);
  });

  it('alta atómica envía credenciales solo en la petición única, sin selector empresarial', () => {
    let resultado = false;
    api.altaControladaConAdministradorNuevo('Nueva empresa', {
      nombreUsuario: 'nuevo.admin', nombreCompleto: 'Administrador Nuevo',
      password: 'ClaveSegura!42'
    }).subscribe(() => { resultado = true; });
    const req = http.expectOne(environment.apiUrl + '/plataforma/empresas/alta-controlada');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.has('X-Empresa-Id')).toBe(false);
    expect(req.request.body).toEqual({
      nombre: 'Nueva empresa',
      administradorNuevo: {
        nombreUsuario: 'nuevo.admin', nombreCompleto: 'Administrador Nuevo',
        password: 'ClaveSegura!42'
      }
    });
    req.flush({ empresaId: 91, administradorUsuarioId: 92, operacionComercialHabilitada: false });
    expect(resultado).toBe(true);
  });

  it('alta atómica no convierte una confirmación incompleta ni un 403 en éxito', () => {
    let errores = 0;
    const cuenta = {
      nombreUsuario: 'nuevo.admin', nombreCompleto: 'Administrador Nuevo',
      password: 'ClaveSegura!42'
    };
    api.altaControladaConAdministradorNuevo('Empresa nueva', cuenta)
      .subscribe({ error: () => { errores++; } });
    http.expectOne(environment.apiUrl + '/plataforma/empresas/alta-controlada')
      .flush({ empresaId: 21, administradorUsuarioId: 0, operacionComercialHabilitada: false });
    api.altaControladaConAdministradorNuevo('Empresa nueva', cuenta)
      .subscribe({ error: () => { errores++; } });
    http.expectOne(environment.apiUrl + '/plataforma/empresas/alta-controlada')
      .flush({}, { status: 403, statusText: 'Forbidden' });
    expect(errores).toBe(2);
  });

  it('rechaza membresías inválidas o amplias', () => {
    let failed = false;
    api.miembros(22).subscribe({ error: () => { failed = true; } });
    http.expectOne(root + '/empresas/22/membresias').flush({
      success: true, data: [{ usuarioId: 11, nombreUsuario: 'U',
        rolId: 1, activa: 'yes', usuarioActivo: true }]
    });
    expect(failed).toBe(true);
  });
});
