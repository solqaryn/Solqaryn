import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { Factura } from '../../core/models/factura.model';
import { FacturaFiscalEmisionComponent } from './factura-fiscal-emision.component';

describe('N7.10.E fiscal issuance UX', () => {
  let httpMock: HttpTestingController;
  const empresaVerificada = signal<number | null>(7);
  const permisoCrear = signal(true);
  const tenantContext = { empresaIdVerificada: empresaVerificada };
  const permisos = { puede: vi.fn(() => permisoCrear()) };

  const factura = {
    id: 41,
    numeroFactura: 'FAC-000041',
    fechaEmision: '2026-09-14T18:00:00Z',
    estado: 'Emitida',
    moneda: 'HNL',
    empresaRTN: '08011999123456',
    clienteIdentidadORTN: '0801199912345',
    subtotal: 100,
    descuento: 0,
    impuesto: 15,
    total: 115,
    detalles: [{
      productoId: 9,
      productoVarianteId: 15,
      cantidad: 1,
      precioUnitario: 100,
      descuento: 0,
      impuesto: 15,
      totalLinea: 115
    }]
  } as unknown as Factura;

  beforeEach(() => {
    vi.clearAllMocks();
    empresaVerificada.set(7);
    permisoCrear.set(true);
    localStorage.removeItem('solqaryn_empresa_solicitada_id');

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: TenantContextService, useValue: tenantContext },
        { provide: PermisosRuntimeService, useValue: permisos }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    localStorage.removeItem('solqaryn_empresa_solicitada_id');
    httpMock.verify();
  });

  function createComponent(): FacturaFiscalEmisionComponent {
    const component = TestBed.runInInjectionContext(() => new FacturaFiscalEmisionComponent());
    component.factura = factura;
    component.jurisdiccion = 'jurisdiction-configured';
    component.proveedor = 'provider-adapter';
    component.tipoDocumento = 'invoice-type';
    return component;
  }

  it('uses only the server-verified tenant in the provider-neutral emission contract', async () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const component = createComponent();

    await component.emitir();

    const req = httpMock.expectOne(`${environment.apiUrl}/facturacion-fiscal/emisiones`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.empresaId).toBe(7);
    expect(req.request.body.jurisdiccion).toBe('jurisdiction-configured');
    expect(req.request.body.proveedor).toBe('provider-adapter');
    expect(req.request.body.tipoDocumento).toBe('invoice-type');
    expect(req.request.body.claveIdempotencia).toBeTruthy();
    expect(req.request.body.hashSnapshot).toBeTruthy();
    req.flush({
      registroId: 1,
      estado: 'Confirmado',
      referenciaExterna: 'external-reference',
      codigoProveedor: 'OK',
      mensaje: 'Confirmado',
      esTransitorio: false,
      idempotente: false,
      reintentoAceptado: false
    });
  });

  it('fails closed when the verified tenant disappears and sends no request', async () => {
    empresaVerificada.set(null);
    const component = createComponent();

    expect(component.formularioValido()).toBe(false);
    await component.emitir();

    httpMock.expectNone(`${environment.apiUrl}/facturacion-fiscal/emisiones`);
  });

  it('reacts to permission revocation and blocks emission without a request', async () => {
    const component = createComponent();
    expect(component.puedeEmitir()).toBe(true);
    expect(component.formularioValido()).toBe(true);

    permisoCrear.set(false);

    expect(component.puedeEmitir()).toBe(false);
    expect(component.formularioValido()).toBe(false);
    await component.emitir();
    httpMock.expectNone(`${environment.apiUrl}/facturacion-fiscal/emisiones`);
  });

  it('reuses the same idempotency key after a transient provider failure', async () => {
    const component = createComponent();

    await component.emitir();
    const first = httpMock.expectOne(`${environment.apiUrl}/facturacion-fiscal/emisiones`);
    const firstKey = first.request.body.claveIdempotencia as string;
    first.flush(
      { message: 'Proveedor temporalmente no disponible.' },
      { status: 503, statusText: 'Service Unavailable' }
    );

    await component.emitir();
    const retry = httpMock.expectOne(`${environment.apiUrl}/facturacion-fiscal/emisiones`);
    expect(retry.request.body.claveIdempotencia).toBe(firstKey);
    retry.flush({
      registroId: 2,
      estado: 'Pendiente',
      referenciaExterna: null,
      codigoProveedor: null,
      mensaje: 'Pendiente',
      esTransitorio: true,
      idempotente: true,
      reintentoAceptado: true
    });
  });
});
