import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { SolicitudCompra } from '../../core/models/solicitud-compra.model';
import { ProductoService } from '../../services/producto.service';
import { ProveedorService } from '../../services/proveedor.service';
import { SolicitudCompraService } from '../../services/solicitud-compra.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { SolicitudesCompraShellComponent } from './solicitudes-compra-shell.component';

describe('QA: listado institucional de solicitudes de compra', () => {
  const borrador: SolicitudCompra = { id: 7, numeroSolicitud: 'SC-007', estado: 'Borrador', proveedorNombre: 'Proveedor de prueba', detalles: [] };
  const aprobada: SolicitudCompra = { ...borrador, id: 8, numeroSolicitud: 'SC-008', estado: 'Aprobada' };
  const respuesta = (items: SolicitudCompra[]) => ({ success: true, message: '', errors: [], data: { items, page: 1, pageSize: 10, totalCount: items.length, totalPages: 1 } });
  const servicio = { getPaged: vi.fn(() => of(respuesta([borrador, aprobada]))) };
  const permisos = { puede: vi.fn((_modulo: string, accion: string) => ['Ver', 'Crear', 'Editar'].includes(accion)) };
  const router = { navigate: vi.fn(async () => true) };
  const route = { queryParamMap: of(convertToParamMap({})) };

  beforeEach(() => {
    vi.clearAllMocks();
    servicio.getPaged.mockReturnValue(of(respuesta([borrador, aprobada])));
    permisos.puede.mockImplementation((_modulo, accion) => ['Ver', 'Crear', 'Editar'].includes(accion));
    TestBed.configureTestingModule({
      imports: [SolicitudesCompraShellComponent, NoopAnimationsModule],
      providers: [
        { provide: SolicitudCompraService, useValue: servicio },
        { provide: ProveedorService, useValue: { getActivos: () => of({ data: [] }) } },
        { provide: ProductoService, useValue: { getPaged: () => of({ data: { items: [] } }) } },
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: ActivatedRoute, useValue: route },
        { provide: Router, useValue: router },
        { provide: AppAlertService, useValue: {} }
      ]
    });
  });

  it('mantiene un solo listado con desplazamiento propio y acciones según estado', () => {
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelectorAll('.app-list-surface table')).toHaveLength(1);
    expect(host.querySelectorAll('.cards-mobile')).toHaveLength(0);
    expect(host.querySelector('.app-table-shell[role="region"][tabindex="0"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Ver detalle de SC-007"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Editar SC-007"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Editar SC-008"]')).toBeNull();
    expect(host.querySelector('mat-paginator')?.textContent).toContain('Registros por página');
    host.querySelector<HTMLButtonElement>('button[aria-label="Ver detalle de SC-007"]')?.click();
    expect(router.navigate).toHaveBeenCalledWith([], { relativeTo: route, queryParams: { detalle: 7, modo: null } });
  });

  it('conserva consulta y oculta creación y edición cuando el permiso no existe', () => {
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Ver');
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('button[aria-label="Ver detalle de SC-007"]')).not.toBeNull();
    expect(host.querySelector('button[aria-label="Editar SC-007"]')).toBeNull();
    expect(host.textContent).not.toContain('Nueva solicitud');
    fixture.componentInstance.abrirEdicion(7);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('solicita páginas distintas y reinicia la página al aplicar filtros', () => {
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    fixture.componentInstance.onPageChange({ pageIndex: 1, pageSize: 25, length: 42 });
    expect(servicio.getPaged).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 25 }));
    fixture.componentInstance.numero = 'SC-007';
    fixture.componentInstance.aplicarFiltros();
    expect(servicio.getPaged).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, numero: 'SC-007' }));
    fixture.componentInstance.limpiarFiltros();
    expect(servicio.getPaged).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, pageSize: 10, numero: '' }));
  });

  it('rechaza una respuesta de listado success=false sin presentar registros como válidos', () => {
    servicio.getPaged.mockReturnValue(of({ ...respuesta([borrador]), success: false, message: 'Consulta rechazada.' }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe('Consulta rechazada.');
    expect(fixture.componentInstance.solicitudes()).toEqual([]);
    expect(fixture.componentInstance.totalCount()).toBe(0);
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('presenta un estado vacío contextual en la superficie compartida', () => {
    servicio.getPaged.mockReturnValue(of(respuesta([])));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement;
    expect(host.querySelector('.app-list-surface .app-list-empty')?.textContent).toContain('No se encontraron solicitudes');
    expect(host.querySelector('mat-paginator')).toBeNull();
  });
});
