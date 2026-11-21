import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { EmpresaConfiguracionService } from './empresa-configuracion.service';
import { EmpresaIdentidadService } from './empresa-identidad.service';

/** PLAN D2043: HttpClient real in Angular, transport mocked; not a live JWT/MySQL/backend assertion. */
describe('C14-01.M05.02 - contrato API DTO privado y errores seguros', () => {
  let http: HttpTestingController;
  let api: EmpresaConfiguracionService;
  let identidad: EmpresaIdentidadService;
  const url = (empresaId: number) => `${environment.apiUrl}/empresa-configuracion/tenant/${empresaId}`;
  const dto = (empresaId: number, nombre: string, logoUrl: string | null = '/marca.svg') => ({
    success: true, data: { empresaId, nombre, logoUrl, rtn: 'PRIVATE-RTN', correoRemitente: 'private@example.invalid' }
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    api = TestBed.inject(EmpresaConfiguracionService);
    identidad = TestBed.inject(EmpresaIdentidadService);
  });
  afterEach(() => http.verify());

  it('consulta GET privado de tenant validado con ruta exacta y DTO estructurado', () => {
    let payload: unknown;
    api.getTenant(1).subscribe(value => { payload = value; });
    const req = http.expectOne(url(1));
    expect(req.request.method).toBe('GET');
    expect(req.request.body).toBeNull();
    expect(req.request.params.keys().length).toBe(0);
    req.flush(dto(1, 'Empresa A', '/a.svg'));
    expect(payload).toEqual(dto(1, 'Empresa A', '/a.svg'));
    http.expectNone(`${environment.apiUrl}/empresa-configuracion/publica`);
  });

  it('el consumidor solo presenta campos permitidos del DTO empresarial y no su RTN/correo', () => {
    identidad.cargarTenant(1).subscribe();
    http.expectOne(url(1)).flush(dto(1, 'Empresa A', '/a.svg'));
    expect(identidad.nombreSistema()).toBe('Empresa A');
    expect(identidad.logoUrl()).toBe('/a.svg');
    expect('rtn' in identidad.config()).toBe(false);
    expect('correoRemitente' in identidad.config()).toBe(false);
  });

  it('cambio A a B neutraliza marca A antes de que el HTTP de B responda', () => {
    identidad.cargarTenant(1).subscribe();
    http.expectOne(url(1)).flush(dto(1, 'Empresa A', '/a.svg'));
    identidad.cargarTenant(2).subscribe();
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
    http.expectOne(url(2)).flush(dto(2, 'Empresa B', '/b.svg'));
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('/b.svg');
  });

  it('DTO 200 con EmpresaId ajeno o nombre malicioso no otorga marca', () => {
    identidad.cargarTenant(1).subscribe();
    http.expectOne(url(1)).flush(dto(2, 'Empresa B', '/b.svg'));
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    identidad.cargarTenant(1).subscribe();
    http.expectOne(url(1)).flush(dto(1, '<svg onload=alert(1)>', '/a.svg'));
    expect(identidad.nombreSistema()).toBe('SOLQARYN');
    expect(identidad.logoUrl()).toBe('');
  });

  it('HTTP 401, 403, 404 y 503 preservan marca neutra sin repetir ni cambiar ruta', () => {
    for (const status of [401, 403, 404, 503]) {
      let received = false;
      identidad.cargarTenant(1).subscribe(() => { received = true; });
      const req = http.expectOne(url(1));
      expect(req.request.method).toBe('GET');
      req.flush({ error: 'PRIVATE_SERVER_DETAIL' }, { status, statusText: 'Denied' });
      expect(received).toBe(true);
      expect(identidad.nombreSistema()).toBe('SOLQARYN');
      expect(identidad.logoUrl()).toBe('');
      http.expectNone(url(1));
    }
  });

  it('respuesta A tardia no reemplaza B aun cuando ambas solicitudes HTTP existan', () => {
    identidad.cargarTenant(1).subscribe();
    const pendingA = http.expectOne(url(1));
    identidad.cargarTenant(2).subscribe();
    const pendingB = http.expectOne(url(2));
    pendingB.flush(dto(2, 'Empresa B', '/b.svg'));
    pendingA.flush(dto(1, 'Empresa A', '/a.svg'));
    expect(identidad.nombreSistema()).toBe('Empresa B');
    expect(identidad.logoUrl()).toBe('/b.svg');
  });
});
