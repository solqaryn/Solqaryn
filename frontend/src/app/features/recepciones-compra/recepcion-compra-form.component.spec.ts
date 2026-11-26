import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { RecepcionCompraSaldoOrden } from '../../core/models/recepcion-compra.model';
import { ApiResponse } from '../../core/models/api-response.model';
import { AlmacenService } from '../../services/almacen.service';
import { OrdenCompraService } from '../../services/orden-compra.service';
import { RecepcionCompraService } from '../../services/recepcion-compra.service';
import { RecepcionCompraFormComponent } from './recepcion-compra-form.component';

describe('QA — recepción de compra: selección y rechazo', () => {
  const respuesta = <T>(data: T, success = true, message = '') => ({ success, message, data, errors: [] });
  const saldo = (id: number): RecepcionCompraSaldoOrden => ({
    ordenCompraId: id, numeroOrden: 'OC-' + id, estadoOrden: 'Aprobada', completa: false,
    lineas: [{ ordenCompraDetalleId: id * 10, productoId: 1, cantidadOrdenada: 5, cantidadAceptadaAcumulada: 0, cantidadPendiente: 5 }]
  });
  const recepciones = { getSaldoOrden: vi.fn(), create: vi.fn() };
  const router = { navigate: vi.fn() };
  const permisos = { puede: vi.fn(() => true) };
  let component: RecepcionCompraFormComponent;
  beforeEach(() => {
    vi.clearAllMocks();
    permisos.puede.mockReturnValue(true);
    TestBed.configureTestingModule({ providers: [
      FormBuilder, { provide: ActivatedRoute, useValue: {} }, { provide: Router, useValue: router },
      { provide: RecepcionCompraService, useValue: recepciones }, { provide: OrdenCompraService, useValue: {} },
      { provide: AlmacenService, useValue: {} }, { provide: PermisosRuntimeService, useValue: permisos }
    ] });
    component = TestBed.runInInjectionContext(() => new RecepcionCompraFormComponent());
    component.puedeCrear.set(true);
  });
  const seleccionar = (id: number) => {
    component.form.controls.ordenCompraId.setValue(id);
    component.seleccionarOrden(id);
  };
  it('ignora el saldo anterior cuando se selecciona otra orden', () => {
    const anterior = new Subject<ApiResponse<RecepcionCompraSaldoOrden>>();
    const actual = new Subject<ApiResponse<RecepcionCompraSaldoOrden>>();
    recepciones.getSaldoOrden.mockReturnValueOnce(anterior).mockReturnValueOnce(actual);
    seleccionar(1);
    seleccionar(2);
    anterior.next(respuesta(saldo(1)));
    anterior.complete();
    expect(component.saldo()).toBeNull();
    expect(component.cargandoSaldo()).toBe(true);
    actual.next(respuesta(saldo(2)));
    actual.complete();
    expect(component.saldo()?.ordenCompraId).toBe(2);
    expect(component.detalles.length).toBe(1);
    expect(component.detalle(0).getRawValue().ordenCompraDetalleId).toBe(20);
  });
  it('no permite guardar con saldo rechazado o de otra orden', () => {
    recepciones.getSaldoOrden.mockReturnValueOnce(of(respuesta(saldo(1), false))).mockReturnValueOnce(of(respuesta(saldo(1))));
    seleccionar(1);
    expect(component.saldo()).toBeNull();
    seleccionar(2);
    expect(component.saldo()).toBeNull();
    component.guardar();
    expect(recepciones.create).not.toHaveBeenCalled();
  });
  it('conserva los datos al rechazar el guardado y no duplica solicitudes', () => {
    recepciones.getSaldoOrden.mockReturnValue(of(respuesta(saldo(2))));
    seleccionar(2);
    component.detalle(0).patchValue({ almacenId: 1, cantidadRecibida: 3 });
    const resultado = new Subject<ReturnType<typeof respuesta<null>>>();
    recepciones.create.mockReturnValue(resultado);
    component.guardar();
    component.guardar();
    expect(recepciones.create).toHaveBeenCalledTimes(1);
    resultado.next(respuesta(null, false, 'Recepción rechazada.'));
    resultado.complete();
    expect(router.navigate).not.toHaveBeenCalled();
    expect(component.guardando()).toBe(false);
    expect(component.detalle(0).getRawValue().cantidadRecibida).toBe(3);
    expect(component.error()).toBe('Recepción rechazada.');
    permisos.puede.mockReturnValue(false);
    component.guardar();
    expect(recepciones.create).toHaveBeenCalledTimes(1);
  });
});
