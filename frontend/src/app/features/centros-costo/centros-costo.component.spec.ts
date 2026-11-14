import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TipoCentroCosto } from '../../core/models/centro-costo.model';
import { CentroCostoService } from '../../services/centro-costo.service';
import { CentrosCostoComponent } from './centros-costo.component';

describe('N4.11.E CentrosCostoComponent deterministic states', () => {
  const service = {
    buscar: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    cambiarEstado: vi.fn(),
    delete: vi.fn()
  };
  const snack = { open: vi.fn() };
  const permisosRuntime = { puede: vi.fn(() => true) };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: CentroCostoService, useValue: service },
        { provide: MatSnackBar, useValue: snack },
        { provide: PermisosRuntimeService, useValue: permisosRuntime }
      ]
    });
  });

  function createComponent(): CentrosCostoComponent {
    return TestBed.runInInjectionContext(() => new CentrosCostoComponent());
  }

  it('settles loading into the empty state after an empty response', () => {
    service.buscar.mockReturnValue(of({ data: { items: [], total: 0, pagina: 1, tamanoPagina: 100 } }));
    const component = createComponent();

    component.ngOnInit();

    expect(component.loading()).toBe(false);
    expect(component.errorCarga()).toBe('');
    expect(component.items()).toEqual([]);
  });

  it('settles loading into a safe error state when the request fails', () => {
    service.buscar.mockReturnValue(throwError(() => new Error('network')));
    const component = createComponent();

    component.cargar();

    expect(component.loading()).toBe(false);
    expect(component.items()).toEqual([]);
    expect(component.errorCarga()).toBe('No se pudieron cargar los centros de costo.');
  });

  it('rejects Sucursal submissions without a sucursalId before calling the API', () => {
    const component = createComponent();
    component.puedeCrear.set(true);
    component.nuevo();
    component.form.patchValue({
      codigo: 'CC-01',
      nombre: 'Centro principal',
      tipo: TipoCentroCosto.Sucursal,
      sucursalId: null
    });

    component.guardar();

    expect(component.errorFormulario()).toBe('Sucursal ID es obligatoria para centros de tipo Sucursal.');
    expect(service.create).not.toHaveBeenCalled();
    expect(service.update).not.toHaveBeenCalled();
  });
});
