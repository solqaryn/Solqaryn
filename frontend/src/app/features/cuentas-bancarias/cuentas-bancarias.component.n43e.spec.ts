import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CuentaBancaria, EstadoCuentaBancaria } from '../../core/models/cuenta-bancaria';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { CuentaBancariaService } from '../../core/services/cuenta-bancaria.service';
import { CuentasBancariasComponent } from './cuentas-bancarias.component';

describe('CuentasBancariasComponent N4.3.E conciliacion', () => {
  let fixture: ComponentFixture<CuentasBancariasComponent>;
  let component: CuentasBancariasComponent;
  let httpMock: HttpTestingController;
  let permisos: { puede: ReturnType<typeof vi.fn> };

  const cuenta: CuentaBancaria = {
    id: 7,
    bancoId: 1,
    nombre: 'Cuenta conciliable',
    numeroCuenta: 'HN-700',
    moneda: 'HNL',
    saldoInicial: 1000,
    estado: EstadoCuentaBancaria.Activa
  };

  beforeEach(async () => {
    permisos = {
      puede: vi.fn((_modulo: string, accion: string) => accion === 'Importar' || accion === 'Crear')
    };

    const cuentaService = {
      getAll: vi.fn().mockReturnValue(of({
        items: [cuenta],
        page: 1,
        pageSize: 50,
        totalCount: 1,
        totalPages: 1
      })),
      create: vi.fn(),
      update: vi.fn(),
      activar: vi.fn(),
      desactivar: vi.fn()
    };

    const navigationState = {
      restore: vi.fn().mockReturnValue({ search: '', estadoFilter: null }),
      persist: vi.fn(),
      clear: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CuentasBancariasComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(withXhr()),
        provideHttpClientTesting(),
        { provide: CuentaBancariaService, useValue: cuentaService },
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: ListNavigationStateService, useValue: navigationState },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CuentasBancariasComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    component.seleccionarCuentaParaConciliacion(cuenta);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('importa un movimiento con cuenta, idempotencia y estado de loading reales', () => {
    const movimientos = [{
      fechaOperacion: '2026-09-02',
      monto: 125.5,
      referenciaExterna: 'EXT-1',
      descripcion: 'Deposito',
      identificadorExternoTransaccion: 'TX-1'
    }];

    component.importarEstadoCuenta('import-7-1', movimientos);

    expect(component.reconSubmitting()).toBe(true);
    const request = httpMock.expectOne((req) => req.url.endsWith('/conciliaciones-bancarias/importaciones-estado-cuenta'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      cuentaBancariaId: 7,
      idempotencyKey: 'import-7-1',
      movimientos
    });

    request.flush(null);

    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(true);
    expect(component.reconMovementRows()).toEqual(movimientos);
    expect(component.reconEmpty()).toBe(false);
  });

  it('expone el estado vacío cuando la importación válida no contiene movimientos', () => {
    component.importarEstadoCuenta('import-empty', []);

    const request = httpMock.expectOne((req) => req.url.endsWith('/conciliaciones-bancarias/importaciones-estado-cuenta'));
    request.flush(null);

    expect(component.reconSuccess()).toBe(true);
    expect(component.reconEmpty()).toBe(true);
    expect(component.reconMovementRows()).toEqual([]);
  });

  it('preserva el detalle de error HTTP y libera submitting al fallar la importación', () => {
    component.importarEstadoCuenta('import-error', []);

    const request = httpMock.expectOne((req) => req.url.endsWith('/conciliaciones-bancarias/importaciones-estado-cuenta'));
    request.flush(
      { detail: 'La clave de idempotencia ya fue usada con otro payload.' },
      { status: 409, statusText: 'Conflict' }
    );

    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(false);
    expect(component.reconError()).toBe('La clave de idempotencia ya fue usada con otro payload.');
  });

  it('registra matches con el contrato exacto de conciliación', () => {
    const matches = [{ movimientoInternoId: 42, identificadorExternoTransaccion: 'TX-42' }];

    component.registrarMatches('match-7-1', matches);

    expect(component.reconSubmitting()).toBe(true);
    const request = httpMock.expectOne((req) => req.url.endsWith('/conciliaciones-bancarias/matches'));
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      cuentaBancariaId: 7,
      idempotencyKey: 'match-7-1',
      matches
    });

    request.flush(null);

    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(true);
    expect(component.reconMatchRows()).toEqual(matches);
  });

  it('bloquea importación y matches cuando RBAC no autoriza las acciones', () => {
    component.puedeImportar.set(false);
    component.puedeCrear.set(false);

    component.importarEstadoCuenta('blocked-import', []);
    component.registrarMatches('blocked-match', []);

    httpMock.expectNone((req) => req.url.includes('/conciliaciones-bancarias/'));
    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(false);
  });
});
