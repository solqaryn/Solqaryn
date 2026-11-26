import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { Almacen } from '../../core/models/almacen.model';
import { ProductoVariante } from '../../core/models/producto.model';
import { AlmacenService } from '../../services/almacen.service';
import { ExistenciaVarianteService } from '../../services/existencia-variante.service';
import { ProductoService } from '../../services/producto.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';
import { ExistenciaVarianteFormComponent } from './existencia-variante-form.component';

describe('QA — existencia por variante: selección vigente', () => {
  const permisos = { puede: vi.fn(() => true) };
  const existencia = { create: vi.fn(), updateConfiguracion: vi.fn() };
  const productos = { getVariantes: vi.fn() };
  const ubicaciones = { getActivas: vi.fn() };
  let component: ExistenciaVarianteFormComponent;
  beforeEach(() => {
    vi.clearAllMocks();
    permisos.puede.mockReturnValue(true);
    TestBed.configureTestingModule({ providers: [{ provide: PermisosRuntimeService, useValue: permisos }] });
    component = TestBed.runInInjectionContext(() => new ExistenciaVarianteFormComponent(
      {} as ActivatedRoute, {} as Router, existencia as unknown as ExistenciaVarianteService,
      productos as unknown as ProductoService, {} as AlmacenService, ubicaciones as unknown as UbicacionAlmacenService
    ));
    component.loading.set(false);
    component.almacenId = 1;
    component.almacenes.set([{ id: 1, activo: true } as Almacen]);
  });
  it('ignora variantes de un producto anterior y exige una combinación vigente', () => {
    const anterior = new Subject<any>();
    const actual = new Subject<any>();
    productos.getVariantes.mockReturnValueOnce(anterior).mockReturnValueOnce(actual);
    component.productoId = 1;
    component.onProductoChange();
    component.productoId = 2;
    component.onProductoChange();
    anterior.next({ success: true, data: [{ id: 11, productoId: 1, activo: true }] });
    anterior.complete();
    expect(component.variantes()).toEqual([]);
    expect(component.cargandoVariantes()).toBe(true);
    actual.next({ success: true, data: [{ id: 22, productoId: 2, activo: true }] });
    actual.complete();
    expect(component.variantes().map(v => v.id)).toEqual([22]);
    component.productoVarianteId = 11;
    expect(component.puedeGuardar()).toBe(false);
    component.productoVarianteId = 22;
    expect(component.puedeGuardar()).toBe(true);
  });
  it('bloquea guardado duplicado, sin permiso o con ubicación de otro almacén', () => {
    component.productoId = 2;
    component.productoVarianteId = 22;
    component.variantes.set([{ id: 22, productoId: 2, activo: true } as ProductoVariante]);
    component.ubicacionAlmacenId = 9;
    expect(component.puedeGuardar()).toBe(false);
    component.ubicacionAlmacenId = null;
    permisos.puede.mockReturnValue(false);
    component.guardar();
    permisos.puede.mockReturnValue(true);
    component.saving.set(true);
    component.guardar();
    expect(existencia.create).not.toHaveBeenCalled();
  });
});
