import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CentroCosto, TipoCentroCosto } from '../../core/models/centro-costo.model';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
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
  const alerts = { confirmar: vi.fn() };
  const item: CentroCosto = { id: 7, codigo: 'CC-07', nombre: 'Operaciones', tipo: TipoCentroCosto.Departamento, activo: true, eliminado: false, fechaCreacion: '', fechaActualizacion: '' };
  const permisosRuntime = { puede: vi.fn(() => true) };

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: CentroCostoService, useValue: service },
        { provide: MatSnackBar, useValue: snack },
        { provide: PermisosRuntimeService, useValue: permisosRuntime },
        { provide: AppAlertService, useValue: alerts }
      ]
    });
  });

  function createComponent(): CentrosCostoComponent {
    return TestBed.runInInjectionContext(() => new CentrosCostoComponent());
  }

  it('settles loading into the empty state after an empty response', () => {
    service.buscar.mockReturnValue(of({ success: true, message: '', errors: [], data: { items: [], total: 0, pagina: 1, tamanoPagina: 100 } }));
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

  it('rechaza identificadores de sucursal negativos, cero o fraccionarios', () => {
    const component = createComponent();
    component.puedeCrear.set(true);
    component.nuevo();
    for (const id of [-1, 0, 1.5]) {
      component.form.patchValue({ codigo: 'CC-01', nombre: 'Centro principal', tipo: TipoCentroCosto.Sucursal, sucursalId: id });
      component.guardar();
      expect(service.create).not.toHaveBeenCalled();
      expect(component.mostrandoFormulario()).toBe(true);
    }
  });

  it('mantiene estado confirmado ante rechazo y bloquea cambios simultáneos', () => {
    const response = new Subject<{ success: boolean; data: CentroCosto; message: string; errors: string[] }>();
    service.cambiarEstado.mockReturnValue(response);
    const component = createComponent();
    component.puedeDesactivar.set(true);
    component.items.set([item]);
    component.cambiarEstado(item);
    component.cambiarEstado(item);
    expect(service.cambiarEstado).toHaveBeenCalledTimes(1);
    response.next({ success: false, data: item, message: 'Estado rechazado.', errors: [] });
    response.complete();
    expect(component.items()).toEqual([item]);
    expect(component.accionEnCurso()).toBeNull();
    expect(service.buscar).not.toHaveBeenCalled();
    expect(snack.open).toHaveBeenCalledWith('Estado rechazado.', 'Cerrar', { duration: 3000 });
  });

  it('cancelar o rechazar eliminación conserva el registro y no recarga', async () => {
    const component = createComponent();
    component.puedeEliminar.set(true);
    component.items.set([item]);
    alerts.confirmar.mockResolvedValueOnce(false).mockResolvedValueOnce(true);
    service.delete.mockReturnValue(of({ success: false, data: {}, message: 'Eliminación rechazada.', errors: [] }));
    await component.eliminar(item);
    expect(service.delete).not.toHaveBeenCalled();
    expect(component.accionEnCurso()).toBeNull();
    await component.eliminar(item);
    expect(service.delete).toHaveBeenCalledTimes(1);
    expect(component.items()).toEqual([item]);
    expect(component.accionEnCurso()).toBeNull();
    expect(service.buscar).not.toHaveBeenCalled();
    expect(snack.open).toHaveBeenCalledWith('Eliminación rechazada.', 'Cerrar', { duration: 3000 });
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

    expect(component.errorFormulario()).toBe('Indica la sucursal para crear o editar un centro de tipo Sucursal.');
    expect(service.create).not.toHaveBeenCalled();
    expect(service.update).not.toHaveBeenCalled();
  });
});
