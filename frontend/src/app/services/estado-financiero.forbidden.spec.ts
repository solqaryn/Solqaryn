import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EstadoFinancieroService } from './estado-financiero.service';
import { TipoEstadoFinanciero } from '../core/models/estado-financiero.model';

describe('EstadoFinancieroService 403 sanitization', () => {
  let service: EstadoFinancieroService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(EstadoFinancieroService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('converts a 403 response into a stable user-safe error without backend details', () => {
    let receivedError: unknown;

    service.generar(TipoEstadoFinanciero.BalanceGeneral, {}).subscribe({
      error: error => {
        receivedError = error;
      },
    });

    const request = httpMock.expectOne(req => req.url.endsWith('/estados-financieros/1'));
    request.flush(
      {
        message: 'Forbidden: Finanzas/Ver denied for internal user 4815',
        detail: 'policy=FinanceStatements.Read; traceId=secret-trace-403',
      },
      { status: 403, statusText: 'Forbidden' },
    );

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toBe(
      'No fue posible generar el estado financiero. Intente nuevamente.',
    );
    expect((receivedError as Error).message).not.toContain('Finanzas/Ver');
    expect((receivedError as Error).message).not.toContain('4815');
    expect((receivedError as Error).message).not.toContain('secret-trace-403');
  });
});
