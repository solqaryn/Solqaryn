import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { CreateCentroCostoDto, TipoCentroCosto, UpdateCentroCostoDto } from '../core/models/centro-costo.model';
import { CentroCostoService } from './centro-costo.service';

describe('N4.11.G CentroCostoService contract', () => {
  let service: CentroCostoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule], providers: [CentroCostoService] });
    service = TestBed.inject(CentroCostoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('uses the canonical activos endpoint', () => {
    service.getActivos().subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo/activos`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: [] });
  });

  it('uses the canonical detail endpoint', () => {
    service.getById(17).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo/17`);
    expect(req.request.method).toBe('GET');
    req.flush({ data: { id: 17 } });
  });

  it('serializes search filters and pagination without dropping false values', () => {
    service.buscar({
      termino: '  CC-01  ',
      tipo: TipoCentroCosto.Sucursal,
      sucursalId: 7,
      activo: false,
      pagina: 2,
      tamanoPagina: 25
    }).subscribe();

    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/centros-costo`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('termino')).toBe('CC-01');
    expect(req.request.params.get('tipo')).toBe(String(TipoCentroCosto.Sucursal));
    expect(req.request.params.get('sucursalId')).toBe('7');
    expect(req.request.params.get('activo')).toBe('false');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamanoPagina')).toBe('25');
    req.flush({ data: { items: [], total: 0, pagina: 2, tamanoPagina: 25 } });
  });

  it('posts the create payload to the canonical collection endpoint', () => {
    const payload: CreateCentroCostoDto = {
      codigo: 'CC-17',
      nombre: 'Centro 17',
      descripcion: 'Contrato create',
      tipo: TipoCentroCosto.Departamento,
      sucursalId: null
    };

    service.create(payload).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ data: { id: 17, ...payload, activo: true } });
  });

  it('puts the update payload to the canonical detail endpoint', () => {
    const payload: UpdateCentroCostoDto = {
      codigo: 'CC-18',
      nombre: 'Centro 18',
      descripcion: null,
      tipo: TipoCentroCosto.Proyecto,
      sucursalId: null,
      activo: false
    };

    service.update(18, payload).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo/18`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ data: { id: 18, ...payload } });
  });

  it('uses the canonical activate/deactivate endpoints', () => {
    service.cambiarEstado(9, true).subscribe();
    const activar = httpMock.expectOne(`${environment.apiUrl}/centros-costo/9/activar`);
    expect(activar.request.method).toBe('PATCH');
    expect(activar.request.body).toEqual({});
    activar.flush({ data: { id: 9, activo: true } });

    service.cambiarEstado(9, false).subscribe();
    const desactivar = httpMock.expectOne(`${environment.apiUrl}/centros-costo/9/desactivar`);
    expect(desactivar.request.method).toBe('PATCH');
    expect(desactivar.request.body).toEqual({});
    desactivar.flush({ data: { id: 9, activo: false } });
  });

  it('targets the canonical delete endpoint', () => {
    service.delete(11).subscribe();
    const req = httpMock.expectOne(`${environment.apiUrl}/centros-costo/11`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ data: {} });
  });
});
