import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TipoCentroCosto } from '../../core/models/centro-costo.model';
import { CentroCostoService } from '../../services/centro-costo.service';
import { CentrosCostoComponent } from './centros-costo.component';

describe('N4.11.G CentroCosto accessibility regression', () => {
  const service = {
    buscar: vi.fn(() => of({ success: true, message: '', errors: [], data: { items: [], total: 0, pagina: 1, tamanoPagina: 20 } })),
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
      imports: [CentrosCostoComponent, NoopAnimationsModule],
      providers: [
        { provide: CentroCostoService, useValue: service },
        { provide: MatSnackBar, useValue: snack },
        { provide: PermisosRuntimeService, useValue: permisosRuntime }
      ]
    });
  });

  it('keeps search, empty state and editor controls accessible', () => {
    const fixture = TestBed.createComponent(CentrosCostoComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('[role="search"]')).not.toBeNull();
    expect(host.querySelector('input[aria-label="Buscar centros de costo"]')).not.toBeNull();
    expect(host.textContent).toContain('Sin centros de costo');

    fixture.componentInstance.nuevo();
    fixture.detectChanges();

    expect(host.querySelector('form[aria-label="Formulario de centro de costo"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Cerrar formulario"]')).not.toBeNull();
  });

  it('exposes explicit labels for row actions in both active and inactive states', () => {
    const fixture = TestBed.createComponent(CentrosCostoComponent);
    fixture.detectChanges();

    fixture.componentInstance.items.set([
      {
        id: 7,
        codigo: 'CC-007',
        nombre: 'Operaciones',
        descripcion: null,
        tipo: TipoCentroCosto.Departamento,
        sucursalId: null,
        sucursalCodigo: null,
        sucursalNombre: null,
        activo: true,
        eliminado: false,
        fechaCreacion: '2026-09-08T00:00:00Z',
        fechaActualizacion: '2026-09-08T00:00:00Z'
      }
    ]);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('button[aria-label="Editar Operaciones"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Desactivar Operaciones"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Eliminar Operaciones"]')).not.toBeNull();

    fixture.componentInstance.items.update(items => items.map(item => ({ ...item, activo: false })));
    fixture.detectChanges();

    expect(host.querySelector('button[aria-label="Activar Operaciones"]')).not.toBeNull();
  });

  it('solicita páginas reales y reinicia la búsqueda al cambiar el filtro', () => {
    const fixture = TestBed.createComponent(CentrosCostoComponent);
    fixture.detectChanges();
    expect(service.buscar).toHaveBeenLastCalledWith({ termino: '', pagina: 1, tamanoPagina: 20 });
    fixture.componentInstance.cambiarPagina({ pageIndex: 1, pageSize: 20, length: 42 });
    expect(service.buscar).toHaveBeenLastCalledWith({ termino: '', pagina: 2, tamanoPagina: 20 });
    fixture.componentInstance.termino.set('  Operaciones  ');
    fixture.componentInstance.buscar();
    expect(service.buscar).toHaveBeenLastCalledWith({ termino: 'Operaciones', pagina: 1, tamanoPagina: 20 });
    fixture.componentInstance.limpiar();
    expect(service.buscar).toHaveBeenLastCalledWith({ termino: '', pagina: 1, tamanoPagina: 20 });
  });

  it('announces load errors through an alert role', () => {
    const fixture = TestBed.createComponent(CentrosCostoComponent);
    fixture.detectChanges();

    fixture.componentInstance.loading.set(false);
    fixture.componentInstance.errorCarga.set('No se pudieron cargar los centros de costo.');
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement;
    const alert = host.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert?.textContent).toContain('No se pudieron cargar los centros de costo.');
  });
});
