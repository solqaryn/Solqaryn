import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import {
  CreateCuentaBancariaDto,
  UpdateCuentaBancariaDto,
  CuentaBancaria,
  CuentaBancariaQueryFilter,
  EstadoCuentaBancaria
} from '../models/cuenta-bancaria';
import { CuentaBancariaPage } from '../models/cuenta-bancaria-page';
import { CuentaBancariaService } from './cuenta-bancaria.service';

describe('CuentaBancariaService', () => {
  let service: CuentaBancariaService;
  let httpTestingController: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/cuentas-bancarias`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CuentaBancariaService, provideHttpClient(withXhr()), provideHttpClientTesting()]
    });
    service = TestBed.inject(CuentaBancariaService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('serializa filtros y devuelve la página canónica', () => {
    const filter: CuentaBancariaQueryFilter = {
      page: 1,
      pageSize: 10,
      bancoId: 5,
      estado: EstadoCuentaBancaria.Activa,
      moneda: 'USD',
      searchTerm: 'test'
    };
    const response: CuentaBancariaPage<CuentaBancaria> = {
      items: [{ id: 1, bancoId: 5, nombre: 'Test', numeroCuenta: '123', moneda: 'USD', saldoInicial: 100, estado: EstadoCuentaBancaria.Activa }],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1
    };

    service.getAll(filter).subscribe(result => expect(result).toEqual(response));
    const req = httpTestingController.expectOne(request =>
      request.url === apiUrl && request.params.get('page') === '1' && request.params.get('pageSize') === '10' &&
      request.params.get('bancoId') === '5' && request.params.get('estado') === '1' && request.params.get('moneda') === 'USD' &&
      request.params.get('searchTerm') === 'test'
    );
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('obtiene cuentas activas', () => {
    const response: CuentaBancaria[] = [{ id: 1, bancoId: 2, nombre: 'Active', numeroCuenta: '456', moneda: 'HNL', saldoInicial: 0, estado: EstadoCuentaBancaria.Activa }];
    service.getActivas().subscribe(result => expect(result).toEqual(response));
    const req = httpTestingController.expectOne(`${apiUrl}/activas`);
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('propaga error en getActivas', () => {
    service.getActivas().subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(500) });
    const req = httpTestingController.expectOne(`${apiUrl}/activas`);
    expect(req.request.method).toBe('GET');
    req.flush('Internal Server Error', { status: 500, statusText: 'Server Error' });
  });

  it('obtiene una cuenta por id', () => {
    const response: CuentaBancaria = { id: 3, bancoId: 1, nombre: 'ById', numeroCuenta: '789', moneda: 'EUR', saldoInicial: 50, estado: EstadoCuentaBancaria.Inactiva };
    service.getById(3).subscribe(result => expect(result).toEqual(response));
    const req = httpTestingController.expectOne(`${apiUrl}/3`);
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('crea una cuenta', () => {
    const dto: CreateCuentaBancariaDto = { bancoId: 4, nombre: 'New', numeroCuenta: '000', moneda: 'USD', saldoInicial: 1000 };
    const response: CuentaBancaria = { ...dto, id: 10, estado: EstadoCuentaBancaria.Activa };
    service.create(dto).subscribe(result => expect(result).toEqual(response));
    const req = httpTestingController.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(response);
  });

  it('actualiza una cuenta', () => {
    const dto: UpdateCuentaBancariaDto = { nombre: 'Updated' };
    service.update(7, dto).subscribe();
    const req = httpTestingController.expectOne(`${apiUrl}/7`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush(null);
  });

  it('propaga error en update', () => {
    const dto: UpdateCuentaBancariaDto = { nombre: 'Error' };
    service.update(8, dto).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(400) });
    const req = httpTestingController.expectOne(`${apiUrl}/8`);
    expect(req.request.method).toBe('PUT');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
  });

  it('propaga 404 en update', () => {
    const dto: UpdateCuentaBancariaDto = { nombre: 'Missing' };
    service.update(404, dto).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(404) });
    const req = httpTestingController.expectOne(`${apiUrl}/404`);
    expect(req.request.method).toBe('PUT');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });
  });

  it('propaga 409 en update', () => {
    const dto: UpdateCuentaBancariaDto = { nombre: 'Conflict' };
    service.update(9, dto).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(409) });
    const req = httpTestingController.expectOne(`${apiUrl}/9`);
    expect(req.request.method).toBe('PUT');
    req.flush('Conflict', { status: 409, statusText: 'Conflict' });
  });

  it('propaga fallo de red en update', () => {
    const dto: UpdateCuentaBancariaDto = { nombre: 'Network' };
    service.update(10, dto).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(0) });
    const req = httpTestingController.expectOne(`${apiUrl}/10`);
    expect(req.request.method).toBe('PUT');
    req.error(new ProgressEvent('error'));
  });

  it('activa una cuenta', () => {
    service.activar(5).subscribe();
    const req = httpTestingController.expectOne(`${apiUrl}/5/activar`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });

  it('desactiva una cuenta', () => {
    service.desactivar(6).subscribe();
    const req = httpTestingController.expectOne(`${apiUrl}/6/desactivar`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({});
    req.flush(null);
  });

  it('propaga error en getAll (list)', () => {
    const filter: CuentaBancariaQueryFilter = { page: 1 };
    service.getAll(filter).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(500) });
    const req = httpTestingController.expectOne(request => request.url === apiUrl && request.params.get('page') === '1');
    expect(req.request.method).toBe('GET');
    req.flush('Internal Server Error', { status: 500, statusText: 'Server Error' });
  });

  it('propaga error en getById (get)', () => {
    service.getById(99).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(404) });
    const req = httpTestingController.expectOne(`${apiUrl}/99`);
    expect(req.request.method).toBe('GET');
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });
  });

  it('propaga error en create', () => {
    const dto: CreateCuentaBancariaDto = { bancoId: 4, nombre: 'New', numeroCuenta: '000', moneda: 'USD', saldoInicial: 1000 };
    service.create(dto).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(400) });
    const req = httpTestingController.expectOne(apiUrl);
    expect(req.request.method).toBe('POST');
    req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
  });

  it('propaga error en activar', () => {
    service.activar(5).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(403) });
    const req = httpTestingController.expectOne(`${apiUrl}/5/activar`);
    expect(req.request.method).toBe('PATCH');
    req.flush('Forbidden', { status: 403, statusText: 'Forbidden' });
  });

  it('propaga error en desactivar', () => {
    service.desactivar(6).subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(409) });
    const req = httpTestingController.expectOne(`${apiUrl}/6/desactivar`);
    expect(req.request.method).toBe('PATCH');
    req.flush('Conflict', { status: 409, statusText: 'Conflict' });
  });

  it('propaga un fallo de red en getAll sin convertirlo en éxito', () => {
    service.getAll().subscribe({ next: () => { throw new Error('debería haber fallado'); }, error: error => expect(error.status).toBe(0) });
    const req = httpTestingController.expectOne(apiUrl);
    expect(req.request.method).toBe('GET');
    req.error(new ProgressEvent('error'));
  });
});
