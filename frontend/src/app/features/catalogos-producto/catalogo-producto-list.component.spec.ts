import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TipoCatalogoProducto } from '../../core/models/catalogo-producto.model';
import { CatalogoProductoService } from '../../services/catalogo-producto.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { CatalogoProductoListComponent } from './catalogo-producto-list.component';

describe('CatalogoProductoListComponent: longitud del nombre', () => {
  const route = { snapshot: { data: { tipo: 'Talla' as TipoCatalogoProducto, singular: 'Talla', titulo: 'Tallas' } } };
  const service = {
    create: vi.fn(() => of({ data: {} })),
    update: vi.fn(() => of({ data: {} })),
    getAll: vi.fn(() => of({ data: [] }))
  };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: ActivatedRoute, useValue: route },
        { provide: CatalogoProductoService, useValue: service },
        { provide: PermisosRuntimeService, useValue: { puede: () => true } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
        { provide: AppAlertService, useValue: {} }
      ]
    });
  });

  function componentFor(tipo: TipoCatalogoProducto): CatalogoProductoListComponent {
    route.snapshot.data.tipo = tipo;
    return TestBed.runInInjectionContext(() => new CatalogoProductoListComponent());
  }

  it.each(['S', 'M', 'L', '0', 'XL', ' M '])('acepta y envía la talla %s', nombre => {
    const component = componentFor('Talla');
    component.nuevo();
    component.form.controls.nombre.setValue(nombre);
    expect(component.form.valid).toBe(true);
    component.guardar();
    expect(service.create).toHaveBeenCalledWith('Talla', expect.objectContaining({ nombre: nombre.trim() }));
  });

  it('permite editar una talla a un solo carácter', () => {
    const component = componentFor('Talla');
    component.nuevo();
    component.editandoId.set(41);
    component.form.controls.nombre.setValue('M');
    component.guardar();
    expect(service.update).toHaveBeenCalledWith('Talla', 41, expect.objectContaining({ nombre: 'M' }));
  });

  it.each(['', '   ', 'A'.repeat(121)])('rechaza una talla inválida: %s', nombre => {
    const component = componentFor('Talla');
    component.nuevo();
    component.form.controls.nombre.setValue(nombre);
    expect(component.form.invalid).toBe(true);
    component.guardar();
    expect(service.create).not.toHaveBeenCalled();
  });

  it.each<TipoCatalogoProducto>(['Color', 'Marca', 'Modelo'])('conserva el mínimo de dos caracteres en %s', tipo => {
    const component = componentFor(tipo);
    component.form.controls.nombre.setValue('A');
    expect(component.form.controls.nombre.hasError('minlength')).toBe(true);
    component.form.controls.nombre.setValue('AB');
    expect(component.form.controls.nombre.valid).toBe(true);
  });
});
