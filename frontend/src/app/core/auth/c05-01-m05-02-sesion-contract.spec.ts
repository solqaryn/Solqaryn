import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

// C05-01.M05.02: HttpClient real con transporte interceptado por Angular; no red externa JWT/MySQL.
const autorizado = {
  nombreUsuario: 'Global', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
};

describe('C05-01.M05.02 contrato API/DTO seguro de sesion de plataforma', () => {
  let service: PlataformaSesionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PlataformaSesionService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(PlataformaSesionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function esperarGet() {
    const request = http.expectOne(req =>
      req.method === 'GET' && req.url.endsWith('/plataforma/sesion')
    );
    expect(request.request.body).toBeNull();
    return request;
  }

  it('valida DTO positivo y concede estado local solo con autoridad Plataforma completa', async () => {
    const response = firstValueFrom(service.obtener());
    esperarGet().flush({ success: true, data: autorizado });
    await expect(response).resolves.toEqual(autorizado);
    expect(service.sesion()).toEqual(autorizado);
  });

  it('rechaza 200 con ambito Empresa aunque declare esAdministrador verdadero', async () => {
    const response = firstValueFrom(service.obtener());
    esperarGet().flush({ success: true, data: { ...autorizado, ambito: 'Empresa' } });
    await expect(response).rejects.toThrow(/sesión de plataforma válida/);
    expect(service.sesion()).toBeNull();
  });

  it('rechaza DTO incompleto, rol vacío y esAdministrador false sin autoridad residual', async () => {
    for (const payload of [
      { ...autorizado, esAdministrador: false },
      { ...autorizado, rolNombre: ' ' },
      { ...autorizado, nombreUsuario: '' },
      null
    ]) {
      const response = firstValueFrom(service.obtener());
      esperarGet().flush({ success: true, data: payload });
      await expect(response).rejects.toThrow();
      expect(service.sesion()).toBeNull();
    }
  });

  it('maneja 403 opaco como error, no conserva sesión y no crea otra solicitud', async () => {
    const response = firstValueFrom(service.obtener());
    esperarGet().flush({ success: false, data: null, message: 'INTERNAL_PROVIDER_DETAIL' }, {
      status: 403, statusText: 'Forbidden'
    });
    await expect(response).rejects.toBeTruthy();
    expect(service.sesion()).toBeNull();
    http.expectNone(req => req.method !== 'GET');
  });

  it('revoca sesión local previa al revalidar y limpieza explícita niega acceso', async () => {
    const first = firstValueFrom(service.obtener());
    esperarGet().flush({ success: true, data: autorizado });
    await first;
    expect(service.sesion()).toEqual(autorizado);
    const pending = firstValueFrom(service.obtener());
    expect(service.sesion()).toBeNull();
    esperarGet().flush({ success: true, data: autorizado });
    await pending;
    service.limpiar();
    expect(service.sesion()).toBeNull();
  });
});
