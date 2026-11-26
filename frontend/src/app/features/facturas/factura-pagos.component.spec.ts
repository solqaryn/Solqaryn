import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { Factura, FacturaPago } from '../../core/models/factura.model';
import { FacturaService } from '../../services/factura.service';
import { MetodoPagoService } from '../../services/metodo-pago.service';
import { PagoOnlineService } from '../../services/pago-online.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { FacturaPagosComponent } from './factura-pagos.component';

describe('QA — pagos de factura: autorización, moneda y rechazo', () => {
  const pago = { id: 3, anulado: false } as FacturaPago;
  const factura = { id: 1, estado: 'Emitida', numeroFactura: 'QA-1', moneda: 'USD', saldoPendiente: 100, pagos: [pago] } as Factura;
  const service = { registrarPago: vi.fn(), anularPago: vi.fn() };
  const permisos = { puede: vi.fn(() => true) };
  const tenant = { empresaIdVerificada: () => 1 };
  const snack = { open: vi.fn() };
  const alerts = { solicitarTexto: vi.fn() };
  let component: FacturaPagosComponent;
  beforeEach(() => {
    vi.clearAllMocks();
    permisos.puede.mockReturnValue(true);
    TestBed.configureTestingModule({ providers: [{ provide: PermisosRuntimeService, useValue: permisos }] });
    component = TestBed.runInInjectionContext(() => new FacturaPagosComponent(
      {} as ActivatedRoute, service as unknown as FacturaService, {} as MetodoPagoService,
      {} as PagoOnlineService, tenant as unknown as TenantContextService, snack as unknown as MatSnackBar,
      alerts as unknown as AppAlertService
    ));
    component.factura.set(factura);
    component.cargando.set(false);
    component.cargandoCatalogos.set(false);
    component.metodosPago.set([{ id: 1, codigo: 'EFECTIVO', nombre: 'Efectivo', tipo: 'Efectivo', activo: true, orden: 1, requiereBanco: false, requiereReferencia: false, permiteCambio: true }]);
    component.pago = { monto: 50, metodoPago: 'EFECTIVO' };
  });
  it('bloquea acciones de pago sin el permiso correspondiente', async () => {
    permisos.puede.mockReturnValue(false);
    component.registrarPago();
    await component.anularPago(pago);
    expect(service.registrarPago).not.toHaveBeenCalled();
    expect(alerts.solicitarTexto).not.toHaveBeenCalled();
    expect(service.anularPago).not.toHaveBeenCalled();
  });
  it('preserva factura y monto cuando el registro es rechazado', () => {
    service.registrarPago.mockReturnValue(of({ success: false, data: null, message: 'Pago rechazado.' }));
    component.registrarPago();
    expect(component.factura()).toEqual(factura);
    expect(component.pago.monto).toBe(50);
    expect(component.guardando()).toBe(false);
    expect(snack.open).toHaveBeenCalledWith('Pago rechazado.', 'Cerrar', { duration: 5000 });
  });
  it('no inicia pago online en una moneda distinta a la de la factura', () => {
    component.pagoOnline = { monto: 50, moneda: 'HNL', proveedor: 'demo' };
    expect(component.puedeIniciarPagoOnline()).toBe(false);
    component.pagoOnline.moneda = 'USD';
    expect(component.puedeIniciarPagoOnline()).toBe(true);
  });
  it('impide dos diálogos de anulación simultáneos y revalida permisos', async () => {
    let responder!: (valor: string | null) => void;
    alerts.solicitarTexto.mockReturnValue(new Promise(resolve => { responder = resolve; }));
    const primero = component.anularPago(pago);
    await component.anularPago(pago);
    expect(alerts.solicitarTexto).toHaveBeenCalledTimes(1);
    permisos.puede.mockReturnValue(false);
    responder('Motivo QA');
    await primero;
    expect(service.anularPago).not.toHaveBeenCalled();
    expect(component.anulandoId()).toBeNull();
  });
});
