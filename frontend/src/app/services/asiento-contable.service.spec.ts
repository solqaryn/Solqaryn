import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AsientoContableService } from './asiento-contable.service';
import { environment } from '../../environments/environment';

describe('AsientoContableService', () => {
  let service: AsientoContableService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [AsientoContableService] });
    service = TestBed.inject(AsientoContableService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends list filters and pagination', () => {
    const response = { data: { items: [], total: 0 } };
    service.getAll({ desde: '2026-09-01', hasta: '2026-09-04', numero: 'AC-1', pagina: 1, tamano: 10 }).subscribe(res => expect(res).toEqual(response as any));
    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/asientos-contables`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('desde')).toBe('2026-09-01');
    expect(req.request.params.get('hasta')).toBe('2026-09-04');
    expect(req.request.params.get('numero')).toBe('AC-1');
    expect(req.request.params.get('pagina')).toBe('1');
    expect(req.request.params.get('tamano')).toBe('10');
    req.flush(response);
  });

  it('gets one asiento by id', () => {
    service.getById(7).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/asientos-contables/7`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: { id: 7 } });
  });

  it('posts a new asiento', () => {
    const dto = { concepto: 'Apertura', detalles: [{ cuentaContableId: 1, debe: 100, haber: 0 }, { cuentaContableId: 2, debe: 0, haber: 100 }] };
    service.create(dto).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/asientos-contables`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush({ data: {} });
  });
});
