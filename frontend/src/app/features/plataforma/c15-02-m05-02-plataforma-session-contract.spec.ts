import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

const plataforma = {
  nombreUsuario: 'Auditora', rolNombre: 'Plataforma',
  ambito: 'Plataforma', esAdministrador: true
} as const;

/** Contrato HttpClient/DTO de la pantalla privada C15-02. HttpTestingController
 * no es HTTP real, no firma JWT y no acredita MySQL/tenancy de backend.
 */
describe('C15-02.M05.02 contrato cliente de sesion de plataforma', () => {
  let http: HttpTestingController;
  let servicio: PlataformaSesionService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PlataformaSesionService, provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
    servicio = TestBed.inject(PlataformaSesionService);
  });

  afterEach(() => http.verify());

  function obtenerPeticion() {
    const request = http.expectOne(r => r.url.endsWith('/plataforma/sesion'));
    expect(request.request.method).toBe('GET');
    return request;
  }

  it('acepta solo DTO global validado en GET y conserva estado sin tenant local', () => {
    let resultado: unknown;
    servicio.obtener().subscribe(value => { resultado = value; });
    expect(servicio.sesion()).toBeNull();
    const request = obtenerPeticion();
    expect(request.request.body).toBeNull();
    request.flush({ success: true, data: plataforma });
    expect(resultado).toEqual(plataforma);
    expect(servicio.sesion()).toEqual(plataforma);
    expect(Object.keys(servicio.sesion()!)).toEqual([
      'nombreUsuario', 'rolNombre', 'ambito', 'esAdministrador'
    ]);
  });

  it('rechaza 403 con mensaje hostil y vacia sesion sin conceder autoridad', () => {
    let error: unknown;
    servicio.obtener().subscribe({ error: reason => { error = reason; } });
    obtenerPeticion().flush({ success: false, message: 'PRIVATE_CONNECTION_STRING_NEVER_IN_UI' },
      { status: 403, statusText: 'Forbidden' });
    expect(error).toBeDefined();
    expect(servicio.sesion()).toBeNull();
  });

  it('deniega respuesta 200 con ambito Empresa o administrador falso', () => {
    for (const incorrecto of [
      { ...plataforma, ambito: 'Empresa', esAdministrador: true },
      { ...plataforma, ambito: 'Plataforma', esAdministrador: false }
    ]) {
      let aceptado = false;
      let rechazado = false;
      servicio.obtener().subscribe({
        next: () => { aceptado = true; },
        error: () => { rechazado = true; }
      });
      obtenerPeticion().flush({ success: true, data: incorrecto });
      expect(aceptado).toBe(false);
      expect(rechazado).toBe(true);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('deniega contrato incompleto o success=false aunque HTTP devuelva 200', () => {
    for (const response of [
      { success: true, data: { ...plataforma, nombreUsuario: ' ' } },
      { success: false, data: plataforma },
      { success: true, data: null }
    ]) {
      let rechazado = false;
      servicio.obtener().subscribe({ error: () => { rechazado = true; } });
      obtenerPeticion().flush(response);
      expect(rechazado).toBe(true);
      expect(servicio.sesion()).toBeNull();
    }
  });

  it('revocacion invalida respuesta antigua sin borrar una autorizacion posterior', () => {
    let obsoletaRechazada = false;
    servicio.obtener().subscribe({ error: () => { obsoletaRechazada = true; } });
    const antigua = obtenerPeticion();
    servicio.limpiar();
    expect(servicio.sesion()).toBeNull();
    let nuevaAceptada = false;
    servicio.obtener().subscribe({ next: () => { nuevaAceptada = true; } });
    const nueva = obtenerPeticion();
    nueva.flush({ success: true, data: plataforma });
    expect(nuevaAceptada).toBe(true);
    antigua.flush({ success: true, data: {
      ...plataforma, nombreUsuario: 'STALE_ADMIN_SHOULD_NOT_REPLACE_NEW'
    } });
    expect(obsoletaRechazada).toBe(true);
    expect(servicio.sesion()?.nombreUsuario).toBe('Auditora');
  });

  it('un 403 antiguo tampoco invalida una sesion nueva confirmada', () => {
    let antiguoRechazado = false;
    servicio.obtener().subscribe({ error: () => { antiguoRechazado = true; } });
    const antigua = obtenerPeticion();
    servicio.obtener().subscribe();
    obtenerPeticion().flush({ success: true, data: plataforma });
    antigua.flush({ success: false, message: 'OLD_PRIVATE_ERROR' },
      { status: 403, statusText: 'Forbidden' });
    expect(antiguoRechazado).toBe(true);
    expect(servicio.sesion()).toEqual(plataforma);
  });
});
