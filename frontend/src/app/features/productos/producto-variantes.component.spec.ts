import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { NEVER } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ProductoVariante } from '../../core/models/producto.model';
import { AlmacenService } from '../../services/almacen.service';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { ProductoService } from '../../services/producto.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { ProductoVariantesComponent } from './producto-variantes.component';

describe('ProductoVariantesComponent: ciclo de stock inicial', () => {
  const variante: ProductoVariante = {
    id: 12, productoId: 9, productoNombre: 'Camisa', tallaId: 4,
    sku: 'CAM-M', cantidad: 5, umbralStockBajo: 2, costo: 10, precio: 20,
    activo: true, tieneStockBajo: false, estaAgotada: false,
    estadoInventario: 'Disponible', fechaCreacion: '', fechaActualizacion: ''
  };
  const service = {
    actualizarVariante: vi.fn(() => NEVER),
    crearVariante: vi.fn(() => NEVER)
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: ActivatedRoute, useValue: {} },
        { provide: CatalogoProductoService, useValue: {} },
        { provide: ProductoService, useValue: service },
        { provide: AlmacenService, useValue: {} },
        { provide: PermisosRuntimeService, useValue: { puede: () => true } },
        { provide: MatSnackBar, useValue: {} },
        { provide: AppAlertService, useValue: {} }
      ]
    });
  });

  afterEach(() => vi.restoreAllMocks());

  function crearComponente(): ProductoVariantesComponent {
    const component = TestBed.runInInjectionContext(() => new ProductoVariantesComponent());
    component.productoId = 9;
    return component;
  }

  it('edita una variante existente sin reutilizar el almacén inicial ni habilitar cambios de stock', () => {
    const component = crearComponente();
    component.form.controls.almacenInicialId.setValue(3);
    expect(() => component.editar(variante)).not.toThrow();
    expect(component.form.controls.almacenInicialId.value).toBeNull();
    expect(component.form.controls.cantidad.disabled).toBe(true);
    expect(component.form.getRawValue().cantidad).toBe(5);
    component.guardar();
    expect(service.actualizarVariante).toHaveBeenCalledWith(9, 12,
      expect.objectContaining({ almacenInicialId: null, cantidad: 5, tallaId: 4 }));
    expect(service.crearVariante).not.toHaveBeenCalled();
  });

  it('al cancelar limpia el almacén y recupera el formulario de alta', () => {
    const component = crearComponente();
    component.editar(variante);
    component.form.controls.almacenInicialId.setValue(3);
    component.cancelar();
    expect(component.editandoId()).toBeNull();
    expect(component.form.controls.almacenInicialId.value).toBeNull();
    expect(component.form.controls.cantidad.enabled).toBe(true);
    expect(component.form.controls.cantidad.value).toBe(0);
  });

  it('no permite crear stock positivo sin seleccionar un almacén operativo', () => {
    const component = crearComponente();
    component.form.patchValue({ tallaId: 4, cantidad: 5, costo: 10, precio: 20 });
    component.guardar();
    expect(component.errorMessage()).toContain('almacén');
    expect(service.crearVariante).not.toHaveBeenCalled();
  });
});
