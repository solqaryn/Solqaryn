import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoPeriodoContable, PeriodoContable } from '../../core/models/periodo-contable.model';
import { PeriodoContableService } from '../../core/services/periodo-contable.service';
import { PeriodosContablesComponent } from './periodos-contables.component';

describe('QA — períodos contables: rechazos de la API', () => {
  const periodo: PeriodoContable = { id: 1, fechaInicio: '2026-11-01', fechaFin: '2026-11-30', estado: EstadoPeriodoContable.Abierto, cerradoEnUtc: null };
  const respuesta = <T>(data: T, success = true, message = '') => ({ success, message, errors: [], data });
  const service = { getPaged: vi.fn(), create: vi.fn(), cerrar: vi.fn() };
  const snack = { open: vi.fn() };
  const permisos = { puede: vi.fn(() => true) };
  let component: PeriodosContablesComponent;
  beforeEach(() => {
    vi.clearAllMocks();
    permisos.puede.mockReturnValue(true);
    service.getPaged.mockReturnValue(of(respuesta({ items: [periodo], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 })));
    TestBed.configureTestingModule({ providers: [
      FormBuilder, { provide: MatSnackBar, useValue: snack },
      { provide: PeriodoContableService, useValue: service },
      { provide: PermisosRuntimeService, useValue: permisos }
    ] });
    component = TestBed.runInInjectionContext(() => new PeriodosContablesComponent());
    component.ngOnInit();
  });
  it('no interpreta un listado rechazado como datos válidos', () => {
    service.getPaged.mockReturnValue(of(respuesta({ items: [periodo], totalPages: 2 }, false, 'Consulta rechazada.')));
    component.cargar();
    expect(component.periodos()).toEqual([]);
    expect(component.totalPages()).toBe(1);
    expect(component.loading()).toBe(false);
    expect(snack.open).toHaveBeenCalledWith('Consulta rechazada.', 'Cerrar', { duration: 4000 });
  });
  it('preserva el alta si la API rechaza el período y no anuncia éxito', () => {
    component.mostrarAlta.set(true);
    component.alta.setValue({ fechaInicio: periodo.fechaInicio, fechaFin: periodo.fechaFin });
    service.create.mockReturnValue(of(respuesta(null, false, 'Período superpuesto.')));
    service.getPaged.mockClear();
    component.crear();
    expect(component.mostrarAlta()).toBe(true);
    expect(component.alta.controls.fechaInicio.value).toBe(periodo.fechaInicio);
    expect(component.guardando()).toBe(false);
    expect(service.getPaged).not.toHaveBeenCalled();
    expect(snack.open).toHaveBeenCalledWith('Período superpuesto.', 'Cerrar', { duration: 5000 });
  });
  it('no confirma cierre rechazado ni envía otro cierre sin autorización', () => {
    service.cerrar.mockReturnValue(of(respuesta(null, false, 'Cierre rechazado.')));
    service.getPaged.mockClear();
    component.cerrar(periodo);
    expect(component.cerrandoId()).toBeNull();
    expect(component.periodos()).toEqual([periodo]);
    expect(service.getPaged).not.toHaveBeenCalled();
    expect(snack.open).toHaveBeenCalledWith('Cierre rechazado.', 'Cerrar', { duration: 5000 });
    permisos.puede.mockReturnValue(false);
    component.cerrar(periodo);
    expect(service.cerrar).toHaveBeenCalledTimes(1);
  });
});
