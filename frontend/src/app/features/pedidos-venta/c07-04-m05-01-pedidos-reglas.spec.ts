import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EstadoPedidoVenta } from './pedido-venta.model';
import { PedidoVentaService } from './pedido-venta.service';

describe('C07-04.M05.01 - reglas HTTP de listado de pedidos reales', () => {
  let servicio: PedidoVentaService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PedidoVentaService, provideHttpClient(), provideHttpClientTesting()]
    });
    servicio = TestBed.inject(PedidoVentaService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('normaliza limites inferiores y devuelve el total del contrato real', () => {
    let total = -1;
    servicio.getPaged({ page: -8, pageSize: 0 }).subscribe(r => { total = r.data.totalCount; });
    const req = http.expectOne(x => x.url.endsWith('/pedidos-venta'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('1');
    expect(req.request.headers.has('Idempotency-Key')).toBe(false);
    req.flush({ success: true, data: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } });
    expect(total).toBe(0);
  });

  it('trunca pagina fraccional, limita lote a 100 y preserva orden', () => {
    servicio.getPaged({ page: 4.9, pageSize: 900, sortBy: 'Id', sortDirection: 'desc' }).subscribe();
    const req = http.expectOne(x => x.url.endsWith('/pedidos-venta'));
    expect(req.request.params.get('page')).toBe('4');
    expect(req.request.params.get('pageSize')).toBe('100');
    expect(req.request.params.get('sortBy')).toBe('Id');
    expect(req.request.params.get('sortDirection')).toBe('desc');
    req.flush({ success: true, data: { items: [], page: 4, pageSize: 100, totalCount: 0, totalPages: 0 } });
  });

  it('transmite exclusivamente filtros positivos explicitos', () => {
    servicio.getPaged({ page: 1, pageSize: 10, cotizacionId: 12, clienteId: 9,
      estado: EstadoPedidoVenta.Confirmado, fechaDesdeUtc: '2026-10-01T00:00:00Z',
      fechaHastaUtc: '2026-10-31T23:59:59Z' }).subscribe();
    const req = http.expectOne(x => x.url.endsWith('/pedidos-venta'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('cotizacionId')).toBe('12');
    expect(req.request.params.get('clienteId')).toBe('9');
    expect(req.request.params.get('estado')).toBe('2');
    expect(req.request.params.get('fechaDesdeUtc')).toBe('2026-10-01T00:00:00Z');
    expect(req.request.params.get('fechaHastaUtc')).toBe('2026-10-31T23:59:59Z');
    req.flush({ success: true, data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
  });

  it('omite filtros nulos o cero, sin pedir otro tenant ni enviar mutaciones', () => {
    servicio.getPaged({ page: 1, pageSize: 10, cotizacionId: 0, clienteId: 0, estado: null }).subscribe();
    const req = http.expectOne(x => x.url.endsWith('/pedidos-venta'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('cotizacionId')).toBe(false);
    expect(req.request.params.has('clienteId')).toBe(false);
    expect(req.request.params.has('estado')).toBe(false);
    expect(req.request.params.has('empresaId')).toBe(false);
    req.flush({ success: true, data: { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 } });
  });

  it('propaga HTTP 503 y no interpreta fallo como exito ni reintenta mutando', () => {
    let exito = false;
    let status: number | null = null;
    servicio.getPaged({ page: 1, pageSize: 10 }).subscribe({
      next: () => { exito = true; },
      error: e => { status = e.status; }
    });
    const req = http.expectOne(x => x.url.endsWith('/pedidos-venta'));
    expect(req.request.method).toBe('GET');
    req.flush({ message: 'Servicio temporalmente indisponible' },
      { status: 503, statusText: 'Service Unavailable' });
    expect(exito).toBe(false);
    expect(status).toBe(503);
  });
});
