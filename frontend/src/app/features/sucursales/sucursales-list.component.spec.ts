import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { SucursalService } from '../../services/sucursal.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { SucursalesListComponent } from './sucursales-list.component';

describe('SucursalesListComponent N6.3.G regression', () => {
  let fixture: ComponentFixture<SucursalesListComponent>;
  let component: SucursalesListComponent;

  const sucursalService = {
    buscar: vi.fn(() => of({
      data: {
        items: [],
        total: 0,
        totalPaginas: 0,
        pagina: 1,
        tamanoPagina: 10
      }
    })),
    activar: vi.fn(),
    desactivar: vi.fn(),
    delete: vi.fn()
  };

  const http = {
    get: vi.fn(() => of({
      data: [
        { id: 7, nombre: 'Empresa Siete', activa: true },
        { id: 8, nombre: 'Empresa Ocho', activa: true }
      ]
    }))
  };

  const permisosRuntime = {
    puede: vi.fn(() => true)
  };

  const snackBar = {
    open: vi.fn()
  };

  const alerts = {
    confirmar: vi.fn()
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    sucursalService.buscar.mockImplementation(() => of({
      data: {
        items: [],
        total: 0,
        totalPaginas: 0,
        pagina: 1,
        tamanoPagina: 10
      }
    }));
    http.get.mockImplementation(() => of({
      data: [
        { id: 7, nombre: 'Empresa Siete', activa: true },
        { id: 8, nombre: 'Empresa Ocho', activa: true }
      ]
    }));

    await TestBed.configureTestingModule({
      imports: [SucursalesListComponent],
      providers: [
        { provide: SucursalService, useValue: sucursalService },
        { provide: HttpClient, useValue: http },
        { provide: PermisosRuntimeService, useValue: permisosRuntime },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: AppAlertService, useValue: alerts }
      ]
    })
      .overrideComponent(SucursalesListComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(SucursalesListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads real Empresa options and keeps legacy ownership visible', () => {
    expect(component.empresas()).toEqual([
      { id: 7, nombre: 'Empresa Siete', activa: true },
      { id: 8, nombre: 'Empresa Ocho', activa: true }
    ]);
    expect(component.nombreEmpresa(7)).toBe('Empresa Siete');
    expect(component.nombreEmpresa(99)).toBe('Empresa #99');
    expect(component.nombreEmpresa(null)).toBe('Sin asignar (legado)');
  });

  it('forwards Empresa filter together with status, search and pagination', () => {
    component.buscar = 'centro';
    component.estado = 'activas';
    component.empresaId = 8;
    component.pagina = 2;
    component.tamanoPagina = 25;

    component.cargar();

    expect(sucursalService.buscar).toHaveBeenLastCalledWith({
      buscar: 'centro',
      activa: true,
      empresaId: 8,
      pagina: 2,
      tamanoPagina: 25
    });
  });

  it('does not fabricate an Empresa filter from null or non-positive values', () => {
    component.empresaId = 0;
    component.cargar();

    expect(sucursalService.buscar).toHaveBeenLastCalledWith(expect.objectContaining({
      empresaId: undefined
    }));

    component.empresaId = null;
    component.cargar();

    expect(sucursalService.buscar).toHaveBeenLastCalledWith(expect.objectContaining({
      empresaId: undefined
    }));
  });

  it('reset clears tenant selection and restores first-page defaults', () => {
    component.buscar = 'norte';
    component.estado = 'inactivas';
    component.empresaId = 7;
    component.pagina = 4;
    component.tamanoPagina = 50;

    component.limpiarFiltros();

    expect(component.buscar).toBe('');
    expect(component.estado).toBe('todas');
    expect(component.empresaId).toBeNull();
    expect(component.pagina).toBe(1);
    expect(component.tamanoPagina).toBe(10);
    expect(sucursalService.buscar).toHaveBeenLastCalledWith({
      buscar: '',
      activa: undefined,
      empresaId: undefined,
      pagina: 1,
      tamanoPagina: 10
    });
  });
});
