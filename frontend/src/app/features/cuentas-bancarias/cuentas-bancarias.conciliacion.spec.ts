import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CuentaBancaria, EstadoCuentaBancaria } from '../../core/models/cuenta-bancaria';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { CuentaBancariaService } from '../../core/services/cuenta-bancaria.service';
import { CuentasBancariasComponent } from './cuentas-bancarias.component';

describe('CuentasBancariasComponent conciliación N4.3.E', () => {
  let fixture: ComponentFixture<CuentasBancariasComponent>;
  let component: CuentasBancariasComponent;
  let httpPost: ReturnType<typeof vi.fn>;
  let puede: ReturnType<typeof vi.fn>;

  const cuenta: CuentaBancaria = {
    id: 7,
    bancoId: 1,
    nombre: 'Cuenta conciliación',
    numeroCuenta: '001-777',
    moneda: 'HNL',
    saldoInicial: 1000,
    estado: EstadoCuentaBancaria.Activa
  };

  beforeEach(async () => {
    httpPost = vi.fn();
    puede = vi.fn((_modulo: string, accion: string) => accion === 'Importar' || accion === 'Crear');

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
        { provide: HttpClient, useValue: { post: httpPost } },
        { provide: CuentaBancariaService, useValue: cuentaService },
        { provide: PermisosRuntimeService, useValue: { puede } },
        { provide: ListNavigationStateService, useValue: navigationState },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => null } } } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CuentasBancariasComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.seleccionarCuentaParaConciliacion(cuenta);
  });

  it('inicializa el permiso Finanzas:Importar y expone la sección de conciliación', () => {
    expect(puede).toHaveBeenCalledWith('Finanzas', 'Importar');
    expect(component.puedeImportar()).toBe(true);
    expect(component.reconSelectedAccount()).toEqual(cuenta);

    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Conciliación bancaria');
    expect(fixture.nativeElement.querySelector('form[aria-label="Importar estado de cuenta"]')).not.toBeNull();
  });

  it('importa estado de cuenta con endpoint y DTO canónicos y refleja éxito', () => {
    httpPost.mockReturnValue(of(undefined));
    const movimientos = [{
      fechaOperacion: '2026-09-02',
      monto: 125.5,
      referenciaExterna: 'REF-001',
      descripcion: 'Depósito conciliable',
      identificadorExternoTransaccion: 'EXT-001'
    }];

    component.importarEstadoCuenta('import-001', movimientos);

    expect(httpPost).toHaveBeenCalledTimes(1);
    expect(httpPost.mock.calls[0][0]).toContain('/conciliaciones-bancarias/importaciones-estado-cuenta');
    expect(httpPost.mock.calls[0][1]).toEqual({
      cuentaBancariaId: cuenta.id,
      idempotencyKey: 'import-001',
      movimientos
    });
    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(true);
    expect(component.reconMovementRows()).toEqual(movimientos);
    expect(component.reconEmpty()).toBe(false);
  });

  it('registra matches con endpoint y DTO canónicos', () => {
    httpPost.mockReturnValue(of(undefined));
    const matches = [{ movimientoInternoId: 99, identificadorExternoTransaccion: 'EXT-099' }];

    component.registrarMatches('match-001', matches);

    expect(httpPost).toHaveBeenCalledTimes(1);
    expect(httpPost.mock.calls[0][0]).toContain('/conciliaciones-bancarias/matches');
    expect(httpPost.mock.calls[0][1]).toEqual({
      cuentaBancariaId: cuenta.id,
      idempotencyKey: 'match-001',
      matches
    });
    expect(component.reconSuccess()).toBe(true);
    expect(component.reconMatchRows()).toEqual(matches);
    expect(component.reconEmpty()).toBe(false);
  });

  it('bloquea la importación cuando Finanzas:Importar no está concedido', () => {
    component.puedeImportar.set(false);

    component.importarEstadoCuenta('denied', []);

    expect(httpPost).not.toHaveBeenCalled();
    expect(component.reconSuccess()).toBe(false);
  });

  it('cierra loading y publica error cuando el backend rechaza la importación', () => {
    const response$ = new Subject<void>();
    httpPost.mockReturnValue(response$);

    component.importarEstadoCuenta('import-error', []);
    expect(component.reconSubmitting()).toBe(true);

    response$.error(new Error('backend error'));

    expect(component.reconSubmitting()).toBe(false);
    expect(component.reconSuccess()).toBe(false);
    expect(component.reconError()).toBe('Error al importar estado de cuenta.');
  });
});
