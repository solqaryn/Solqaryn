import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

describe('C13-04.M05.02 contrato HTTP/DTO de la consola global (cliente)', () => {
  const endpoint = environment.apiUrl + '/plataforma/sesion';
  const admin = Object.freeze({
    nombreUsuario: 'usuario-global',
    rolNombre: 'Administrador',
    ambito: 'Plataforma',
    esAdministrador: true
  });
  let http: HttpTestingController;
  let servicio: PlataformaSesionService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
    servicio = TestBed.inject(PlataformaSesionService);
  });

  afterEach(() => http.verify());

  function consultar() {
    const respuestas: unknown[] = [];
    const errores: unknown[] = [];
    servicio.obtener().subscribe({
      next: valor => respuestas.push(valor),
      error: error => errores.push(error)
    });
    return { respuestas, errores };
  }

  function exito(data: unknown, success = true) {
    return { success, data, message: '', errors: [] };
  }

  it('es GET exclusivo, observable frio y contrato positivo con autoridad del backend', () => {
    const consulta = servicio.obtener();
    http.expectNone(endpoint);
    const respuestas: unknown[] = [];
    consulta.subscribe(valor => respuestas.push(valor));
    const request = http.expectOne(endpoint);
    expect(request.request.method).toBe('GET');
    expect(request.request.body).toBeNull();
    request.flush(exito(admin));
    expect(respuestas).toHaveLength(1);
    expect(servicio.sesion()?.ambito).toBe('Plataforma');
    expect(servicio.sesion()?.esAdministrador).toBe(true);
  });

  it('rechaza envelope success=false, data nula y respuestas ambiguas HTTP 200', () => {
    for (const payload of [
      exito(admin, false), exito(null), { data: admin },
      { success: true, data: null }, []
    ]) {
      const { respuestas, errores } = consultar();
      http.expectOne(endpoint).flush(payload);
      expect(respuestas).toHaveLength(0);
      expect(errores).toHaveLength(1);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('rechaza un administrador empresarial, un flag false y ambito inventado', () => {
    for (const data of [
      { ...admin, ambito: 'Empresa' },
      { ...admin, esAdministrador: false },
      { ...admin, ambito: 'GlobalFalso' }
    ]) {
      const { respuestas, errores } = consultar();
      http.expectOne(endpoint).flush(exito(data));
      expect(respuestas).toHaveLength(0);
      expect(errores).toHaveLength(1);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('rechaza nombres de usuario o rol vacios, no textuales o whitespace', () => {
    for (const data of [
      { ...admin, nombreUsuario: '' },
      { ...admin, nombreUsuario: '   ' },
      { ...admin, nombreUsuario: 123 },
      { ...admin, rolNombre: '' },
      { ...admin, rolNombre: ' \t ' },
      { ...admin, rolNombre: null }
    ]) {
      const { respuestas, errores } = consultar();
      http.expectOne(endpoint).flush(exito(data));
      expect(respuestas).toHaveLength(0);
      expect(errores).toHaveLength(1);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('revoca de inmediato la sesion anterior antes de segunda respuesta HTTP', () => {
    consultar();
    http.expectOne(endpoint).flush(exito(admin));
    expect(servicio.sesion()).not.toBeNull();

    const { respuestas, errores } = consultar();
    expect(servicio.sesion()).toBeNull();
    const request = http.expectOne(endpoint);
    expect(request.request.method).toBe('GET');
    request.flush({}, { status: 403, statusText: 'Forbidden' });
    expect(respuestas).toHaveLength(0);
    expect(errores).toHaveLength(1);
    expect(servicio.sesion()).toBeNull();
  });

  it('fallos 401, 403, 500 y 503 nunca dejan autoridad residual', () => {
    for (const status of [401, 403, 500, 503]) {
      const { respuestas, errores } = consultar();
      http.expectOne(endpoint).flush({ errors: ['error opaco'] }, {
        status, statusText: 'Error'
      });
      expect(respuestas).toHaveLength(0);
      expect(errores).toHaveLength(1);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('rechaza JSON malformed aunque el servidor devuelva 200 y no cachea permisos', () => {
    consultar();
    http.expectOne(endpoint).flush(exito(admin));
    expect(servicio.sesion()).not.toBeNull();

    const { respuestas, errores } = consultar();
    http.expectOne(endpoint).flush('no-es-json', {
      status: 200, statusText: 'OK'
    });
    expect(respuestas).toHaveLength(0);
    expect(errores).toHaveLength(1);
    expect(servicio.sesion()).toBeNull();
  });

  it('limpiar borra la sesion autorizada sin generar solicitudes HTTP mutantes', () => {
    consultar();
    http.expectOne(endpoint).flush(exito(admin));
    expect(servicio.sesion()).not.toBeNull();
    servicio.limpiar();
    servicio.limpiar();
    expect(servicio.sesion()).toBeNull();
    http.expectNone(endpoint);
  });
});
