import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, type Mocked, vi } from 'vitest';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import {
  CuentaBancaria,
  EstadoCuentaBancaria
} from '../../core/models/cuenta-bancaria';
import { CuentaBancariaPage } from '../../core/models/cuenta-bancaria-page';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { CuentaBancariaService } from '../../core/services/cuenta-bancaria.service';
import { CuentasBancariasComponent } from './cuentas-bancarias.component';

type CuentaServiceMock = Mocked<
  Pick<CuentaBancariaService, 'getAll' | 'create' | 'update' | 'activar' | 'desactivar'>
>;
type PermisosMock = Mocked<Pick<PermisosRuntimeService, 'puede'>>;

interface NavigationSnapshot {
  search: string;
  estadoFilter: EstadoCuentaBancaria | null;
}

function createNavigationStateMock() {
  return {
    restore: vi.fn(
      (_scope: string, _route: ActivatedRoute, _defaults: NavigationSnapshot): NavigationSnapshot => ({
        search: '',
        estadoFilter: null
      })
    ),
    persist: vi.fn(
      (
        _scope: string,
        _route: ActivatedRoute,
        _state: NavigationSnapshot,
        _defaults: NavigationSnapshot
      ): void => undefined
    ),
    clear: vi.fn((_scope: string): void => undefined)
  };
}

