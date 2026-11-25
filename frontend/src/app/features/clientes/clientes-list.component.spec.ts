import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { Cliente } from '../../core/models/cliente.model';
import { ClienteService } from '../../services/cliente.service';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { ClientesListComponent } from './clientes-list.component';

describe('ClientesListComponent: tabla única con scroll propio', () => {
  const cliente: Cliente = {
    id: 1, nombre: 'Cliente de prueba', telefono: '00000000',
    identidadORTN: '00000000000000', correo: 'cliente@example.test',
    activo: true, totalVentas: 2, totalVendido: 100,
    tipoClienteId: 1, tipoClienteNombre: 'Sin clasificar',
    tipoClienteColorHex: '#808080', fechaCreacion: '2026-01-01'
  };
  let fixture: ComponentFixture<ClientesListComponent>;
  let clientes: Cliente[];
  let puedeOperar: boolean;

  beforeEach(() => {
    clientes = [cliente];
    puedeOperar = true;
    TestBed.configureTestingModule({
      imports: [ClientesListComponent],
      providers: [
        provideRouter([]),
        { provide: ClienteService, useValue: { getAll: () => of({ data: clientes }) } },
        { provide: TipoClienteService, useValue: { getAll: () => of({ data: [] }) } },
        { provide: PermisosRuntimeService, useValue: { puede: () => puedeOperar } },
        { provide: ListNavigationStateService, useValue: {
          restore: (_scope: string, _route: unknown, defaults: object) => defaults,
          persist: vi.fn(), clear: vi.fn()
        } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
        { provide: AppAlertService, useValue: { confirmar: vi.fn() } }
      ]
    });
  });

  async function render(): Promise<HTMLElement> {
    fixture = TestBed.createComponent(ClientesListComponent);
    fixture.autoDetectChanges();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('mantiene las nueve columnas, una sola fila y el paginador fuera del scroll', async () => {
    const root = await render();
    const scroll = root.querySelector<HTMLElement>('.table-scroll')!;
    expect(scroll).toBeTruthy();
    expect(scroll.getAttribute('role')).toBe('region');
    expect(scroll.getAttribute('aria-label')).toBe('Tabla de clientes');
    expect(scroll.tabIndex).toBe(0);
    expect(root.querySelectorAll('table')).toHaveLength(1);
    expect(scroll.querySelectorAll('thead th')).toHaveLength(9);
    expect(scroll.querySelectorAll('tbody tr')).toHaveLength(1);
    expect(scroll.querySelector('tbody tr')?.textContent).toContain(cliente.nombre);
    expect(scroll.querySelectorAll('tbody td')).toHaveLength(9);
    expect(root.querySelector('.cards-mobile')).toBeNull();
    expect(root.querySelector('mat-paginator')).toBeTruthy();
    expect(scroll.querySelector('mat-paginator')).toBeNull();
    expect(scroll.querySelector('[title="Ver detalle"]')).toBeTruthy();
    expect(scroll.querySelector('[title="Editar"]')).toBeTruthy();
    expect(scroll.querySelector('[title="Eliminar"]')).toBeTruthy();
  });

  it('conserva el estado vacío en la misma tabla', async () => {
    clientes = [];
    const root = await render();
    const empty = root.querySelector('.table-scroll tbody td.empty');
    expect(empty?.getAttribute('colspan')).toBe('9');
    expect(empty?.textContent).toContain('No hay clientes');
    expect(root.querySelector('.cards-mobile')).toBeNull();
  });

  it('mantiene las restricciones de permisos sin duplicar acciones', async () => {
    puedeOperar = false;
    const root = await render();
    expect(root.querySelector('.table-scroll .actions [title="Ver detalle"]')).toBeTruthy();
    expect(root.querySelector('.table-scroll .actions [title="Editar"]')).toBeNull();
    expect(root.querySelector('.table-scroll .actions [title="Eliminar"]')).toBeNull();
    expect(root.querySelector('.table-scroll mat-slide-toggle')).toBeNull();
    expect(root.querySelector('.table-scroll .badge.success')?.textContent).toContain('Activo');
  });
});
