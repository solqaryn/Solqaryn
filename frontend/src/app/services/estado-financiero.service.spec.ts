import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { TipoEstadoFinanciero, EstadoFinancieroFiltro } from '../core/models/estado-financiero.model';
import { EstadoFinancieroService } from './estado-financiero.service';

describe('EstadoFinancieroService contract regression', () => {
  let service: EstadoFinancieroService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [EstadoFinancieroService],
    });
    service = TestBed.inject(EstadoFinancieroService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('uses the canonical endpoint with expected type', () => {
    const filtro: EstadoFinancieroFiltro = {};
    service.generar(TipoEstadoFinanciero.BalanceGeneral, filtro).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/estados-financieros/1`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('adds periodoContableId when present', () => {
    const filtro: EstadoFinancieroFiltro = { periodoContableId: 42 };
    service.generar(TipoEstadoFinanciero.EstadoResultados, filtro).subscribe();

    const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/estados-financieros/2`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('periodoContableId')).toBe('42');
    req.flush({});
  });

  it('adds date parameters when present', () => {
    const filtro: EstadoFinancieroFiltro = { fechaDesde: '2023-01-01', fechaHasta: '2023-12-31' };
    service.generar(TipoEstadoFinanciero.BalanceComprobacion, filtro).subscribe();

    const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/estados-financieros/3`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('fechaDesde')).toBe('2023-01-01');
    expect(req.request.params.get('fechaHasta')).toBe('2023-12-31');
    expect(req.request.params.has('periodoContableId')).toBe(false);
    req.flush({});
  });

  it('omits parameters that are absent', () => {
    const filtro: EstadoFinancieroFiltro = { fechaDesde: '2024-01-01' };
    service.generar(TipoEstadoFinanciero.LibroDiario, filtro).subscribe();

    const req = httpMock.expectOne(req => req.url === `${environment.apiUrl}/estados-financieros/4`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('fechaDesde')).toBe('2024-01-01');
    expect(req.request.params.has('fechaHasta')).toBe(false);
    expect(req.request.params.has('periodoContableId')).toBe(false);
    req.flush({});
  });

  it('converts generic internal server errors to the redacted generic generation error', () => {
    let receivedError: any;
    const filtro: EstadoFinancieroFiltro = {};
    service.generar(TipoEstadoFinanciero.BalanceGeneral, filtro).subscribe({
      error: err => receivedError = err,
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/estados-financieros/1`);
    req.flush('Internal Server Error', { status: 500, statusText: 'Server Error' });

    expect(receivedError).toBeInstanceOf(Error);
    expect(receivedError.message).toBe('No fue posible generar el estado financiero. Intente nuevamente.');
  });
});
