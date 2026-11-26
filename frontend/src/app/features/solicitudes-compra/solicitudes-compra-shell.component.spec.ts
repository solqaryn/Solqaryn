import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';
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
  const detalle = { ...borrador, detalles: [{ id: 1, productoId: 2, cantidadSolicitada: 3 }] };
  const solicitada: SolicitudCompra = { ...detalle, estado: 'Solicitada' };
  const resultado = (data: SolicitudCompra, success = true, message = '') => ({ success, message, errors: [], data });
  const servicio = {
    getPaged: vi.fn(() => of(respuesta([borrador, aprobada]))),
    getById: vi.fn(() => of(resultado(detalle))),
    create: vi.fn(), update: vi.fn(), enviar: vi.fn(), aprobar: vi.fn(), rechazar: vi.fn()
  };
  const alerts = { confirmar: vi.fn(), solicitarTexto: vi.fn() };
  const permisos = { puede: vi.fn((_modulo: string, accion: string) => ['Ver', 'Crear', 'Editar'].includes(accion)) };
  const router = { navigate: vi.fn(async () => true) };
  const route = { queryParamMap: new BehaviorSubject(convertToParamMap({})) };

  beforeEach(() => {
    vi.clearAllMocks();
    route.queryParamMap.next(convertToParamMap({}));
    servicio.getPaged.mockReturnValue(of(respuesta([borrador, aprobada])));
    servicio.getById.mockReturnValue(of(resultado(detalle)));
    permisos.puede.mockImplementation((_modulo, accion) => ['Ver', 'Crear', 'Editar'].includes(accion));
    TestBed.configureTestingModule({
      imports: [SolicitudesCompraShellComponent, NoopAnimationsModule],
      providers: [
        { provide: SolicitudCompraService, useValue: servicio },
        { provide: ProveedorService, useValue: { getActivos: () => of({ data: [] }) } },
        { provide: ProductoService, useValue: { getPaged: () => of({ data: { items: [] } }), getVariantes: () => of({ data: [] }) } },
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: ActivatedRoute, useValue: route },
        { provide: Router, useValue: router },
        { provide: AppAlertService, useValue: alerts }
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

  it('impide formularios y guardados por URL directa sin permiso', () => {
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Ver');
    route.queryParamMap.next(convertToParamMap({ modo: 'nueva' }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    expect(component.modoFormulario()).toBeNull();
    component.guardar();
    route.queryParamMap.next(convertToParamMap({ modo: 'editar', detalle: 7 }));
    fixture.detectChanges();
    expect(component.modoFormulario()).toBeNull();
    expect(component.detalle()?.id).toBe(7);
    component.guardar();
    expect(servicio.create).not.toHaveBeenCalled();
    expect(servicio.update).not.toHaveBeenCalled();
  });

  it('no abre edición con detalle rechazado o pendiente de carga', () => {
    const response = new Subject<ReturnType<typeof resultado>>();
    servicio.getById.mockReturnValue(response);
    route.queryParamMap.next(convertToParamMap({ modo: 'editar', detalle: 7 }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    expect(component.detalleLoading()).toBe(true);
    expect(component.modoFormulario()).toBeNull();
    component.guardar();
    response.next(resultado(detalle, false, 'Consulta rechazada.'));
    response.complete();
    fixture.detectChanges();
    expect(component.detalle()).toBeNull();
    expect(component.detalleLoading()).toBe(false);
    expect(component.detalleError()).toBe('Consulta rechazada.');
    expect(servicio.update).not.toHaveBeenCalled();
  });

  it('preserva el borrador tras un guardado rechazado e impide envíos duplicados', () => {
    const response = new Subject<ReturnType<typeof resultado>>();
    servicio.update.mockReturnValue(response);
    route.queryParamMap.next(convertToParamMap({ modo: 'editar', detalle: 7 }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    expect(component.modoFormulario()).toBe('editar');
    component.formNotas = 'Conservar notas';
    component.guardar();
    component.guardar();
    expect(servicio.update).toHaveBeenCalledTimes(1);
    response.next(resultado(detalle, false, 'Guardado rechazado.'));
    response.complete();
    expect(component.modoFormulario()).toBe('editar');
    expect(component.formNotas).toBe('Conservar notas');
    expect(component.formError()).toBe('Guardado rechazado.');
    expect(component.guardando()).toBe(false);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('conserva el detalle visible si se rechaza una acción', () => {
    permisos.puede.mockReturnValue(true);
    servicio.enviar.mockReturnValue(of(resultado(detalle, false, 'Envío rechazado.')));
    route.queryParamMap.next(convertToParamMap({ detalle: 7 }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    fixture.componentInstance.enviar(detalle);
    fixture.detectChanges();
    expect(servicio.enviar).toHaveBeenCalledWith(7);
    expect(fixture.componentInstance.detalle()).toEqual(detalle);
    expect(fixture.componentInstance.accionando()).toBe(false);
    expect(fixture.nativeElement.querySelector('.detail-card [role="alert"]')?.textContent).toContain('Envío rechazado.');
    expect(fixture.nativeElement.querySelector('#solicitud-numero')?.textContent).toContain('SC-007');
  });

  it('cancelar el rechazo no muta el registro ni muestra un error', async () => {
    permisos.puede.mockReturnValue(true);
    servicio.getById.mockReturnValue(of(resultado(solicitada)));
    alerts.solicitarTexto.mockResolvedValue(null);
    route.queryParamMap.next(convertToParamMap({ detalle: 7 }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    await fixture.componentInstance.rechazar(solicitada);
    expect(alerts.solicitarTexto).toHaveBeenCalledTimes(1);
    expect(servicio.rechazar).not.toHaveBeenCalled();
    expect(fixture.componentInstance.detalleError()).toBeNull();
    expect(fixture.componentInstance.detalle()).toEqual(solicitada);
    expect(fixture.componentInstance.confirmando()).toBe(false);
  });

  it('bloquea confirmaciones duplicadas y revalida permiso antes de aprobar', async () => {
    permisos.puede.mockReturnValue(true);
    servicio.getById.mockReturnValue(of(resultado(solicitada)));
    let resolver!: (value: boolean) => void;
    alerts.confirmar.mockReturnValue(new Promise<boolean>(resolve => { resolver = resolve; }));
    route.queryParamMap.next(convertToParamMap({ detalle: 7 }));
    const fixture = TestBed.createComponent(SolicitudesCompraShellComponent);
    fixture.detectChanges();
    const first = fixture.componentInstance.aprobar(solicitada);
    await fixture.componentInstance.aprobar(solicitada);
    expect(alerts.confirmar).toHaveBeenCalledTimes(1);
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Ver');
    resolver(true);
    await first;
    expect(servicio.aprobar).not.toHaveBeenCalled();
    expect(fixture.componentInstance.confirmando()).toBe(false);
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
