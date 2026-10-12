import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { SuscripcionSaaSService } from './suscripcion-saas.service';

describe('SuscripcionSaaSService', () => {
  let service: SuscripcionSaaSService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withXhr()), provideHttpClientTesting()] });
    service = TestBed.inject(SuscripcionSaaSService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the verified tenant route to read subscription and filtered limits', () => {
    service.obtenerActual(7).subscribe();
    const current = http.expectOne(`${environment.apiUrl}/saas/tenants/7/suscripcion`);
    expect(current.request.method).toBe('GET');
    current.flush({ success: true, data: {}, message: '', errors: [] });

    service.obtenerLimites(7, ' USUARIOS ', 2, 25).subscribe();
    const limits = http.expectOne(req => req.url === `${environment.apiUrl}/saas/tenants/7/limites`);
    expect(limits.request.method).toBe('GET');
    expect(limits.request.params.get('clave')).toBe('USUARIOS');
    expect(limits.request.params.get('pagina')).toBe('2');
    expect(limits.request.params.get('tamanoPagina')).toBe('25');
    limits.flush({ success: true, data: { items: [], pagina: 2, tamanoPagina: 25, total: 0 }, message: '', errors: [] });
  });

  it('sends the durable idempotency key on onboarding without putting tenant in the body', () => {
    const body = { planCodigo: 'PRO', inicioUtc: '2026-09-13T00:00:00.000Z' };
    service.onboarding(11, body, ' retry-key-1 ').subscribe();

    const request = http.expectOne(`${environment.apiUrl}/saas/tenants/11/onboarding`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Idempotency-Key')).toBe('retry-key-1');
    expect(request.request.body).toEqual(body);
    expect(request.request.body.empresaId).toBeUndefined();
    request.flush({ success: true, data: {}, message: '', errors: [] });
  });

  it('gets a normalized module entitlement inside the verified tenant route', () => {
    const response = {
      success: true,
      data: {
        moduloClave: 'INVENTARIO',
        habilitado: true,
        motivo: 1,
        planId: 10,
        planCodigo: 'PRO'
      },
      message: '',
      errors: []
    };

    service.obtenerEntitlementModulo(17, ' inventario ').subscribe(res => expect(res).toEqual(response as any));

    const request = http.expectOne(`${environment.apiUrl}/saas/tenants/17/modulos/INVENTARIO/entitlement`);
    expect(request.request.method).toBe('GET');
    request.flush(response);
  });

  it('fails closed before HTTP when module key is empty', () => {
    expect(() => service.obtenerEntitlementModulo(17, '   ')).toThrowError('La clave del módulo es obligatoria.');
    http.expectNone(() => true);
  });

  it('fails closed before HTTP when tenant is invalid', () => {
    expect(() => service.obtenerActual(0)).toThrowError('Se requiere un tenant verificado.');
    expect(() => service.obtenerEntitlementModulo(0, 'VENTAS')).toThrowError('Se requiere un tenant verificado.');
    http.expectNone(() => true);
  });
});