describe('CuentasBancariasComponent', () => {
  let component: CuentasBancariasComponent;
  let fixture: ComponentFixture<CuentasBancariasComponent>;
  let cuentaService: CuentaServiceMock;
  let permisos: PermisosMock;
  let navigationState: ReturnType<typeof createNavigationStateMock>;

  const cuentasPage: CuentaBancariaPage<CuentaBancaria> = {
    items: [
      {
        id: 1,
        bancoId: 1,
        nombre: 'Cuenta HNL',
        numeroCuenta: '123',
        moneda: 'HNL',
        saldoInicial: 100,
        estado: EstadoCuentaBancaria.Activa
      },
      {
        id: 2,
        bancoId: 1,
        nombre: 'Cuenta USD',
        numeroCuenta: '456',
        moneda: 'USD',
        saldoInicial: 200,
        estado: EstadoCuentaBancaria.Inactiva
      }
    ],
    page: 1,
    pageSize: 50,
    totalCount: 2,
    totalPages: 1
  };

  beforeEach(async () => {
    cuentaService = {
      getAll: vi.fn<CuentaBancariaService['getAll']>().mockReturnValue(of(cuentasPage)),
      create: vi.fn<CuentaBancariaService['create']>(),
      update: vi.fn<CuentaBancariaService['update']>(),
      activar: vi.fn<CuentaBancariaService['activar']>(),
      desactivar: vi.fn<CuentaBancariaService['desactivar']>()
    };

    permisos = {
      puede: vi.fn<PermisosRuntimeService['puede']>().mockReturnValue(false)
    };

    navigationState = createNavigationStateMock();

    await TestBed.configureTestingModule({
      imports: [CuentasBancariasComponent, NoopAnimationsModule],
      providers: [
        { provide: CuentaBancariaService, useValue: cuentaService },
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: ListNavigationStateService, useValue: navigationState },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: { get: () => null } } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CuentasBancariasComponent);
    component = fixture.componentInstance;
  });

  it('crea el componente y carga la lista paginada con contrato canónico', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(cuentaService.getAll).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      searchTerm: undefined,
      estado: undefined
    });
    expect(component.cuentas()).toEqual(cuentasPage.items);
    expect(component.loading()).toBe(false);
  });

  it('inicializa RBAC usando permisos Finanzas exactos', () => {
    permisos.puede.mockImplementation(
      (_modulo: string, accion: string) => accion === 'Crear' || accion === 'Activar'
    );

    fixture.detectChanges();

    expect(permisos.puede).toHaveBeenCalledWith('Finanzas', 'Crear');
    expect(permisos.puede).toHaveBeenCalledWith('Finanzas', 'Activar');
    expect(permisos.puede).toHaveBeenCalledWith('Finanzas', 'Desactivar');
    expect(component.puedeCrear()).toBe(true);
    expect(component.puedeActivar()).toBe(true);
    expect(component.puedeDesactivar()).toBe(false);
  });

  it('restaura y persiste filtros de navegación al cargar', () => {
    navigationState.restore.mockReturnValue({
      search: 'usd',
      estadoFilter: EstadoCuentaBancaria.Activa
    });

    fixture.detectChanges();

    expect(cuentaService.getAll).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      searchTerm: 'usd',
      estado: EstadoCuentaBancaria.Activa
    });
    expect(navigationState.persist).toHaveBeenCalled();
  });

  it('limpia filtros y vuelve a cargar la lista', () => {
    fixture.detectChanges();
    cuentaService.getAll.mockClear();

    component.search = 'algo';
    component.estadoFilter = EstadoCuentaBancaria.Inactiva;
    component.limpiarFiltros();

    expect(navigationState.clear).toHaveBeenCalledWith('cuentas-bancarias');
    expect(component.search).toBe('');
    expect(component.estadoFilter).toBeNull();
    expect(cuentaService.getAll).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      searchTerm: undefined,
      estado: undefined
    });
  });

  it('no crea una cuenta y marca los controles como tocados cuando el formulario es inválido', () => {
    fixture.detectChanges();

    component.formulario.controls.nombre.setValue('');
    component.guardarCuenta();

    expect(component.formulario.touched).toBe(true);
    expect(cuentaService.create).not.toHaveBeenCalled();
  });

  it('no permite envíos duplicados mientras la creación está pendiente', () => {
    fixture.detectChanges();
    cuentaService.getAll.mockClear();

    const dto = {
      bancoId: 1,
      nombre: 'Cuenta Lenta',
      numeroCuenta: '444',
      moneda: 'HNL',
      saldoInicial: 100
    };
    component.formulario.setValue(dto);

    const response$ = new Subject<any>();
    cuentaService.create.mockReturnValue(response$);

    component.guardarCuenta();
    component.guardarCuenta();

    expect(cuentaService.create).toHaveBeenCalledTimes(1);
    expect(component.submitting()).toBe(true);

    response$.next({ ...dto, id: 4, estado: EstadoCuentaBancaria.Activa });
    response$.complete();
  });

  it('restablece el estado submitting al completar la creación con éxito', () => {
    fixture.detectChanges();

    const dto = {
      bancoId: 1,
      nombre: 'Nueva Cuenta',
      numeroCuenta: '999',
      moneda: 'HNL',
      saldoInicial: 500
    };
    component.formulario.setValue(dto);
    cuentaService.create.mockReturnValue(of({ ...dto, id: 3, estado: EstadoCuentaBancaria.Activa }));

    component.guardarCuenta();

    expect(component.submitting()).toBe(false);
  });

  it('restablece el estado submitting cuando la creación falla', () => {
    fixture.detectChanges();

    const dto = {
      bancoId: 1,
      nombre: 'Cuenta Error',
      numeroCuenta: '000',
      moneda: 'HNL',
      saldoInicial: 0
    };
    component.formulario.setValue(dto);

    const error$ = new Subject<any>();
    cuentaService.create.mockReturnValue(error$);

    component.guardarCuenta();
    expect(component.submitting()).toBe(true);

    error$.error(new Error('Network Error'));

    expect(component.submitting()).toBe(false);
  });

  it('crea una cuenta válida, resetea el formulario y recarga', () => {
    fixture.detectChanges();
    cuentaService.getAll.mockClear();

    const dto = {
      bancoId: 1,
      nombre: 'Nueva Cuenta',
      numeroCuenta: '999',
      moneda: 'HNL',
      saldoInicial: 500
    };

    component.formulario.setValue(dto);
    cuentaService.create.mockReturnValue(
      of({ ...dto, id: 3, estado: EstadoCuentaBancaria.Activa })
    );
    component.mostrarFormulario.set(true);

    component.guardarCuenta();

    expect(cuentaService.create).toHaveBeenCalledWith(dto);
    expect(component.mostrarFormulario()).toBe(false);
    expect(component.formulario.getRawValue()).toEqual({
      bancoId: 0,
      nombre: '',
      numeroCuenta: '',
      moneda: 'HNL',
      saldoInicial: 0
    });
    expect(cuentaService.getAll).toHaveBeenCalledTimes(1);
  });

  it('bloquea activar/desactivar cuando RBAC no lo permite', () => {
    fixture.detectChanges();
    const cuenta = cuentasPage.items[0];

    component.activar(cuenta);
    component.desactivar(cuenta);

    expect(cuentaService.activar).not.toHaveBeenCalled();
    expect(cuentaService.desactivar).not.toHaveBeenCalled();
  });

  it('ejecuta activar y desactivar cuando RBAC lo permite y recarga', () => {
    permisos.puede.mockReturnValue(true);
    fixture.detectChanges();
    cuentaService.getAll.mockClear();
    cuentaService.activar.mockReturnValue(of(undefined));
    cuentaService.desactivar.mockReturnValue(of(undefined));
    const cuenta = cuentasPage.items[0];

    component.activar(cuenta);
    component.desactivar(cuenta);

    expect(cuentaService.activar).toHaveBeenCalledWith(cuenta.id);
    expect(cuentaService.desactivar).toHaveBeenCalledWith(cuenta.id);
    expect(cuentaService.getAll).toHaveBeenCalledTimes(2);
  });

  it('mantiene visible el estado de carga mientras getAll sigue pendiente', () => {
    const response$ = new Subject<CuentaBancariaPage<CuentaBancaria>>();
    cuentaService.getAll.mockReturnValue(response$);

    fixture.detectChanges();

    expect(component.loading()).toBe(true);
    expect(fixture.nativeElement.querySelector('.loading-shade')).not.toBeNull();

    response$.next(cuentasPage);
    response$.complete();
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(fixture.nativeElement.querySelector('.loading-shade')).toBeNull();
  });

  it('restablece loading cuando getAll termina con error', () => {
    const response$ = new Subject<CuentaBancariaPage<CuentaBancaria>>();
    cuentaService.getAll.mockReturnValue(response$);

    fixture.detectChanges();
    expect(component.loading()).toBe(true);

    response$.error(new Error('Network error'));
    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(fixture.nativeElement.querySelector('.loading-shade')).toBeNull();
  });

  it('muestra el estado vacío cuando la consulta no devuelve cuentas', () => {
    cuentaService.getAll.mockReturnValue(
      of({ items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 })
    );

    fixture.detectChanges();

    expect(component.cuentas()).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('No se encontraron cuentas bancarias.');
  });

  it('abre el modo de edición y rellena el formulario si tiene permiso Editar', () => {
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Editar');
    fixture.detectChanges();
    const cuenta = cuentasPage.items[0];

    component.editar(cuenta);

    expect(component.puedeEditar()).toBe(true);
    expect(component.cuentaEnEdicion()).toEqual(cuenta);
    expect(component.mostrarFormulario()).toBe(true);
    expect(component.formulario.getRawValue()).toMatchObject({
      nombre: cuenta.nombre,
      numeroCuenta: cuenta.numeroCuenta,
      moneda: cuenta.moneda,
      saldoInicial: cuenta.saldoInicial,
      bancoId: cuenta.bancoId
    });
  });

  it('rechaza abrir el modo de edición si no tiene permiso Editar', () => {
    permisos.puede.mockReturnValue(false);
    fixture.detectChanges();
    const cuenta = cuentasPage.items[0];

    component.editar(cuenta);

    expect(component.cuentaEnEdicion()).toBeNull();
    expect(component.mostrarFormulario()).toBe(false);
  });

  it('actualiza una cuenta, resetea el formulario y cierra el modo de edición', () => {
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Editar');
    fixture.detectChanges();
    cuentaService.getAll.mockClear();
    const cuenta = cuentasPage.items[0];

    component.editar(cuenta);

    const dto = { nombre: 'Nombre Editado' };
    component.formulario.patchValue(dto);
    cuentaService.update.mockReturnValue(of(undefined));

    component.guardarCuenta();

    expect(cuentaService.update).toHaveBeenCalledWith(cuenta.id, dto);
    expect(component.mostrarFormulario()).toBe(false);
    expect(component.cuentaEnEdicion()).toBeNull();
    expect(component.formulario.getRawValue()).toEqual({
      bancoId: 0,
      nombre: '',
      numeroCuenta: '',
      moneda: 'HNL',
      saldoInicial: 0
    });
    expect(cuentaService.getAll).toHaveBeenCalledTimes(1);
  });

  it('limpia el estado de edición al cerrar o resetear el formulario (ocultar formulario)', () => {
    permisos.puede.mockImplementation((_modulo, accion) => accion === 'Editar');
    fixture.detectChanges();
    const cuenta = cuentasPage.items[0];

    component.editar(cuenta);
    expect(component.cuentaEnEdicion()).toEqual(cuenta);

    component.mostrarFormulario.set(false);
    fixture.detectChanges();

    expect(component.cuentaEnEdicion()).toBeNull();
    expect(component.formulario.getRawValue()).toEqual({
      bancoId: 0,
      nombre: '',
      numeroCuenta: '',
      moneda: 'HNL',
      saldoInicial: 0
    });
  });
});
