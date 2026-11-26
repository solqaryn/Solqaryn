import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { Factura } from '../../core/models/factura.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { signal } from '@angular/core';
import { CuentasPorCobrarComponent } from './cuentas-por-cobrar.component';

describe('CuentasPorCobrarComponent', () => {
  let http: HttpTestingController;
  const permisos = signal(new Set<string>());
  const puede = (modulo: string, accion: string) => permisos().has(modulo + ':' + accion);

  beforeEach(async () => {
    permisos.set(new Set(['Facturacion:Ver', 'Facturacion:Aplicar', 'Ventas:Ver']));
    await TestBed.configureTestingModule({
      imports: [CuentasPorCobrarComponent],
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), provideRouter([]), { provide: PermisosRuntimeService, useValue: { puede } }]
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
    expect(fixture.componentInstance.errorMessage()).toBe('Tu perfil no tiene permiso para consultar cuentas por cobrar.');
    expect(fixture.nativeElement.textContent).toContain('Tu perfil no tiene permiso');
    expect(fixture.nativeElement.textContent).not.toContain('Forbidden');
  });

  it('mantiene saldos separados por moneda y oculta acciones no autorizadas', () => {
    permisos.set(new Set(['Facturacion:Ver']));
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    const base = { id: 1, ventaId: 10, numeroFactura: 'FAC-HNL', numeroVentaOrigen: 'VEN-10',
      clienteNombre: 'Cliente QA', estado: 'Emitida', estadoPago: 'Pendiente',
      total: 100, totalPagado: 0, saldoPendiente: 100, moneda: 'HNL' } as unknown as Factura;
    request.flush({ success: true, data: [
      base,
      { ...base, id: 2, numeroFactura: 'FAC-USD', moneda: 'USD', total: 50, saldoPendiente: 50 },
      { ...base, id: 3, numeroFactura: 'FAC-HNL-2', total: 25, saldoPendiente: 25 }
    ], message: '', errors: [] });
    fixture.detectChanges();
    expect(fixture.componentInstance.saldosPorMoneda()).toEqual([{ moneda: 'HNL', total: 125 }, { moneda: 'USD', total: 50 }]);
    const host: HTMLElement = fixture.nativeElement;
    const resumen = host.querySelector('[aria-label="Saldo pendiente por moneda"]');
    expect(resumen?.textContent).toContain('HNL 125.00');
    expect(resumen?.textContent).toContain('USD 50.00');
    expect(resumen?.textContent).not.toContain('HNL 175.00');
    const links = Array.from(host.querySelectorAll('a'), anchor => anchor.getAttribute('href'));
    expect(links).toContain('/facturas/1');
    expect(links).not.toContain('/ventas/10');
    expect(links.some(link => link?.endsWith('/pagos'))).toBe(false);
    expect(host.textContent).toContain('VEN-10');
    permisos.set(new Set(['Facturacion:Ver', 'Facturacion:Aplicar', 'Ventas:Ver']));
    fixture.detectChanges();
    expect(host.querySelector('a[href="/facturas/1/pagos"]')).not.toBeNull();
    expect(host.querySelector('a[href="/ventas/10"]')).not.toBeNull();
  });

  it('limpia información anterior ante sesión caducada 401', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    const request = http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`);
    const factura = { id: 1, ventaId: 10, numeroFactura: 'FAC-PRIVADA-QA', clienteNombre: 'Cliente de prueba', total: 100, totalPagado: 0, saldoPendiente: 100, moneda: 'HNL' } as unknown as Factura;
    request.flush({ success: true, data: [factura], message: '', errors: [] });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('FAC-PRIVADA-QA');
    fixture.componentInstance.cargar();
    http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`).flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();
    expect(fixture.componentInstance.cuentas()).toEqual([]);
    expect(fixture.componentInstance.errorMessage()).toContain('Vuelve a iniciar sesión');
    expect(fixture.nativeElement.textContent).not.toContain('FAC-PRIVADA-QA');
    expect(fixture.nativeElement.textContent).not.toContain('Unauthorized');
  });

  it('no permite aplicar pagos a saldos cero o negativos ni los marca vencidos', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    const base = { id: 1, ventaId: 10, numeroFactura: 'FAC-POSITIVA', fechaVencimiento: '2020-01-01T00:00:00Z', clienteNombre: 'Cliente QA', total: 100, totalPagado: 90, saldoPendiente: 10, moneda: 'HNL' } as unknown as Factura;
    http.expectOne(`${environment.apiUrl}/cuentas-por-cobrar`).flush({ success: true, data: [
      base, { ...base, id: 2, numeroFactura: 'FAC-CERO', saldoPendiente: 0 }, { ...base, id: 3, numeroFactura: 'FAC-NEGATIVA', saldoPendiente: -5 }
    ], message: '', errors: [] });
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('a[href="/facturas/1/pagos"]')).not.toBeNull();
    expect(host.querySelector('a[href="/facturas/2/pagos"]')).toBeNull();
    expect(host.querySelector('a[href="/facturas/3/pagos"]')).toBeNull();
    expect(fixture.componentInstance.totalVencidos()).toBe(1);
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
