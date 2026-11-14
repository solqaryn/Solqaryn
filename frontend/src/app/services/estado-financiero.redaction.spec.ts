import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { EstadoFinancieroService } from './estado-financiero.service';
import { TipoEstadoFinanciero } from '../core/models/estado-financiero.model';

describe('EstadoFinancieroService redaction', () => {
  let service: EstadoFinancieroService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    service = TestBed.inject(EstadoFinancieroService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('redacts sensitive backend details from HTTP errors', () => {
    let receivedError: unknown;

    service.generar(TipoEstadoFinanciero.BalanceGeneral, {}).subscribe({
      error: error => {
        receivedError = error;
      },
    });

    const request = httpMock.expectOne(req => req.url.endsWith('/estados-financieros/1'));
    request.flush(
      { message: 'ORA-00942: table FINANZAS_SECRET does not exist', stackTrace: '/srv/api/secret.cs:42' },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(receivedError).toBeInstanceOf(Error);
    expect((receivedError as Error).message).toBe('No fue posible generar el estado financiero. Intente nuevamente.');
    expect((receivedError as Error).message).not.toContain('ORA-00942');
    expect((receivedError as Error).message).not.toContain('FINANZAS_SECRET');
  });
});
