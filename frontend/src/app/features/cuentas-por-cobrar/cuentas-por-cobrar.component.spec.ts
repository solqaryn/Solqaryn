import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Factura } from '../../core/models/factura.model';
import { CuentasPorCobrarComponent } from './cuentas-por-cobrar.component';

describe('CuentasPorCobrarComponent', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CuentasPorCobrarComponent],
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('renderiza saldo y enlaces a factura, venta y pagos desde el endpoint CxC', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();

    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    expect(request.request.method).toBe('GET');
    const factura = {
      id: 10,
      ventaId: 20,
      numeroFactura: 'FAC-001',
      numeroVentaOrigen: 'VEN-020',
      clienteNombre: 'Cliente QA',
      fechaVencimiento: '2026-09-01T00:00:00Z',
      estado: 'Emitida',
      estadoPago: 'Pendiente',
      total: 1000,
      totalPagado: 250,
      saldoPendiente: 750,
      moneda: 'HNL'
    } as unknown as Factura;
    request.flush({ success: true, data: [factura], message: '', errors: [] });
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('FAC-001');
    expect(text).toContain('VEN-020');
    expect(text).toContain('Cliente QA');
    expect(text).toContain('750.00');

    const anchors = fixture.nativeElement.querySelectorAll('a') as NodeListOf<HTMLAnchorElement>;
    const hrefs = Array.from(anchors, (anchor) => anchor.getAttribute('href'));
    expect(hrefs).toContain('/facturas/10');
    expect(hrefs).toContain('/ventas/20');
    expect(hrefs).toContain('/facturas/10/pagos');
  });

  it('falla de forma explicita ante 403 sin exponer datos', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    request.flush({ message: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();

    expect(fixture.componentInstance.cuentas()).toEqual([]);
    expect(fixture.componentInstance.errorMessage()).toBe('Forbidden');
    expect(fixture.nativeElement.textContent).toContain('Forbidden');
  });

  it('no acepta success=false como carga valida', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    request.flush({ success: false, data: [], message: 'Contrato rechazado', errors: [] });
    fixture.detectChanges();

    expect(fixture.componentInstance.cuentas()).toEqual([]);
    expect(fixture.componentInstance.errorMessage()).toBe('Contrato rechazado');
  });
});
