import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../environments/environment';
import { EstadoPagoOnline, PagoOnlineService } from './pago-online.service';

describe('PagoOnlineService', () => {
  let service: PagoOnlineService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(PagoOnlineService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('inicia pagos sin campos PAN/CVV y transmite Idempotency-Key', () => {
    service.iniciar({
      empresaId: 7,
      facturaId: 42,
      proveedor: 'demo',
      monto: 125.5,
      moneda: 'HNL'
    }, 'n78e-1234567890123456').subscribe();

    const request = httpMock.expectOne(`${environment.apiUrl}/pagos-online/iniciar`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Idempotency-Key')).toBe('n78e-1234567890123456');
    expect(request.request.body).toEqual({
      empresaId: 7,
      facturaId: 42,
      proveedor: 'demo',
      monto: 125.5,
      moneda: 'HNL'
    });
    expect(request.request.body.pan).toBeUndefined();
    expect(request.request.body.cvv).toBeUndefined();

    request.flush({ success: true, message: 'ok', data: { pago: {}, reutilizado: false } });
  });

  it('lista por tenant, factura y estado', () => {
    service.listar(7, 42, EstadoPagoOnline.Pendiente, 2, 10).subscribe();

    const request = httpMock.expectOne((req) =>
      req.url === `${environment.apiUrl}/pagos-online`
      && req.params.get('empresaId') === '7'
      && req.params.get('facturaId') === '42'
      && req.params.get('estado') === '1'
      && req.params.get('pagina') === '2'
      && req.params.get('tamanoPagina') === '10'
    );
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, message: 'ok', data: { items: [], total: 0, pagina: 2, tamanoPagina: 10 } });
  });
});
