import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ReporteComprasFiltroDto } from '../models/reporte-compras.models';
import { ReporteComprasService } from './reporte-compras.service';

describe('ReporteComprasService', () => {
  let service: ReporteComprasService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReporteComprasService],
    });
    service = TestBed.inject(ReporteComprasService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('maps accepted filters to GET query params', () => {
    const filtro: ReporteComprasFiltroDto = {
      desdeUtc: '2026-09-01T00:00:00Z',
      hastaUtc: '2026-09-10T00:00:00Z',
      proveedorId: 10,
      estadoFactura: 'Registrada',
      page: 1,
      pageSize: 50,
    };

    service.getDetalle(filtro).subscribe();

    const req = httpMock.expectOne((request) =>
      request.url === `${environment.apiUrl}/compras/reportes/detalle` &&
      request.params.get('desdeUtc') === filtro.desdeUtc &&
      request.params.get('hastaUtc') === filtro.hastaUtc &&
      request.params.get('proveedorId') === '10' &&
      request.params.get('estadoFactura') === 'Registrada' &&
      request.params.get('page') === '1' &&
      request.params.get('pageSize') === '50',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 } });
  });

  it('omits null and undefined optional filters', () => {
    service.getDetalle({ page: 1, pageSize: 20, proveedorId: null, productoId: undefined }).subscribe();

    const req = httpMock.expectOne((request) => request.url === `${environment.apiUrl}/compras/reportes/detalle`);
    expect(req.request.params.has('proveedorId')).toBe(false);
    expect(req.request.params.has('productoId')).toBe(false);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ success: true, data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } });
  });
});
